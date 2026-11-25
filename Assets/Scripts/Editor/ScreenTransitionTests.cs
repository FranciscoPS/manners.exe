using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class ScreenTransitionTests
{
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    [UnityTest]
    public IEnumerator ReturningToTheMenuFromThePauseTurnsTheScreenOffBeforeLoadingAndBackOnAfterwards()
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
            PauseMenu pause = Object.FindFirstObjectByType<PauseMenu>();
            pause.TogglePause(); yield return null;
            ConfirmDialog dialog = Get<OptionsMenu>(pause, "optionsMenu").Confirmation;
            string level = SceneManager.GetActiveScene().path;
            ScreenTransition transition = Object.FindFirstObjectByType<ScreenTransition>();
            Canvas canvas = Get<Canvas>(transition, "canvas");
            RawImage screen = Get<RawImage>(transition, "screen");

            pause.OnReturnToMainMenuButtonPressed(); yield return null;
            Get<Button>(dialog, "confirmButton").onClick.Invoke();
            yield return null; yield return null;

            Assert.IsTrue(ScreenTransition.IsRunning, "Confirmar inicia la transición de pantalla.");
            Assert.AreEqual(level, SceneManager.GetActiveScene().path, "La escena no cambia mientras la pantalla se apaga.");
            Assert.AreEqual(0f, Time.timeScale, "La partida sigue congelada mientras la pantalla se apaga.");
            Assert.IsTrue(canvas.enabled && screen.gameObject.activeInHierarchy && screen.raycastTarget, "La pantalla de transición cubre y bloquea los clics.");

            float deadline = Time.realtimeSinceStartup + 10f;
            while (ScreenTransition.IsRunning && Time.realtimeSinceStartup < deadline) yield return null;

            Assert.IsFalse(ScreenTransition.IsRunning, "La transición termina.");
            Assert.AreEqual(UIAuthoringTools.Scenes[0], SceneManager.GetActiveScene().path, "Se carga el menú principal.");
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsFalse(canvas.enabled, "Al encenderse la pantalla, la transición se oculta.");
            Assert.IsFalse(screen.gameObject.activeSelf);
        }
        finally
        {
            Time.timeScale = 1;
            if (hadTutorial) PlayerPrefs.SetInt("TutorialCompleted_v1", tutorialPreference); else PlayerPrefs.DeleteKey("TutorialCompleted_v1");
            PlayerPrefs.Save();
        }
        yield return new ExitPlayMode();
    }

    private static T Get<T>(Object component, string name) => (T)component.GetType().GetField(name, Fields).GetValue(component);
}
