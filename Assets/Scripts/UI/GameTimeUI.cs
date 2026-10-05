using UnityEngine;
using UnityEngine.UI;
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

    [Header("Placa de fondo")]
    [Tooltip("Placa detrás del cronómetro. Cambia de color y da un golpe junto con el texto al entrar en overtime.")]
    [SerializeField] private Graphic plate;
    [SerializeField] private Color overtimePlateColor = new Color(1f, 0.29f, 0.18f);

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

        if (gameTimeText != null)
        {
            gameTimeText.color = overtimeColor;
        }

        if (plate != null)
        {
            plate.color = overtimePlateColor;
            plate.rectTransform.Punch();
        }

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
        label.fontStyle = gameTimeText.fontStyle;
        label.characterSpacing = gameTimeText.characterSpacing;
        label.raycastTarget = false;

        RectTransform rt = label.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -8f);
        rt.sizeDelta = new Vector2(300f, 40f);

        return label;
    }
}
