using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameTimeUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI gameTimeText;
    [Tooltip("Etiqueta OVERTIME guardada en la escena. Ajusta su TMP directamente.")]
    [SerializeField] private TextMeshProUGUI overtimeLabel;
    [Tooltip("Icono del reloj de la placa. En overtime pasa a su color de alerta.")]
    [SerializeField] private Graphic glyph;

    [Header("Display Settings")]
    [SerializeField] private Color timeColor = Color.white;
    [Tooltip("Color del cronometro al entrar en overtime (tiempo agotado).")]
    [SerializeField] private Color overtimeColor = new Color(1f, 0.15f, 0.15f);
    [Tooltip("Color del icono del reloj en overtime.")]
    [SerializeField] private Color overtimeGlyphColor = new Color(1f, 0.35f, 0.27f);

    [Header("Placa de fondo")]
    [Tooltip("Placa detrás del cronómetro. Cambia de color y da un golpe junto con el texto al entrar en overtime.")]
    [SerializeField] private Graphic plate;
    [SerializeField] private Color overtimePlateColor = new Color(1f, 0.29f, 0.18f);

    private string lastFormattedTime;

    private void Awake()
    {
        if (gameTimeText == null)
        {
            gameTimeText = GetComponent<TextMeshProUGUI>();
        }
    }

    private void OnEnable()
    {
        GameLocalization.LanguageChanged += ApplyTime;
        ApplyTime();
    }

    private void OnDisable()
    {
        GameLocalization.LanguageChanged -= ApplyTime;
    }

    private void Start()
    {
        if (gameTimeText != null)
        {
            gameTimeText.color = timeColor;
        }

        if (overtimeLabel != null)
        {
            overtimeLabel.gameObject.SetActive(false);
        }

        GameTimeManager manager = GameTimeManager.Instance;
        if (manager != null && !manager.IsGameActive)
        {
            manager.StartGame();
        }

        GameEvents.OnGameTimeUpdated += UpdateTimeDisplay;
        GameEvents.OnMatchTimeExpired += EnterOvertime;

        if (manager != null && manager.IsOvertime) EnterOvertime();
        ApplyTime();
    }

    private void OnDestroy()
    {
        GameEvents.OnGameTimeUpdated -= UpdateTimeDisplay;
        GameEvents.OnMatchTimeExpired -= EnterOvertime;
    }

    private void UpdateTimeDisplay(string formattedTime)
    {
        lastFormattedTime = formattedTime;
        if (gameTimeText != null)
        {
            gameTimeText.text = formattedTime;
        }
    }

    private void ApplyTime()
    {
        if (gameTimeText == null) return;

        GameTimeManager manager = GameTimeManager.Instance;
        string value = manager != null && manager.IsGameActive ? manager.GetDisplayTime() : lastFormattedTime;
        if (!string.IsNullOrEmpty(value)) gameTimeText.text = value;
    }

    private void EnterOvertime()
    {
        if (gameTimeText != null)
        {
            gameTimeText.color = overtimeColor;
        }

        if (glyph != null)
        {
            glyph.color = overtimeGlyphColor;
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
