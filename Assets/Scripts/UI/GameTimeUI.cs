using UnityEngine;
using TMPro;

/// <summary>
/// Muestra el tiempo total de la partida actual en la UI
/// </summary>
public class GameTimeUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI gameTimeText;
    
    [Header("Display Settings")]
    [SerializeField] private Color timeColor = Color.white;
    [SerializeField] private bool showHoursIfNeeded = true;
    
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
    }
    
    private void Update()
    {
        if (gameTimeText == null || GameTimeManager.Instance == null)
            return;
        
        UpdateTimeDisplay();
    }
    
    private void UpdateTimeDisplay()
    {
        string timeString = showHoursIfNeeded 
            ? GameTimeManager.Instance.GetFormattedTimeLong()
            : GameTimeManager.Instance.GetFormattedTime();
        
        gameTimeText.text = timeString;
    }
}
