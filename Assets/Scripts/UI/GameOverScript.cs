using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class GameOverUI : MonoBehaviour
{
    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 1f;

    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Panel Reference")]
    [SerializeField] private GameObject gameOverPanel;

    [Header("Game Over Stats UI")]
    [SerializeField] private TextMeshProUGUI survivalTimeText;
    [SerializeField] private TextMeshProUGUI levelReachedText;
    [SerializeField] private TextMeshProUGUI enemiesKilledText;
    [SerializeField] private TextMeshProUGUI buildingsDestroyedText;
    [SerializeField] private TextMeshProUGUI coinsCollectedText;
    [SerializeField] private TextMeshProUGUI diamondsCollectedText;

    [Header("Upgrade Levels UI")]
    [SerializeField] private TextMeshProUGUI damageUpgradeLevelText;
    [SerializeField] private TextMeshProUGUI attackSpeedUpgradeLevelText;
    [SerializeField] private TextMeshProUGUI attackRangeUpgradeLevelText;
    [SerializeField] private TextMeshProUGUI moveSpeedUpgradeLevelText;
    [SerializeField] private TextMeshProUGUI magnetRangeUpgradeLevelText;
    [SerializeField] private TextMeshProUGUI multiShotUpgradeLevelText;
    [SerializeField] private TextMeshProUGUI explosiveShotUpgradeLevelText;
    [SerializeField] private TextMeshProUGUI knockbackUpgradeLevelText;

    private bool isTransitioning = false;
    private bool statsUpdated = false;

    private GameObject fadeOverlay;
    private CanvasGroup fadeCanvasGroup;

    private void Update()
    {

        if (gameOverPanel != null && gameOverPanel.activeSelf && !statsUpdated)
        {

            if (GameSessionStats.Instance != null && GameSessionStats.Instance.SurvivalTimeUpdated > 0.1f)
            {
                statsUpdated = true;

                GameSessionStats.Instance.EndSession();

                if (MusicManager.Instance != null)
                {
                    MusicManager.Instance.ReduceVolume();
                }

                UpdateGameOverStats();
            }
        }

        if (gameOverPanel != null && !gameOverPanel.activeSelf && statsUpdated)
        {
            statsUpdated = false;
        }
    }

    private void UpdateGameOverStats()
    {
        if (GameSessionStats.Instance == null)
        {
            return;
        }

        if (LeaderboardManager.Instance != null)
        {
            LeaderboardManager.Instance.SubmitEntry(
                GameSessionStats.Instance.SurvivalTime,
                GameSessionStats.Instance.MaxLevelReached,
                GameSessionStats.Instance.EnemiesKilled,
                GameSessionStats.Instance.CoinsCollected,
                GameSessionStats.Instance.DiamondsCollected
            );
        }

        if (survivalTimeText != null)
        {
            survivalTimeText.text = $"Tiempo sobrevivido: {GameSessionStats.Instance.GetFormattedSurvivalTime()}";
        }

        if (levelReachedText != null)
        {
            levelReachedText.text = $"Nivel alcanzado: {GameSessionStats.Instance.MaxLevelReached}";
        }

        if (enemiesKilledText != null)
        {
            enemiesKilledText.text = $"Enemigos eliminados: {GameSessionStats.Instance.EnemiesKilled}";
        }

        if (buildingsDestroyedText != null)
        {
            buildingsDestroyedText.text = $"Edificios destruidos : {GameSessionStats.Instance.BuildingsDestroyed}";
        }

        if (coinsCollectedText != null)
        {
            coinsCollectedText.text = $"Monedas recolectadas: {GameSessionStats.Instance.CoinsCollected}";
        }

        if (diamondsCollectedText != null)
        {
            diamondsCollectedText.text = $"Gemas recolectadas: {GameSessionStats.Instance.DiamondsCollected}";
        }

        UpdateUpgradeLevels();
    }

    private void UpdateUpgradeLevels()
    {
        Dictionary<UpgradeType, int> upgradeLevels = GameSessionStats.Instance.GetUpgradeLevels();

        if (damageUpgradeLevelText != null)
        {
            int level = upgradeLevels.ContainsKey(UpgradeType.Damage) ? upgradeLevels[UpgradeType.Damage] : 0;
            damageUpgradeLevelText.text = level.ToString();
        }

        if (attackSpeedUpgradeLevelText != null)
        {
            int level = upgradeLevels.ContainsKey(UpgradeType.AttackSpeed) ? upgradeLevels[UpgradeType.AttackSpeed] : 0;
            attackSpeedUpgradeLevelText.text = level.ToString();
        }

        if (attackRangeUpgradeLevelText != null)
        {
            int level = upgradeLevels.ContainsKey(UpgradeType.AttackRange) ? upgradeLevels[UpgradeType.AttackRange] : 0;
            attackRangeUpgradeLevelText.text = level.ToString();
        }

        if (moveSpeedUpgradeLevelText != null)
        {
            int level = upgradeLevels.ContainsKey(UpgradeType.MoveSpeed) ? upgradeLevels[UpgradeType.MoveSpeed] : 0;
            moveSpeedUpgradeLevelText.text = level.ToString();
        }

        if (magnetRangeUpgradeLevelText != null)
        {
            int level = upgradeLevels.ContainsKey(UpgradeType.MagnetRange) ? upgradeLevels[UpgradeType.MagnetRange] : 0;
            magnetRangeUpgradeLevelText.text = level.ToString();
        }

        if (multiShotUpgradeLevelText != null)
        {
            int level = upgradeLevels.ContainsKey(UpgradeType.MultiShot) ? upgradeLevels[UpgradeType.MultiShot] : 0;
            multiShotUpgradeLevelText.text = level.ToString();
        }

        if (explosiveShotUpgradeLevelText != null)
        {
            int level = upgradeLevels.ContainsKey(UpgradeType.ExplosiveShot) ? upgradeLevels[UpgradeType.ExplosiveShot] : 0;
            explosiveShotUpgradeLevelText.text = level.ToString();
        }

        if (knockbackUpgradeLevelText != null)
        {
            int level = upgradeLevels.ContainsKey(UpgradeType.Knockback) ? upgradeLevels[UpgradeType.Knockback] : 0;
            knockbackUpgradeLevelText.text = level.ToString();
        }
    }

    public void Retry()
    {
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.ResetSessionCurrency();
        }

        if (GameTimeManager.Instance != null)
        {
            GameTimeManager.Instance.ResetGame();
        }

        if (GameSessionStats.Instance != null)
        {
            GameSessionStats.Instance.ResetStats();
        }

        if (PlayerStatsManager.Instance != null)
        {
            PlayerStatsManager.Instance.ResetUpgrades();
        }

        if (isTransitioning) return;
        isTransitioning = true;

        Time.timeScale = 1f;

        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.RestoreVolume();
        }

        CreateFadeOverlayIfNeeded();

        fadeCanvasGroup.alpha = 0f;
        fadeOverlay.SetActive(true);

        fadeCanvasGroup.DOFade(1f, fadeDuration).OnComplete(() =>
        {
            ResetPlayerEnemyLayerCollision();
            TutorialManager.MarkSessionRestart();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex, LoadSceneMode.Single);
        });
    }

    public void GoToMainMenu()
    {
        if (isTransitioning) return;
        isTransitioning = true;

        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.ResetSessionCurrency();
        }

        if (GameSessionStats.Instance != null)
        {
            GameSessionStats.Instance.ResetStats();
        }

        if (PlayerStatsManager.Instance != null)
        {
            PlayerStatsManager.Instance.ResetUpgrades();
        }

        Time.timeScale = 1f;

        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.RestoreVolume();
            MusicManager.Instance.StopMusic();
        }

        CreateFadeOverlayIfNeeded();

        fadeCanvasGroup.alpha = 0f;
        fadeOverlay.SetActive(true);

        fadeCanvasGroup.DOFade(1f, fadeDuration).OnComplete(() =>
        {
            ResetPlayerEnemyLayerCollision();
            TutorialManager.ClearSession();
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

    private void CreateFadeOverlayIfNeeded()
    {
        if (fadeCanvasGroup != null && fadeOverlay != null) return;

        fadeOverlay = new GameObject("GameOver_FadeOverlay");
        DontDestroyOnLoad(fadeOverlay);

        Canvas canvas = fadeOverlay.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        fadeOverlay.AddComponent<CanvasScaler>();
        fadeOverlay.AddComponent<GraphicRaycaster>();

        fadeCanvasGroup = fadeOverlay.AddComponent<CanvasGroup>();
        fadeCanvasGroup.alpha = 0f;

        GameObject imgObj = new GameObject("FadeImage");
        imgObj.transform.SetParent(fadeOverlay.transform, false);

        RectTransform rt = imgObj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = imgObj.AddComponent<Image>();
        img.color = Color.black;

        fadeOverlay.AddComponent<FadeOverlayController>();

        fadeOverlay.SetActive(false);
    }
}
