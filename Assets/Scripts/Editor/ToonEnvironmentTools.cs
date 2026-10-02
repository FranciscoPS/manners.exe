using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

public static class ToonEnvironmentTools
{
    public const string StyleAssetPath = "Assets/Resources/ToonEnvironmentStyle.asset";
    public const string ReportPath = "Logs/toon-style-report.txt";
    public const string ValidationPath = "Logs/toon-style-validation.txt";

    private static readonly string[] PrefabFolders = { "Assets/Prefabs/Buildings", "Assets/Prefabs/Props", "Assets/Prefabs/Map" };

    private static readonly string[] DefaultExcludedMaterialPaths =
    {
        "Assets/Materials/Map/ShopRangeMAT.mat",
        "Assets/Materials/Map/Floor.mat",
        "Assets/Materials/Map/FloorMAT.mat",
        "Assets/Materials/Map/FloorMap2.mat",
        "Assets/Materials/Map/VoidMat.mat",
    };

    private static readonly string[] DefaultEmbeddedMaterialPrefabs = { "Assets/Prefabs/Buildings/ItemShop.prefab" };

    private static readonly HashSet<string> ConvertibleShaders = new HashSet<string>
    {
        "Universal Render Pipeline/Lit",
        "Universal Render Pipeline/Simple Lit",
        "Universal Render Pipeline/Baked Lit",
        "Universal Render Pipeline/Unlit",
        "Toon/Toon",
        "Toon/Toon 3D as 2D (URP)",
        ToonEnvironmentStyle.ShaderName,
    };

    private sealed class MaterialUse
    {
        public Material material;
        public string firstPrefab;
    }

    [MenuItem("Tools/Manners/Visual toon/1. Convertir materiales de edificios y props al shader toon", false, 80)]
    public static void ConvertEnvironmentMaterialsFromMenu()
    {
        var report = new StringBuilder();
        ConvertEnvironmentMaterials(report);
        AssetDatabase.SaveAssets();
        Debug.Log(report.ToString());
    }

    [MenuItem("Tools/Manners/Visual toon/2. Configurar renderers de edificios y props (sin sombras ni probes)", false, 81)]
    public static void ConfigureEnvironmentRenderersFromMenu()
    {
        var report = new StringBuilder();
        ConfigureEnvironmentRenderers(report);
        AssetDatabase.SaveAssets();
        Debug.Log(report.ToString());
    }

    [MenuItem("Tools/Manners/Visual toon/3. Aplicar estilo del asset a los materiales gestionados", false, 82)]
    public static void ApplyStyleFromMenu()
    {
        var report = new StringBuilder();
        ApplyStyleToMaterials(report);
        AssetDatabase.SaveAssets();
        Debug.Log(report.ToString());
    }

    [MenuItem("Tools/Manners/Visual toon/4. Generar máscara de ventanas para un material...", false, 83)]
    public static void OpenMaskWindow() => ToonEmissionMaskWindow.Open();

    [MenuItem("Tools/Manners/Visual toon/5. Añadir pasada de contorno a los renderers URP", false, 84)]
    public static void EnsureOutlineRendererFeaturesFromMenu()
    {
        var report = new StringBuilder();
        EnsureOutlineRendererFeatures(report);
        AssetDatabase.SaveAssets();
        Debug.Log(report.ToString());
    }

    [MenuItem("Tools/Manners/Visual toon/6. Limitar texturas del mapa a 1024 en WebGL", false, 85)]
    public static void ConfigureWebTexturesFromMenu()
    {
        var report = new StringBuilder();
        ConfigureWebTextures(report);
        Debug.Log(report.ToString());
    }

    [MenuItem("Tools/Manners/Visual toon/7. Validar configuración toon", false, 86)]
    public static void ValidateFromMenu()
    {
        var report = new StringBuilder();
        bool passed = Validate(report);
        if (passed) Debug.Log(report.ToString());
        else Debug.LogWarning(report.ToString());
    }

