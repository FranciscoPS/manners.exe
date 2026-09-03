using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class GameOverUI : MonoBehaviour
{
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

    [Header("Sinergias")]
    [Tooltip("Aviso que aparece solo si en esta partida se descubrió una pista o una sinergia nueva.")]
    [SerializeField] private TextMeshProUGUI synergyDiscoveryText;

    private bool isTransitioning = false;
    private bool statsUpdated = false;

    private GameObject fadeOverlay;
    private CanvasGroup fadeCanvasGroup;
    private CanvasGroup gameOverCanvasGroup;

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

        StartCoroutine(CheckLeaderboardQualification());

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
            buildingsDestroyedText.text = $"Edificios destruidos: {GameSessionStats.Instance.BuildingsDestroyed}";
        }

        if (coinsCollectedText != null)
        {
            coinsCollectedText.text = $"Monedas recolectadas: {GameSessionStats.Instance.CoinsCollected}";
        }

        if (diamondsCollectedText != null)
        {
            diamondsCollectedText.text = $"Gemas recolectadas: {GameSessionStats.Instance.DiamondsCollected}";
        }

        UpdateSynergyDiscovery();
    }

    private void UpdateSynergyDiscovery()
    {
        if (synergyDiscoveryText == null) return;

        int newSynergies = SynergyDiscovery.NewSynergiesThisRun;
        int newPieces = SynergyDiscovery.NewPiecesThisRun;

        if (newSynergies > 0)
            synergyDiscoveryText.text = newSynergies == 1 ? "¡Nueva sinergia descubierta!" : $"¡{newSynergies} sinergias nuevas descubiertas!";
        else if (newPieces > 0)
            synergyDiscoveryText.text = newPieces == 1 ? "¡Nueva pista de sinergia encontrada!" : $"¡{newPieces} pistas de sinergia nuevas!";

        synergyDiscoveryText.gameObject.SetActive(newSynergies > 0 || newPieces > 0);
    }

    private void SetGameOverVisible(bool visible)
    {
        if (gameOverPanel == null) return;

        if (gameOverCanvasGroup == null)
        {
            gameOverCanvasGroup = gameOverPanel.GetComponent<CanvasGroup>();
            if (gameOverCanvasGroup == null)
                gameOverCanvasGroup = gameOverPanel.AddComponent<CanvasGroup>();
        }

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
