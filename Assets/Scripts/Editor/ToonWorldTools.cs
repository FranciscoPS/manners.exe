using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class ToonWorldTools
{
    public const string TerrainMaterialPath = "Assets/Materials/Map/ToonTerrain.mat";
    public const string TerrainDetailPath = "Assets/Materials/Map/ToonTerrainDetail.png";
    public const string GroundShadowMaterialPath = "Assets/Materials/Map/ToonGroundShadow.mat";
    public const string GroundShadowMeshFolder = "Assets/3DModels/Map/GroundShadows";
    public const string GroundShadowObjectName = "ToonGroundShadow";
    public const string GroundShadowFeatureName = "ToonGroundShadow";
    public const string PlayerPrefabPath = "Assets/Prefabs/Characters/Player.prefab";

    private const float GroundShadowBoundsFactor = 1.5f;
    private static readonly string[] TerrainPrefabFolders = { "Assets/Prefabs/Map" };

    private static readonly string[] DefaultGroundReceiverPaths =
    {
        "Assets/Materials/Map/Floor.mat",
    };

    private static readonly string[] NoLongerExcludedPaths =
    {
        "Assets/Materials/Map/Floor.mat",
        "Assets/Materials/Map/FloorMAT.mat",
    };

    private static readonly HashSet<string> ConvertibleCharacterShaders = new HashSet<string>
    {
        "Universal Render Pipeline/Lit",
        "Universal Render Pipeline/Simple Lit",
        "Universal Render Pipeline/Unlit",
        "Toon/Toon",
        "Toon/Toon (Tessellation)",
        "Toon/Toon(Tessellation)",
        ToonEnvironmentStyle.ShaderName,
        ToonEnvironmentStyle.CharacterShaderName,
    };

    [MenuItem("Tools/Manners/Visual toon/8. Configurar terreno toon (piso y montañas)", false, 87)]
    public static void ConfigureTerrainFromMenu() => RunFromMenu(ConfigureTerrain);

    [MenuItem("Tools/Manners/Visual toon/9. Generar sombras planas de edificios", false, 88)]
    public static void BuildGroundShadowsFromMenu() => RunFromMenu(report =>
    {
        BuildGroundShadows(report);
        EnsureGroundShadowRendererFeatures(report);
    });

    [MenuItem("Tools/Manners/Visual toon/10. Convertir personajes al shader toon", false, 89)]
    public static void ConvertCharactersFromMenu() => RunFromMenu(ConvertCharacters);

    private static void RunFromMenu(Action<StringBuilder> action)
    {
        var report = new StringBuilder();
        action(report);
        AssetDatabase.SaveAssets();
        Debug.Log(report.ToString());
    }

    public static void EnsureWorldDefaults(ToonEnvironmentStyle style, StringBuilder report)
    {
        bool changed = false;
        if (style.groundReceiverMaterials.Count == 0 && style.terrainMaterial == null)
        {
            foreach (string path in NoLongerExcludedPaths)
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material != null && style.excludedMaterials.Remove(material))
                {
                    changed = true;
                    report.AppendLine($"ESTILO {path} deja de estar excluido: el piso ya forma parte del estilo toon.");
                }
            }
            foreach (string path in DefaultGroundReceiverPaths)
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material != null && !style.groundReceiverMaterials.Contains(material)) { style.groundReceiverMaterials.Add(material); changed = true; }
            }
        }
        var player = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        if (player != null && style.characterMaterials.Count == 0)
        {
            if (style.groundShadowCharacterPrefabs.Count == 0) { style.groundShadowCharacterPrefabs.Add(player); changed = true; }
            if (style.rimLightCharacterPrefabs.Count == 0) { style.rimLightCharacterPrefabs.Add(player); changed = true; }
        }
        if (changed) EditorUtility.SetDirty(style);
    }

    public static void ConfigureTerrain(StringBuilder report)
    {
        ToonEnvironmentStyle style = ToonEnvironmentTools.GetOrCreateStyle();
        Shader shader = Shader.Find(ToonEnvironmentStyle.TerrainShaderName);
        if (shader == null)
        {
            report.AppendLine("ERROR: no se encontró el shader " + ToonEnvironmentStyle.TerrainShaderName);
            return;
        }

        int terrains = 0;
        foreach (string path in AssetDatabase.FindAssets("t:Prefab", TerrainPrefabFolders).Select(AssetDatabase.GUIDToAssetPath).Distinct().OrderBy(value => value, StringComparer.Ordinal))
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null || asset.GetComponentInChildren<Terrain>(true) == null) continue;
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                bool changed = false;
                foreach (Terrain terrain in root.GetComponentsInChildren<Terrain>(true))
                {
                    TerrainData data = terrain.terrainData;
                    if (data == null) continue;
                    if (style.terrainMaterial != null && terrains > 0)
                    {
                        report.AppendLine($"AVISO {path}: hay más de un terreno; '{terrain.name}' reutiliza el material del primero y solo se verá bien si comparte TerrainData.");
                    }
                    Material material = EnsureTerrainMaterial(style, shader, data, report);
                    terrains++;
                    if (terrain.materialTemplate != material) { terrain.materialTemplate = material; changed = true; }
                    if (terrain.shadowCastingMode != ShadowCastingMode.Off) { terrain.shadowCastingMode = ShadowCastingMode.Off; changed = true; }
                    if (terrain.reflectionProbeUsage != ReflectionProbeUsage.Off) { terrain.reflectionProbeUsage = ReflectionProbeUsage.Off; changed = true; }
                    report.AppendLine($"TERRENO {path} -> '{terrain.name}': material {AssetDatabase.GetAssetPath(material)}, {data.alphamapLayers} capas, sin proyectar sombras ni reflection probes.");
                }
                if (changed) PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
        if (terrains == 0) report.AppendLine("AVISO: no se encontró ningún Terrain en " + string.Join(", ", TerrainPrefabFolders));
        EditorUtility.SetDirty(style);
    }

    private static Material EnsureTerrainMaterial(ToonEnvironmentStyle style, Shader shader, TerrainData data, StringBuilder report)
    {
        Material material = style.terrainMaterial;
        bool created = false;
        if (material == null) material = AssetDatabase.LoadAssetAtPath<Material>(TerrainMaterialPath);
        if (material == null)
        {
            material = new Material(shader) { name = Path.GetFileNameWithoutExtension(TerrainMaterialPath) };
            EditorAssetUtility.EnsureFolder(Path.GetDirectoryName(TerrainMaterialPath).Replace('\\', '/'));
            AssetDatabase.CreateAsset(material, TerrainMaterialPath);
            created = true;
        }
        if (material.shader != shader) material.shader = shader;
        style.terrainMaterial = material;

        Texture2D[] alphamaps = data.alphamapTextures;
        material.SetTexture(ToonEnvironmentStyle.TerrainControl1Id, alphamaps.Length > 1 ? alphamaps[1] : null);
        if (data.alphamapLayers > ToonEnvironmentStyle.MaxTerrainLayers)
            report.AppendLine($"AVISO terreno: tiene {data.alphamapLayers} capas y el shader toon dibuja las primeras {ToonEnvironmentStyle.MaxTerrainLayers}.");

        if (created)
        {
            TerrainLayer[] layers = data.terrainLayers;
            for (int i = 0; i < layers.Length && i < ToonEnvironmentStyle.MaxTerrainLayers; i++)
            {
                Color flat = LayerFlatColor(layers[i], style.terrainPaletteBrightness);
                material.SetColor("_LayerColor" + i, flat);
                report.AppendLine($"  capa {i} ({(layers[i] != null ? layers[i].name : "vacía")}): color plano inicial #{ColorUtility.ToHtmlStringRGB(flat)}");
            }
            Texture2D detail = EnsureTerrainDetailTexture(report);
            if (detail != null)
            {
                material.SetTexture("_DetailMap", detail);
                material.SetFloat("_DetailTiling", 14f);
                material.SetFloat("_DetailThreshold", 0.42f);
                material.SetVector("_DetailStrength0", new Vector4(0.06f, 0.06f, 0.06f, 0.06f));
                material.SetVector("_DetailStrength1", new Vector4(0.06f, 0.06f, 0.06f, 0.06f));
                material.EnableKeyword(ToonEnvironmentStyle.TerrainDetailKeyword);
            }
        }
        style.ApplyTerrainShadingTo(material);
        material.enableInstancing = true;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Texture2D EnsureTerrainDetailTexture(StringBuilder report)
    {
        var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(TerrainDetailPath);
        if (existing != null) return existing;
        Texture2D pattern = BuildDetailPattern(256, 4);
        try
        {
            EditorAssetUtility.EnsureFolder(Path.GetDirectoryName(TerrainDetailPath).Replace('\\', '/'));
            File.WriteAllBytes(TerrainDetailPath, pattern.EncodeToPNG());
        }
        finally
        {
            Object.DestroyImmediate(pattern);
        }
        AssetDatabase.ImportAsset(TerrainDetailPath, ImportAssetOptions.ForceUpdate);
        if (AssetImporter.GetAtPath(TerrainDetailPath) is TextureImporter importer)
        {
            importer.textureType = TextureImporterType.SingleChannel;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.singleChannelComponent = TextureImporterSingleChannelComponent.Red;
            importer.SetTextureSettings(settings);
            importer.sRGBTexture = false;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.mipmapEnabled = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
        report.AppendLine($"TERRENO trama de detalle generada en {TerrainDetailPath} (se puede sustituir por una pintada a mano, canal rojo, tileable).");
        return AssetDatabase.LoadAssetAtPath<Texture2D>(TerrainDetailPath);
    }

    public static Texture2D BuildDetailPattern(int size, int cells)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGB24, true, true)
        {
            name = "ToonTerrainDetail",
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Trilinear,
        };
        var pixels = new Color32[size * size];
        var values = new float[size * size];
        float min = float.MaxValue, max = float.MinValue;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)size, v = y / (float)size;
                float value = PeriodicNoise(u, v, cells, 11) + 0.5f * PeriodicNoise(u, v, cells * 2, 23) + 0.25f * PeriodicNoise(u, v, cells * 4, 47);
                values[y * size + x] = value;
                min = Mathf.Min(min, value);
                max = Mathf.Max(max, value);
            }
        for (int i = 0; i < values.Length; i++)
        {
            byte value = (byte)Mathf.RoundToInt(Mathf.InverseLerp(min, max, values[i]) * 255f);
            pixels[i] = new Color32(value, value, value, 255);
        }
        texture.SetPixels32(pixels);
        texture.Apply(true);
        return texture;
    }

    private static float PeriodicNoise(float u, float v, int cells, int seed)
    {
        float x = u * cells, y = v * cells;
        int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
        float tx = x - x0, ty = y - y0;
        tx = tx * tx * (3f - 2f * tx);
        ty = ty * ty * (3f - 2f * ty);
        float a = LatticeValue(x0, y0, cells, seed), b = LatticeValue(x0 + 1, y0, cells, seed);
        float c = LatticeValue(x0, y0 + 1, cells, seed), d = LatticeValue(x0 + 1, y0 + 1, cells, seed);
        return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), ty);
    }

    private static float LatticeValue(int x, int y, int cells, int seed)
    {
        x = ((x % cells) + cells) % cells;
        y = ((y % cells) + cells) % cells;
        uint hash = (uint)(x * 374761393 + y * 668265263 + seed * 1274126177);
        hash = (hash ^ (hash >> 13)) * 1274126177u;
        hash ^= hash >> 16;
        return (hash & 0xFFFF) / 65535f;
    }

    public static Color LayerFlatColor(TerrainLayer layer, float brightness)
    {
        if (layer == null || layer.diffuseTexture == null) return Color.gray;
        Color average = AverageColor(layer.diffuseTexture).linear;
        Vector4 remap = layer.diffuseRemapMax;
        var flat = new Color(Mathf.Clamp01(average.r * remap.x * brightness), Mathf.Clamp01(average.g * remap.y * brightness), Mathf.Clamp01(average.b * remap.z * brightness), 1f);
        return flat.gamma;
    }

    private static Color AverageColor(Texture2D texture)
    {
        RenderTexture rt = RenderTexture.GetTemporary(8, 8, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        RenderTexture previous = RenderTexture.active;
        var readable = new Texture2D(8, 8, TextureFormat.RGBA32, false);
        try
        {
            Graphics.Blit(texture, rt);
            RenderTexture.active = rt;
            readable.ReadPixels(new Rect(0, 0, 8, 8), 0, 0);
            readable.Apply();
            Color sum = Color.black;
            Color[] pixels = readable.GetPixels();
            foreach (Color pixel in pixels) sum += pixel;
            sum /= pixels.Length;
            sum.a = 1f;
            return sum;
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            Object.DestroyImmediate(readable);
        }
    }

    public static Material EnsureGroundShadowMaterial(ToonEnvironmentStyle style, StringBuilder report)
    {
        Shader shader = Shader.Find(ToonEnvironmentStyle.GroundShadowShaderName);
        if (shader == null)
        {
            report.AppendLine("ERROR: no se encontró el shader " + ToonEnvironmentStyle.GroundShadowShaderName);
            return null;
        }
        Material material = style.groundShadowMaterial;
        if (material == null) material = AssetDatabase.LoadAssetAtPath<Material>(GroundShadowMaterialPath);
        if (material == null)
        {
            material = new Material(shader) { name = Path.GetFileNameWithoutExtension(GroundShadowMaterialPath) };
            EditorAssetUtility.EnsureFolder(Path.GetDirectoryName(GroundShadowMaterialPath).Replace('\\', '/'));
            AssetDatabase.CreateAsset(material, GroundShadowMaterialPath);
            report.AppendLine("SOMBRAS material creado en " + GroundShadowMaterialPath);
        }
        if (material.shader != shader) material.shader = shader;
        material.SetColor(ToonEnvironmentStyle.BaseColorId, Color.white);
        material.SetShaderPassEnabled(ToonEnvironmentStyle.GroundShadowPassLightMode, true);
        style.ApplyGroundShadowTo(material);
        EditorUtility.SetDirty(material);
        if (style.groundShadowMaterial != material)
        {
            style.groundShadowMaterial = material;
            EditorUtility.SetDirty(style);
        }
        return material;
    }

    public static void BuildGroundShadows(StringBuilder report)
    {
        ToonEnvironmentStyle style = ToonEnvironmentTools.GetOrCreateStyle();
        Material material = EnsureGroundShadowMaterial(style, report);
        if (material == null) return;
        EditorAssetUtility.EnsureFolder(GroundShadowMeshFolder);

        List<string> paths = ToonScopeTools.Prefabs(style, style.groundShadowPrefabFolders);
        if (paths.Count == 0)
        {
            report.AppendLine("AVISO sombras: las escenas del alcance no usan ningún prefab de las carpetas de sombras del estilo.");
            return;
        }
        paths.Sort((a, b) =>
        {
            int rankA = IsVariant(a) ? 1 : 0;
            int rankB = IsVariant(b) ? 1 : 0;
            return rankA != rankB ? rankA.CompareTo(rankB) : string.CompareOrdinal(a, b);
        });

        int built = 0, skipped = 0, inherited = 0;
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string path in paths)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (root == null) continue;
            try
            {
                Transform parent = ResolveShadowParent(root);
                Transform existing = FindShadowObject(root.transform);
                if (IsVariant(path) && existing != null && PrefabUtility.IsPartOfPrefabInstance(existing.gameObject))
                {
                    inherited++;
                    report.AppendLine($"SOMBRA {path}: variante, hereda la sombra de su prefab base.");
                    continue;
                }

                List<MeshFilter> filters = CollectShadowCasters(parent, style);
                float height = 0f;
                int sourceVertices = 0;
                Mesh mesh = null;
                if (filters.Count > 0) mesh = BuildShadowMesh(parent, filters, out height, out sourceVertices);
                if (mesh == null || height < style.groundShadowMinHeight)
                {
                    if (mesh != null) Object.DestroyImmediate(mesh);
                    if (existing != null)
                    {
                        Object.DestroyImmediate(existing.gameObject);
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                    }
                    skipped++;
                    report.AppendLine($"SOMBRA {path}: sin sombra plana (altura {height:F2} m, mínimo {style.groundShadowMinHeight:F2}).");
                    continue;
                }

                string meshName = UniqueName(Path.GetFileNameWithoutExtension(path), usedNames) + "_GroundShadow";
                Mesh saved = SaveShadowMesh(mesh, GroundShadowMeshFolder + "/" + meshName + ".asset");
                GameObject shadow = existing != null ? existing.gameObject : new GameObject(GroundShadowObjectName);
                shadow.name = GroundShadowObjectName;
                shadow.transform.SetParent(parent, false);
                shadow.transform.localPosition = Vector3.zero;
                shadow.transform.localRotation = Quaternion.identity;
                shadow.transform.localScale = Vector3.one;
                shadow.layer = parent.gameObject.layer;
                GameObjectUtility.SetStaticEditorFlags(shadow, 0);
                MeshFilter filter = shadow.GetComponent<MeshFilter>();
                if (filter == null) filter = shadow.AddComponent<MeshFilter>();
                filter.sharedMesh = saved;
                MeshRenderer renderer = shadow.GetComponent<MeshRenderer>();
                if (renderer == null) renderer = shadow.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                ToonEnvironmentTools.ConfigureRenderer(renderer);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                built++;
                report.AppendLine($"SOMBRA {path}: malla {meshName} ({saved.vertexCount} vértices de {sourceVertices}, altura {height:F2} m) bajo '{parent.name}'.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
        report.AppendLine($"RESUMEN sombras planas: {built} prefabs con sombra, {inherited} variantes que la heredan, {skipped} sin sombra por altura.");
    }

    private static bool IsVariant(string path)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        return asset != null && PrefabUtility.GetPrefabAssetType(asset) == PrefabAssetType.Variant;
    }

    private static string UniqueName(string name, HashSet<string> used)
    {
        string candidate = name;
        int index = 2;
        while (!used.Add(candidate)) candidate = name + "_" + index++;
        return candidate;
    }

    public static Transform FindShadowObject(Transform root)
    {
        foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            if (transform != root && transform.name == GroundShadowObjectName) return transform;
        return null;
    }

    private static Transform ResolveShadowParent(GameObject root)
    {
        var destroyed = root.GetComponent<BuildingDestroyedVisual>();
        if (destroyed != null)
        {
            var normal = new SerializedObject(destroyed).FindProperty("normalVisual").objectReferenceValue as GameObject;
            if (normal != null) return normal.transform;
        }
        var building = root.GetComponent<BuildingsScript>();
        if (building == null) return root.transform;
        var visual = new SerializedObject(building).FindProperty("visual").objectReferenceValue as GameObject;
        if (visual != null) return visual.transform;
        foreach (Transform child in root.transform)
            if (string.Equals(child.name, "visual", StringComparison.OrdinalIgnoreCase)) return child;
        foreach (Transform child in root.transform)
            if (child.name != GroundShadowObjectName && child.GetComponentInChildren<Renderer>() != null) return child;
        return root.transform;
    }

    private static List<MeshFilter> CollectShadowCasters(Transform parent, ToonEnvironmentStyle style)
    {
        var filters = new List<MeshFilter>();
        foreach (MeshFilter filter in parent.GetComponentsInChildren<MeshFilter>(false))
        {
            if (filter.sharedMesh == null || filter.name == GroundShadowObjectName) continue;
            var renderer = filter.GetComponent<MeshRenderer>();
            if (renderer == null || !renderer.enabled) continue;
            bool excluded = false;
            foreach (Material material in renderer.sharedMaterials)
                if (material != null && (style.excludedMaterials.Contains(material) || material == style.groundShadowMaterial)) excluded = true;
            if (!excluded) filters.Add(filter);
        }
        return filters;
    }

    private static Mesh BuildShadowMesh(Transform parent, List<MeshFilter> filters, out float height, out int sourceVertices)
    {
        var positions = new List<Vector3>();
        var indices = new List<int>();
        var lookup = new Dictionary<Vector3Int, int>();
        Matrix4x4 toParent = parent.worldToLocalMatrix;
        Vector3 lossy = parent.lossyScale;
        float unit = Mathf.Max(1e-5f, Mathf.Min(Mathf.Abs(lossy.x), Mathf.Min(Mathf.Abs(lossy.y), Mathf.Abs(lossy.z))));
        float quantize = 500f * unit;
        float minY = float.MaxValue, maxY = float.MinValue;
        sourceVertices = 0;

        foreach (MeshFilter filter in filters)
        {
            Mesh source = filter.sharedMesh;
            Vector3[] vertices = source.vertices;
            sourceVertices += vertices.Length;
            Matrix4x4 toWorld = filter.transform.localToWorldMatrix;
            var remap = new int[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 world = toWorld.MultiplyPoint3x4(vertices[i]);
                minY = Mathf.Min(minY, world.y);
                maxY = Mathf.Max(maxY, world.y);
                Vector3 local = toParent.MultiplyPoint3x4(world);
                var key = new Vector3Int(Mathf.RoundToInt(local.x * quantize), Mathf.RoundToInt(local.y * quantize), Mathf.RoundToInt(local.z * quantize));
                if (!lookup.TryGetValue(key, out int index))
                {
                    index = positions.Count;
                    positions.Add(local);
                    lookup.Add(key, index);
                }
                remap[i] = index;
            }
            for (int submesh = 0; submesh < source.subMeshCount; submesh++)
            {
                if (source.GetTopology(submesh) != MeshTopology.Triangles) continue;
                int[] triangles = source.GetTriangles(submesh);
                for (int t = 0; t + 2 < triangles.Length; t += 3)
                {
                    int a = remap[triangles[t]], b = remap[triangles[t + 1]], c = remap[triangles[t + 2]];
                    if (a == b || b == c || a == c) continue;
                    indices.Add(a);
                    indices.Add(b);
                    indices.Add(c);
                }
            }
        }

        height = maxY - minY;
        if (indices.Count == 0) return null;
        var mesh = new Mesh { name = GroundShadowObjectName };
        mesh.indexFormat = positions.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
        mesh.SetVertices(positions);
        mesh.SetTriangles(indices, 0, false);
        mesh.RecalculateBounds();
        Bounds bounds = mesh.bounds;
        bounds.Expand(2f * height * GroundShadowBoundsFactor / unit);
        mesh.bounds = bounds;
        return mesh;
    }

    private static Mesh SaveShadowMesh(Mesh mesh, string path)
    {
        mesh.name = Path.GetFileNameWithoutExtension(path);
        mesh.UploadMeshData(true);
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null)
        {
            EditorUtility.CopySerialized(mesh, existing);
            EditorUtility.SetDirty(existing);
            Object.DestroyImmediate(mesh);
            return existing;
        }
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    public static RenderObjects FindGroundShadowFeature(ScriptableRendererData renderer)
    {
        foreach (ScriptableRendererFeature feature in renderer.rendererFeatures)
            if (feature is RenderObjects renderObjects && renderObjects.settings.filterSettings.PassNames != null
                && Array.IndexOf(renderObjects.settings.filterSettings.PassNames, ToonEnvironmentStyle.GroundShadowPassLightMode) >= 0)
                return renderObjects;
        return null;
    }

    public static void EnsureGroundShadowRendererFeatures(StringBuilder report)
    {
        foreach (UniversalRendererData renderer in ToonEnvironmentTools.MainRendererAssets())
        {
            string path = AssetDatabase.GetAssetPath(renderer);
            RenderObjects feature = FindGroundShadowFeature(renderer);
            bool created = feature == null;
            if (created)
            {
                feature = ScriptableObject.CreateInstance<RenderObjects>();
                feature.name = GroundShadowFeatureName;
                AssetDatabase.AddObjectToAsset(feature, renderer);
            }
            bool changed = ToonEnvironmentTools.ConfigurePassFeature(feature, GroundShadowFeatureName, ToonEnvironmentStyle.GroundShadowPassLightMode);
            if (changed) EditorUtility.SetDirty(feature);

            var serialized = new SerializedObject(renderer);
            SerializedProperty features = serialized.FindProperty("m_RendererFeatures");
            SerializedProperty map = serialized.FindProperty("m_RendererFeatureMap");
            var ordered = new List<ScriptableRendererFeature>();
            for (int i = 0; i < features.arraySize; i++)
            {
                var item = features.GetArrayElementAtIndex(i).objectReferenceValue as ScriptableRendererFeature;
                if (item != null && item != feature) ordered.Add(item);
            }
            RenderObjects outline = ToonEnvironmentTools.FindOutlineFeature(renderer);
            int target = outline != null && ordered.Contains(outline) ? ordered.IndexOf(outline) : ordered.Count;
            ordered.Insert(target, feature);

            bool reordered = features.arraySize != ordered.Count;
            for (int i = 0; !reordered && i < ordered.Count; i++)
                if (features.GetArrayElementAtIndex(i).objectReferenceValue != ordered[i]) reordered = true;
            if (reordered)
            {
                features.arraySize = ordered.Count;
                map.arraySize = ordered.Count;
                for (int i = 0; i < ordered.Count; i++)
                {
                    features.GetArrayElementAtIndex(i).objectReferenceValue = ordered[i];
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(ordered[i], out _, out long localId);
                    map.GetArrayElementAtIndex(i).longValue = localId;
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(renderer);
            }
            report.AppendLine($"SOMBRAS {path}: feature '{GroundShadowFeatureName}' {(created ? "añadido" : changed || reordered ? "ajustado" : "ya presente")} antes del contorno.");
        }
    }

    public static IEnumerable<string> CharacterPrefabPaths(ToonEnvironmentStyle style) => ToonScopeTools.Prefabs(style, style.characterPrefabFolders);

    public static void ConvertCharacters(StringBuilder report)
    {
        ToonEnvironmentStyle style = ToonEnvironmentTools.GetOrCreateStyle();
        ToonScopeTools.Refresh();
        EnsureWorldDefaults(style, report);
        Shader shader = Shader.Find(ToonEnvironmentStyle.CharacterShaderName);
        if (shader == null)
        {
            report.AppendLine("ERROR: no se encontró el shader " + ToonEnvironmentStyle.CharacterShaderName);
            return;
        }

        ToonScopeTools.ExtractScopedEmbeddedMaterials(style, report);
        List<string> paths = CharacterPrefabPaths(style).ToList();
        report.AppendLine("PERSONAJES en el alcance: " + string.Join(", ", paths.Select(Path.GetFileNameWithoutExtension)));

        var uses = new Dictionary<Material, string>();
        var rim = new HashSet<Material>();
        var casters = new HashSet<Material>();
        foreach (string path in paths)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;
            bool wantsRim = style.rimLightCharacterPrefabs.Contains(prefab);
            bool castsShadow = style.groundShadowCharacterPrefabs.Contains(prefab);
            foreach (Renderer renderer in prefab.GetComponentsInChildren<Renderer>(true))
            {
                if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)) continue;
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material material = materials[i];
                    if (material == null)
                    {
                        report.AppendLine($"FALTANTE material nulo en {path} -> {renderer.name} (slot {i}).");
                        continue;
                    }
                    if (!uses.ContainsKey(material)) uses.Add(material, path);
                    if (wantsRim) rim.Add(material);
                    if (castsShadow) casters.Add(material);
                }
            }
        }

        var ordered = uses.Keys.ToList();
        for (int i = 0; i < ordered.Count; i++)
        {
            Material parent = ordered[i].parent;
            int guard = 0;
            while (parent != null && guard++ < 16)
            {
                if (!uses.ContainsKey(parent))
                {
                    uses.Add(parent, uses[ordered[i]] + " (padre de variante)");
                    ordered.Add(parent);
                }
                if (rim.Contains(ordered[i])) rim.Add(parent);
                if (casters.Contains(ordered[i])) casters.Add(parent);
                parent = parent.parent;
            }
        }
        ordered.Sort((a, b) =>
        {
            int depth = ToonEnvironmentTools.ParentDepth(a).CompareTo(ToonEnvironmentTools.ParentDepth(b));
            return depth != 0 ? depth : string.CompareOrdinal(AssetDatabase.GetAssetPath(a), AssetDatabase.GetAssetPath(b));
        });

        int converted = 0;
        foreach (Material material in ordered)
        {
            string path = AssetDatabase.GetAssetPath(material);
            if (style.excludedMaterials.Contains(material))
            {
                report.AppendLine($"EXCLUIDO {path}");
                continue;
            }
            if (string.IsNullOrEmpty(path) || !path.EndsWith(".mat", StringComparison.OrdinalIgnoreCase) || AssetDatabase.IsSubAsset(material))
            {
                report.AppendLine($"EMBEBIDO sin extraer {material.name} en {uses[material]}");
                continue;
            }
            if (material.shader == null || !ConvertibleCharacterShaders.Contains(material.shader.name))
            {
                report.AppendLine($"OMITIDO (shader {material.shader?.name}) {path}");
                continue;
            }
            string outcome = ConvertCharacterMaterial(material, shader, style, rim.Contains(material), casters.Contains(material));
            report.AppendLine($"PERSONAJE {path}: {outcome}");
            if (!style.characterMaterials.Contains(material)) style.characterMaterials.Add(material);
            style.environmentMaterials.Remove(material);
            if (casters.Contains(material))
            {
                if (!style.groundShadowCasterMaterials.Contains(material)) style.groundShadowCasterMaterials.Add(material);
            }
            else style.groundShadowCasterMaterials.Remove(material);
            converted++;
        }
        style.characterMaterials.RemoveAll(material => material == null);
        style.groundShadowCasterMaterials.RemoveAll(material => material == null);
        EditorUtility.SetDirty(style);

        int renderersChanged = 0, prefabsChanged = 0;
        foreach (string path in paths)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (root == null) continue;
            try
            {
                bool changed = false;
                foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)) continue;
                    if (!UsesManagedCharacterMaterial(renderer, style)) continue;
                    if (ToonEnvironmentTools.ConfigureRenderer(renderer))
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
        report.AppendLine($"RESUMEN personajes: {converted} materiales convertidos, {style.groundShadowCasterMaterials.Count} con sombra plana, {renderersChanged} renderers ajustados en {prefabsChanged} prefabs.");
    }

    private static bool UsesManagedCharacterMaterial(Renderer renderer, ToonEnvironmentStyle style)
    {
        foreach (Material material in renderer.sharedMaterials)
            if (material != null && style.characterMaterials.Contains(material)) return true;
        return false;
    }

    private static string ConvertCharacterMaterial(Material material, Shader shader, ToonEnvironmentStyle style, bool rim, bool castsShadow)
    {
        string previous = material.shader != null ? material.shader.name : "sin shader";
        Texture baseMap = ToonEnvironmentTools.FirstTexture(material, "_BaseMap", "_MainTex");
        Color baseColor = ToonEnvironmentTools.FirstColor(material, Color.white, "_BaseColor", "_Color");
        baseColor.a = 1f;
        bool hadEmission = material.IsKeywordEnabled("_EMISSION") && material.HasProperty("_EmissionColor");
        Texture emissionMap = hadEmission ? ToonEnvironmentTools.FirstTexture(material, "_EmissionMap") : null;
        Color emissionColor = hadEmission ? material.GetColor("_EmissionColor") : Color.black;
        hadEmission &= emissionColor.maxColorComponent > 0.001f;
        bool alphaClip = material.IsKeywordEnabled("_ALPHATEST_ON");

        if (material.parent != null && AssetDatabase.IsSubAsset(material.parent)) material.parent = null;
        bool alreadyConverted = material.shader == shader;
        if (!alreadyConverted)
        {
            material.shader = shader;
            material.SetTexture(ToonEnvironmentStyle.BaseMapId, baseMap);
            material.SetColor(ToonEnvironmentStyle.BaseColorId, baseColor);
        }
        ToonEnvironmentTools.SetOpaque(material);
        material.SetFloat(ToonEnvironmentStyle.StencilRefId, 0f);
        if (material.parent == null) style.ApplyCharacterShadingTo(material);

        var keywords = new List<string> { "_RECEIVE_SHADOWS_OFF" };
        material.SetFloat("_ReceiveShadowsOff", 1f);
        material.SetFloat("_RimEnabled", rim ? 1f : 0f);
        if (rim) keywords.Add(ToonEnvironmentStyle.RimKeyword);
        if (hadEmission)
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
            material.SetColor(ToonEnvironmentStyle.EmissionColorId, Color.black);
            material.SetFloat("_EmissionEnabled", 0f);
        }
        material.shaderKeywords = keywords.ToArray();
        material.SetShaderPassEnabled(ToonEnvironmentStyle.OutlinePassLightMode, style.characterOutlines);
        material.SetShaderPassEnabled(ToonEnvironmentStyle.GroundShadowPassLightMode, castsShadow);
        ToonEnvironmentTools.RemoveUnusedTextureReferences(material);
        EditorUtility.SetDirty(material);

        var notes = new List<string> { alreadyConverted ? "ya estaba convertido" : "desde " + previous };
        if (rim) notes.Add("luz de borde");
        if (castsShadow) notes.Add("sombra plana");
        if (hadEmission) notes.Add("emisión conservada");
        if (alphaClip) notes.Add("AVISO: usaba recorte por alfa, el shader toon lo dibuja sólido");
        if (baseMap == null) notes.Add("sin textura, solo color");
        return string.Join(", ", notes);
    }

    public static void ApplyStyle(StringBuilder report)
    {
        ToonEnvironmentStyle style = ToonEnvironmentTools.GetOrCreateStyle();
        int characters = 0;
        foreach (Material material in style.characterMaterials)
        {
            if (material == null) continue;
            material.SetShaderPassEnabled(ToonEnvironmentStyle.OutlinePassLightMode, style.characterOutlines);
            EditorUtility.SetDirty(material);
            if (material.parent != null) continue;
            style.ApplyCharacterShadingTo(material);
            characters++;
        }
        foreach (Material material in style.groundReceiverMaterials)
        {
            if (material == null || material.parent != null) continue;
            style.ApplyShadingTo(material);
            EditorUtility.SetDirty(material);
        }
        if (style.terrainMaterial != null)
        {
            style.ApplyTerrainShadingTo(style.terrainMaterial);
            EditorUtility.SetDirty(style.terrainMaterial);
        }
        if (style.groundShadowMaterial != null)
        {
            style.ApplyGroundShadowTo(style.groundShadowMaterial);
            EditorUtility.SetDirty(style.groundShadowMaterial);
        }
        report.AppendLine($"RESUMEN estilo del mundo: {characters} materiales de personajes, {style.groundReceiverMaterials.Count} pisos, terreno {(style.terrainMaterial != null ? "sí" : "no")}, sombra plana {(style.groundShadowMaterial != null ? "sí" : "no")}.");
    }

    public static void Validate(ToonEnvironmentStyle style, List<string> problems, List<string> notes)
    {
        ValidateShader(ToonEnvironmentStyle.CharacterShaderName, true, problems);
        ValidateShader(ToonEnvironmentStyle.GroundShadowShaderName, true, problems);
        ValidateShader(ToonEnvironmentStyle.TerrainShaderName, false, problems);
        if (style == null) return;

        Shader characterShader = Shader.Find(ToonEnvironmentStyle.CharacterShaderName);
        foreach (Material material in style.characterMaterials)
        {
            if (material == null) { problems.Add("Referencia nula en la lista de materiales de personajes."); continue; }
            string path = AssetDatabase.GetAssetPath(material);
            if (material.shader != characterShader) problems.Add($"{path} no usa el shader toon de personajes ({material.shader?.name}).");
            if (material.HasProperty("_Surface") && material.GetFloat("_Surface") != 0f) problems.Add($"{path} está guardado como transparente (_Surface=1).");
            bool casts = style.groundShadowCasterMaterials.Contains(material);
            if (material.GetShaderPassEnabled(ToonEnvironmentStyle.GroundShadowPassLightMode) != casts)
                problems.Add($"{path}: la pasada de sombra plana debería estar {(casts ? "activa" : "apagada")}; ejecuta 'Convertir personajes'.");
            if (material.GetShaderPassEnabled(ToonEnvironmentStyle.OutlinePassLightMode) != style.characterOutlines)
                problems.Add($"{path}: la pasada de contorno no coincide con 'Character Outlines' del estilo; ejecuta 'Aplicar estilo'.");
        }
        if (style.characterMaterials.Count == 0) problems.Add("El estilo no gestiona ningún material de personaje: ejecuta 'Convertir personajes'.");

        foreach (string path in CharacterPrefabPaths(style))
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;
            foreach (Renderer renderer in prefab.GetComponentsInChildren<Renderer>(true))
            {
                if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)) continue;
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material == null) problems.Add($"{path}: material vacío en '{renderer.name}' (se verá magenta).");
                    else if (material.shader != characterShader && ConvertibleCharacterShaders.Contains(material.shader.name))
                        problems.Add($"{path}: '{material.name}' sigue en {material.shader.name}.");
                }
                if (UsesManagedCharacterMaterial(renderer, style) && renderer.shadowCastingMode != ShadowCastingMode.Off)
                    problems.Add($"{path}: '{renderer.name}' sigue proyectando sombra en tiempo real además de la sombra plana.");
            }
        }

        Shader terrainShader = Shader.Find(ToonEnvironmentStyle.TerrainShaderName);
        if (style.terrainMaterial == null) problems.Add("Falta el material del terreno toon: ejecuta 'Configurar terreno toon'.");
        else if (style.terrainMaterial.shader != terrainShader) problems.Add("El material del terreno no usa el shader toon de terreno.");
        foreach (string path in AssetDatabase.FindAssets("t:Prefab", TerrainPrefabFolders).Select(AssetDatabase.GUIDToAssetPath).Distinct())
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;
            foreach (Terrain terrain in prefab.GetComponentsInChildren<Terrain>(true))
            {
                if (terrain.materialTemplate != style.terrainMaterial) problems.Add($"{path}: el terreno '{terrain.name}' no usa el material toon.");
                if (terrain.shadowCastingMode != ShadowCastingMode.Off) problems.Add($"{path}: el terreno '{terrain.name}' sigue proyectando sombras.");
                TerrainData data = terrain.terrainData;
                if (data == null || style.terrainMaterial == null) continue;
                Texture2D[] alphamaps = data.alphamapTextures;
                Texture expected = alphamaps.Length > 1 ? alphamaps[1] : null;
                if (style.terrainMaterial.GetTexture(ToonEnvironmentStyle.TerrainControl1Id) != expected)
                    problems.Add($"{path}: el material del terreno no apunta al mapa de las capas 4 a 7; ejecuta 'Configurar terreno toon'.");
                if (data.alphamapLayers > ToonEnvironmentStyle.MaxTerrainLayers)
                    problems.Add($"{path}: el terreno tiene {data.alphamapLayers} capas y solo se dibujan {ToonEnvironmentStyle.MaxTerrainLayers}.");
                if (!terrain.drawInstanced) notes.Add($"{path}: el terreno no usa Draw Instanced; el sombreado toon usa normales por vértice.");
            }
        }

        Shader shadowShader = Shader.Find(ToonEnvironmentStyle.GroundShadowShaderName);
        if (style.groundShadowMaterial == null) problems.Add("Falta el material de las sombras planas: ejecuta 'Generar sombras planas'.");
        else
        {
            if (style.groundShadowMaterial.shader != shadowShader) problems.Add("El material de sombras planas no usa su shader.");
            if (!style.groundShadowMaterial.GetShaderPassEnabled(ToonEnvironmentStyle.GroundShadowPassLightMode))
                problems.Add("El material de sombras planas quedó con la pasada apagada en el asset.");
        }
        Shader environmentShader = Shader.Find(ToonEnvironmentStyle.ShaderName);
        foreach (Material material in style.groundReceiverMaterials)
        {
            if (material == null) { problems.Add("Referencia nula en la lista de pisos que reciben sombra."); continue; }
            string path = AssetDatabase.GetAssetPath(material);
            if (material.shader != environmentShader) problems.Add($"{path} (piso) no usa el shader toon.");
            else if (material.GetFloat(ToonEnvironmentStyle.StencilRefId) != 8f) problems.Add($"{path} (piso) no está marcado para recibir sombras planas.");
        }

        int shadowed = 0;
        foreach (string path in ToonScopeTools.Prefabs(style, style.groundShadowPrefabFolders))
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;
            Transform shadow = FindShadowObject(prefab.transform);
            if (shadow == null) continue;
            shadowed++;
            var filter = shadow.GetComponent<MeshFilter>();
            var renderer = shadow.GetComponent<MeshRenderer>();
            if (filter == null || filter.sharedMesh == null) problems.Add($"{path}: la sombra plana no tiene malla.");
            if (renderer == null || renderer.sharedMaterial != style.groundShadowMaterial) problems.Add($"{path}: la sombra plana no usa el material compartido.");
        }
        if (shadowed == 0) problems.Add("Ningún prefab de edificio tiene sombra plana: ejecuta 'Generar sombras planas'.");
        else notes.Add($"{shadowed} prefabs de edificios y props con sombra plana.");

        foreach (UniversalRendererData renderer in ToonEnvironmentTools.MainRendererAssets())
        {
            string path = AssetDatabase.GetAssetPath(renderer);
            RenderObjects shadow = FindGroundShadowFeature(renderer);
            RenderObjects outline = ToonEnvironmentTools.FindOutlineFeature(renderer);
            if (shadow == null) problems.Add($"{path}: falta el RenderObjects '{GroundShadowFeatureName}'.");
            else
            {
                if (!shadow.isActive || shadow.settings.Event != RenderPassEvent.AfterRenderingOpaques) problems.Add($"{path}: el feature '{shadow.name}' debe estar activo y en AfterRenderingOpaques.");
                if (outline != null && renderer.rendererFeatures.IndexOf(shadow) > renderer.rendererFeatures.IndexOf(outline))
                    problems.Add($"{path}: las sombras planas deben dibujarse antes que el contorno.");
            }
        }
    }

    private static void ValidateShader(string name, bool requireBatcher, List<string> problems)
    {
        Shader shader = Shader.Find(name);
        if (shader == null) problems.Add("Shader " + name + " no encontrado.");
        else if (ShaderUtil.ShaderHasError(shader)) problems.Add("El shader " + name + " tiene errores de compilación.");
        else if (requireBatcher && ToonEnvironmentTools.IsSrpBatcherCompatible(shader, out string reason) == false) problems.Add("El shader " + name + " no es compatible con el SRP Batcher: " + reason);
    }

    public static bool IsInFolders(string path, string[] folders)
    {
        if (folders == null) return false;
        foreach (string folder in folders)
            if (!string.IsNullOrEmpty(folder) && path.StartsWith(folder.TrimEnd('/') + "/", StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }
}
