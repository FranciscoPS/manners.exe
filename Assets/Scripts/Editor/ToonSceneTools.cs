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

public static class ToonSceneTools
{
    public const string ToonProfilePath = "Assets/Settings/ToonPostProcessing.asset";
    public const string SourceProfilePath = "Assets/Settings/SampleSceneProfile.asset";
    private static readonly Vector3 DefaultSunEuler = new Vector3(50f, 330f, 0f);

    [MenuItem("Tools/Manners/Visual toon/11. Ajustar escenas (post-procesado toon, sin sombras en tiempo real)", false, 90)]
    public static void ConfigureScenesFromMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string original = SceneManager.GetActiveScene().path;
        var report = new StringBuilder();
        ConfigureScenes(report);
        AssetDatabase.SaveAssets();
        if (!string.IsNullOrEmpty(original) && File.Exists(original)) EditorSceneManager.OpenScene(original, OpenSceneMode.Single);
        Debug.Log(report.ToString());
    }

    public static VolumeProfile EnsureToonProfile(ToonEnvironmentStyle style, StringBuilder report)
    {
        VolumeProfile profile = style.postProcessingProfile;
        if (profile == null) profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ToonProfilePath);
        if (profile == null)
        {
            if (!AssetDatabase.CopyAsset(SourceProfilePath, ToonProfilePath))
            {
                report.AppendLine($"ERROR: no se pudo crear {ToonProfilePath} a partir de {SourceProfilePath}.");
                return null;
            }
            profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ToonProfilePath);
            if (profile.TryGet(out Tonemapping tonemapping))
            {
                tonemapping.mode.Override(TonemappingMode.None);
                EditorUtility.SetDirty(tonemapping);
            }
            if (profile.TryGet(out ChromaticAberration aberration))
            {
                aberration.active = true;
                aberration.intensity.Override(0f);
                EditorUtility.SetDirty(aberration);
            }
            EditorUtility.SetDirty(profile);
            report.AppendLine($"POST {ToonProfilePath}: creado desde {SourceProfilePath} con tonemapping en None y aberración cromática en 0. El resto (bloom, viñeta) queda igual. Los valores se fijan como override porque el perfil por defecto del pipeline sigue siendo {SourceProfilePath}.");
        }
        if (style.postProcessingProfile != profile)
        {
            style.postProcessingProfile = profile;
            EditorUtility.SetDirty(style);
        }
        return profile;
    }

    public static void ConfigureScenes(StringBuilder report)
    {
        ToonEnvironmentStyle style = ToonEnvironmentTools.GetOrCreateStyle();
        VolumeProfile toonProfile = EnsureToonProfile(style, report);
        var sourceProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(SourceProfilePath);
        AssetDatabase.SaveAssets();
        HashSet<Material> managed = ManagedMaterials(style);
        foreach (string path in ToonScopeTools.ScenePaths(style))
        {
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            GameObject[] roots = scene.GetRootGameObjects();
            int renderers = 0;
            foreach (Renderer renderer in roots.SelectMany(root => root.GetComponentsInChildren<Renderer>(true)))
            {
                if (!IsMeshRenderer(renderer) || !UsesManaged(renderer, managed)) continue;
                if (!ToonEnvironmentTools.ConfigureRenderer(renderer)) continue;
                renderers++;
                MarkChanged(renderer);
            }

            int volumes = 0;
            if (toonProfile != null)
                foreach (Volume volume in roots.SelectMany(root => root.GetComponentsInChildren<Volume>(true)))
                {
                    if (!volume.isGlobal || volume.sharedProfile == toonProfile) continue;
                    if (volume.sharedProfile != null && volume.sharedProfile != sourceProfile) continue;
                    volume.sharedProfile = toonProfile;
                    volumes++;
                    MarkChanged(volume);
                }

            int cameras = 0;
            foreach (Camera camera in roots.SelectMany(root => root.GetComponentsInChildren<Camera>(true)))
            {
                if (!camera.CompareTag("MainCamera")) continue;
                var data = camera.GetComponent<UniversalAdditionalCameraData>();
                if (data == null || !data.renderPostProcessing) continue;
                AntialiasingMode wanted = style.antialiasing ? AntialiasingMode.FastApproximateAntialiasing : AntialiasingMode.None;
                if (data.antialiasing == wanted || (!style.antialiasing && data.antialiasing != AntialiasingMode.FastApproximateAntialiasing)) continue;
                data.antialiasing = wanted;
                cameras++;
                MarkChanged(data);
            }

            bool sun = ConfigureSun(scene, style, report);
            if (renderers > 0 || volumes > 0 || cameras > 0 || sun)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            report.AppendLine($"ESCENA {path}: {renderers} renderers sin sombras en tiempo real, {volumes} volúmenes con el perfil toon, {cameras} cámaras con FXAA{(sun ? ", luz direccional ajustada" : "")}.");
        }
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    private static void MarkChanged(Object target)
    {
        EditorUtility.SetDirty(target);
        if (PrefabUtility.IsPartOfPrefabInstance(target)) PrefabUtility.RecordPrefabInstancePropertyModifications(target);
    }

    private static bool ConfigureSun(Scene scene, ToonEnvironmentStyle style, StringBuilder report)
    {
        Light[] lights = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Light>(true)).ToArray();
        Light sun = lights.FirstOrDefault(light => light.type == LightType.Directional && light.enabled && light.gameObject.activeInHierarchy);
        bool changed = false;
        if (sun == null)
        {
            sun = lights.FirstOrDefault(light => light.type == LightType.Directional);
            if (sun == null)
            {
                var go = new GameObject("Directional Light");
                SceneManager.MoveGameObjectToScene(go, scene);
                sun = go.AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.color = Color.white;
                sun.intensity = 1f;
                sun.transform.rotation = Quaternion.Euler(DefaultSunEuler);
            }
            sun.enabled = true;
            if (!sun.gameObject.activeSelf) sun.gameObject.SetActive(true);
            changed = true;
            report.AppendLine($"SOL {scene.path}: no había luz direccional activa; '{sun.name}' queda como sol. El corte de luz y sombra toon y las sombras planas dependen de ella.");
        }
        if (RenderSettings.sun != sun)
        {
            RenderSettings.sun = sun;
            changed = true;
        }
        if (!style.realtimeShadows && sun.shadows != LightShadows.None)
        {
            sun.shadows = LightShadows.None;
            changed = true;
            report.AppendLine($"SOL {scene.path}: '{sun.name}' deja de proyectar sombras en tiempo real; ya no se genera el shadow map (las sombras son las planas del estilo).");
        }
        if (changed) MarkChanged(sun);
        return changed;
    }

    public static void Validate(ToonEnvironmentStyle style, List<string> problems, List<string> notes)
    {
        if (style == null) return;
        if (style.postProcessingProfile == null) problems.Add("Falta el perfil de post-procesado toon: ejecuta 'Ajustar escenas'.");
        else
        {
            if (style.postProcessingProfile.TryGet(out Tonemapping tonemapping) && tonemapping.active && tonemapping.mode.overrideState && tonemapping.mode.value != TonemappingMode.None)
                notes.Add("El perfil toon tiene tonemapping activo: los colores planos se verán apagados respecto a las texturas.");
            if (style.postProcessingProfile.TryGet(out ChromaticAberration aberration) && aberration.active && aberration.intensity.value > 0.08f)
                notes.Add($"El perfil toon tiene aberración cromática en {aberration.intensity.value:F2}: por encima de 0.08 triplica las líneas de contorno en los bordes de la pantalla.");
        }

        HashSet<Material> managed = ManagedMaterials(style);
        foreach (string path in ToonScopeTools.ScenePaths(style))
        {
            Scene preview = EditorSceneManager.OpenPreviewScene(path);
            try
            {
                int casters = 0, otherCasters = 0;
                var legacy = new Dictionary<string, int>();
                bool sun = false;
                foreach (GameObject root in preview.GetRootGameObjects())
                {
                    foreach (Light light in root.GetComponentsInChildren<Light>(false))
                    {
                        if (light.type != LightType.Directional || !light.enabled) continue;
                        sun = true;
                        if (!style.realtimeShadows && light.shadows != LightShadows.None)
                            problems.Add($"{path}: la luz direccional '{light.name}' sigue proyectando sombras en tiempo real; ejecuta 'Ajustar escenas' o activa 'Realtime Shadows' en el estilo.");
                    }
                    foreach (Volume volume in root.GetComponentsInChildren<Volume>(false))
                        if (volume.isGlobal && volume.enabled && style.postProcessingProfile != null && volume.sharedProfile != style.postProcessingProfile)
                            problems.Add($"{path}: el Global Volume '{volume.name}' no usa el perfil de post-procesado toon; ejecuta 'Ajustar escenas'.");
                    foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(false))
                    {
                        if (!IsMeshRenderer(renderer) || !renderer.enabled) continue;
                        bool isManaged = UsesManaged(renderer, managed);
                        if (renderer.shadowCastingMode != ShadowCastingMode.Off)
                        {
                            if (isManaged) casters++;
                            else otherCasters++;
                        }
                        foreach (Material material in renderer.sharedMaterials)
                        {
                            if (material == null || material.shader == null) continue;
                            string shader = material.shader.name;
                            if (shader.StartsWith("Universal Render Pipeline/") && !shader.Contains("Particles") && !shader.Contains("Terrain") || shader.StartsWith("Toon/"))
                            {
                                string key = shader + " | " + material.name;
                                legacy[key] = legacy.TryGetValue(key, out int count) ? count + 1 : 1;
                            }
                        }
                    }
                }
                if (casters > 0) problems.Add($"{path}: {casters} renderers toon siguen proyectando sombras en tiempo real; ejecuta 'Ajustar escenas'.");
                if (otherCasters > 0 && style.realtimeShadows) notes.Add($"{path}: {otherCasters} renderers que no son toon proyectan sombras en tiempo real (mantienen vivo el shadow map).");
                if (!sun) problems.Add($"{path}: no hay luz direccional activa; el sombreado toon y las sombras planas la necesitan.");
                foreach (var pair in legacy.OrderByDescending(pair => pair.Value))
                    notes.Add($"{path}: {pair.Value} renderers siguen fuera del estilo toon ({pair.Key}).");
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(preview);
            }
        }
    }

    private static HashSet<Material> ManagedMaterials(ToonEnvironmentStyle style)
    {
        var managed = new HashSet<Material>(style.environmentMaterials);
        managed.UnionWith(style.characterMaterials);
        managed.UnionWith(style.groundReceiverMaterials);
        managed.Remove(null);
        return managed;
    }

    private static bool IsMeshRenderer(Renderer renderer) => renderer is MeshRenderer || renderer is SkinnedMeshRenderer;

    private static bool UsesManaged(Renderer renderer, HashSet<Material> managed)
    {
        foreach (Material material in renderer.sharedMaterials)
            if (material != null && managed.Contains(material)) return true;
        return false;
    }
}
