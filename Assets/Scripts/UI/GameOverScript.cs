using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    [Header("Fade Settings")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 1f;

    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private bool isTransitioning = false;

    private void Awake()
    {
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
        }
    }

    public void Retry()
    {
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.ResetSessionCurrency();
        }

        // Resetear el timer de partida
        if (GameTimeManager.Instance != null)
        {
            GameTimeManager.Instance.ResetGame();
        }

        if (isTransitioning) return;
        isTransitioning = true;

        Time.timeScale = 1f;

        fadeImage.DOFade(1f, fadeDuration).OnComplete(() =>
        {
            ResetPlayerEnemyLayerCollision();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        });
    }

    public void GoToMainMenu()
    {
        if (isTransitioning) return;
        isTransitioning = true;

        Time.timeScale = 1f;

        fadeImage.DOFade(1f, fadeDuration).OnComplete(() =>
        {
            ResetPlayerEnemyLayerCollision();
            SceneManager.LoadScene(mainMenuSceneName);
        });
    }

    private void ResetPlayerEnemyLayerCollision()
    {
        int playerLayer = LayerMask.NameToLayer("Player");
        int enemyLayer = LayerMask.NameToLayer("Enemy");

        if (playerLayer >= 0 && enemyLayer >= 0)
        {
            Physics.IgnoreLayerCollision(playerLayer, enemyLayer, false);
        }
    }
}