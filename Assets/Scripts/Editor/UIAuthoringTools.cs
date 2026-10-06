using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class UIAuthoringTools
{
    public static readonly string[] Scenes = { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/Final Levels/LEVEL 1/LEVEL 1.unity", "Assets/Scenes/Sandbox.unity", "Assets/Scenes/CityTest.unity" };
    private static readonly Dictionary<string, string[]> Required = new Dictionary<string, string[]>
    {
        { "ChestAnnouncement", new[] { "text", "textRect", "group", "plate" } },
        { "OvertimeAlert", new[] { "canvas", "text", "textRect", "flashImage", "audioSource" } },
        { "ChestOpeningSequence", new[] { "canvasRoot", "promptText", "skipHintText", "aura", "showcase", "chestBurstPrefab" } },
        { "ChestShowcase", new[] { "view", "showcaseCamera" } },
        { "OverrideActivationHUD", new[] { "canvas", "strip", "banner", "titleText", "nameText", "iconGhost", "cellPrefab" } },
        { "OverrideStripCellUI", new[] { "icon", "fallback" } },
        { "HoverTooltipUI", new[] { "panelRect", "titleText", "bodyText" } },
        { "FloatingTextManager", new[] { "floatingTextPrefab", "worldCanvas" } },
        { "FloatingText", new[] { "textMesh", "canvasGroup" } },
        { "UpgradeButton", new[] { "upgradeNameText", "descriptionText", "valuesText", "costText", "button", "canvasGroup", "premiumVisuals" } },
        { "PremiumUpgradeVisuals", new[] { "holo", "aura" } },
        { "RadiantAuraVFX", new[] { "primaryRay", "secondaryRay" } },
        { "GraphicsSettingsMenu", new[] { "entryButton", "applyButton", "resetButton", "backButton", "keepButton", "revertButton", "controls", "confirmationText", "statusText", "syncHint" } },
        { "LanguageSelector", new[] { "previousButton", "nextButton", "valueLabel" } },
        { "MainMenuUIManager", new[] { "graphicsSettings", "fadeOverlay", "fadeCanvasGroup" } },
        { "PauseMenu", new[] { "graphicsSettings" } },
        { "GameOverUI", new[] { "gameOverCanvasGroup", "fadeOverlay", "fadeCanvasGroup" } },
        { "TutorialManager", new[] { "typingSource", "messageText", "nextButton" } },
        { "ExperienceUI", new[] { "barRect", "expBarFill", "levelText", "expText" } }
    };

    [MenuItem("Tools/Manners/UI/Editar/Aviso de cofre", false, 200)]
    private static void Chest() => Open("Assets/Prefabs/UI/Notifications/ChestAnnouncement.prefab");
    [MenuItem("Tools/Manners/UI/Editar/Cinemática de cofre", false, 201)]
    private static void Sequence() => Open("Assets/Prefabs/UI/Chest/ChestOpeningSequence.prefab");
    [MenuItem("Tools/Manners/UI/Editar/Idioma", false, 202)]
    private static void Language() => Open("Assets/Prefabs/UI/Settings/LanguageSelector.prefab");
    [MenuItem("Tools/Manners/UI/Editar/Gráficos", false, 203)]
    private static void Graphics() => Open("Assets/Prefabs/UI/Settings/GraphicsSettingsPanel.prefab");
    [MenuItem("Tools/Manners/UI/Editar/Números flotantes", false, 204)]
    private static void Floating() => Open("Assets/Prefabs/UI/FloatingText.prefab");
    [MenuItem("Tools/Manners/UI/Editar/Estilo y sprites", false, 205)]
    private static void Style() => Select(UIStyleTools.StyleAssetPath);
    [MenuItem("Tools/Manners/UI/Editar/Composición y anchors", false, 206)]
    private static void ContentFrame() => Open("Assets/Prefabs/UI/Layout/ContentFrame.prefab");
    [MenuItem("Tools/Manners/UI/Editar/HUD de sobrecargas", false, 207)]
    private static void OverrideHud() => Open("Assets/Prefabs/UI/OverrideHudPanel.prefab");
    [MenuItem("Tools/Manners/Configuraciones/Producción", false, 220)]
    private static void Production() => Select(GameAssetPaths.Production);
    [MenuItem("Tools/Manners/Configuraciones/Sandbox", false, 221)]
    private static void Sandbox() => Select(GameAssetPaths.Sandbox);
    private static void Open(string path) => AssetDatabase.OpenAsset(AssetDatabase.LoadMainAssetAtPath(path));
    private static void Select(string path) { Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(path); EditorGUIUtility.PingObject(Selection.activeObject); }

    [MenuItem("Tools/Manners/UI/Validar componentes e idiomas", false, 230)]
    public static void ValidateFromMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string original = SceneManager.GetActiveScene().path;
        try { Debug.Log(Validate()); }
        finally { if (!string.IsNullOrEmpty(original)) EditorSceneManager.OpenScene(original, OpenSceneMode.Single); }
    }

    public static string Validate()
    {
        var report = new StringBuilder(); var errors = new List<string>(); int components = 0, labels = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/UI" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid); GameObject root = PrefabUtility.LoadPrefabContents(path);
            try { ValidateRoot(root, path, errors, ref components, ref labels, true); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        foreach (string path in Scenes)
        {
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            foreach (GameObject root in scene.GetRootGameObjects()) ValidateRoot(root, path, errors, ref components, ref labels, false);
        }
        foreach (string folder in new[] { GameAssetPaths.Production, GameAssetPaths.Sandbox })
        {
            foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid); Object asset = AssetDatabase.LoadMainAssetAtPath(path);
                ValidateLanguages(new SerializedObject(asset), path, errors);
                string suffix = folder == GameAssetPaths.Production ? "_Production" : "_Sandbox";
                if (!Path.GetFileNameWithoutExtension(path).EndsWith(suffix, StringComparison.Ordinal)) errors.Add("Nombre ambiguo: " + path);
            }
        }
        foreach (string resource in new[] { "UI/UIStyle_Production", "UI/RuntimeUIPrefabs_Production", "UpgradeDatabase_Production", "ShopUpgradeDatabase_Production", "OverrideDatabase_Production", "TutorialConfig_Production", "ChestOpeningConfig_Production" })
            if (Resources.Load(resource) == null) errors.Add("Resources ausente: " + resource);
        RuntimeUIPrefabs registry = Resources.Load<RuntimeUIPrefabs>("UI/RuntimeUIPrefabs_Production");
        if (registry != null && (registry.chestAnnouncement == null || registry.overtimeAlert == null || registry.chestOpeningSequence == null || registry.overrideActivationHUD == null || registry.hoverTooltip == null)) errors.Add("Registro de prefabs incompleto.");
        report.AppendLine($"{Scenes.Length} escenas; {components} componentes; {labels} etiquetas bilingües; {errors.Count} errores.");
        foreach (string error in errors) report.AppendLine(error);
        Directory.CreateDirectory("Logs/UIReview"); File.WriteAllText("Logs/UIReview/components.txt", report.ToString());
        if (errors.Count > 0) throw new InvalidOperationException(report.ToString());
        return report.ToString();
    }

    private static void ValidateRoot(GameObject root, string scope, List<string> errors, ref int count, ref int labels, bool prefab)
    {
        foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) > 0) errors.Add(scope + ": script ausente en " + transform.name);
        foreach (MonoBehaviour component in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (component == null) continue; count++;
            string location = scope + "/" + component.name + " (" + component.GetType().Name + ")";
            var serialized = new SerializedObject(component);
            if (Required.TryGetValue(component.GetType().Name, out string[] fields))
                foreach (string field in fields)
                {
                    if (prefab && field == "entryButton") continue;
                    SerializedProperty property = serialized.FindProperty(field);
                    if (property == null || property.objectReferenceValue == null) errors.Add(location + ": falta " + field);
                }
            if (component is LocalizedText text) { labels++; if (!text.Content.HasBothLanguages) errors.Add(location + ": traducción incompleta"); }
            ValidateLanguages(serialized, location, errors);
            if (component is TutorialManager)
            {
                SerializedProperty steps = serialized.FindProperty("steps"); var ids = new HashSet<string>();
                if (steps.arraySize == 0) errors.Add(location + ": tutorial sin pasos");
                for (int i = 0; i < steps.arraySize; i++) if (!ids.Add(steps.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue)) errors.Add(location + ": id de tutorial repetido");
                for (int i = 0; i < steps.arraySize; i++) foreach (string field in new[] { "nextStepId", "yesGoesToStep", "noGoesToStep" })
                {
                    string id = steps.GetArrayElementAtIndex(i).FindPropertyRelative(field).stringValue;
                    if (!string.IsNullOrEmpty(id) && !ids.Contains(id)) errors.Add(location + ": destino de tutorial ausente " + id);
                }
            }
        }
    }
    private static void ValidateLanguages(SerializedObject serialized, string location, List<string> errors)
    {
        SerializedProperty property = serialized.GetIterator();
        while (property.Next(true))
        {
            if (property.propertyType != SerializedPropertyType.Generic || property.isArray || property.type != nameof(LocalizedString)) continue;
            string english = property.FindPropertyRelative("english").stringValue;
            string spanish = property.FindPropertyRelative("spanish").stringValue;
            if (string.IsNullOrEmpty(english) != string.IsNullOrEmpty(spanish)) errors.Add(location + ": traducción incompleta en " + property.propertyPath);
        }
    }
}
