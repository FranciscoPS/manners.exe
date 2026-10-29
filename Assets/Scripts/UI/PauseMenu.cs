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

    private bool isPaused = false;
    private LevelUpManager levelUpManager;
    private PlayerHealth playerHealth;

    private bool reducedVolumeApplied = false;
    private bool audioSettingsChangedWhilePaused = false;

    void Start()
    {
        if (optionsMenu != null) optionsMenu.Closed += OnOptionsClosed;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        levelUpManager = FindFirstObjectByType<LevelUpManager>();

        playerHealth = FindFirstObjectByType<PlayerHealth>();

        AudioSettingsMenu.AudioSettingsChanged += OnAudioSettingsChanged;
    }

    private void OnDestroy()
    {
        AudioSettingsMenu.AudioSettingsChanged -= OnAudioSettingsChanged;
        if (optionsMenu != null) optionsMenu.Closed -= OnOptionsClosed;
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
        if ((optionsMenu != null && optionsMenu.IsOpen) || GraphicsSettingsMenu.ConsumedBackThisFrame || OptionsMenu.ConsumedBackThisFrame)
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
