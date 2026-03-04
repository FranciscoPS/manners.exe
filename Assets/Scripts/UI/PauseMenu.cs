using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject helpPanel;
    [SerializeField] private GameObject audioPanel;
    [SerializeField] private GameObject gameOverPanel;

    [Header("Help Sub-Panels")]
    [SerializeField] private GameObject movimientoHelpPanel;
    [SerializeField] private GameObject experienciaHelpPanel;
    [SerializeField] private GameObject enemigosHelpPanel;
    [SerializeField] private GameObject mejorasHelpPanel;

    [Header("Opciones de escena")]
    [Tooltip("Índice de la escena del menú principal en Build Settings (por defecto 0)")]
    [SerializeField] private int mainMenuSceneIndex = 0;

    private bool isPaused = false;
    private GameObject currentHelpSubPanel;
    private LevelUpManager levelUpManager;
    private PlayerHealth playerHealth;

    private bool reducedVolumeApplied = false;
    private bool audioSettingsChangedWhilePaused = false;

    void Start()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (helpPanel != null)
            helpPanel.SetActive(false);

        if (audioPanel != null)
            audioPanel.SetActive(false);

        DeactivateAllHelpSubPanels();

        levelUpManager = FindFirstObjectByType<LevelUpManager>();

        playerHealth = FindFirstObjectByType<PlayerHealth>();

        AudioSettingsMenu.AudioSettingsChanged += OnAudioSettingsChanged;
    }

    private void OnDestroy()
    {
        AudioSettingsMenu.AudioSettingsChanged -= OnAudioSettingsChanged;
    }

    private void OnAudioSettingsChanged()
    {

        audioSettingsChangedWhilePaused = true;
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {

            if (levelUpManager != null && levelUpManager.IsLevelUpActive())
            {
                return;
            }

            if (playerHealth != null && playerHealth.IsDead)
            {
                return;
            }

            if (gameOverPanel != null && gameOverPanel.activeSelf)
            {
                return;
            }

            if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialPanelActive)
            {
                return;
            }

            TogglePause();
        }
    }

    public void TogglePause()
    {
        if (isPaused) Resume();
        else Pause();
    }

    private void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;

        if (pausePanel != null)
            pausePanel.SetActive(true);

        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.ReduceVolume();
            reducedVolumeApplied = true;

            audioSettingsChangedWhilePaused = false;
        }
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;

        CloseAllPauseUI();

        if (MusicManager.Instance != null && reducedVolumeApplied)
        {
            if (!audioSettingsChangedWhilePaused)
            {
                MusicManager.Instance.RestoreVolume();
            }
            reducedVolumeApplied = false;
            audioSettingsChangedWhilePaused = false;
        }
    }

    public void OnRestartButtonPressed()
    {
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.ResetSessionCurrency();
        }

        if (GameTimeManager.Instance != null)
        {
            GameTimeManager.Instance.ResetGame();
        }

        Time.timeScale = 1f;

        if (MusicManager.Instance != null && reducedVolumeApplied && !audioSettingsChangedWhilePaused)
        {
            MusicManager.Instance.RestoreVolume();
        }

        ResetPlayerEnemyLayerCollision();
        TutorialManager.MarkSessionRestart();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex, LoadSceneMode.Single);
    }

    public void OnReturnToMainMenuButtonPressed()
    {

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
            if (reducedVolumeApplied && !audioSettingsChangedWhilePaused)
                MusicManager.Instance.RestoreVolume();

            MusicManager.Instance.RestoreVolume();
            MusicManager.Instance.StopMusic();
        }
        TutorialManager.ClearSession();
        SceneManager.LoadScene(mainMenuSceneIndex, LoadSceneMode.Single);
    }

    public void OnHelpButtonPressed()
    {
        isPaused = true;
        Time.timeScale = 0f;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (helpPanel != null)
            helpPanel.SetActive(true);

        DeactivateAllHelpSubPanels();
        currentHelpSubPanel = null;
    }

    public void OnAudioButtonPressed()
    {
        isPaused = true;
        Time.timeScale = 0f;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (audioPanel != null)
            audioPanel.SetActive(true);

        DeactivateAllHelpSubPanels();
        currentHelpSubPanel = null;
    }

    public void OnHelpReturnButtonPressed()
    {
        if (helpPanel != null)
            helpPanel.SetActive(false);

        DeactivateAllHelpSubPanels();
        currentHelpSubPanel = null;

        if (pausePanel != null)
            pausePanel.SetActive(true);

        isPaused = true;
        Time.timeScale = 0f;
    }

    public void OnAudioReturnButtonPressed()
    {
        if (audioPanel != null)
            audioPanel.SetActive(false);

        if (pausePanel != null)
            pausePanel.SetActive(true);

        isPaused = true;
        Time.timeScale = 0f;
    }

    public void OnMovimientoHelpButtonPressed()
    {
        ShowHelpSubPanel(movimientoHelpPanel);
    }

    public void OnExperienciaHelpButtonPressed()
    {
        ShowHelpSubPanel(experienciaHelpPanel);
    }

    public void OnEnemigosHelpButtonPressed()
    {
        ShowHelpSubPanel(enemigosHelpPanel);
    }

    public void OnMejorasHelpButtonPressed()
    {
        ShowHelpSubPanel(mejorasHelpPanel);
    }

    private void ShowHelpSubPanel(GameObject subPanel)
    {
        if (subPanel == null) return;

        if (currentHelpSubPanel == subPanel) return;

        if (currentHelpSubPanel != null)
        {
            currentHelpSubPanel.SetActive(false);
            currentHelpSubPanel = null;
        }

        subPanel.SetActive(true);
        currentHelpSubPanel = subPanel;
    }

    private void DeactivateAllHelpSubPanels()
    {
        if (movimientoHelpPanel != null) movimientoHelpPanel.SetActive(false);
        if (experienciaHelpPanel != null) experienciaHelpPanel.SetActive(false);
        if (enemigosHelpPanel != null) enemigosHelpPanel.SetActive(false);
        if (mejorasHelpPanel != null) mejorasHelpPanel.SetActive(false);
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
    private void CloseAllPauseUI()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (helpPanel != null) helpPanel.SetActive(false);
        if (audioPanel != null) audioPanel.SetActive(false);

        DeactivateAllHelpSubPanels();
        currentHelpSubPanel = null;
    }
}
