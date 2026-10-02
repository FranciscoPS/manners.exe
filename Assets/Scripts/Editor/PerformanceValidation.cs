using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>Read-only asset inventory and explicit release-build validation. Never saves a scene.</summary>
public static class PerformanceValidation
{
    private const string ReportPath = "Logs/performance-validation.json";
    private const string BuildPath = "/tmp/manners-performance/MannersPerformance.app";

    [Serializable] private sealed class Check { public string name; public bool passed; public string detail; }
    [Serializable] private sealed class Count { public string name; public long count; }
    [Serializable] private sealed class TextureRecord
    {
        public string path, name, type, graphicsFormat;
        public int width, height, mipmaps, maxImportSize;
        public bool readable, streaming, importedMipmaps;
        public long editorResidentBytes;
    }
    [Serializable] private sealed class CameraRecord
    {
        public string name, rendererName;
        public bool active, enabled, hdr, postProcessing, shadows, occlusion;
        public int targetWidth, targetHeight, cullingMask, rendererIndex;
        public float farClip;
    }
    [Serializable] private sealed class LightRecord
    {
        public string name, type, bakeType, shadows;
        public bool active, enabled;
        public float intensity;
    }
    [Serializable] private sealed class TerrainRecord
    {
        public string name;
        public bool active, instanced;
        public int heightmapResolution, layers, alphamaps, alphamapResolution, trees, detailPrototypes;
        public Vector3 size;
        public float pixelError, detailDistance, treeDistance;
    }
    [Serializable] private sealed class SceneRecord
    {
        public string path, serializedLightingDataReference, serializedOcclusionDataReference;
        public int gameObjects, activeGameObjects, renderers, activeEnabledRenderers, shadowCasters;
        public int uniqueMaterials, materialSlots, staticRenderers, occluderRenderers, lightmappedRenderers;
        public int missingScripts, lodGroups, lodLevels, colliders, rigidbodies, animators;
        public long sourceMeshTrianglesAcrossActiveRenderers;
        public List<Count> rendererTypes = new List<Count>();
        public List<Count> shaderUsage = new List<Count>();
        public List<Count> materialUsage = new List<Count>();
        public List<CameraRecord> cameras = new List<CameraRecord>();
        public List<LightRecord> lights = new List<LightRecord>();
        public List<TerrainRecord> terrains = new List<TerrainRecord>();
        public List<string> dependencies = new List<string>();
    }
    [Serializable] private sealed class Report
    {
        public string utc, unityVersion, activeBuildTarget;
        public string limitations = "Preview-scene inventory resolves prefab variants and binary dependencies, but does not run gameplay. Counts are not visible draw calls. Texture bytes are Editor imported-asset resident memory, not target build VRAM. Mesh totals exclude terrain tessellation and do not include render passes.";
        public bool passed;
        public long uniqueTextureEditorResidentBytes;
        public List<Check> checks = new List<Check>();
        public List<SceneRecord> scenes = new List<SceneRecord>();
        public List<TextureRecord> textures = new List<TextureRecord>();
        public List<string> errors = new List<string>();
    }
    [Serializable] private sealed class BuildRecord
    {
        public string utc, result, outputPath, platform, duration;
        public int errors, warnings;
        public ulong bytes;
        public List<string> messages = new List<string>();
    }

