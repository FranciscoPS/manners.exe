using UnityEngine;

public class OptionsMenu : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject helpPanel;
    [SerializeField] private GameObject optionsPanel;

    public void OnReturnButtonPressed()
    {
        if (optionsPanel != null)
            optionsPanel.SetActive(false);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
    }

    public void OnHelpButtonPressed()
    {
        if (optionsPanel != null)
            optionsPanel.SetActive(false);

        if (helpPanel != null)
            helpPanel.SetActive(true);
    }
    public void ShowOptions()
    {
        if (optionsPanel != null)
            optionsPanel.SetActive(true);
    }
}
