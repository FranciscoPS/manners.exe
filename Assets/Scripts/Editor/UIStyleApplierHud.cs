using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static partial class UIStyleApplier
{
    private const string HudPrefabPath = "Assets/Prefabs/UI/OverrideHudPanel.prefab";
    public const string HudReportPath = "Logs/ui-hud-report.txt";

    [MenuItem("Tools/Manners/UI/Corregir HUD: vida, niveles de sobrecargas y daño", false, 114)]
    public static void ApplyHudFromMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string original = SceneManager.GetActiveScene().path;
        var hudReport = new StringBuilder();
        ApplyHud(UIStyleTools.GetOrCreateStyle(), hudReport);
        AssetDatabase.SaveAssets();
        if (!string.IsNullOrEmpty(original) && File.Exists(original))
            EditorSceneManager.OpenScene(original, OpenSceneMode.Single);
        Debug.Log(hudReport.ToString());
    }

    public static void ApplyHudBatch()
    {
        var hudReport = new StringBuilder();
        bool passed = true;
        try
        {
            ApplyHud(UIStyleTools.GetOrCreateStyle(), hudReport);
            AssetDatabase.SaveAssets();
        }
        catch (Exception exception)
        {
            hudReport.AppendLine("EXCEPTION: " + exception);
            passed = false;
        }

        Directory.CreateDirectory("Logs");
        File.WriteAllText(HudReportPath, hudReport.ToString());
        if (passed) Debug.Log(hudReport.ToString());
        else Debug.LogError(hudReport.ToString());
        if (Application.isBatchMode) EditorApplication.Exit(passed ? 0 : 1);
    }

    public static void ApplyHud(UIStyle targetStyle, StringBuilder targetReport)
    {
        style = targetStyle;
        report = targetReport;

        if (File.Exists(HudPrefabPath))
        {
            ResetCounters();
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(HudPrefabPath);
            try
            {
                StyleOverrideLevelTexts(prefabRoot.transform);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, HudPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
            report.AppendLine($"HUD {HudPrefabPath}: {textCount} textos de nivel ajustados.");
        }

        AssetDatabase.SaveAssets();

        foreach (string path in ScenePaths)
        {
            if (!File.Exists(path)) continue;
            ResetCounters();
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            Canvas.ForceUpdateCanvases();
            int touched = 0;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true))
                {
                    if (!canvas.isRootCanvas && canvas.transform.parent != null && canvas.transform.parent.GetComponentInParent<Canvas>(true) != null) continue;
                    if (canvas.renderMode == RenderMode.WorldSpace) continue;
                    touched += StyleHudRoot(canvas.transform);
                }
            }

            touched += StyleDamageNumbers(scene);
            if (touched == 0)
            {
                report.AppendLine($"HUD {path}: sin barra de vida, panel de sobrecargas ni números de daño; no se tocó.");
                continue;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            report.AppendLine($"HUD {path}: {touched} elementos ajustados, {createdCount} objetos nuevos.");
        }

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    private static int StyleHudRoot(Transform root)
    {
        int touched = 0;

        Transform health = DirectChild(root, "HealthBarContainer");
        if (health != null)
        {
            StyleHealthBar((RectTransform)health);
            KeepOverridePanelBelowHealth(root, (RectTransform)health);
            touched++;
        }

        foreach (OverrideHudPanel hud in root.GetComponentsInChildren<OverrideHudPanel>(true))
        {
            if (!hud.gameObject.activeSelf)
            {
                hud.gameObject.SetActive(true);
                Touch(hud.gameObject);
                report.AppendLine($"HUD: el panel de sobrecargas '{hud.name}' estaba desactivado en la escena y se vuelve a activar.");
            }

            StyleOverrideLevelTexts(hud.transform);
            touched++;
        }

        foreach (HoldToSelectButton hold in root.GetComponentsInChildren<HoldToSelectButton>(true))
        {
            StyleHoldFill(hold.transform);
            touched++;
        }

        return touched;
    }
}