    [MenuItem("Tools/Manners/Performance/Validate Graphics and Inventory Build Scenes", false, 60)]
    public static void RunChecks()
    {
        var report = new Report
        {
            utc = DateTime.UtcNow.ToString("o"), unityVersion = Application.unityVersion,
            activeBuildTarget = EditorUserBuildSettings.activeBuildTarget.ToString()
        };
        try
        {
            CheckSettings(report);
            CheckRenderAssets(report);
            try
            {
                foreach (string check in PerformanceRegressionChecks.Run())
                    Require(report, check, true, "Native Unity runtime regression check in an isolated preview scene.");
            }
            catch (Exception exception)
            {
                Require(report, "Native runtime regression checks", false, exception.ToString());
            }
            var dependencyPaths = new HashSet<string>();
            foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
            {
                if (!buildScene.enabled) continue;
                try
                {
                    SceneRecord scene = InspectScene(buildScene.path);
                    report.scenes.Add(scene);
                    foreach (string path in scene.dependencies) dependencyPaths.Add(path);
                }
                catch (Exception exception) { report.errors.Add(buildScene.path + ": " + exception); }
            }
            // Resources may be loaded by path at runtime and are therefore part of the build contract.
            foreach (string path in AssetDatabase.GetAllAssetPaths())
            {
                if (!path.StartsWith("Assets/", StringComparison.Ordinal) || !path.Contains("/Resources/") || AssetDatabase.IsValidFolder(path)) continue;
                foreach (string dependency in AssetDatabase.GetDependencies(path, true)) dependencyPaths.Add(dependency);
            }
            InspectTextures(dependencyPaths, report);
        }
        catch (Exception exception) { report.errors.Add(exception.ToString()); }
        report.passed = report.errors.Count == 0 && report.checks.TrueForAll(check => check.passed);
        WriteJson(ReportPath, report);
        if (report.passed) Debug.Log("[PerformanceValidation] Checks passed. Read-only inventory: " + ReportPath);
        else Debug.LogError("[PerformanceValidation] Checks failed; see " + ReportPath);
    }

    private static void Require(Report report, string name, bool passed, string detail)
    {
        report.checks.Add(new Check { name = name, passed = passed, detail = detail });
    }

    private static void CheckSettings(Report report)
    {
        var balanced = GameGraphicsSettings.CreatePreset(GameGraphicsSettings.GraphicsPreset.Balanced);
        var eco = GameGraphicsSettings.CreatePreset(GameGraphicsSettings.GraphicsPreset.Eco);
        var high = GameGraphicsSettings.CreatePreset(GameGraphicsSettings.GraphicsPreset.High);
        Require(report, "Balanced limits work by default", balanced.frameRate == 60 && !balanced.vSync && !balanced.ambientOcclusion,
            "60 FPS, software cap, no ambient occlusion by default.");
        Require(report, "Eco reduces rendering work", eco.frameRate <= balanced.frameRate && eco.renderScale < balanced.renderScale && !eco.shadows && !eco.postProcessing,
            "Eco lowers frame rate and internal scale and disables shadows/postprocessing.");
        Require(report, "High offers ambient occlusion", high.ambientOcclusion, "High keeps an explicit visual-quality alternative.");
        Require(report, "Eco removes the toon outline pass", !eco.outlines && balanced.outlines && high.outlines,
            "Eco skips the environment outline draw calls; Balanced and High keep the Denshattack-style lines.");
        var invalid = new GameGraphicsSettings.SettingsData
        {
            preset = (GameGraphicsSettings.GraphicsPreset)999, frameRate = -1,
            renderScale = float.NaN, textureMipmapLimit = 100, resolutionHeight = -5
        };
        var safe = GameGraphicsSettings.Sanitize(invalid);
        Require(report, "Corrupt saved settings recover", safe.preset == GameGraphicsSettings.GraphicsPreset.Balanced && safe.frameRate == 60 && safe.renderScale == 1f && safe.textureMipmapLimit == 2 && safe.resolutionHeight == 1080,
            "Unknown enums, unsupported frame rates, NaN, invalid mip limits and resolutions recover.");
        Require(report, "Sanitize does not mutate its caller", float.IsNaN(invalid.renderScale) && invalid.textureMipmapLimit == 100,
            "Draft settings stay separate from sanitized state.");
        invalid.renderScale = float.PositiveInfinity;
        Require(report, "Infinite scale recovers", GameGraphicsSettings.Sanitize(invalid).renderScale == 1f, "Positive infinity falls back to a finite scale.");
        invalid.renderScale = -10f;
        Require(report, "Scale has a lower bound", GameGraphicsSettings.Sanitize(invalid).renderScale == 0.5f, "Internal render scale cannot become zero or negative.");
        invalid.renderScale = 4f;
        Require(report, "Scale cannot accidentally supersample", GameGraphicsSettings.Sanitize(invalid).renderScale == 1f, "Saved data cannot request excessive supersampling.");
        Vector2Int wide = GameGraphicsSettings.CalculateResolution(2880, 1800, 1080);
        Vector2Int small = GameGraphicsSettings.CalculateResolution(1366, 768, 1080);
        Vector2Int native = GameGraphicsSettings.CalculateResolution(3840, 2160, 0);
        Vector2Int fallback = GameGraphicsSettings.CalculateResolution(0, 0, 1080);
        Require(report, "Full HD output on a Retina display", wide == new Vector2Int(1920, 1080), "FullScreenWindow scales 1920x1080 with letterboxing on 16:10.");
        Require(report, "Resolution never upscales a small display", small == new Vector2Int(1365, 768), "16:9 output fits inside 1366x768.");
        Require(report, "Native display choice remains native", native == new Vector2Int(3840, 2160), "Native request preserves actual display dimensions.");
        Require(report, "Missing display dimensions remain valid", fallback.x > 0 && fallback.y > 0, "No zero-sized render target.");
    }

