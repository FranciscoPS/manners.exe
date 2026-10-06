using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class UIBilingualPreview
{
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    private static readonly string[] MenuPanels = { "MainMenuPanel", "OptionsPanel", "AudioPanel", "ControlesPanel", "MapSelectionPanel", "SobrecargasMenuPanel", "GraphicsSettingsPanel" };
    private static readonly string[] GamePanels = { "HUD", "PausePanel", "LevelUpPanel", "GameOverPanel", "InitialsEntryUI", "Tutorial" };

    [MenuItem("Tools/Manners/UI/Capturar inglés y español", false, 231)]
    public static void CaptureFromMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string original = SceneManager.GetActiveScene().path;
        try { Capture(); }
        finally { if (!string.IsNullOrEmpty(original)) EditorSceneManager.OpenScene(original, OpenSceneMode.Single); }
    }

    public static void RunBatch()
    {
        Debug.Log(UIAuthoringTools.Validate()); Capture();
    }
    public static void Capture()
    {
        Directory.CreateDirectory("Logs/UIReview"); var report = new StringBuilder();
        GameLanguage original = GameLocalization.Language; bool saved = PlayerPrefs.HasKey("GameLanguage"); int savedValue = PlayerPrefs.GetInt("GameLanguage");
        try
        {
            foreach (GameLanguage language in new[] { GameLanguage.English, GameLanguage.Spanish })
            {
                GameLocalization.SetLanguage(language);
                foreach (Vector2Int size in new[] { new Vector2Int(1920, 1080), new Vector2Int(1280, 720), new Vector2Int(2560, 1080) })
                {
                    foreach (string panel in MenuPanels) Shot(UIAuthoringTools.Scenes[0], panel, language, size, report);
                    foreach (string panel in GamePanels) Shot(UIAuthoringTools.Scenes[1], panel, language, size, report);
                    foreach (string notification in new[] { "ChestAnnouncement", "OvertimeAlert", "OverrideActivationHUD" }) Shot(UIAuthoringTools.Scenes[1], notification, language, size, report);
                }
            }
        }
        finally
        {
            GameLocalization.SetLanguage(original);
            if (saved) PlayerPrefs.SetInt("GameLanguage", savedValue); else PlayerPrefs.DeleteKey("GameLanguage"); PlayerPrefs.Save();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            File.WriteAllText("Logs/UIReview/layout.txt", report.ToString());
        }
        Debug.Log("Bilingual UI previews saved in Logs/UIReview.");
    }
    private static void Shot(string path, string panel, GameLanguage language, Vector2Int size, StringBuilder report)
    {
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        Transform canvas = scene.GetRootGameObjects().First(r => r.name == "Canvas").transform;
        if (path == UIAuthoringTools.Scenes[0])
        {
            foreach (Transform child in canvas) child.gameObject.SetActive(child.name == panel || child.name == "StyleMenuBacking" || child.name == "StyleMenuDecoration");
            if (panel == "SobrecargasMenuPanel") SetActive(canvas.Find(panel + "/OverrideHintsPanel"), true);
        }
        else
        {
            foreach (string name in new[] { "PausePanel", "LevelUpPanel", "GameOverPanel", "InitialsEntryUI", "GraphicsSettingsPanel" }) SetActive(canvas.Find(name), name == panel);
            TutorialManager tutorial = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<TutorialManager>(true)).FirstOrDefault();
            if (tutorial != null)
            {
                GameObject tutorialPanel = Get<GameObject>(tutorial, "tutorialPanel"); tutorialPanel.SetActive(panel == "Tutorial");
                if (panel == "Tutorial")
                {
                    TutorialStep[] steps = Get<TutorialStep[]>(tutorial, "steps");
                    Get<TMP_Text>(tutorial, "messageText").text = steps.OrderByDescending(s => s.text.Length).First().text;
                    Get<TMP_Text>(tutorial, "messageText").maxVisibleCharacters = int.MaxValue;
                }
            }
        }
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (LocalizedText text in root.GetComponentsInChildren<LocalizedText>(true)) text.ApplyLanguage(language);
            foreach (LanguageSelector selector in root.GetComponentsInChildren<LanguageSelector>(true)) Get<TMP_Text>(selector, "valueLabel").text = language == GameLanguage.Spanish ? "Español" : "English";
            foreach (CanvasGroup group in root.GetComponentsInChildren<CanvasGroup>(true)) if (group.name.Contains("GameOver") || group.name.Contains("Initials")) group.alpha = 1;
            foreach (ExperienceUI experience in root.GetComponentsInChildren<ExperienceUI>(true)) Get<TMP_Text>(experience, "levelText").text = Get<LocalizedString>(experience, "levelLabel").Value + "1";
            foreach (GameOverUI over in root.GetComponentsInChildren<GameOverUI>(true))
            {
                string[] labels = { "survivalLabel", "levelLabel", "killsLabel", "buildingsLabel", "coinsLabel", "gemsLabel" };
                string[] targets = { "survivalTimeText", "levelReachedText", "enemiesKilledText", "buildingsDestroyedText", "coinsCollectedText", "diamondsCollectedText" };
                for (int i = 0; i < labels.Length; i++) { TMP_Text text = Get<TMP_Text>(over, targets[i]); if (text != null) text.text = Get<LocalizedString>(over, labels[i]).Format(i == 0 ? (object)"15:43" : 123); }
            }
            foreach (UIModalVisibility modal in root.GetComponentsInChildren<UIModalVisibility>())
                foreach (GameObject target in Get<GameObject[]>(modal, "obscuredObjects")) if (target != null) target.SetActive(false);
            foreach (LevelUpManager level in root.GetComponentsInChildren<LevelUpManager>(true))
            {
                Get<TMP_Text>(level, "levelUpText").text = Get<LocalizedString>(level, "levelTitle").Format(5);
                SetActive(Get<TMP_Text>(level, "closeInstructionText").transform, false);
                SetActive(Get<TMP_Text>(level, "cooldownWarningText").transform, false);
            }
        }
        if (panel == "GraphicsSettingsPanel")
        {
            GraphicsSettingsMenu menu = canvas.GetComponentsInChildren<GraphicsSettingsMenu>(true).First();
            SerializedProperty rows = new SerializedObject(menu).FindProperty("rows");
            string[] values = language == GameLanguage.Spanish ? new[] { "Equilibrado", "60 FPS", "Desactivado", "Solo en la build", "Solo en la build", "100%", "Completo", "Activado", "Activado", "Activado", "Activado" } : new[] { "Balanced", "60 FPS", "Off", "In build only", "In build only", "100%", "Full", "On", "On", "On", "On" };
            for (int i = 0; i < rows.arraySize; i++) ((TMP_Text)rows.GetArrayElementAtIndex(i).FindPropertyRelative("value").objectReferenceValue).text = values[i];
            Get<TMP_Text>(menu, "syncHint").text = Get<LocalizedString>(menu, "fpsHint").Value;
            Get<TMP_Text>(menu, "statusText").text = Get<LocalizedString>(menu, "applyHint").Value;
        }
        if (panel == "ChestAnnouncement" || panel == "OvertimeAlert" || panel == "OverrideActivationHUD")
        {
            string folder = panel == "OverrideActivationHUD" ? "Overrides" : "Notifications";
            GameObject root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/" + folder + "/" + panel + ".prefab"));
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true)) if (!transform.name.Contains("Ghost")) transform.gameObject.SetActive(true);
            foreach (CanvasGroup group in root.GetComponentsInChildren<CanvasGroup>(true)) group.alpha = 1;
            foreach (LocalizedText text in root.GetComponentsInChildren<LocalizedText>(true)) text.ApplyLanguage(language);
            if (panel == "OverrideActivationHUD")
            {
                OverrideActivationHUD hud = root.GetComponent<OverrideActivationHUD>();
                Get<TMP_Text>(hud, "titleText").text = Get<LocalizedString>(hud, "activatedMessage").Get(language);
                Get<TMP_Text>(hud, "nameText").text = language == GameLanguage.Spanish ? "Pulso Electromagnético" : "Electromagnetic pulse";
                Get<TMP_Text>(hud, "nameText").transform.parent.gameObject.SetActive(false);
                Get<RectTransform>(hud, "bannerIconRect").gameObject.SetActive(false);
            }
        }
        string tag = language + "-" + size.x + "-" + panel;
        UIStylePreview.Capture(scene, "Logs/UIReview/" + tag + ".png", size.x, size.y, report, tag);
    }
    private static T Get<T>(Object component, string field) => (T)component.GetType().GetField(field, Fields).GetValue(component);
    private static void SetActive(Transform target, bool active) { if (target != null) target.gameObject.SetActive(active); }
}
