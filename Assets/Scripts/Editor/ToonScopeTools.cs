using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ToonScopeTools
{
    private sealed class Slot
    {
        public string assetPath;
        public bool scene;
        public int[] objectPath;
        public int rendererIndex;
        public int slot;
        public string key;
    }

    private static HashSet<string> cachedDependencies;
    private static string cachedKey;

    public static void Refresh()
    {
        cachedDependencies = null;
        cachedKey = null;
    }

    public static string[] ScenePaths(ToonEnvironmentStyle style)
    {
        if (style == null || style.scenesToScan == null) return new string[0];
        return style.scenesToScan.Where(path => !string.IsNullOrEmpty(path) && File.Exists(path)).ToArray();
    }

    public static HashSet<string> Dependencies(ToonEnvironmentStyle style)
    {
        string[] scenes = ScenePaths(style);
        string key = string.Join("|", scenes);
        if (cachedDependencies == null || cachedKey != key)
        {
            cachedDependencies = new HashSet<string>(AssetDatabase.GetDependencies(scenes, true), StringComparer.OrdinalIgnoreCase);
            cachedKey = key;
        }
        return cachedDependencies;
    }

    public static List<string> Prefabs(ToonEnvironmentStyle style, string[] folders)
    {
        string[] valid = folders == null ? new string[0] : folders.Where(folder => !string.IsNullOrEmpty(folder) && AssetDatabase.IsValidFolder(folder)).ToArray();
        if (valid.Length == 0) return new List<string>();
        HashSet<string> dependencies = Dependencies(style);
        return AssetDatabase.FindAssets("t:Prefab", valid).Select(AssetDatabase.GUIDToAssetPath).Distinct()
            .Where(dependencies.Contains).OrderBy(path => path, StringComparer.Ordinal).ToList();
    }

    public static string MaterialKey(Material material) => AssetDatabase.GetAssetPath(material) + "|" + material.name;

    public static void ExtractScopedEmbeddedMaterials(ToonEnvironmentStyle style, StringBuilder report)
    {
        var toExtract = new Dictionary<Material, string>();
        var environmentKeys = new HashSet<string>();

        var prefabPaths = new List<string>(Prefabs(style, style.characterPrefabFolders));
        foreach (GameObject prefab in style.prefabsWithEmbeddedMaterials)
            if (prefab != null) prefabPaths.Add(AssetDatabase.GetAssetPath(prefab));
        foreach (string path in prefabPaths.Distinct())
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;
            foreach (Renderer renderer in prefab.GetComponentsInChildren<Renderer>(true))
            {
                if (!IsMeshRenderer(renderer)) continue;
                foreach (Material material in renderer.sharedMaterials)
                    if (IsExtractable(material, style) && !toExtract.ContainsKey(material)) toExtract.Add(material, MaterialKey(material));
            }
        }

        foreach (string scenePath in ScenePaths(style))
        {
            Scene preview = EditorSceneManager.OpenPreviewScene(scenePath);
            try
            {
                foreach (GameObject root in preview.GetRootGameObjects())
                    foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                        foreach (Material material in renderer.sharedMaterials)
                        {
                            if (!IsExtractable(material, style)) continue;
                            if (!ToonWorldTools.IsInFolders(AssetDatabase.GetAssetPath(material), style.sceneModelFolders)) continue;
                            string key = MaterialKey(material);
                            environmentKeys.Add(key);
                            if (!toExtract.ContainsKey(material)) toExtract.Add(material, key);
                        }
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(preview);
            }
        }

        if (toExtract.Count == 0)
        {
            report.AppendLine("EXTRACCIÓN: no queda ningún material embebido en los modelos que usan las escenas del alcance.");
            return;
        }

        var models = new HashSet<string>(toExtract.Keys.Select(AssetDatabase.GetAssetPath), StringComparer.OrdinalIgnoreCase);
        var keys = new HashSet<string>(toExtract.Values);
        List<Slot> slots = RecordSlots(models, keys);

        var targets = new Dictionary<string, string>();
        var reimport = new HashSet<string>();
        foreach (var pair in toExtract)
        {
            Material material = pair.Key;
            string modelPath = AssetDatabase.GetAssetPath(material);
            if (!(AssetImporter.GetAtPath(modelPath) is ModelImporter))
            {
                report.AppendLine($"EMBEBIDO no extraíble {material.name} en {modelPath} (no es un modelo).");
                continue;
            }
            string folder = Path.GetDirectoryName(modelPath).Replace('\\', '/') + "/Materials";
            EditorAssetUtility.EnsureFolder(folder);
            string name = ToonEnvironmentTools.SanitizeFileName(Path.GetFileNameWithoutExtension(modelPath) + "_" + material.name);
            string target = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + name + ".mat");
            string materialName = material.name;
            string error = AssetDatabase.ExtractAsset(material, target);
            if (string.IsNullOrEmpty(error))
            {
                reimport.Add(modelPath);
                targets[pair.Value] = target;
                report.AppendLine($"EXTRAIDO {materialName} de {modelPath} -> {target}");
            }
            else report.AppendLine($"ERROR al extraer {materialName} de {modelPath}: {error}");
        }
        foreach (string modelPath in reimport)
        {
            AssetDatabase.WriteImportSettingsIfDirty(modelPath);
            AssetDatabase.ImportAsset(modelPath, ImportAssetOptions.ForceUpdate);
        }
        AssetDatabase.SaveAssets();
        var extracted = new Dictionary<string, Material>();
        foreach (var pair in targets) extracted[pair.Key] = AssetDatabase.LoadAssetAtPath<Material>(pair.Value);

        int restored = RestoreSlots(slots, extracted, report);
        foreach (string key in environmentKeys)
            if (extracted.TryGetValue(key, out Material material) && material != null && !style.additionalEnvironmentMaterials.Contains(material))
                style.additionalEnvironmentMaterials.Add(material);
        EditorUtility.SetDirty(style);
        AssetDatabase.SaveAssets();
        report.AppendLine($"RESUMEN extracción: {extracted.Count} materiales extraídos de {reimport.Count} modelos; {restored} referencias directas reasignadas en prefabs y escenas.");
    }

    private static bool IsMeshRenderer(Renderer renderer) => renderer is MeshRenderer || renderer is SkinnedMeshRenderer;

    private static bool IsExtractable(Material material, ToonEnvironmentStyle style) =>
        material != null && AssetDatabase.IsSubAsset(material) && !style.excludedMaterials.Contains(material);

    private static List<Slot> RecordSlots(HashSet<string> models, HashSet<string> keys)
    {
        var slots = new List<Slot>();
        foreach (string path in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }).Select(AssetDatabase.GUIDToAssetPath).Distinct())
        {
            if (!DependsOn(path, models)) continue;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;
            Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Material[] materials = renderers[i].sharedMaterials;
                for (int s = 0; s < materials.Length; s++)
                {
                    if (materials[s] == null || !AssetDatabase.IsSubAsset(materials[s])) continue;
                    string key = MaterialKey(materials[s]);
                    if (keys.Contains(key)) slots.Add(new Slot { assetPath = path, rendererIndex = i, slot = s, key = key });
                }
            }
        }
        foreach (string path in AssetDatabase.FindAssets("t:Scene", new[] { "Assets" }).Select(AssetDatabase.GUIDToAssetPath).Distinct())
        {
            if (!DependsOn(path, models)) continue;
            Scene preview = EditorSceneManager.OpenPreviewScene(path);
            try
            {
                GameObject[] roots = preview.GetRootGameObjects();
                for (int r = 0; r < roots.Length; r++)
                    foreach (Renderer renderer in roots[r].GetComponentsInChildren<Renderer>(true))
                    {
                        Material[] materials = renderer.sharedMaterials;
                        for (int s = 0; s < materials.Length; s++)
                        {
                            if (materials[s] == null || !AssetDatabase.IsSubAsset(materials[s])) continue;
                            string key = MaterialKey(materials[s]);
                            if (keys.Contains(key)) slots.Add(new Slot { assetPath = path, scene = true, objectPath = ObjectPath(renderer.transform, r), slot = s, key = key });
                        }
                    }
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(preview);
            }
        }
        return slots;
    }

    private static bool DependsOn(string assetPath, HashSet<string> models)
    {
        foreach (string dependency in AssetDatabase.GetDependencies(assetPath, true))
            if (models.Contains(dependency)) return true;
        return false;
    }

    private static int[] ObjectPath(Transform transform, int rootIndex)
    {
        var indices = new List<int>();
        while (transform.parent != null)
        {
            indices.Add(transform.GetSiblingIndex());
            transform = transform.parent;
        }
        indices.Add(rootIndex);
        indices.Reverse();
        return indices.ToArray();
    }

    private static int RestoreSlots(List<Slot> slots, Dictionary<string, Material> extracted, StringBuilder report)
    {
        int restored = 0;
        foreach (var group in slots.Where(slot => !slot.scene).GroupBy(slot => slot.assetPath))
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(group.Key);
            if (asset == null) continue;
            Renderer[] assetRenderers = asset.GetComponentsInChildren<Renderer>(true);
            if (!group.Any(slot => NeedsRestore(assetRenderers, slot))) continue;
            GameObject root = PrefabUtility.LoadPrefabContents(group.Key);
            try
            {
                Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
                int fixedHere = 0;
                foreach (Slot slot in group)
                {
                    if (!NeedsRestore(renderers, slot) || !extracted.TryGetValue(slot.key, out Material material) || material == null) continue;
                    Material[] materials = renderers[slot.rendererIndex].sharedMaterials;
                    materials[slot.slot] = material;
                    renderers[slot.rendererIndex].sharedMaterials = materials;
                    fixedHere++;
                }
                if (fixedHere > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, group.Key);
                    restored += fixedHere;
                    report.AppendLine($"REASIGNADO {group.Key}: {fixedHere} referencias directas a materiales embebidos apuntan ahora al material extraído.");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        foreach (var group in slots.Where(slot => slot.scene).GroupBy(slot => slot.assetPath))
        {
            bool needed;
            Scene preview = EditorSceneManager.OpenPreviewScene(group.Key);
            try
            {
                GameObject[] previewRoots = preview.GetRootGameObjects();
                needed = group.Any(slot => SceneSlotIsEmpty(previewRoots, slot));
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(preview);
            }
            if (!needed) continue;

            Scene scene = EditorSceneManager.OpenScene(group.Key, OpenSceneMode.Single);
            GameObject[] roots = scene.GetRootGameObjects();
            int fixedHere = 0;
            foreach (Slot slot in group)
            {
                Renderer renderer = ResolveRenderer(roots, slot);
                if (renderer == null || !extracted.TryGetValue(slot.key, out Material material) || material == null) continue;
                Material[] materials = renderer.sharedMaterials;
                if (slot.slot >= materials.Length || materials[slot.slot] != null) continue;
                materials[slot.slot] = material;
                renderer.sharedMaterials = materials;
                EditorUtility.SetDirty(renderer);
                if (PrefabUtility.IsPartOfPrefabInstance(renderer)) PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                fixedHere++;
            }
            if (fixedHere > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                restored += fixedHere;
                report.AppendLine($"REASIGNADO {group.Key}: {fixedHere} referencias directas a materiales embebidos apuntan ahora al material extraído.");
            }
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
        return restored;
    }

    private static bool NeedsRestore(Renderer[] renderers, Slot slot)
    {
        if (slot.rendererIndex >= renderers.Length) return false;
        Material[] materials = renderers[slot.rendererIndex].sharedMaterials;
        return slot.slot < materials.Length && materials[slot.slot] == null;
    }

    private static bool SceneSlotIsEmpty(GameObject[] roots, Slot slot)
    {
        Renderer renderer = ResolveRenderer(roots, slot);
        if (renderer == null) return false;
        Material[] materials = renderer.sharedMaterials;
        return slot.slot < materials.Length && materials[slot.slot] == null;
    }

    private static Renderer ResolveRenderer(GameObject[] roots, Slot slot)
    {
        if (slot.objectPath == null || slot.objectPath.Length == 0 || slot.objectPath[0] >= roots.Length) return null;
        Transform current = roots[slot.objectPath[0]].transform;
        for (int i = 1; i < slot.objectPath.Length; i++)
        {
            if (slot.objectPath[i] >= current.childCount) return null;
            current = current.GetChild(slot.objectPath[i]);
        }
        return current.GetComponent<Renderer>();
    }
}