    public static void RunAllBatch()
    {
        var report = new StringBuilder();
        bool passed;
        try
        {
            ToonStylePreview.CaptureLevel1("antes", report);
            ConvertEnvironmentMaterials(report);
            ConfigureEnvironmentRenderers(report);
            EnsureOutlineRendererFeatures(report);
            ConfigureWebTextures(report);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            passed = Validate(report);
            ToonStylePreview.CaptureLevel1("despues", report);
        }
        catch (Exception exception)
        {
            report.AppendLine("EXCEPTION: " + exception);
            passed = false;
        }
        Directory.CreateDirectory("Logs");
        File.WriteAllText(ReportPath, report.ToString());
        if (passed) Debug.Log("[ToonEnvironmentTools] Conversión completa. Informe: " + ReportPath);
        else Debug.LogError("[ToonEnvironmentTools] Conversión con problemas. Informe: " + ReportPath);
        if (Application.isBatchMode) EditorApplication.Exit(passed ? 0 : 1);
    }

    public static ToonEnvironmentStyle GetOrCreateStyle()
    {
        var style = AssetDatabase.LoadAssetAtPath<ToonEnvironmentStyle>(StyleAssetPath);
        if (style != null) return style;
        style = ScriptableObject.CreateInstance<ToonEnvironmentStyle>();
        foreach (string path in DefaultExcludedMaterialPaths)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) style.excludedMaterials.Add(material);
        }
        foreach (string path in DefaultEmbeddedMaterialPrefabs)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null) style.prefabsWithEmbeddedMaterials.Add(prefab);
        }
        EditorAssetUtility.EnsureFolder(Path.GetDirectoryName(StyleAssetPath).Replace('\\', '/'));
        AssetDatabase.CreateAsset(style, StyleAssetPath);
        AssetDatabase.SaveAssets();
        return style;
    }

    public static void ConvertEnvironmentMaterials(StringBuilder report)
    {
        ToonEnvironmentStyle style = GetOrCreateStyle();
        Shader toon = Shader.Find(ToonEnvironmentStyle.ShaderName);
        if (toon == null)
        {
            report.AppendLine("ERROR: no se encontró el shader " + ToonEnvironmentStyle.ShaderName);
            return;
        }

        ExtractEmbeddedMaterials(style, report);
        List<MaterialUse> uses = CollectEnvironmentMaterials(report);
        AddFolderMaterials(uses, style);
        AddParentMaterials(uses);
        var convertible = new List<MaterialUse>();
        foreach (MaterialUse use in uses)
        {
            Material material = use.material;
            string path = AssetDatabase.GetAssetPath(material);
            if (style.excludedMaterials.Contains(material))
            {
                report.AppendLine($"EXCLUIDO {path}");
                continue;
            }
            if (string.IsNullOrEmpty(path) || !path.EndsWith(".mat", StringComparison.OrdinalIgnoreCase) || AssetDatabase.IsSubAsset(material))
            {
                report.AppendLine($"EMBEBIDO (extraer del FBX antes de convertir) {material.name} en {use.firstPrefab}");
                continue;
            }
            if (material.shader == null || !ConvertibleShaders.Contains(material.shader.name))
            {
                report.AppendLine($"OMITIDO (shader {material.shader?.name}) {path}");
                continue;
            }
            convertible.Add(use);
        }

        convertible.Sort((a, b) =>
        {
            int depth = ParentDepth(a.material).CompareTo(ParentDepth(b.material));
            return depth != 0 ? depth : string.CompareOrdinal(AssetDatabase.GetAssetPath(a.material), AssetDatabase.GetAssetPath(b.material));
        });

        int windowIndex = 0;
        foreach (MaterialUse use in convertible)
        {
            string path = AssetDatabase.GetAssetPath(use.material);
            string outcome = ConvertMaterial(use.material, toon, style, ref windowIndex);
            report.AppendLine($"CONVERTIDO {path}: {outcome}");
            if (!style.environmentMaterials.Contains(use.material)) style.environmentMaterials.Add(use.material);
        }
        style.environmentMaterials.RemoveAll(material => material == null);
        EditorUtility.SetDirty(style);
        report.AppendLine($"RESUMEN materiales: {convertible.Count} convertidos, {style.environmentMaterials.Count} gestionados en {StyleAssetPath}");
    }

    private static void ExtractEmbeddedMaterials(ToonEnvironmentStyle style, StringBuilder report)
    {
        var reimport = new HashSet<string>();
        foreach (GameObject prefab in style.prefabsWithEmbeddedMaterials)
        {
            if (prefab == null) continue;
            foreach (Renderer renderer in prefab.GetComponentsInChildren<Renderer>(true))
            {
                if (!IsEnvironmentRenderer(renderer)) continue;
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material == null || !AssetDatabase.IsSubAsset(material) || style.excludedMaterials.Contains(material)) continue;
                    string modelPath = AssetDatabase.GetAssetPath(material);
                    string folder = Path.GetDirectoryName(modelPath).Replace('\\', '/') + "/Materials";
                    EditorAssetUtility.EnsureFolder(folder);
                    string target = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + SanitizeFileName(material.name) + ".mat");
                    string error = AssetDatabase.ExtractAsset(material, target);
                    if (string.IsNullOrEmpty(error))
                    {
                        reimport.Add(modelPath);
                        report.AppendLine($"EXTRAIDO {material.name} de {modelPath} -> {target}");
                    }
                    else report.AppendLine($"ERROR al extraer {material.name} de {modelPath}: {error}");
                }
            }
        }
        foreach (string modelPath in reimport)
        {
            AssetDatabase.WriteImportSettingsIfDirty(modelPath);
            AssetDatabase.ImportAsset(modelPath, ImportAssetOptions.ForceUpdate);
        }
        if (reimport.Count > 0) AssetDatabase.SaveAssets();
    }

    private static string SanitizeFileName(string name)
    {
        foreach (char invalid in Path.GetInvalidFileNameChars()) name = name.Replace(invalid, '_');
        return name.Trim();
    }

    private static void AddFolderMaterials(List<MaterialUse> uses, ToonEnvironmentStyle style)
    {
        if (style.materialFolders == null) return;
        var folders = style.materialFolders.Where(folder => !string.IsNullOrEmpty(folder) && AssetDatabase.IsValidFolder(folder)).ToArray();
        if (folders.Length == 0) return;
        var known = new HashSet<Material>(uses.Select(use => use.material));
        foreach (string guid in AssetDatabase.FindAssets("t:Material", folders))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null && known.Add(material)) uses.Add(new MaterialUse { material = material, firstPrefab = "(carpeta de materiales, sin prefab)" });
        }
    }

    private static void AddParentMaterials(List<MaterialUse> uses)
    {
        var known = new HashSet<Material>(uses.Select(use => use.material));
        for (int i = 0; i < uses.Count; i++)
        {
            Material parent = uses[i].material.parent;
            int guard = 0;
            while (parent != null && guard++ < 16)
            {
                if (known.Add(parent)) uses.Add(new MaterialUse { material = parent, firstPrefab = uses[i].firstPrefab + " (padre de variante)" });
                parent = parent.parent;
            }
        }
    }

    private static bool IsInAutoEmissionFolder(Material material, ToonEnvironmentStyle style)
    {
        string path = AssetDatabase.GetAssetPath(material);
        if (style.autoEmissionFolders == null) return false;
        foreach (string folder in style.autoEmissionFolders)
            if (!string.IsNullOrEmpty(folder) && path.StartsWith(folder.TrimEnd('/') + "/", StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    private static bool HasSiblingWindowMaterial(Material material, ToonEnvironmentStyle style)
    {
        string directory = Path.GetDirectoryName(AssetDatabase.GetAssetPath(material)).Replace('\\', '/');
        foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { directory }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetDirectoryName(path).Replace('\\', '/') != directory) continue;
            var sibling = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (sibling != null && sibling != material && style.IsWindowMaterial(sibling)) return true;
        }
        return false;
    }

    private static int ParentDepth(Material material)
    {
        int depth = 0;
        Material parent = material.parent;
        while (parent != null && depth < 16)
        {
            depth++;
            parent = parent.parent;
        }
        return depth;
    }

    private static List<MaterialUse> CollectEnvironmentMaterials(StringBuilder report)
    {
        var uses = new Dictionary<Material, MaterialUse>();
        foreach (string path in EnvironmentPrefabPaths())
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;
            foreach (Renderer renderer in prefab.GetComponentsInChildren<Renderer>(true))
            {
                if (!IsEnvironmentRenderer(renderer)) continue;
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null)
                    {
                        report.AppendLine($"FALTANTE material nulo en {path} -> {HierarchyPath(renderer.transform)} (slot {i}); se verá magenta.");
                        continue;
                    }
                    if (!uses.ContainsKey(materials[i])) uses.Add(materials[i], new MaterialUse { material = materials[i], firstPrefab = path });
                }
            }
        }
        return uses.Values.ToList();
    }

    private static IEnumerable<string> EnvironmentPrefabPaths()
    {
        return AssetDatabase.FindAssets("t:Prefab", PrefabFolders)
            .Select(AssetDatabase.GUIDToAssetPath)
            .Distinct()
            .OrderBy(path => path, StringComparer.Ordinal);
    }

    private static bool IsEnvironmentRenderer(Renderer renderer) => renderer is MeshRenderer || renderer is SkinnedMeshRenderer;

    private static string HierarchyPath(Transform transform)
    {
        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }
        return path;
    }

    private static string ConvertMaterial(Material material, Shader toon, ToonEnvironmentStyle style, ref int windowIndex)
    {
        bool alreadyToon = material.shader == toon;
        Texture baseMap = FirstTexture(material, "_BaseMap", "_MainTex");
        Color baseColor = FirstColor(material, Color.white, "_BaseColor", "_Color");
        baseColor.a = 1f;
        Texture maskMap = FirstTexture(material, "_MetallicGlossMap", "_MaskMap");
        bool keepEmission = alreadyToon && material.IsKeywordEnabled("_EMISSION") && material.GetTexture(ToonEnvironmentStyle.EmissionMapId) != null;

        if (!alreadyToon) material.shader = toon;
        material.SetTexture(ToonEnvironmentStyle.BaseMapId, baseMap);
        material.SetColor(ToonEnvironmentStyle.BaseColorId, baseColor);
        material.SetFloat("_Surface", 0f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_Cull", (float)CullMode.Back);
        material.SetFloat("_SrcBlend", (float)BlendMode.One);
        material.SetFloat("_DstBlend", (float)BlendMode.Zero);
        material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        material.SetFloat("_DstBlendAlpha", (float)BlendMode.Zero);
        material.SetFloat("_ZWrite", 1f);
        material.SetFloat("_ReceiveShadowsOff", 1f);
        material.renderQueue = -1;
        material.SetOverrideTag("RenderType", "Opaque");
        material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        if (material.parent == null) style.ApplyShadingTo(material);

        var keywords = new List<string> { "_RECEIVE_SHADOWS_OFF" };
        string outcome;
        if (keepEmission)
        {
            keywords.Add("_EMISSION");
            material.SetFloat("_EmissionEnabled", 1f);
            outcome = "emisión existente conservada";
        }
        else
        {
            Texture emissionMap = null;
            Color emissionColor = Color.black;
            string method;
            if (style.IsWindowMaterial(material) && baseMap != null)
            {
                emissionMap = baseMap;
                emissionColor = style.windowTextureEmission;
                method = "material de ventana: toda la textura emite";
            }
            else if (baseMap == null) method = "sin textura base; sin emisión";
            else if (!IsInAutoEmissionFolder(material, style)) method = "sin emisión automática (fuera de las carpetas de edificios); usa la herramienta de máscaras si lleva luces";
            else if (HasSiblingWindowMaterial(material, style)) method = "fachada con material de ventana aparte; sin emisión propia";
            else
            {
                ToonEmissionMaskBuilder.Result result = ToonEmissionMaskBuilder.Automatic(baseMap, maskMap);
                if (result.mask != null)
                {
                    emissionMap = ToonEmissionMaskBuilder.SaveMaskAsset(result.mask, baseMap);
                    Object.DestroyImmediate(result.mask);
                    emissionColor = style.WindowEmissionColor(windowIndex++);
                    method = $"máscara generada por {result.method} (cobertura {result.coverage:P1})";
                }
                else method = "sin emisión: " + result.warning;
            }

            if (emissionMap != null)
            {
                material.SetTexture(ToonEnvironmentStyle.EmissionMapId, emissionMap);
                material.SetColor(ToonEnvironmentStyle.EmissionColorId, emissionColor);
                material.SetFloat(ToonEnvironmentStyle.EmissionBaseTintId, 0f);
                material.SetFloat("_EmissionEnabled", 1f);
                keywords.Add("_EMISSION");
            }
            else
            {
                material.SetTexture(ToonEnvironmentStyle.EmissionMapId, null);
                material.SetFloat("_EmissionEnabled", 0f);
            }
            outcome = method;
        }

        material.shaderKeywords = keywords.ToArray();
        material.SetShaderPassEnabled(ToonEnvironmentStyle.OutlinePassLightMode, true);
        RemoveUnusedTextureReferences(material);
        EditorUtility.SetDirty(material);
        return outcome;
    }

    private static Texture FirstTexture(Material material, params string[] names)
    {
        foreach (string name in names)
            if (material.HasProperty(name) && material.GetTexture(name) != null) return material.GetTexture(name);
        return null;
    }

    private static Color FirstColor(Material material, Color fallback, params string[] names)
    {
        foreach (string name in names)
            if (material.HasProperty(name)) return material.GetColor(name);
        return fallback;
    }

    public static void RemoveUnusedTextureReferences(Material material)
    {
        var valid = new HashSet<string>();
        Shader shader = material.shader;
        for (int i = 0; i < shader.GetPropertyCount(); i++)
            if (shader.GetPropertyType(i) == UnityEngine.Rendering.ShaderPropertyType.Texture) valid.Add(shader.GetPropertyName(i));

        var serialized = new SerializedObject(material);
        SerializedProperty texEnvs = serialized.FindProperty("m_SavedProperties.m_TexEnvs");
        for (int i = texEnvs.arraySize - 1; i >= 0; i--)
        {
            string name = texEnvs.GetArrayElementAtIndex(i).FindPropertyRelative("first").stringValue;
            if (!valid.Contains(name)) texEnvs.DeleteArrayElementAtIndex(i);
        }
        SerializedProperty invalidKeywords = serialized.FindProperty("m_InvalidKeywords");
        if (invalidKeywords != null) invalidKeywords.ClearArray();
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void ConfigureEnvironmentRenderers(StringBuilder report)
    {
        ToonEnvironmentStyle style = GetOrCreateStyle();
        var ordered = EnvironmentPrefabPaths().ToList();
        ordered.Sort((a, b) =>
        {
            int rankA = PrefabRank(a);
            int rankB = PrefabRank(b);
            return rankA != rankB ? rankA.CompareTo(rankB) : string.CompareOrdinal(a, b);
        });

        int prefabsChanged = 0;
        int renderersChanged = 0;
        foreach (string path in ordered)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (root == null) continue;
            try
            {
                bool changed = false;
                foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    if (!IsEnvironmentRenderer(renderer) || UsesExcludedMaterial(renderer, style)) continue;
                    if (ConfigureRenderer(renderer))
                    {
                        changed = true;
                        renderersChanged++;
                    }
                }
                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    prefabsChanged++;
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
        report.AppendLine($"RESUMEN renderers: {renderersChanged} renderers ajustados en {prefabsChanged} prefabs (sin sombras, sin probes, sin motion vectors por objeto).");
    }

    private static int PrefabRank(string path)
    {
        if (path.StartsWith("Assets/Prefabs/Map/", StringComparison.Ordinal)) return 2;
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        return asset != null && PrefabUtility.GetPrefabAssetType(asset) == PrefabAssetType.Variant ? 1 : 0;
    }

    private static bool UsesExcludedMaterial(Renderer renderer, ToonEnvironmentStyle style)
    {
        foreach (Material material in renderer.sharedMaterials)
            if (material != null && style.excludedMaterials.Contains(material)) return true;
        return false;
    }

    public static bool ConfigureRenderer(Renderer renderer)
    {
        bool changed = false;
        if (renderer.shadowCastingMode != ShadowCastingMode.Off) { renderer.shadowCastingMode = ShadowCastingMode.Off; changed = true; }
        if (renderer.receiveShadows) { renderer.receiveShadows = false; changed = true; }
        if (renderer.lightProbeUsage != LightProbeUsage.Off) { renderer.lightProbeUsage = LightProbeUsage.Off; changed = true; }
        if (renderer.reflectionProbeUsage != ReflectionProbeUsage.Off) { renderer.reflectionProbeUsage = ReflectionProbeUsage.Off; changed = true; }
        if (renderer.motionVectorGenerationMode != MotionVectorGenerationMode.Camera) { renderer.motionVectorGenerationMode = MotionVectorGenerationMode.Camera; changed = true; }
        return changed;
    }

    public const string OutlineFeatureName = "ToonOutline";

    public static IEnumerable<UniversalRendererData> MainRendererAssets()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:UniversalRendererData", new[] { "Assets/Settings" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
            if (renderer != null && !IsMinimapRenderer(renderer)) yield return renderer;
        }
    }

    public static bool IsMinimapRenderer(ScriptableRendererData renderer) =>
        renderer != null && renderer.name.IndexOf("Minimap", StringComparison.OrdinalIgnoreCase) >= 0;

    public static RenderObjects FindOutlineFeature(ScriptableRendererData renderer)
    {
        foreach (ScriptableRendererFeature feature in renderer.rendererFeatures)
            if (feature is RenderObjects renderObjects && renderObjects.settings.filterSettings.PassNames != null
                && Array.IndexOf(renderObjects.settings.filterSettings.PassNames, ToonEnvironmentStyle.OutlinePassLightMode) >= 0)
                return renderObjects;
        return null;
    }

    public static void EnsureOutlineRendererFeatures(StringBuilder report)
    {
        foreach (UniversalRendererData renderer in MainRendererAssets())
        {
            string path = AssetDatabase.GetAssetPath(renderer);
            RenderObjects existing = FindOutlineFeature(renderer);
            if (existing != null)
            {
                bool changed = ConfigureOutlineFeature(existing);
                if (changed) EditorUtility.SetDirty(existing);
                report.AppendLine($"CONTORNO {path}: feature '{existing.name}' ya presente{(changed ? " (ajustado)" : "")}.");
                continue;
            }
            var feature = ScriptableObject.CreateInstance<RenderObjects>();
            feature.name = OutlineFeatureName;
            ConfigureOutlineFeature(feature);
            AssetDatabase.AddObjectToAsset(feature, renderer);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long localId);
            var serialized = new SerializedObject(renderer);
            SerializedProperty features = serialized.FindProperty("m_RendererFeatures");
            SerializedProperty map = serialized.FindProperty("m_RendererFeatureMap");
            features.arraySize++;
            features.GetArrayElementAtIndex(features.arraySize - 1).objectReferenceValue = feature;
            map.arraySize++;
            map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(renderer);
            report.AppendLine($"CONTORNO {path}: feature '{OutlineFeatureName}' añadido (AfterRenderingOpaques, cola opaca, pasada {ToonEnvironmentStyle.OutlinePassLightMode}).");
        }
    }

    private static bool ConfigureOutlineFeature(RenderObjects feature)
    {
        bool changed = false;
        RenderObjects.RenderObjectsSettings settings = feature.settings;
        if (settings.passTag != OutlineFeatureName) { settings.passTag = OutlineFeatureName; changed = true; }
        if (settings.Event != RenderPassEvent.AfterRenderingOpaques) { settings.Event = RenderPassEvent.AfterRenderingOpaques; changed = true; }
        if (settings.filterSettings.RenderQueueType != RenderQueueType.Opaque) { settings.filterSettings.RenderQueueType = RenderQueueType.Opaque; changed = true; }
        if (settings.filterSettings.LayerMask.value != ~0) { settings.filterSettings.LayerMask = ~0; changed = true; }
        string[] passNames = settings.filterSettings.PassNames;
        if (passNames == null || passNames.Length != 1 || passNames[0] != ToonEnvironmentStyle.OutlinePassLightMode)
        {
            settings.filterSettings.PassNames = new[] { ToonEnvironmentStyle.OutlinePassLightMode };
            changed = true;
        }
        if (settings.overrideMaterial != null) { settings.overrideMaterial = null; changed = true; }
        if (settings.overrideDepthState) { settings.overrideDepthState = false; changed = true; }
        if (!feature.isActive) { feature.SetActive(true); changed = true; }
        return changed;
    }

    public const int WebMaxTextureSize = 1024;

    public static void ConfigureWebTextures(StringBuilder report)
    {
        ToonEnvironmentStyle style = GetOrCreateStyle();
        var textures = new HashSet<Texture>();
        foreach (Material material in style.environmentMaterials)
        {
            if (material == null) continue;
            foreach (int id in new[] { ToonEnvironmentStyle.BaseMapId, ToonEnvironmentStyle.EmissionMapId })
            {
                Texture texture = material.HasProperty(id) ? material.GetTexture(id) : null;
                if (texture != null) textures.Add(texture);
            }
        }
        int changed = 0;
        foreach (Texture texture in textures)
        {
            string path = AssetDatabase.GetAssetPath(texture);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;
            TextureImporterPlatformSettings web = importer.GetPlatformTextureSettings("WebGL");
            int sourceMax = Mathf.Max(importer.maxTextureSize, Mathf.Max(texture.width, texture.height));
            if (sourceMax <= WebMaxTextureSize) continue;
            if (web.overridden && web.maxTextureSize <= WebMaxTextureSize) continue;
            TextureImporterPlatformSettings defaults = importer.GetDefaultPlatformTextureSettings();
            web.overridden = true;
            web.maxTextureSize = WebMaxTextureSize;
            web.format = TextureImporterFormat.Automatic;
            web.textureCompression = defaults.textureCompression;
            web.compressionQuality = defaults.compressionQuality;
            web.crunchedCompression = defaults.crunchedCompression;
            importer.SetPlatformTextureSettings(web);
            importer.SaveAndReimport();
            changed++;
            report.AppendLine($"TEXTURA WEB {path}: máximo {WebMaxTextureSize} px solo en WebGL (escritorio conserva {importer.maxTextureSize}).");
        }
        report.AppendLine($"RESUMEN texturas: {changed} texturas limitadas a {WebMaxTextureSize} px en WebGL de {textures.Count} usadas por los materiales toon.");
    }

    public static void ApplyStyleToMaterials(StringBuilder report)
    {
        ToonEnvironmentStyle style = GetOrCreateStyle();
        int applied = 0;
        foreach (Material material in style.environmentMaterials)
        {
            if (material == null || material.parent != null) continue;
            style.ApplyShadingTo(material);
            EditorUtility.SetDirty(material);
            applied++;
        }
        report.AppendLine($"RESUMEN estilo: valores de sombra y contorno aplicados a {applied} materiales (las variantes heredan de su padre).");
    }

    public static bool? IsSrpBatcherCompatible(Shader shader)
    {
        var method = typeof(ShaderUtil).GetMethod("GetSRPBatcherCompatibilityCode",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        if (method == null || shader == null) return null;
        object code = method.Invoke(null, new object[] { shader, 0 });
        return code is int value ? value == 0 : (bool?)null;
    }

    public static bool Validate(StringBuilder report)
    {
        var problems = new List<string>();
        var notes = new List<string>();

        Shader toon = Shader.Find(ToonEnvironmentStyle.ShaderName);
        if (toon == null) problems.Add("Shader " + ToonEnvironmentStyle.ShaderName + " no encontrado.");
        else if (ShaderUtil.ShaderHasError(toon)) problems.Add("El shader toon tiene errores de compilación (ver consola).");
        else if (IsSrpBatcherCompatible(toon) == false) problems.Add("El shader toon no es compatible con el SRP Batcher (revisar el CBUFFER UnityPerMaterial).");

        var style = AssetDatabase.LoadAssetAtPath<ToonEnvironmentStyle>(StyleAssetPath);
        if (style == null) problems.Add("Falta el asset " + StyleAssetPath + " (los ajustes gráficos no podrán apagar los contornos).");
        else
        {
            if (style.environmentMaterials.Count == 0) problems.Add("El estilo no gestiona ningún material: ejecuta la conversión.");
            foreach (Material material in style.environmentMaterials)
            {
                if (material == null) { problems.Add("Referencia nula en la lista de materiales gestionados."); continue; }
                string path = AssetDatabase.GetAssetPath(material);
                if (material.shader != toon) problems.Add($"{path} no usa el shader toon ({material.shader?.name}).");
                if (!material.IsKeywordEnabled("_RECEIVE_SHADOWS_OFF")) notes.Add($"{path} recibe sombras en tiempo real (más caro; opcional).");
                if (!material.GetShaderPassEnabled(ToonEnvironmentStyle.OutlinePassLightMode)) problems.Add($"{path} tiene la pasada de contorno apagada en el asset.");
                if (material.HasProperty("_Surface") && material.GetFloat("_Surface") != 0f) problems.Add($"{path} está guardado como transparente (_Surface=1).");
                if (material.renderQueue != toon.renderQueue && material.renderQueue != -1) problems.Add($"{path} tiene render queue {material.renderQueue}.");
                if (material.IsKeywordEnabled("_EMISSION") && material.GetTexture(ToonEnvironmentStyle.EmissionMapId) == null) problems.Add($"{path} tiene emisión activa sin máscara.");
                if (!material.IsKeywordEnabled("_EMISSION")) notes.Add($"{path} sin emisión (sin ventanas encendidas).");
            }
        }

        foreach (string guid in AssetDatabase.FindAssets("t:UniversalRendererData", new[] { "Assets/Settings" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
            if (renderer == null) continue;
            var serialized = new SerializedObject(renderer);
            SerializedProperty priming = serialized.FindProperty("m_DepthPrimingMode");
            if (priming != null && priming.intValue != 0) problems.Add($"{path}: Depth Priming debe estar en Disabled para que se vean los contornos inverted-hull.");
            SerializedProperty renderingMode = serialized.FindProperty("m_RenderingMode");
            if (renderingMode != null && renderingMode.intValue == 1) problems.Add($"{path}: Deferred no soporta el shader toon (usa Forward).");
            RenderObjects outline = FindOutlineFeature(renderer);
            if (IsMinimapRenderer(renderer))
            {
                if (outline != null) notes.Add($"{path}: el minimapa dibuja contornos (coste extra sin beneficio).");
            }
            else if (outline == null) problems.Add($"{path}: falta el RenderObjects '{OutlineFeatureName}' que dibuja la pasada de contorno; ejecuta 'Añadir pasada de contorno'.");
            else if (!outline.isActive || outline.settings.Event != RenderPassEvent.AfterRenderingOpaques) problems.Add($"{path}: el feature '{outline.name}' debe estar activo y en AfterRenderingOpaques.");
        }

        foreach (string guid in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset", new[] { "Assets/Settings" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
            if (pipeline == null) continue;
            if (!pipeline.supportsHDR) notes.Add($"{path}: HDR desactivado; las ventanas no podrán superar el umbral de Bloom.");
        }

        int castingRenderers = 0;
        int missingMaterials = 0;
        foreach (string path in EnvironmentPrefabPaths())
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;
            foreach (Renderer renderer in prefab.GetComponentsInChildren<Renderer>(true))
            {
                if (!IsEnvironmentRenderer(renderer)) continue;
                if (style != null && UsesExcludedMaterial(renderer, style)) continue;
                if (renderer.shadowCastingMode != ShadowCastingMode.Off) castingRenderers++;
                foreach (Material material in renderer.sharedMaterials)
                    if (material == null) missingMaterials++;
            }
        }
        if (castingRenderers > 0) problems.Add($"{castingRenderers} renderers de edificios/props siguen proyectando sombras: ejecuta 'Configurar renderers'.");
        if (missingMaterials > 0) notes.Add($"{missingMaterials} slots de material vacíos en prefabs de edificios (piezas rotas con materiales no versionados): se ven magenta.");

        report.AppendLine(problems.Count == 0 ? "VALIDACIÓN toon: OK" : $"VALIDACIÓN toon: {problems.Count} problema(s)");
        foreach (string problem in problems) report.AppendLine("  PROBLEMA " + problem);
        foreach (string note in notes) report.AppendLine("  NOTA " + note);
        Directory.CreateDirectory("Logs");
        File.WriteAllText(ValidationPath, report.ToString());
        return problems.Count == 0;
    }
}
