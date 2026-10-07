using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LevelOptimizationTools
{
    private static readonly string[] StaticGeometryFolders =
    {
        "Assets/Prefabs/Buildings",
        "Assets/Prefabs/Map"
    };

    private const StaticEditorFlags GeometryFlags =
        StaticEditorFlags.BatchingStatic
        | StaticEditorFlags.OccluderStatic
        | StaticEditorFlags.OccludeeStatic
        | StaticEditorFlags.ReflectionProbeStatic;

    [MenuItem("Tools/Manners/Performance/1. Marcar edificios y mapa como estáticos", false, 40)]
    public static void MarkBuildingsAndMapStatic()
    {
        var paths = new HashSet<string>(AssetDatabase.FindAssets("t:Prefab", StaticGeometryFolders).Select(AssetDatabase.GUIDToAssetPath));
        var destructibles = new List<GameObject>();
        int prefabsTouched = 0;
        int objectsMarked = 0;
        int objectsCleared = 0;

        foreach (string path in paths.OrderBy(p => AssetDatabase.GetDependencies(p, true).Count(paths.Contains)))
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (root == null) continue;

            bool hasRuntimeRendererMutator = root.GetComponentInChildren<BuildingFader>(true) != null
                || root.GetComponentInChildren<BuildingsScript>(true) != null;

            bool changed = false;
            if (hasRuntimeRendererMutator)
            {
                destructibles.Clear();
                CollectStaticDestructibles(root, destructibles);
                ClearGeometryFlags(destructibles);
                objectsCleared += destructibles.Count;
                changed = destructibles.Count > 0;

                if (changed)
                    Debug.Log($"[LevelOptimizationTools] {path}: {destructibles.Count} objeto(s) de edificios destructibles desmarcados como estáticos (static batching los congela en la build).");
            }
            else
            {
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (GameObjectUtility.GetStaticEditorFlags(t.gameObject) == GeometryFlags) continue;

                    GameObjectUtility.SetStaticEditorFlags(t.gameObject, GeometryFlags);
                    objectsMarked++;
                    changed = true;
                }
            }

            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                prefabsTouched++;
            }

            PrefabUtility.UnloadPrefabContents(root);
        }

        int sceneObjectsCleared = EditorApplication.isPlaying ? 0 : ClearStaticDestructiblesInOpenScenes(destructibles);

        AssetDatabase.SaveAssets();
        Debug.Log($"[LevelOptimizationTools] {objectsMarked} objeto(s) marcados como estáticos y {objectsCleared} objeto(s) de edificios destructibles desmarcados en {prefabsTouched} prefab(s) de {StaticGeometryFolders.Length} carpeta(s); {sceneObjectsCleared} más en las escenas abiertas (guardalas si cambiaron). Ahora abrí cada escena de nivel y corré el paso 2 para bakear Occlusion Culling.");
    }

    public static void CollectStaticDestructibles(GameObject root, List<GameObject> results)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if ((GameObjectUtility.GetStaticEditorFlags(t.gameObject) & GeometryFlags) == 0) continue;
            if (t.GetComponentInParent<BuildingsScript>(true) == null && t.GetComponentInParent<BuildingFader>(true) == null) continue;

            results.Add(t.gameObject);
        }
    }

    private static int ClearStaticDestructiblesInOpenScenes(List<GameObject> buffer)
    {
        int cleared = 0;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;

            buffer.Clear();
            foreach (GameObject root in scene.GetRootGameObjects())
                CollectStaticDestructibles(root, buffer);

            if (buffer.Count == 0) continue;

            Undo.RecordObjects(buffer.ToArray(), "Desmarcar edificios destructibles estáticos");
            ClearGeometryFlags(buffer);
            EditorSceneManager.MarkSceneDirty(scene);
            cleared += buffer.Count;
            Debug.Log($"[LevelOptimizationTools] {scene.path}: {buffer.Count} objeto(s) de edificios destructibles desmarcados como estáticos.");
        }

        return cleared;
    }

    private static void ClearGeometryFlags(List<GameObject> objects)
    {
        foreach (GameObject go in objects)
        {
            GameObjectUtility.SetStaticEditorFlags(go, GameObjectUtility.GetStaticEditorFlags(go) & ~GeometryFlags);
            if (PrefabUtility.IsPartOfPrefabInstance(go))
                PrefabUtility.RecordPrefabInstancePropertyModifications(go);
        }
    }

    [MenuItem("Tools/Manners/Performance/2. Bakear Occlusion Culling (escena actual)", false, 41)]
    public static void BakeOcclusionCullingForCurrentScene()
    {
        if (StaticOcclusionCulling.isRunning)
        {
            Debug.LogWarning("[LevelOptimizationTools] Ya hay un bake de Occlusion Culling en curso.");
            return;
        }

        StaticOcclusionCulling.Compute();
        Debug.Log("[LevelOptimizationTools] Bake de Occlusion Culling iniciado para la escena abierta. Repetí esto en cada escena de nivel (LEVEL 1, CityTest, MilitaryBase, Sandbox) — cada una necesita su propio bake, no lo comparten. Progreso visible en Window > Rendering > Occlusion Culling.");
    }

    [MenuItem("Tools/Manners/Performance/3. Activar GPU Instancing en materiales", false, 42)]
    public static void EnableGpuInstancingOnMaterials()
    {
        string[] guids = AssetDatabase.FindAssets("t:Material");
        int enabled = 0;
        int alreadyOn = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            if (mat.enableInstancing)
            {
                alreadyOn++;
                continue;
            }

            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            enabled++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[LevelOptimizationTools] GPU Instancing activado en {enabled} material(es) ({alreadyOn} ya lo tenían).");
    }
}
