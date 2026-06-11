using UnityEngine;
using TMPro;

public class GameTimeUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI gameTimeText;
    [Tooltip("Etiqueta OVERTIME (opcional). Si se deja vacia, se crea automaticamente debajo del cronometro.")]
    [SerializeField] private TextMeshProUGUI overtimeLabel;

    [Header("Display Settings")]
    [SerializeField] private Color timeColor = Color.white;
    [Tooltip("Color del cronometro al entrar en overtime (tiempo agotado).")]
    [SerializeField] private Color overtimeColor = new Color(1f, 0.15f, 0.15f);

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

        if (overtimeLabel != null)
        {
            overtimeLabel.gameObject.SetActive(false);
        }

        if (GameTimeManager.Instance != null)
        {

            if (!GameTimeManager.Instance.IsGameActive)
            {
                GameTimeManager.Instance.StartGame();
            }
        }

        GameEvents.OnGameTimeUpdated += UpdateTimeDisplay;
        GameEvents.OnMatchTimeExpired += EnterOvertime;
    }

    private void OnDestroy()
    {

        GameEvents.OnGameTimeUpdated -= UpdateTimeDisplay;
        GameEvents.OnMatchTimeExpired -= EnterOvertime;
    }

    private void UpdateTimeDisplay(string formattedTime)
    {
        if (gameTimeText != null)
        {
            gameTimeText.text = formattedTime;
        }
    }

    private void EnterOvertime()
    {
        // Cronometro en rojo.
        if (gameTimeText != null)
        {
            gameTimeText.color = overtimeColor;
        }

        // Etiqueta OVERTIME debajo del cronometro (se crea si no se asigno una).
        if (overtimeLabel == null)
        {
            overtimeLabel = CreateOvertimeLabel();
        }

        if (overtimeLabel != null)
        {
            overtimeLabel.text = "OVERTIME";
            overtimeLabel.color = overtimeColor;
            overtimeLabel.gameObject.SetActive(true);
        }
    }

    private TextMeshProUGUI CreateOvertimeLabel()
    {
        if (gameTimeText == null) return null;

        GameObject labelObj = new GameObject("OvertimeLabel");
        labelObj.transform.SetParent(gameTimeText.transform, false);

        TextMeshProUGUI label = labelObj.AddComponent<TextMeshProUGUI>();
        label.font = gameTimeText.font;
        label.fontSharedMaterial = gameTimeText.fontSharedMaterial;
        label.fontSize = gameTimeText.fontSize * 0.55f;
        label.alignment = TextAlignmentOptions.Center;
        label.fontStyle = FontStyles.Bold;
        label.raycastTarget = false;

        // Posicion: justo debajo del cronometro, centrado horizontalmente.
        RectTransform rt = label.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -4f);
        rt.sizeDelta = new Vector2(300f, 40f);

        return label;
    }
}
