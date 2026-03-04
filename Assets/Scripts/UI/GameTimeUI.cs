using UnityEngine;
using TMPro;

public class GameTimeUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI gameTimeText;

    [Header("Display Settings")]
    [SerializeField] private Color timeColor = Color.white;

    private void Start()
    {
        if (gameTimeText == null)
        {
            gameTimeText = GetComponent<TextMeshProUGUI>();
        }

        if (gameTimeText != null)
        {
            gameTimeText.color = timeColor;
        }

        if (GameTimeManager.Instance != null)
        {

            if (!GameTimeManager.Instance.IsGameActive)
            {
                GameTimeManager.Instance.StartGame();
            }
        }

        GameEvents.OnGameTimeUpdated += UpdateTimeDisplay;
    }

    private void OnDestroy()
    {

        GameEvents.OnGameTimeUpdated -= UpdateTimeDisplay;
    }

    private void UpdateTimeDisplay(string formattedTime)
    {
        if (gameTimeText != null)
        {
            gameTimeText.text = formattedTime;
        }
    }
}
