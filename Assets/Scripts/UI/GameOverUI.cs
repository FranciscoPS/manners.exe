using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using DG.Tweening;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class GameOverUI : MonoBehaviour, IUpdateable
{
    [Header("Textos editables por idioma")]
    [SerializeField] private LocalizedString survivalLabel = new LocalizedString("Time survived: {0}", "Tiempo sobrevivido: {0}");
    [SerializeField] private LocalizedString levelLabel = new LocalizedString("Level reached: {0}", "Nivel alcanzado: {0}");
    [SerializeField] private LocalizedString killsLabel = new LocalizedString("Enemies defeated: {0}", "Enemigos eliminados: {0}");
    [SerializeField] private LocalizedString buildingsLabel = new LocalizedString("Buildings destroyed: {0}", "Edificios destruidos: {0}");
    [SerializeField] private LocalizedString coinsLabel = new LocalizedString("Coins collected: {0}", "Monedas recolectadas: {0}");
    [SerializeField] private LocalizedString gemsLabel = new LocalizedString("Gems collected: {0}", "Gemas recolectadas: {0}");
    [SerializeField] private LocalizedString newOverride = new LocalizedString("New override discovered!", "¡Nueva sobrecarga descubierta!");
    [SerializeField] private LocalizedString newOverridesMessage = new LocalizedString("{0} new overrides discovered!", "¡{0} sobrecargas nuevas descubiertas!");
    [SerializeField] private LocalizedString newHint = new LocalizedString("New override hint found!", "¡Nueva pista de sobrecarga encontrada!");
    [SerializeField] private LocalizedString newHints = new LocalizedString("{0} new override hints found!", "¡{0} pistas de sobrecarga nuevas!");

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 1f;

    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Panel Reference")]
    [SerializeField] private GameObject gameOverPanel;

    [Header("Leaderboard / Iniciales")]
    [SerializeField] private GameObject initialsEntryPanel;
    [SerializeField] private InitialsEntryUI initialsEntryUI;

    [Header("Game Over Stats UI")]
    [SerializeField] private TextMeshProUGUI survivalTimeText;
    [SerializeField] private TextMeshProUGUI levelReachedText;
    [SerializeField] private TextMeshProUGUI enemiesKilledText;
    [SerializeField] private TextMeshProUGUI buildingsDestroyedText;
    [SerializeField] private TextMeshProUGUI coinsCollectedText;
    [SerializeField] private TextMeshProUGUI diamondsCollectedText;

    [Header("Sobrecargas")]
    [Tooltip("Aviso que aparece solo si en esta partida se descubrió una pista o una sobrecarga nueva.")]
    [SerializeField, FormerlySerializedAs("synergyDiscoveryText")] private TextMeshProUGUI overrideDiscoveryText;

    private bool isTransitioning = false;
    private bool statsUpdated = false;
    private bool qualificationChecked;

    [SerializeField] private GameObject fadeOverlay;
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private CanvasGroup gameOverCanvasGroup;

    public bool IsActive => isActiveAndEnabled;
    public void OnUpdate(float deltaTime)
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
            qualificationChecked = false;
        }
    }

    private void UpdateGameOverStats()
    {
        if (GameSessionStats.Instance == null)
        {
            return;
        }

        if (!qualificationChecked)
        {
            qualificationChecked = true;
            StartCoroutine(CheckLeaderboardQualification());
        }

        if (survivalTimeText != null)
        {
            survivalTimeText.text = survivalLabel.Format(GameSessionStats.Instance.GetFormattedSurvivalTime());
        }

        if (levelReachedText != null)
        {
            levelReachedText.text = levelLabel.Format(GameSessionStats.Instance.MaxLevelReached);
        }

        if (enemiesKilledText != null)
        {
            enemiesKilledText.text = killsLabel.Format(GameSessionStats.Instance.EnemiesKilled);
        }

        if (buildingsDestroyedText != null)
        {
            buildingsDestroyedText.text = buildingsLabel.Format(GameSessionStats.Instance.BuildingsDestroyed);
        }

        if (coinsCollectedText != null)
        {
            coinsCollectedText.text = coinsLabel.Format(GameSessionStats.Instance.CoinsCollected);
        }

        if (diamondsCollectedText != null)
        {
            diamondsCollectedText.text = gemsLabel.Format(GameSessionStats.Instance.DiamondsCollected);
        }

        UpdateOverrideDiscovery();
    }

    private void UpdateOverrideDiscovery()
    {
        if (overrideDiscoveryText == null) return;

        int newOverrides = OverrideDiscovery.NewOverridesThisRun;
        int newPieces = OverrideDiscovery.NewPiecesThisRun;

        if (newOverrides > 0)
            overrideDiscoveryText.text = newOverrides == 1 ? newOverride.Value : newOverridesMessage.Format(newOverrides);
        else if (newPieces > 0)
            overrideDiscoveryText.text = newPieces == 1 ? newHint.Value : newHints.Format(newPieces);

        overrideDiscoveryText.gameObject.SetActive(newOverrides > 0 || newPieces > 0);
    }

    private void SetGameOverVisible(bool visible)
    {
        if (gameOverPanel == null) return;

        if (gameOverCanvasGroup == null) return;

        gameOverCanvasGroup.alpha = visible ? 1f : 0f;
        gameOverCanvasGroup.interactable = visible;
        gameOverCanvasGroup.blocksRaycasts = visible;
    }

    private IEnumerator CheckLeaderboardQualification()
    {
        bool fetchDone = false;
        List<LeaderboardEntry> top = null;

        GlobalLeaderboardService.Instance.FetchTop(
            entries => { top = entries; fetchDone = true; },
            () => { fetchDone = true; }
        );

        yield return new WaitUntil(() => fetchDone);

        if (top == null || initialsEntryPanel == null || initialsEntryUI == null)
            yield break;

        int maxEntries = LeaderboardConfig.Instance != null ? LeaderboardConfig.Instance.MaxEntries : 5;
        float survivalTime = GameSessionStats.Instance.SurvivalTime;
        bool qualifies = top.Count < maxEntries || survivalTime > top[top.Count - 1].SurvivalTime;

        if (!qualifies)
            yield break;

        string initials = null;
        void OnConfirmed(string value) => initials = value;

        initialsEntryUI.OnInitialsConfirmed += OnConfirmed;
        SetGameOverVisible(false);
        initialsEntryPanel.SetActive(true);

        yield return new WaitUntil(() => initials != null);

        initialsEntryUI.OnInitialsConfirmed -= OnConfirmed;
        initialsEntryPanel.SetActive(false);
        SetGameOverVisible(true);

        var entry = new LeaderboardEntry
        {
            Initials = initials,
            SurvivalTime = survivalTime,
            Level = GameSessionStats.Instance.MaxLevelReached,
            Kills = GameSessionStats.Instance.EnemiesKilled,
            Coins = GameSessionStats.Instance.CoinsCollected,
            Gems = GameSessionStats.Instance.DiamondsCollected
        };

        GlobalLeaderboardService.Instance.SubmitAndRefreshTop(entry, (list, rank) => { }, () => { });
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

        if (fadeOverlay == null || fadeCanvasGroup == null) return;
        DontDestroyOnLoad(fadeOverlay);

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

        if (fadeOverlay == null || fadeCanvasGroup == null) return;
        DontDestroyOnLoad(fadeOverlay);

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

    private void OnEnable() { GameLocalization.LanguageChanged += RefreshLanguage; UpdateManager.Instance?.Register(this); }
    private void OnDisable() { GameLocalization.LanguageChanged -= RefreshLanguage; UpdateManager.Instance?.Unregister(this); }
    private void RefreshLanguage()
    {
        if (gameOverPanel != null && gameOverPanel.activeSelf) UpdateGameOverStats();
    }
}
