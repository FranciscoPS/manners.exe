using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameTimeUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI gameTimeText;
    [Tooltip("Etiqueta OVERTIME guardada en la escena. Ajusta su TMP directamente.")]
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

        if (overtimeLabel != null)
        {
            if (overtimeLabel.transform.parent.name == "OvertimeTab") overtimeLabel.transform.parent.gameObject.SetActive(true);
            overtimeLabel.color = overtimeColor;
            overtimeLabel.gameObject.SetActive(true);
        }
    }

}
