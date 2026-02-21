using UnityEngine;

public class HelpScript : MonoBehaviour
{
    [Header("Panels de ayuda")]
    [SerializeField] private GameObject movimientoPanel;
    [SerializeField] private GameObject experienciaPanel;
    [SerializeField] private GameObject enemigosPanel;
    [SerializeField] private GameObject mejorasPanel;

    [Header("Panel de opciones")]
    [SerializeField] private GameObject optionsPanel;

    [Header("Panel de help")]
    [SerializeField] private GameObject helpPanel;

    private GameObject currentPanel;

    private void Awake()
    {
        SetActivePanel(null);
    }

    public void OnMovimientoButton()
    {
        SetActivePanel(movimientoPanel);
    }

    public void OnExperienciaButton()
    {
        SetActivePanel(experienciaPanel);
    }

    public void OnEnemigosButton()
    {
        SetActivePanel(enemigosPanel);
    }

    public void OnMejorasButton()
    {
        SetActivePanel(mejorasPanel);
    }

    public void OnReturnButtonPressed()
    {
        if (helpPanel != null)
            helpPanel.SetActive(false);

        if (optionsPanel != null)
            optionsPanel.SetActive(true);
    }

    private void SetActivePanel(GameObject panelToShow)
    {
        if (currentPanel == panelToShow) return;

        if (currentPanel != null)
        {
            currentPanel.SetActive(false);
            currentPanel = null;
        }

        if (panelToShow != null)
        {
            if (optionsPanel != null)
                optionsPanel.SetActive(false);

            panelToShow.SetActive(true);
            currentPanel = panelToShow;
        }
    }
}
