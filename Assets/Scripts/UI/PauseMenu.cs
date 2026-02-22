using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject helpPanel;

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

    void Start()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (helpPanel != null)
            helpPanel.SetActive(false);

        DeactivateAllHelpSubPanels();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
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
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    public void OnRestartButtonPressed()
    {
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.ResetSessionCurrency();
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex, LoadSceneMode.Single);
    }

    public void OnReturnToMainMenuButtonPressed()
    {
        Time.timeScale = 1f;
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
}