    private static void CheckRenderAssets(Report report)
    {
        foreach (string path in new[] { "Assets/Settings/PC_RPAsset.asset", "Assets/Settings/Mobile_RPAsset.asset" })
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
            Require(report, path + " exists", pipeline != null, "URP asset resolves through AssetDatabase.");
            if (pipeline == null) continue;
            Require(report, path + " preserves water depth", pipeline.supportsCameraDepthTexture, "Water/DepthFade uses SceneDepth.");
            Require(report, path + " omits unused opaque copy", !pipeline.supportsCameraOpaqueTexture, "No current custom shader samples SceneColor.");
            var serialized = new SerializedObject(pipeline);
            var renderers = serialized.FindProperty("m_RendererDataList");
            var names = new List<string>();
            for (int i = 0; renderers != null && i < renderers.arraySize; i++)
            {
                var renderer = renderers.GetArrayElementAtIndex(i).objectReferenceValue as ScriptableRendererData;
                if (renderer != null) names.Add(renderer.name);
            }
            Require(report, path + " has minimap renderer", names.Contains("Minimap_Renderer"), string.Join(", ", names));
            if (path.Contains("PC_RPAsset")) Require(report, "PC has no-AO alternative", names.Contains("PC_NoAO_Renderer"), string.Join(", ", names));
        }
        foreach (string path in new[] { "Assets/Settings/PC_NoAO_Renderer.asset", "Assets/Settings/Minimap_Renderer.asset" })
        {
            var renderer = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(path);
            bool onlyOutline = renderer != null && renderer.rendererFeatures.TrueForAll(feature => feature is RenderObjects);
            bool minimapBare = !path.Contains("Minimap") || (renderer != null && renderer.rendererFeatures.Count == 0);
            Require(report, path + " has no costly renderer features", onlyOutline && minimapBare,
                "Ambient occlusion cannot leak into the low-cost renderer; only the toon outline RenderObjects pass is allowed, and the minimap draws none.");
        }
        const string depthSubgraph = "Assets/Shaders/Water/DepthFade.shadersubgraph";
        Require(report, "Water depth dependency documented", File.Exists(depthSubgraph) && File.ReadAllText(depthSubgraph).Contains("SceneDepthNode"), depthSubgraph);
    }

    private static SceneRecord InspectScene(string path)
    {
        var record = new SceneRecord { path = path };
        record.dependencies.AddRange(AssetDatabase.GetDependencies(path, true));
        string yaml = File.ReadAllText(path);
        record.serializedLightingDataReference = Regex.Match(yaml, @"(?m)^  m_LightingDataAsset: (.+)$").Groups[1].Value;
        record.serializedOcclusionDataReference = Regex.Match(yaml, @"(?m)^  m_OcclusionCullingData: (.+)$").Groups[1].Value;
        Scene preview = EditorSceneManager.OpenPreviewScene(path);
        try
        {
            var objects = preview.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).Select(transform => transform.gameObject).ToArray();
            record.gameObjects = objects.Length;
            record.activeGameObjects = objects.Count(go => go.activeInHierarchy);
            var materials = new HashSet<Material>();
            var rendererTypes = new Dictionary<string, long>();
            var shaders = new Dictionary<string, long>();
            var materialUses = new Dictionary<string, long>();
            foreach (GameObject go in objects)
            {
                record.missingScripts += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
                record.colliders += go.GetComponents<Collider>().Length;
                record.rigidbodies += go.GetComponents<Rigidbody>().Length;
                record.animators += go.GetComponents<Animator>().Length;
                foreach (LODGroup group in go.GetComponents<LODGroup>()) { record.lodGroups++; record.lodLevels += group.lodCount; }
                foreach (Renderer renderer in go.GetComponents<Renderer>())
                {
                    record.renderers++;
                    Increment(rendererTypes, renderer.GetType().Name);
                    bool active = go.activeInHierarchy && renderer.enabled;
                    if (active) record.activeEnabledRenderers++;
                    if (active && renderer.shadowCastingMode != ShadowCastingMode.Off) record.shadowCasters++;
                    StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(go);
                    if ((flags & StaticEditorFlags.BatchingStatic) != 0) record.staticRenderers++;
                    if ((flags & StaticEditorFlags.OccluderStatic) != 0) record.occluderRenderers++;
                    if (renderer.lightmapIndex >= 0 && renderer.lightmapIndex < 65534) record.lightmappedRenderers++;
                    foreach (Material material in renderer.sharedMaterials)
                    {
                        if (material == null) continue;
                        materials.Add(material);
                        if (!active) continue;
                        record.materialSlots++;
                        Increment(materialUses, AssetDatabase.GetAssetPath(material) + " :: " + material.name);
                        Increment(shaders, material.shader != null ? material.shader.name : "<missing shader>");
                    }
                    if (!active) continue;
                    MeshFilter filter = go.GetComponent<MeshFilter>();
                    Mesh mesh = renderer is SkinnedMeshRenderer skinned ? skinned.sharedMesh : filter != null ? filter.sharedMesh : null;
                    if (mesh == null) continue;
                    for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
                        if (mesh.GetTopology(submesh) == MeshTopology.Triangles) record.sourceMeshTrianglesAcrossActiveRenderers += mesh.GetIndexCount(submesh) / 3;
                }
                foreach (Light light in go.GetComponents<Light>()) record.lights.Add(new LightRecord
                {
                    name = go.name, active = go.activeInHierarchy, enabled = light.enabled, type = light.type.ToString(),
                    bakeType = light.lightmapBakeType.ToString(), shadows = light.shadows.ToString(), intensity = light.intensity
                });
                foreach (Camera camera in go.GetComponents<Camera>())
                {
                    var data = camera.GetComponent<UniversalAdditionalCameraData>();
                    int rendererIndex = data != null ? new SerializedObject(data).FindProperty("m_RendererIndex").intValue : -1;
                    record.cameras.Add(new CameraRecord
                    {
                        name = go.name, active = go.activeInHierarchy, enabled = camera.enabled, hdr = camera.allowHDR,
                        postProcessing = data != null && data.renderPostProcessing, shadows = data == null || data.renderShadows,
                        occlusion = camera.useOcclusionCulling, farClip = camera.farClipPlane, cullingMask = camera.cullingMask,
                        rendererIndex = rendererIndex, rendererName = "Serialized renderer index; runtime settings may override it",
                        targetWidth = camera.targetTexture != null ? camera.targetTexture.width : 0,
                        targetHeight = camera.targetTexture != null ? camera.targetTexture.height : 0
                    });
                }
                foreach (Terrain terrain in go.GetComponents<Terrain>())
                {
                    TerrainData data = terrain.terrainData;
                    if (data == null) continue;
                    record.terrains.Add(new TerrainRecord
                    {
                        name = go.name, active = go.activeInHierarchy && terrain.enabled, instanced = terrain.drawInstanced,
                        heightmapResolution = data.heightmapResolution, layers = data.terrainLayers.Length,
                        alphamaps = data.alphamapTextureCount, alphamapResolution = data.alphamapResolution,
                        trees = data.treeInstanceCount, detailPrototypes = data.detailPrototypes.Length,
                        size = data.size, pixelError = terrain.heightmapPixelError,
                        detailDistance = terrain.detailObjectDistance, treeDistance = terrain.treeDistance
                    });
                }
            }
            record.uniqueMaterials = materials.Count;
            record.rendererTypes = Counts(rendererTypes);
            record.shaderUsage = Counts(shaders);
            record.materialUsage = Counts(materialUses);
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }
        return record;
    }

    private static void InspectTextures(HashSet<string> paths, Report report)
    {
        var seen = new HashSet<int>();
        foreach (string path in paths.OrderBy(path => path))
        {
            if (!path.StartsWith("Assets/", StringComparison.Ordinal) && !path.StartsWith("Packages/", StringComparison.Ordinal)) continue;
            if (AssetDatabase.IsValidFolder(path) || path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase)) continue;
            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (!(asset is Texture texture) || !seen.Add(texture.GetInstanceID())) continue;
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                var texture2D = texture as Texture2D;
                long bytes = Profiler.GetRuntimeMemorySizeLong(texture);
                report.uniqueTextureEditorResidentBytes += bytes;
                report.textures.Add(new TextureRecord
                {
                    path = path, name = texture.name, type = texture.GetType().Name,
                    graphicsFormat = texture.graphicsFormat.ToString(), width = texture.width, height = texture.height,
                    mipmaps = texture2D != null ? texture2D.mipmapCount : 0, editorResidentBytes = bytes,
                    maxImportSize = importer != null ? importer.maxTextureSize : 0,
                    readable = importer != null && importer.isReadable, streaming = importer != null && importer.streamingMipmaps,
                    importedMipmaps = importer != null && importer.mipmapEnabled
                });
            }
        }
    }

    private static void Increment(Dictionary<string, long> counts, string name)
    {
        counts.TryGetValue(name, out long count);
        counts[name] = count + 1;
    }

    private static List<Count> Counts(Dictionary<string, long> counts) => counts.OrderByDescending(pair => pair.Value)
        .Select(pair => new Count { name = pair.Key, count = pair.Value }).ToList();

    private static void WriteJson(string path, object report)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, JsonUtility.ToJson(report, true));
    }

    [MenuItem("Tools/Manners/Performance/Build macOS Release for Performance Test", false, 61)]
    public static void BuildMacPlayer()
    {
        var result = new BuildRecord { utc = DateTime.UtcNow.ToString("o"), outputPath = BuildPath, platform = "StandaloneOSX" };
        bool originalFrameTiming = PlayerSettings.enableFrameTimingStats;
        string originalProductName = PlayerSettings.productName;
        string originalIdentifier = PlayerSettings.GetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone);
        try
        {
            PlayerSettings.enableFrameTimingStats = true;
            PlayerSettings.productName = "MannersPerformanceValidation";
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone, "com.manners.performance.validation");
            Directory.CreateDirectory(Path.GetDirectoryName(BuildPath));
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
                locationPathName = BuildPath, target = BuildTarget.StandaloneOSX, options = BuildOptions.None
            });
            result.result = report.summary.result.ToString();
            result.duration = report.summary.totalTime.ToString();
            result.errors = report.summary.totalErrors;
            result.warnings = report.summary.totalWarnings;
            result.bytes = report.summary.totalSize;
            foreach (BuildStep step in report.steps)
                foreach (BuildStepMessage message in step.messages)
                    if (message.type == LogType.Error || message.type == LogType.Exception || message.type == LogType.Warning)
                        result.messages.Add(message.type + ": " + message.content);
        }
        catch (Exception exception) { result.result = "Exception"; result.errors++; result.messages.Add(exception.ToString()); }
        finally
        {
            PlayerSettings.enableFrameTimingStats = originalFrameTiming;
            PlayerSettings.productName = originalProductName;
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone, originalIdentifier);
            AssetDatabase.SaveAssets();
            WriteJson("Logs/performance-build-mac.json", result);
        }
        Debug.Log("[PerformanceValidation] macOS release build: " + result.result + " at " + BuildPath);
    }
}
