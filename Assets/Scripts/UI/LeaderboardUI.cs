using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using DG.Tweening;

public class LeaderboardUI : MonoBehaviour
{
    [Header("Referencias (se auto-detectan si se dejan vacías)")]
    [Tooltip("Texto donde se listan los 5 puntajes. Hijo 'Scores'.")]
    [SerializeField] private TextMeshProUGUI leaderboardText;
    [Tooltip("Título del panel. Hijo 'Title'.")]
    [SerializeField] private TextMeshProUGUI titleText;
    [Tooltip("Panel completo (para el pulse). Si está vacío usa este RectTransform.")]
    [SerializeField] private RectTransform panelRect;
    [Tooltip("Línea de leyenda vieja ('T: Tiempo | N: Nivel...'). Hijo 'Instructions'. Se oculta para dar aire.")]
    [SerializeField] private GameObject legendObject;

    [Header("Título")]
    [SerializeField] private string titleString = "Top 5 jugadores";
    [SerializeField] private bool titlePulse = true;
    [SerializeField] private float titlePulseScale = 1.18f;
    [SerializeField] private float titlePulseDuration = 0.8f;
    [Tooltip("Oculta la leyenda inferior para que no se encime con los puntajes.")]
    [SerializeField] private bool hideLegend = true;

    [Header("Efecto RGB")]
    [SerializeField] private bool rgbTitle = true;
    [Tooltip("También cicla el color base de las filas (solo afecta al texto sin etiqueta de color).")]
    [SerializeField] private bool rgbEntries = false;
    [Tooltip("Velocidad del ciclo de color (ciclos por segundo aprox).")]
    [SerializeField] private float rgbSpeed = 0.35f;
    [SerializeField][Range(0f, 1f)] private float rgbSaturation = 0.85f;
    [SerializeField][Range(0f, 1f)] private float rgbValue = 1f;

    [Header("Pulse del panel completo (opcional)")]
    [SerializeField] private bool panelPulse = false;
    [SerializeField] private float panelPulseScale = 1.03f;
    [SerializeField] private float panelPulseDuration = 1.6f;

    [Header("Filas")]
    [Tooltip("Espacio extra entre filas (unidades TMP). 0 = sin cambios.")]
    [SerializeField] private float entryLineSpacing = 8f;
    [Tooltip("Ajusta el tamaño de fuente para que las 5 filas siempre quepan.")]
    [SerializeField] private bool autoSizeEntries = true;

    private bool _started;
    private float _hue;
    private Tween _titleTween;
    private Tween _panelTween;
    private RectTransform _titleRect;

    // Colores de medalla para el top 3 + resto.
    private static readonly Color Gold = new Color(1f, 0.84f, 0.0f);
    private static readonly Color Silver = new Color(0.80f, 0.83f, 0.88f);
    private static readonly Color Bronze = new Color(0.85f, 0.55f, 0.25f);
    private static readonly Color Rest = new Color(0.82f, 0.86f, 0.95f);

    private void Awake()
    {
        ResolveReferences();
    }

    private void ResolveReferences()
    {
        if (titleText == null)
        {
            Transform t = transform.Find("Title");
            if (t != null) titleText = t.GetComponent<TextMeshProUGUI>();
        }
        if (legendObject == null)
        {
            Transform t = transform.Find("Instructions");
            if (t != null) legendObject = t.gameObject;
        }
        if (panelRect == null) panelRect = GetComponent<RectTransform>();
        if (titleText != null) _titleRect = titleText.rectTransform;
    }

    private void Start()
    {
        _started = true;

        if (titleText != null)
            titleText.text = titleString;

        if (hideLegend && legendObject != null)
            legendObject.SetActive(false);

        StyleEntries();
        Refresh();
        StartAnimations();
    }

    private void OnEnable()
    {
        // Solo refresca/anima si ya pasó Start (panel activado en runtime).
        if (_started)
        {
            Refresh();
            StartAnimations();
        }
    }

    private void OnDisable()
    {
        StopAnimations();
    }

    private void OnDestroy()
    {
        StopAnimations();
    }

    private void Update()
    {
        if (!_started) return;
        if (!rgbTitle && !rgbEntries) return;

        _hue += Time.unscaledDeltaTime * rgbSpeed;
        if (_hue >= 1f) _hue -= 1f;

        if (rgbTitle && titleText != null)
            titleText.color = Color.HSVToRGB(_hue, rgbSaturation, rgbValue);

        if (rgbEntries && leaderboardText != null)
            leaderboardText.color = Color.HSVToRGB((_hue + 0.5f) % 1f, rgbSaturation, rgbValue);
    }

    private void StyleEntries()
    {
        if (leaderboardText == null) return;

        leaderboardText.richText = true;
        leaderboardText.alignment = TextAlignmentOptions.Left;

        if (entryLineSpacing != 0f)
            leaderboardText.lineSpacing = entryLineSpacing;

        if (autoSizeEntries)
        {
            leaderboardText.enableAutoSizing = true;
            leaderboardText.fontSizeMin = 16f;
            leaderboardText.fontSizeMax = 44f;
        }
    }

    private void StartAnimations()
    {
        StopAnimations();

        if (titlePulse && _titleRect != null)
        {
            _titleRect.localScale = Vector3.one;
            _titleTween = _titleRect.DOScale(titlePulseScale, titlePulseDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);
        }

        if (panelPulse && panelRect != null)
        {
            panelRect.localScale = Vector3.one;
            _panelTween = panelRect.DOScale(panelPulseScale, panelPulseDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);
        }
    }

    private void StopAnimations()
    {
        _titleTween?.Kill();
        _panelTween?.Kill();
        _titleTween = null;
        _panelTween = null;
        if (_titleRect != null) _titleRect.localScale = Vector3.one;
        if (panelRect != null) panelRect.localScale = Vector3.one;
    }

    public void Refresh()
    {
        if (leaderboardText == null) return;

        List<LeaderboardEntry> entries = LeaderboardManager.Instance != null
            ? LeaderboardManager.Instance.LoadEntries()
            : new List<LeaderboardEntry>();

        var sb = new StringBuilder();
        for (int i = 0; i < 5; i++)
        {
            if (i < entries.Count)
                sb.AppendLine(FormatEntry(i + 1, entries[i]));
            else
                sb.AppendLine(FormatEmpty(i + 1));
        }

        leaderboardText.text = sb.ToString().TrimEnd();
    }

    private static string RankHex(int rank)
    {
        Color c = rank == 1 ? Gold : rank == 2 ? Silver : rank == 3 ? Bronze : Rest;
        return ColorUtility.ToHtmlStringRGB(c);
    }

    private static string FormatEntry(int rank, LeaderboardEntry e)
    {
        // Rango con color de medalla; tiempo y stats al mismo tamaño.
        string hex = RankHex(rank);
        string time = LeaderboardManager.FormatTime(e.SurvivalTime);
        return $"<color=#{hex}><b>{rank}</b></color>   <b>{time}</b>   " +
               $"<color=#FFFFFFCC>Nv {e.Level} · {e.Kills} kills</color>";
    }

    private static string FormatEmpty(int rank)
    {
        return $"<color=#FFFFFF44><b>{rank}</b>   --:--</color>";
    }
}
