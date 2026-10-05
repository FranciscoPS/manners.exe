using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class UIStylePreview
{
    private const string MainMenu = "Assets/Scenes/MainMenu.unity";
    private const string Level1 = "Assets/Scenes/Final Levels/LEVEL 1/LEVEL 1.unity";
    private const int Width = 1920;
    private const int Height = 1080;

    private readonly struct Shot
    {
        public readonly string Scene;
        public readonly string Name;
        public readonly string[] Toggles;

        public Shot(string scene, string name, params string[] toggles)
        {
            Scene = scene;
            Name = name;
            Toggles = toggles;
        }
    }

    private static readonly Shot[] Shots =
    {
        new Shot(MainMenu, "menu-principal"),
        new Shot(MainMenu, "menu-opciones", "-Canvas/MainMenuPanel", "+Canvas/OptionsPanel"),
        new Shot(MainMenu, "menu-mejoras", "-Canvas/MainMenuPanel", "+Canvas/UpgradesPanel", "+Canvas/UpgradesPanel/NormalPanel", "+Canvas/UpgradesPanel/NormalPanel/RPPanel"),
        new Shot(MainMenu, "menu-tienda", "-Canvas/MainMenuPanel", "+Canvas/TiendaPanel", "+Canvas/TiendaPanel/Skins"),
        new Shot(MainMenu, "menu-sobrecargas", "-Canvas/MainMenuPanel", "+Canvas/SobrecargasMenuPanel", "+Canvas/SobrecargasMenuPanel/OverrideHintsPanel"),
        new Shot(MainMenu, "menu-audio", "-Canvas/MainMenuPanel", "+Canvas/AudioPanel"),
        new Shot(MainMenu, "menu-controles", "-Canvas/MainMenuPanel", "+Canvas/ControlesPanel"),
        new Shot(MainMenu, "menu-mapas", "-Canvas/MainMenuPanel", "+Canvas/MapSelectionPanel"),
        new Shot(Level1, "nivel-hud"),
        new Shot(Level1, "nivel-mejoras", "+Canvas/LevelUpPanel"),
        new Shot(Level1, "nivel-pausa", "+Canvas/PausePanel"),
        new Shot(Level1, "nivel-gameover", "+Canvas/GameOverPanel"),
        new Shot(Level1, "nivel-iniciales", "+Canvas/InitialsEntryUI"),
        new Shot(Level1, "nivel-tutorial", "+TutorialCanvas/Panel"),
    };

    [MenuItem("Tools/Manners/UI/Capturar vistas previas (PNG en Logs)", false, 130)]
    public static void CaptureFromMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string original = SceneManager.GetActiveScene().path;
        var report = new StringBuilder();
        CaptureAll("manual", report);
        if (!string.IsNullOrEmpty(original) && File.Exists(original))
            EditorSceneManager.OpenScene(original, OpenSceneMode.Single);
        Debug.Log(report.ToString());
    }

    public static void CaptureAll(string tag, StringBuilder report)
    {
        Directory.CreateDirectory("Logs");
        foreach (Shot shot in Shots)
        {
            if (!File.Exists(shot.Scene)) continue;

            Scene scene = EditorSceneManager.OpenScene(shot.Scene, OpenSceneMode.Single);
            foreach (string toggle in shot.Toggles)
            {
                GameObject target = Find(scene, toggle.Substring(1));
                if (target != null) target.SetActive(toggle[0] == '+');
                else report.AppendLine($"PREVIEW {tag}: no se encontró '{toggle.Substring(1)}' en {shot.Scene}.");
            }

            string path = $"Logs/ui-preview-{tag}-{shot.Name}.png";
            Capture(scene, path, Width, Height);
            report.AppendLine($"PREVIEW {tag}: {path}");
        }

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    private static GameObject Find(Scene scene, string path)
    {
        int slash = path.IndexOf('/');
        string rootName = slash < 0 ? path : path.Substring(0, slash);
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name != rootName) continue;
            if (slash < 0) return root;
            Transform found = root.transform.Find(path.Substring(slash + 1));
            if (found != null) return found.gameObject;
        }
        return null;
    }

    public static void Capture(Scene scene, string path, int width, int height)
    {
        Camera main = null;
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Camera camera in root.GetComponentsInChildren<Camera>(true))
                if (camera.CompareTag("MainCamera")) main = camera;

        bool previousAsync = ShaderUtil.allowAsyncCompilation;
        ShaderUtil.allowAsyncCompilation = false;
        var sceneTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        var finalTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        var created = new List<Object>();
        var switched = new List<Canvas>();
        try
        {
            if (main != null)
            {
                RenderTexture previous = main.targetTexture;
                main.targetTexture = sceneTexture;
                main.Render();
                main.Render();
                main.targetTexture = previous;
            }

            var cameraObject = new GameObject("UI preview camera") { hideFlags = HideFlags.HideAndDontSave };
            created.Add(cameraObject);
            cameraObject.transform.position = new Vector3(0f, -5000f, 0f);
            Camera uiCamera = cameraObject.AddComponent<Camera>();
            uiCamera.clearFlags = CameraClearFlags.SolidColor;
            uiCamera.backgroundColor = Color.black;
            uiCamera.orthographic = true;
            uiCamera.nearClipPlane = 0.1f;
            uiCamera.farClipPlane = 200f;
            uiCamera.targetTexture = finalTexture;
            var data = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = false;
            data.renderShadows = false;

            var backgroundObject = new GameObject("UI preview background") { hideFlags = HideFlags.HideAndDontSave };
            created.Add(backgroundObject);
            Canvas backgroundCanvas = backgroundObject.AddComponent<Canvas>();
            backgroundCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            backgroundCanvas.worldCamera = uiCamera;
            backgroundCanvas.planeDistance = 100f;
            backgroundCanvas.sortingOrder = -30000;
            var imageObject = new GameObject("Background", typeof(RectTransform)) { hideFlags = HideFlags.HideAndDontSave };
            imageObject.transform.SetParent(backgroundObject.transform, false);
            RawImage image = imageObject.AddComponent<RawImage>();
            image.texture = sceneTexture;
            RectTransform imageRect = image.rectTransform;
            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.one;
            imageRect.offsetMin = imageRect.offsetMax = Vector2.zero;

            foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!canvas.isRootCanvas || canvas == backgroundCanvas) continue;
                if (canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                switched.Add(canvas);
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = uiCamera;
                canvas.planeDistance = 50f;
            }

            Canvas.ForceUpdateCanvases();
            uiCamera.Render();
            Canvas.ForceUpdateCanvases();
            uiCamera.Render();

            RenderTexture.active = finalTexture;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }
        finally
        {
            ShaderUtil.allowAsyncCompilation = previousAsync;
            RenderTexture.active = null;
            foreach (Canvas canvas in switched)
            {
                if (canvas == null) continue;
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.worldCamera = null;
            }
            foreach (Object item in created) Object.DestroyImmediate(item);
            sceneTexture.Release();
            finalTexture.Release();
            Object.DestroyImmediate(sceneTexture);
            Object.DestroyImmediate(finalTexture);
        }
    }
}
