using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class ToonStylePreview
{
    public const string Level1ScenePath = "Assets/Scenes/Final Levels/LEVEL 1/LEVEL 1.unity";
    public const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const int Width = 1920;
    private const int Height = 1080;

    private static readonly string[] Level1Enemies = { "Basic Enemy", "BEnemy2", "BEnemy3", "Fast Enemy", "FEnemy2", "FEnemy3" };

    [MenuItem("Tools/Manners/Visual toon/Capturar vistas previas (PNG en Logs)", false, 99)]
    public static void CaptureFromMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string original = SceneManager.GetActiveScene().path;
        var report = new StringBuilder();
        CaptureAll("manual", report);
        if (!string.IsNullOrEmpty(original) && File.Exists(original)) EditorSceneManager.OpenScene(original, OpenSceneMode.Single);
        Debug.Log(report.ToString());
    }

    public static void CaptureAll(string tag, StringBuilder report)
    {
        Directory.CreateDirectory("Logs");
        CaptureLevel(Level1ScenePath, "nivel1", tag, Level1Enemies, report);
        CaptureMenu(tag, report);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    public static void CaptureLevel1(string tag, StringBuilder report)
    {
        Directory.CreateDirectory("Logs");
        CaptureLevel(Level1ScenePath, "nivel1", tag, Level1Enemies, report);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    private static void CaptureLevel(string scenePath, string sceneTag, string tag, string[] enemies, StringBuilder report)
    {
        if (!File.Exists(scenePath)) return;
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        Camera main = FindMainCamera(scene);
        if (main == null)
        {
            report.AppendLine($"PREVIEW {tag}: no se encontró la cámara principal en {scenePath}.");
            return;
        }
        ToonEnvironmentStyle.ApplyGroundShadows(true, ToonEnvironmentTools.GetOrCreateStyle());
        string prefix = $"Logs/toon-preview-{tag}-{sceneTag}";
        Render(main, prefix + "-juego.png");
        report.AppendLine($"PREVIEW {tag}: vista de juego -> {prefix}-juego.png");

        GameObject player = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Select(transform => transform.gameObject).FirstOrDefault(go => go.CompareTag("Player"));
        Vector3 focus = player != null ? player.transform.position : main.transform.position + main.transform.forward * 20f;
        Vector3 offset = main.transform.position - focus;
        Shot(main, new Vector3(-20f, focus.y, 20f) + offset, main.transform.rotation, prefix + "-ciudad.png", tag, report);
        Shot(main, new Vector3(40f, focus.y, -10f) + offset, main.transform.rotation, prefix + "-ciudad2.png", tag, report);

        Terrain terrain = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Terrain>(false)).FirstOrDefault();
        if (terrain != null)
        {
            Vector3 size = terrain.terrainData.size;
            Vector3 center = terrain.transform.position + new Vector3(size.x * 0.5f, 0f, size.z * 0.5f);
            Shot(main, center + new Vector3(0f, 190f, -200f), Quaternion.Euler(45f, 0f, 0f), prefix + "-aerea.png", tag, report);
        }
        CaptureCharacters(main, focus, enemies, prefix + "-personajes.png", tag, report);
    }

    private static void CaptureMenu(string tag, StringBuilder report)
    {
        if (!File.Exists(MainMenuScenePath)) return;
        Scene scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        Camera main = FindMainCamera(scene);
        if (main == null) return;
        string prefix = $"Logs/toon-preview-{tag}-menu";
        var first = new Vector3(30f, 25f, 0f);
        var second = new Vector3(-21.2f, 25f, 21.2f);
        Shot(main, first, Quaternion.LookRotation(-first), prefix + "-orbita1.png", tag, report);
        Shot(main, second, Quaternion.LookRotation(-second), prefix + "-orbita2.png", tag, report);
    }

    private static void CaptureCharacters(Camera main, Vector3 focus, string[] names, string path, string tag, StringBuilder report)
    {
        var spawned = new List<GameObject>();
        try
        {
            for (int i = 0; i < names.Length; i++)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/Characters/{names[i]}.prefab");
                if (prefab == null) continue;
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.hideFlags = HideFlags.DontSave;
                float column = i - (names.Length - 1) * 0.5f;
                instance.transform.position = focus + Vector3.right * (column * 3.2f) + Vector3.forward * (Mathf.Abs(column) < 1f ? 5.5f : 3f);
                instance.transform.rotation = Quaternion.Euler(0f, 150f + i * 12f, 0f);
                spawned.Add(instance);
            }
            Shot(main, focus + new Vector3(0f, 8.5f, -9f), Quaternion.Euler(33f, 0f, 0f), path, tag, report);
        }
        finally
        {
            foreach (GameObject go in spawned) Object.DestroyImmediate(go);
        }
    }

    private static void Shot(Camera main, Vector3 position, Quaternion rotation, string path, string tag, StringBuilder report)
    {
        var go = new GameObject("Toon preview camera") { hideFlags = HideFlags.HideAndDontSave };
        try
        {
            var camera = go.AddComponent<Camera>();
            camera.CopyFrom(main);
            camera.targetTexture = null;
            var data = go.AddComponent<UniversalAdditionalCameraData>();
            var mainData = main.GetComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = mainData == null || mainData.renderPostProcessing;
            data.renderShadows = mainData == null || mainData.renderShadows;
            if (mainData != null) data.volumeLayerMask = mainData.volumeLayerMask;
            camera.transform.SetPositionAndRotation(position, rotation);
            Render(camera, path);
            report.AppendLine($"PREVIEW {tag}: {path}");
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    private static Camera FindMainCamera(Scene scene)
    {
        Camera fallback = null;
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Camera camera in root.GetComponentsInChildren<Camera>(true))
            {
                if (camera.CompareTag("MainCamera")) return camera;
                if (fallback == null && camera.targetTexture == null) fallback = camera;
            }
        return fallback;
    }

    public static void Render(Camera camera, string path)
    {
        var previousTarget = camera.targetTexture;
        bool previousAsync = ShaderUtil.allowAsyncCompilation;
        var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
        var texture = new Texture2D(Width, Height, TextureFormat.RGB24, false);
        try
        {
            ShaderUtil.allowAsyncCompilation = false;
            camera.targetTexture = rt;
            camera.Render();
            camera.Render();
            RenderTexture.active = rt;
            texture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
        }
        finally
        {
            ShaderUtil.allowAsyncCompilation = previousAsync;
            RenderTexture.active = null;
            camera.targetTexture = previousTarget;
            Object.DestroyImmediate(texture);
            rt.Release();
            Object.DestroyImmediate(rt);
        }
    }
}
