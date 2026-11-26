using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class GameCursorTests
{
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    private const float Step = 0.02f;

    [Test]
    public void TheCursorScalesWithTheResolutionAndOnlyGrowsWhenItWouldFallBelowTheMinimum()
    {
        float atTenEighty = GameCursor.MinimumSizeScale(36f, 1f, 22f);
        float atFourK = GameCursor.MinimumSizeScale(36f, 2f, 22f);
        float inATinyWindow = GameCursor.MinimumSizeScale(36f, 0.5f, 22f);

        Assert.AreEqual(1f, atTenEighty);
        Assert.AreEqual(1f, atFourK);
        Assert.AreEqual(22f / 18f, inATinyWindow, 0.0001f);
    }

    [UnityTest]
    public IEnumerator HoveringAButtonShowsTheHandAndLeavingItBringsBackTheArrow()
    {
        EditorSceneManager.OpenScene(UIAuthoringTools.Scenes[0], OpenSceneMode.Single);
        yield return new EnterPlayMode();
        yield return new WaitForSecondsRealtime(.25f);
        GameCursor cursor = Object.FindFirstObjectByType<GameCursor>();
        Assert.IsNotNull(cursor, "El cursor se crea solo desde RuntimeUIPrefabs.");
        cursor.enabled = false;
        Button play = FindVisibleButton();
        Assert.IsNotNull(play, "El menú principal tiene al menos un botón visible.");
        Vector2 overButton = ScreenCenter(play);
        CanvasGroup handGroup = Get<CanvasGroup>(cursor, "handGroup");
        CanvasGroup arrowGroup = Get<CanvasGroup>(cursor, "arrowGroup");

        for (int i = 0; i < 20; i++) cursor.Refresh(overButton, false, false, Step);

        Assert.IsTrue(cursor.IsPointing, "Sobre un botón el cursor es la manita.");
        Assert.Greater(handGroup.alpha, 0.9f);
        Assert.Less(arrowGroup.alpha, 0.1f);

        for (int i = 0; i < 20; i++) cursor.Refresh(new Vector2(-50f, -50f), false, false, Step);

        Assert.IsFalse(cursor.IsPointing, "Fuera de los botones vuelve la flecha.");
        Assert.Less(handGroup.alpha, 0.1f);
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator HoveringAPauseMenuButtonShowsOnlyTheHand()
    {
        bool hadTutorial = PlayerPrefs.HasKey("TutorialCompleted_v1");
        int tutorialPreference = PlayerPrefs.GetInt("TutorialCompleted_v1", 0);
        PlayerPrefs.SetInt("TutorialCompleted_v1", 1);
        EditorSceneManager.OpenScene(UIAuthoringTools.Scenes[1], OpenSceneMode.Single);
        yield return new EnterPlayMode();
        try
        {
            yield return new WaitForSecondsRealtime(.25f);
            EnemySpawnManager.Instance?.StopAllCoroutines();
            GameCursor cursor = Object.FindFirstObjectByType<GameCursor>();
            cursor.enabled = false;
            PauseMenu pause = Object.FindFirstObjectByType<PauseMenu>();
            pause.TogglePause();
            yield return new WaitForSecondsRealtime(.2f);
            Button button = FindVisibleButton(Get<GameObject>(pause, "pausePanel"));
            Assert.IsNotNull(button, "La pausa tiene al menos un botón visible.");
            Vector2 overButton = ScreenCenter(button);
            CanvasGroup handGroup = Get<CanvasGroup>(cursor, "handGroup");
            CanvasGroup arrowGroup = Get<CanvasGroup>(cursor, "arrowGroup");

            for (int i = 0; i < 20; i++) cursor.Refresh(overButton, false, false, Step);

            Assert.AreEqual(0f, Time.timeScale, "La partida sigue en pausa.");
            Assert.IsTrue(cursor.IsPointing, "Sobre un botón de la pausa el cursor es la manita.");
            Assert.Greater(handGroup.alpha, 0.9f);
            Assert.Less(arrowGroup.alpha, 0.1f, "La flecha no queda encima de la manita en la pausa.");
        }
        finally
        {
            Time.timeScale = 1;
            if (hadTutorial) PlayerPrefs.SetInt("TutorialCompleted_v1", tutorialPreference); else PlayerPrefs.DeleteKey("TutorialCompleted_v1");
            PlayerPrefs.Save();
        }
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator MovingTheMouseInTheMenuKeepsTheChosenFrameRateAndRestingGoesBackToThirty()
    {
        EditorSceneManager.OpenScene(UIAuthoringTools.Scenes[0], OpenSceneMode.Single);
        yield return new EnterPlayMode();
        yield return new WaitForSecondsRealtime(.25f);
        GameCursor cursor = Object.FindFirstObjectByType<GameCursor>();
        cursor.enabled = false;
        float responsive = Get<float>(cursor, "responsiveSeconds");
        int chosen = GameGraphicsSettings.Current.frameRate;
        MethodInfo tick = typeof(GameGraphicsSettings).GetMethod("Tick", BindingFlags.Static | BindingFlags.NonPublic);
        typeof(GameGraphicsSettings).GetMethod("SetFocus", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { true });
        cursor.Refresh(new Vector2(10f, 30f), false, false, Step);
        yield return new WaitForSecondsRealtime(responsive + .5f);
        tick.Invoke(null, null);
        Assert.AreEqual(Mathf.Min(30, chosen), Application.targetFrameRate, "El menú en reposo corre a 30 FPS.");

        cursor.Refresh(new Vector2(12f, 34f), false, false, Step);
        tick.Invoke(null, null);

        Assert.AreEqual(chosen, Application.targetFrameRate, "Mover el mouse en el menú usa los FPS elegidos en Gráficos.");

        yield return new WaitForSecondsRealtime(responsive + .5f);
        tick.Invoke(null, null);

        Assert.AreEqual(Mathf.Min(30, chosen), Application.targetFrameRate, "Sin mover el mouse el menú vuelve a 30 FPS.");
        yield return new ExitPlayMode();
    }

    private static Button FindVisibleButton(GameObject root = null)
    {
        Button[] buttons = root != null ? root.GetComponentsInChildren<Button>() : Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
        foreach (Button button in buttons)
            if (button.isActiveAndEnabled && button.IsInteractable() && button.GetComponentInParent<Canvas>().renderMode == RenderMode.ScreenSpaceOverlay)
                return button;
        return null;
    }

    private static Vector2 ScreenCenter(Button button)
    {
        RectTransform rect = (RectTransform)button.transform;
        return RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
    }

    private static T Get<T>(Object component, string name) => (T)component.GetType().GetField(name, Fields).GetValue(component);
}
