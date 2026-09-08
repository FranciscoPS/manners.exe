using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Batch entry point. Requires a graphics device (omit -nographics).
// -executeMethod WebBuildRenderingChecks.Run -batchmode -quit
public static class WebBuildRenderingChecks
{
    public static void Run()
    {
        if (!Application.isBatchMode)
            throw new InvalidOperationException("Run this check in a separate batch-mode Editor.");

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        int previousQuality = QualitySettings.GetQualityLevel();
        var camera = new GameObject("Rendering check camera").AddComponent<Camera>();
        camera.transform.position = new Vector3(0, 15, -15);
        camera.transform.LookAt(Vector3.zero);
        camera.fieldOfView = 60;
        camera.aspect = 1;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.magenta;
        camera.useOcclusionCulling = false;
        var target = new RenderTexture(256, 256, 24);
        camera.targetTexture = target;
        var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        material.color = Color.white;
        var texture = new Texture2D(256, 256, TextureFormat.RGB24, false);
        var report = new StringBuilder();
        int failures = 0;
        int renders = 0;

        try
        {
            for (int quality = 0; quality < QualitySettings.names.Length; quality++)
            {
                QualitySettings.SetQualityLevel(quality);
                foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Characters" }))
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                    if (!prefab.GetComponent<EnemyController>()) continue;
                    var root = Object.Instantiate(prefab);
                    try
                    {
                        // White silhouettes measure visibility independently of lighting/texture color.
                        foreach (var renderer in root.GetComponentsInChildren<Renderer>())
                        {
                            var materials = renderer.sharedMaterials;
                            for (int i = 0; i < materials.Length; i++) materials[i] = material;
                            renderer.sharedMaterials = materials;
                        }
                        var controller = new SerializedObject(root.GetComponent<EnemyController>());
                        for (int positionTest = 0; positionTest < 2; positionTest++)
                        for (int direction = 0; direction < 8; direction++)
                        {
                            var heading = Quaternion.Euler(0, direction * 45, 0);
                            root.transform.position = positionTest == 0 ? Vector3.zero : heading * Vector3.forward * 10;
                            var facing = positionTest == 0 ? heading : Quaternion.LookRotation(-root.transform.position);
                            for (int visual = 1; visual <= 2; visual++)
                            {
                                var transform = controller.FindProperty("visual" + visual).objectReferenceValue as Transform;
                                if (transform && controller.FindProperty("applyToVisual" + visual).boolValue)
                                    transform.rotation = facing;
                            }
                            camera.Render();
                            RenderTexture.active = target;
                            texture.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
                            texture.Apply();
                            int pixels = 0;
                            foreach (var pixel in texture.GetPixels32())
                                if (pixel.g > 100) pixels++;
                            renders++;
                            if (pixels == 0) failures++;
                            report.AppendLine($"{QualitySettings.names[quality]} {prefab.name}: test={(positionTest == 0 ? "rotation" : "approach")}, heading={direction * 45}, visiblePixels={pixels}");
                        }
                    }
                    finally { Object.DestroyImmediate(root); }
                }
            }
        }
        finally
        {
            QualitySettings.SetQualityLevel(previousQuality);
            RenderTexture.active = null;
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(material);
            target.Release();
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(camera.gameObject);
            Directory.CreateDirectory("Logs");
            report.AppendLine($"RESULT: {renders} renders, {failures} invisible enemies.");
            File.WriteAllText("Logs/web-rendering-checks.txt", report.ToString());
        }
        if (failures > 0) throw new InvalidOperationException($"{failures}/{renders} enemy renders were invisible.");
        Debug.Log($"Web rendering checks passed: {renders} renders.");
    }
}
