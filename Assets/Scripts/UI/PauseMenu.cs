using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject helpPanel;
    [SerializeField] private GameObject audioPanel; 
    [SerializeField] private GameObject gameOverPanel; // Para detectar si estamos en game over

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

    // Estado para manejar la reducción/restauración de volumen sin sobrescribir cambios del usuario
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
        
        // Buscar LevelUpManager para verificar si la tienda/level up están activos
        levelUpManager = FindFirstObjectByType<LevelUpManager>();
        
        // Buscar PlayerHealth para verificar si el jugador está muerto
        playerHealth = FindFirstObjectByType<PlayerHealth>();

        // Suscribirse al evento de cambios de audio
        AudioSettingsMenu.AudioSettingsChanged += OnAudioSettingsChanged;
    }

    private void OnDestroy()
    {
        AudioSettingsMenu.AudioSettingsChanged -= OnAudioSettingsChanged;
    }

    private void OnAudioSettingsChanged()
    {
        // Marcar que el usuario modificó los sliders mientras estaba pausado
        audioSettingsChangedWhilePaused = true;
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            // No permitir abrir el menú de pausa si la tienda o level up están activos
            if (levelUpManager != null && levelUpManager.IsLevelUpActive())
            {
                return;
            }
            
            // No permitir abrir el menú de pausa si el jugador está muerto
            if (playerHealth != null && playerHealth.IsDead)
            {
                return;
            }
            
            // No permitir abrir el menú de pausa si el panel de game over está activo
            if (gameOverPanel != null && gameOverPanel.activeSelf)
            {
                return;
            }

            // No permitir abrir el menú de pausa mientras el panel del tutorial está visible
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

        // Bajar el volumen de la música
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.ReduceVolume();
            reducedVolumeApplied = true;
            // reset flag cada vez que abrimos pausa
            audioSettingsChangedWhilePaused = false;
        }
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;

        CloseAllPauseUI();

        // Restaurar el volumen de la música sólo si no se cambiaron los ajustes
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

        // Resetear el timer de partida
        if (GameTimeManager.Instance != null)
        {
            GameTimeManager.Instance.ResetGame();
        }

        Time.timeScale = 1f;
        
        // Restaurar volumen antes de reiniciar (si no hubo cambios)
        if (MusicManager.Instance != null && reducedVolumeApplied && !audioSettingsChangedWhilePaused)
        {
            MusicManager.Instance.RestoreVolume();
        }
        
        // CRÍTICO: Resetear colisiones Player-Enemy antes de recargar
        // Esto previene el bug de invulnerabilidad al reiniciar
        ResetPlayerEnemyLayerCollision();
        
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex, LoadSceneMode.Single);
    }

    public void OnReturnToMainMenuButtonPressed()
    {
        // Resetear currency y stats antes de ir al main menu
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
        
        // Restaurar y detener la música del juego antes de volver al menú (si no hubo cambios)
        if (MusicManager.Instance != null)
        {
            if (reducedVolumeApplied && !audioSettingsChangedWhilePaused)
                MusicManager.Instance.RestoreVolume();

            MusicManager.Instance.RestoreVolume(); // asegurar estado consistente al salir a main menu
            MusicManager.Instance.StopMusic();
        }
        
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
