using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class UIAuthoringTests
{
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    [Test]
    public void AuthoredInterfacesKeepReferencesAndCompleteTranslations()
    {
        Assert.DoesNotThrow(() => UIAuthoringTools.Validate());
    }

    [Test]
    public void RenamedSandboxAssetsRemainIndependentAndPairable()
    {
        var result = SandboxDiffTool.Compare(); Assert.GreaterOrEqual(result.pairs.Count, 30);
        foreach (var pair in result.pairs)
        {
            Assert.IsTrue(AssetDatabase.GetAssetPath(pair.production).StartsWith(GameAssetPaths.Production + "/", StringComparison.Ordinal));
            Assert.IsTrue(AssetDatabase.GetAssetPath(pair.sandbox).StartsWith(GameAssetPaths.Sandbox + "/", StringComparison.Ordinal));
            Assert.AreNotSame(pair.production, pair.sandbox);
            if (pair.production is OverrideData identity) Assert.AreEqual(GameAssetPaths.BaseName(identity.name), identity.PersistentId);
        }
        UpgradeDatabase database = AssetDatabase.LoadAssetAtPath<UpgradeDatabase>(SandboxSetupTools.UpgradeDatabasePath);
        foreach (UpgradeData upgrade in database.allUpgrades) Assert.IsTrue(AssetDatabase.GetAssetPath(upgrade).StartsWith(GameAssetPaths.Sandbox + "/", StringComparison.Ordinal));
    }

    [UnityTest]
    public IEnumerator LanguageGraphicsPauseChestAndDamageWorkWithAuthoredObjects()
    {
        bool hadLanguage = PlayerPrefs.HasKey("GameLanguage"); int languagePreference = PlayerPrefs.GetInt("GameLanguage");
        int tutorialPreference = PlayerPrefs.GetInt("TutorialCompleted_v1", 0);
        bool hadTutorial = PlayerPrefs.HasKey("TutorialCompleted_v1");
        PlayerPrefs.DeleteKey("GameLanguage");
        PlayerPrefs.SetInt("TutorialCompleted_v1", 1);
        EditorSceneManager.OpenScene(UIAuthoringTools.Scenes[0], OpenSceneMode.Single);
        yield return new EnterPlayMode();
        try
        {
            yield return new WaitForSecondsRealtime(.25f);
            Assert.AreEqual(GameLanguage.English, GameLocalization.Language);
            MainMenuUIManager menu = Object.FindFirstObjectByType<MainMenuUIManager>();
            GameObject options = Get<GameObject>(menu, "optionsPanel"); options.SetActive(true);
            LanguageSelector selector = options.GetComponentInChildren<LanguageSelector>(true);
            Get<Button>(selector, "nextButton").onClick.Invoke();
            yield return null;
            Assert.AreEqual(GameLanguage.Spanish, GameLocalization.Language);
            Assert.AreEqual("Español", Get<TMP_Text>(selector, "valueLabel").text);
            foreach (LocalizedText text in options.GetComponentsInChildren<LocalizedText>()) Assert.AreEqual(text.Content.Get(GameLanguage.Spanish), text.GetComponent<TMP_Text>().text);
            menu.OnGraphicsButtonPressed(); yield return null;
            GraphicsSettingsMenu graphics = Get<GraphicsSettingsMenu>(menu, "graphicsSettings");
            Assert.IsTrue(graphics.IsOpen);
            var rows = new SerializedObject(graphics).FindProperty("rows");
            Button increase = rows.GetArrayElementAtIndex(1).FindPropertyRelative("next").objectReferenceValue as Button;
            Button decrease = rows.GetArrayElementAtIndex(1).FindPropertyRelative("previous").objectReferenceValue as Button;
            TMP_Text rate = rows.GetArrayElementAtIndex(1).FindPropertyRelative("value").objectReferenceValue as TMP_Text;
            string before = rate.text; increase.onClick.Invoke(); Assert.AreNotEqual(before, rate.text); decrease.onClick.Invoke(); Assert.AreEqual(before, rate.text);
            Get<Button>(graphics, "backButton").onClick.Invoke(); Assert.IsTrue(options.activeSelf);
            SceneManager.LoadScene("LEVEL 1"); yield return new WaitForSecondsRealtime(.25f);
            TutorialManager tutorial = Object.FindFirstObjectByType<TutorialManager>();
            if (tutorial != null && tutorial.IsTutorialPanelActive) Get<Button>(tutorial, "skipAllButton").onClick.Invoke();
            Assert.IsFalse(tutorial != null && tutorial.IsTutorialPanelActive);
            EnemySpawnManager.Instance?.StopAllCoroutines();
            Assert.AreEqual(GameLanguage.Spanish, GameLocalization.Language);
            PauseMenu pause = Object.FindFirstObjectByType<PauseMenu>(); pause.TogglePause();
            Assert.AreEqual(0, Time.timeScale);
            Assert.IsTrue(Get<GameObject>(pause, "pausePanel").activeInHierarchy);
            selector = Get<GameObject>(pause, "pausePanel").GetComponentInChildren<LanguageSelector>();
            Assert.IsNotNull(selector, "La pausa debe mostrar el selector de idioma.");
            Get<Button>(selector, "nextButton").onClick.Invoke(); Assert.AreEqual(GameLanguage.English, GameLocalization.Language);
            pause.OnGraphicsButtonPressed(); yield return null;
            graphics = Get<GraphicsSettingsMenu>(pause, "graphicsSettings"); Assert.IsTrue(graphics.IsOpen);
            Get<Button>(graphics, "backButton").onClick.Invoke(); Assert.IsTrue(Get<GameObject>(pause, "pausePanel").activeSelf);
            pause.Resume(); Assert.AreEqual(1, Time.timeScale);
            ChestAnnouncement.Show(); yield return null;
            ChestAnnouncement chest = Object.FindFirstObjectByType<ChestAnnouncement>(); Assert.IsNotNull(chest);
            TMP_Text chestText = Get<TMP_Text>(chest, "text"); chestText.ForceMeshUpdate(); Assert.IsFalse(chestText.isTextOverflowing);
            GameLocalization.SetLanguage(GameLanguage.Spanish); yield return null; chestText.ForceMeshUpdate(); Assert.IsFalse(chestText.isTextOverflowing); Assert.IsTrue(chestText.text.Contains("COFRE"));
            var registry = RuntimeUIPrefabs.Instance; Assert.IsNotNull(registry); Assert.IsNotNull(registry.chestOpeningSequence);
            FloatingTextManager damage = Object.FindFirstObjectByType<FloatingTextManager>(); Assert.IsNotNull(damage);
            Canvas damageCanvas = Get<Canvas>(damage, "worldCanvas"); int existing = damageCanvas.transform.childCount;
            Vector3 center = Camera.main.transform.position + Camera.main.transform.forward * 10;
            damage.ShowDamage(123, center); damage.ShowDamage(456, center); yield return null;
            FloatingText[] active = damageCanvas.GetComponentsInChildren<FloatingText>(); Assert.AreEqual(2, active.Length); Assert.AreEqual(existing, damageCanvas.transform.childCount);
            Assert.AreNotSame(active[0], active[1]);
            yield return new WaitForSecondsRealtime(Get<float>(chest, "displayDuration"));
            bool completed = false; ChestOpeningSequence.Play(null, null, () => completed = true);
            yield return new WaitForSecondsRealtime(.5f);
            ChestShowcase showcase = Object.FindFirstObjectByType<ChestShowcase>(); Assert.IsNotNull(showcase); Assert.IsTrue(showcase.IsActive);
            Camera chestCamera = Get<Camera>(showcase, "showcaseCamera"); Assert.IsTrue(chestCamera.enabled); Assert.IsNotNull(chestCamera.targetTexture);
            chestCamera.Render();
            Directory.CreateDirectory("Logs/UIReview");
            UIStylePreview.Capture(SceneManager.GetActiveScene(), "Logs/UIReview/play-chest.png", 1920, 1080);
            float deadline = Time.realtimeSinceStartup + 12;
            while (!completed && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsTrue(completed, "La cinemática debe terminar y entregar el control.");
            GameLocalization.SetLanguage(GameLanguage.English);
        }
        finally
        {
            Time.timeScale = 1;
            if (hadLanguage) PlayerPrefs.SetInt("GameLanguage", languagePreference); else PlayerPrefs.DeleteKey("GameLanguage");
            if (hadTutorial) PlayerPrefs.SetInt("TutorialCompleted_v1", tutorialPreference); else PlayerPrefs.DeleteKey("TutorialCompleted_v1");
            PlayerPrefs.Save();
        }
        yield return new ExitPlayMode();
    }
    private static T Get<T>(Object component, string name) => (T)component.GetType().GetField(name, Fields).GetValue(component);
}
