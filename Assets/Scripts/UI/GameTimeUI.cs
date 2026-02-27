using UnityEngine;
using TMPro;

/// <summary>
/// Muestra el tiempo total de la partida actual en la UI
/// Refactorizado para usar eventos en vez de polling cada frame
/// </summary>
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
        
        // Forzar inicialización de GameTimeManager y asegurar que esté activo
        if (GameTimeManager.Instance != null)
        {
            // Si el juego no está activo, iniciarlo
            if (!GameTimeManager.Instance.IsGameActive)
            {
                GameTimeManager.Instance.StartGame();
            }
        }
        
        // Suscribirse al evento de tiempo
        GameEvents.OnGameTimeUpdated += UpdateTimeDisplay;
    }
    
    private void OnDestroy()
    {
        // Desuscribirse del evento
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
