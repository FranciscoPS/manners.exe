using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour, IUpdateable
{
    [Header("UI")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject gameOverPanel;
    [Tooltip("Menú de opciones compartido con el menú principal (prefab OptionsMenu). Desde la pausa se abre completo o directamente en las sobrecargas.")]
    [SerializeField] private OptionsMenu optionsMenu;

    [Header("Opciones de escena")]
    [Tooltip("Índice de la escena del menú principal en Build Settings (por defecto 0)")]
    [SerializeField] private int mainMenuSceneIndex = 0;

    [Header("Confirmación antes de salir de la partida")]
    [SerializeField] private LocalizedString restartTitle = new LocalizedString("Restart the run?", "¿Reiniciar la partida?");
    [SerializeField] private LocalizedString restartMessage = new LocalizedString("You will lose the progress of this run.", "Perderás el progreso de esta partida.");
    [SerializeField] private LocalizedString restartConfirm = new LocalizedString("Restart", "Reiniciar");
    [SerializeField] private LocalizedString mainMenuTitle = new LocalizedString("Back to the main menu?", "¿Volver al menú principal?");
    [SerializeField] private LocalizedString mainMenuMessage = new LocalizedString("You will lose the progress of this run.", "Perderás el progreso de esta partida.");
    [SerializeField] private LocalizedString mainMenuConfirm = new LocalizedString("Main menu", "Menú principal");

    private bool isPaused = false;
    private LevelUpManager levelUpManager;
    private PlayerHealth playerHealth;

    private bool reducedVolumeApplied = false;
    private bool audioSettingsChangedWhilePaused = false;

    void Start()
    {
        if (optionsMenu != null)
        {
            optionsMenu.Closed += OnOptionsClosed;
            optionsMenu.AudioClosed += OnAudioClosed;
        }

        if (pausePanel != null)
            pausePanel.SetActive(false);

        levelUpManager = FindFirstObjectByType<LevelUpManager>();

        playerHealth = FindFirstObjectByType<PlayerHealth>();

        AudioSettingsMenu.AudioSettingsChanged += OnAudioSettingsChanged;
    }

    private void OnDestroy()
    {
        AudioSettingsMenu.AudioSettingsChanged -= OnAudioSettingsChanged;
        if (optionsMenu != null)
        {
            optionsMenu.Closed -= OnOptionsClosed;
            optionsMenu.AudioClosed -= OnAudioClosed;
        }
    }

    private void OnAudioSettingsChanged()
    {
        audioSettingsChangedWhilePaused = true;
    }

    public bool IsActive => isActiveAndEnabled;
    private void OnEnable() => UpdateManager.Instance?.Register(this);
    private void OnDisable() => UpdateManager.Instance?.Unregister(this);

    public void OnUpdate(float deltaTime)
    {
        if (ScreenTransition.IsRunning) return;

        if ((optionsMenu != null && (optionsMenu.IsOpen || optionsMenu.IsConfirming)) || GraphicsSettingsMenu.ConsumedBackThisFrame
            || AccessibilitySettingsMenu.ConsumedBackThisFrame || OptionsMenu.ConsumedBackThisFrame || ConfirmDialog.ConsumedBackThisFrame)
            return;

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
        if (!Confirm(restartTitle, restartMessage, restartConfirm, RestartRun)) RestartRun();
    }

    public void OnReturnToMainMenuButtonPressed()
    {
        if (!Confirm(mainMenuTitle, mainMenuMessage, mainMenuConfirm, ReturnToMainMenu)) ReturnToMainMenu();
    }

    private bool Confirm(LocalizedString title, LocalizedString message, LocalizedString confirm, System.Action action)
    {
        ConfirmDialog dialog = optionsMenu != null ? optionsMenu.Confirmation : null;
        if (dialog == null) return false;
        dialog.Show(title, message, confirm, action);
        return true;
    }

    private void RestartRun()
    {
        ScreenTransition.LoadScene(SceneManager.GetActiveScene().buildIndex, PrepareRestart);
    }

    private void PrepareRestart()
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
    }

    private void ReturnToMainMenu()
    {
        if (MusicManager.Instance != null)
            MusicManager.Instance.StopMusic();

        ScreenTransition.LoadScene(mainMenuSceneIndex, PrepareMainMenu);
    }

    private void PrepareMainMenu()
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
        }
        TutorialManager.ClearSession();
    }

    public void OnOptionsButtonPressed()
    {
        if (!OpenOptions()) return;
        optionsMenu.Open();
    }

    public void OnOverridesButtonPressed()
    {
        if (!OpenOptions()) return;
        optionsMenu.OpenOverrides();
    }

    public void OnGraphicsButtonPressed()
    {
        if (!OpenOptions()) return;
        optionsMenu.ShowGraphics();
    }

    public void OnAudioButtonPressed()
    {
        if (!OpenOptions()) return;
        optionsMenu.ShowAudio();
    }

    private bool OpenOptions()
    {
        if (optionsMenu == null) return false;

        isPaused = true;
        Time.timeScale = 0f;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        return true;
    }

    private void OnOptionsClosed()
    {
        if (!isPaused) return;

        if (MusicManager.Instance != null && reducedVolumeApplied)
            MusicManager.Instance.ReduceVolume();

        audioSettingsChangedWhilePaused = false;

        if (pausePanel != null)
            pausePanel.SetActive(true);

        Time.timeScale = 0f;
    }

    private void OnAudioClosed()
    {
        if (!isPaused) return;

        if (MusicManager.Instance != null && reducedVolumeApplied)
            MusicManager.Instance.ReduceVolume();

        audioSettingsChangedWhilePaused = false;
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
        if (optionsMenu != null && optionsMenu.IsOpen) optionsMenu.Close();

        if (pausePanel != null) pausePanel.SetActive(false);
    }
}
