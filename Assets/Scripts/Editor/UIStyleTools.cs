using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class UIStyleTools
{
    public const string StyleAssetPath = "Assets/Resources/UIStyle.asset";
    public const string MaterialFolder = "Assets/Materials/UI";
    public const string ReportPath = "Logs/ui-style-report.txt";

    private const string LogPrefix = "[UIStyle]";

    [MenuItem("Tools/Manners/UI/0. Aplicar todo el estilo (pasos 1 a 3 en orden)", false, 100)]
    public static void RunAllFromMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string original = SceneManager.GetActiveScene().path;
        RunAll(false);
        Reopen(original);
    }

    [MenuItem("Tools/Manners/UI/1. Generar sprites, fuente y materiales del estilo", false, 111)]
    public static void BuildKitFromMenu()
    {
        var report = new StringBuilder();
        BuildKit(report);
        AssetDatabase.SaveAssets();
        Debug.Log(report.ToString());
    }

    [MenuItem("Tools/Manners/UI/2. Aplicar estilo a escenas y prefabs", false, 112)]
    public static void ApplyFromMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string original = SceneManager.GetActiveScene().path;
        var report = new StringBuilder();
        UIStyleApplier.ApplyAll(GetOrCreateStyle(), report);
        AssetDatabase.SaveAssets();
        Reopen(original);
        Debug.Log(report.ToString());
    }

    [MenuItem("Tools/Manners/UI/3. Validar interfaz (fuentes, sprites y textos que no caben)", false, 113)]
    public static void ValidateFromMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string original = SceneManager.GetActiveScene().path;
        var report = new StringBuilder();
        bool passed = UIStyleApplier.Validate(GetOrCreateStyle(), report);
        Reopen(original);
        if (passed) Debug.Log(report.ToString());
        else Debug.LogWarning(report.ToString());
    }

    public static void RunAllBatch()
    {
        bool passed = RunAll(true);
        if (Application.isBatchMode) EditorApplication.Exit(passed ? 0 : 1);
    }

    private static bool RunAll(bool capture)
    {
        var report = new StringBuilder();
        bool passed;
        try
        {
            UIStyle style = BuildKit(report);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            UIStyleApplier.ApplyAll(style, report);
            AssetDatabase.SaveAssets();
            passed = UIStyleApplier.Validate(style, report);
            if (capture) UIStylePreview.CaptureAll("despues", report);
        }
        catch (Exception exception)
        {
            report.AppendLine("EXCEPTION: " + exception);
            passed = false;
        }

        Directory.CreateDirectory("Logs");
        File.WriteAllText(ReportPath, report.ToString());
        if (passed) Debug.Log($"{LogPrefix} Estilo aplicado. Informe: {ReportPath}");
        else Debug.LogError($"{LogPrefix} Estilo aplicado con avisos. Informe: {ReportPath}");
        return passed;
    }

    private static void Reopen(string scenePath)
    {
        if (!string.IsNullOrEmpty(scenePath) && File.Exists(scenePath))
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
    }

    public static UIStyle GetOrCreateStyle()
    {
        UIStyle style = AssetDatabase.LoadAssetAtPath<UIStyle>(StyleAssetPath);
        if (style != null) return style;

        EditorAssetUtility.EnsureFolder("Assets/Resources");
        style = ScriptableObject.CreateInstance<UIStyle>();
        AssetDatabase.CreateAsset(style, StyleAssetPath);
        return style;
    }

    [MenuItem("Tools/Manners/UI/Restablecer paleta, forma y movimiento del estilo", false, 131)]
    public static void ResetIdentityFromMenu()
    {
        if (!EditorUtility.DisplayDialog("Estilo de la interfaz", "Se descartan los ajustes hechos a mano en UIStyle.asset y vuelven los valores de la identidad. Después hay que aplicar el estilo (paso 0).", "Restablecer", "Cancelar")) return;
        var report = new StringBuilder();
        ResetIdentity(GetOrCreateStyle(), report);
        BuildKit(report);
        Debug.Log(report.ToString());
    }

    private static void ResetIdentity(UIStyle style, StringBuilder report)
    {
        UIStyle fresh = ScriptableObject.CreateInstance<UIStyle>();
        fresh.version = UIStyle.IdentityVersion;
        JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(fresh), style);
        UnityEngine.Object.DestroyImmediate(fresh);
        EditorUtility.SetDirty(style);
        report.AppendLine($"ESTILO: paleta, forma y movimiento restablecidos a la identidad v{UIStyle.IdentityVersion}.");
    }

    public static UIStyle BuildKit(StringBuilder report)
    {
        UIStyle style = GetOrCreateStyle();
        if (style.version != UIStyle.IdentityVersion) ResetIdentity(style, report);
        UIStyleSpriteBuilder.BuildAll(style, report);
        UIStyleFontBuilder.Build(style, report);
        BuildMaterials(style, report);
        EditorUtility.SetDirty(style);
        AssetDatabase.SaveAssets();
        return style;
    }

    private static void BuildMaterials(UIStyle style, StringBuilder report)
    {
        EditorAssetUtility.EnsureFolder(MaterialFolder);
        Shader shader = Shader.Find(UIStyle.PlateShaderName);
        if (shader == null)
        {
            report.AppendLine($"MATERIALES: no se encontró el shader '{UIStyle.PlateShaderName}'.");
            return;
        }

        Color none = new Color(0f, 0f, 0f, 0f);
        style.plateStripes = PlateMaterial("UIPlate Stripes", shader, new Color(0.9f, 0.88f, 0.95f, 1f), none, 30f, 0.5f, new Vector2(1f, -1f), 22f, 0.3f);
        style.plateStripesBold = PlateMaterial("UIPlate Stripes Bold", shader, new Color(0.76f, 0.72f, 0.9f, 1f), none, 36f, 0.5f, new Vector2(1f, -1f), 34f, 0.3f);
        style.plateHazard = PlateMaterial("UIPlate Hazard", shader, new Color(0.7f, 0.68f, 0.76f, 1f), none, 44f, 0.5f, new Vector2(1f, -1f), 30f, 0.3f);
        style.screenScan = PlateMaterial("UIPlate Screen", shader, Color.white, new Color(0.3f, 0.5f, 0.9f, 0.055f), 5f, 0.5f, new Vector2(0f, 1f), 10f, -1f);
        style.scrimStripes = PlateMaterial("UIPlate Scrim", shader, Color.white, new Color(0.2f, 0.36f, 0.8f, 0.05f), 8f, 0.5f, new Vector2(0f, 1f), 14f, -1f);
        report.AppendLine($"MATERIALES: 5 materiales de placa en {MaterialFolder}.");
    }

    private static Material PlateMaterial(string name, Shader shader, Color multiply, Color add, float size, float duty, Vector2 direction, float speed, float fillMin)
    {
        string path = $"{MaterialFolder}/{name}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.shader = shader;
        material.SetColor("_PatternMul", multiply);
        material.SetColor("_PatternAdd", add);
        material.SetFloat("_PatternSize", size);
        material.SetFloat("_PatternDuty", duty);
        material.SetVector("_PatternDir", new Vector4(direction.x, direction.y, 0f, 0f));
        material.SetFloat("_PatternSpeed", speed);
        material.SetFloat("_FillMin", fillMin);
        material.SetFloat("_FillMax", 2f);
        EditorUtility.SetDirty(material);
        return material;
    }
}
