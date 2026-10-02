using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class ToonStylePreview
{
    public const string Level1ScenePath = "Assets/Scenes/Final Levels/LEVEL 1/LEVEL 1.unity";
    private const int Width = 1920;
    private const int Height = 1080;

    [MenuItem("Tools/Manners/Visual toon/Capturar vista previa del Nivel 1 (PNG en Logs)", false, 90)]
    public static void CaptureFromMenu()
    {
        if (EditorSceneManager.GetActiveScene().isDirty &&
            !EditorUtility.DisplayDialog("Vista previa toon", "La escena abierta tiene cambios sin guardar. Se abrirá LEVEL 1 sin guardar. ¿Continuar?", "Continuar", "Cancelar"))
            return;
        var report = new StringBuilder();
        CaptureLevel1("manual", report);
        Debug.Log(report.ToString());
    }

    public static void CaptureLevel1(string tag, StringBuilder report)
    {
        Directory.CreateDirectory("Logs");
        Scene scene = EditorSceneManager.OpenScene(Level1ScenePath, OpenSceneMode.Single);
        Camera main = FindMainCamera(scene);
        if (main == null)
        {
            report.AppendLine("PREVIEW: no se encontró la cámara principal en LEVEL 1.");
            return;
        }
        string overview = $"Logs/toon-preview-{tag}-nivel1.png";
        Render(main, overview);
        report.AppendLine($"PREVIEW {tag}: vista de juego -> {overview}");

        CaptureDetail(scene, main, "Edif2", $"Logs/toon-preview-{tag}-detalle.png", tag, report);
        CaptureDetail(scene, main, "Building2", $"Logs/toon-preview-{tag}-detalle-ciudad-vieja.png", tag, report);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    private static void CaptureDetail(Scene scene, Camera main, string namePrefix, string path, string tag, StringBuilder report)
    {
        Transform target = FindDetailTarget(scene, namePrefix);
        if (target == null)
        {
            report.AppendLine($"PREVIEW {tag}: no se encontró ningún objeto activo que empiece por '{namePrefix}'.");
            return;
        }
        var go = new GameObject("Toon preview camera") { hideFlags = HideFlags.HideAndDontSave };
        try
        {
            var detail = go.AddComponent<Camera>();
            detail.CopyFrom(main);
            detail.targetTexture = null;
            var data = go.AddComponent<UniversalAdditionalCameraData>();
            var mainData = main.GetComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = mainData == null || mainData.renderPostProcessing;
            data.renderShadows = mainData == null || mainData.renderShadows;
            detail.transform.rotation = main.transform.rotation;
            detail.transform.position = target.position + Vector3.up * 3f - main.transform.forward * 26f;
            Render(detail, path);
            report.AppendLine($"PREVIEW {tag}: detalle de '{target.name}' -> {path}");
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

    private static Transform FindDetailTarget(Scene scene, string namePrefix)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(false))
                if (transform.gameObject.activeInHierarchy && transform.name.StartsWith(namePrefix, System.StringComparison.OrdinalIgnoreCase))
                    return transform;
        return null;
    }

    public static void Render(Camera camera, string path)
    {
        var previousTarget = camera.targetTexture;
        var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
        var texture = new Texture2D(Width, Height, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = rt;
            camera.Render();
            RenderTexture.active = rt;
            texture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = null;
            camera.targetTexture = previousTarget;
            Object.DestroyImmediate(texture);
            rt.Release();
            Object.DestroyImmediate(rt);
        }
    }
}
