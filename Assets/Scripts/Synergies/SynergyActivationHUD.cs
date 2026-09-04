using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class SynergyActivationHUD : MonoBehaviour
{
    private static SynergyActivationHUD instance;
    private static bool isQuitting;

    [Header("Tipografía")]
    [Tooltip("Nombre (o inicio del nombre) del TMP_FontAsset del juego a usar. Se busca entre las fuentes ya cargadas por la escena.")]
    [SerializeField] private string preferredFontName = "CyberpunkCraftpixPixel";
    [SerializeField] private float titleFontSize = 50f;
    [SerializeField] private float nameFontSize = 40f;
    [Tooltip("Grosor del contorno oscuro para que el texto se lea sobre el juego sin cuadro de fondo.")]
    [SerializeField] private float outlineWidth = 0.22f;
    [SerializeField] private Color outlineColor = new Color(0.02f, 0.02f, 0.08f, 1f);

    [Header("Posición (debajo de la barra de XP y el timer)")]
    [Tooltip("Distancia desde el borde superior a la tira de iconos activos, en px de referencia 1920x1080.")]
    [SerializeField] private float stripTopOffset = 196f;
    [Tooltip("Distancia desde el borde superior al centro del aviso.")]
    [SerializeField] private float bannerTopOffset = 300f;

    [Header("Tira de sinergias activas")]
    [SerializeField] private float cellSize = 54f;
    [SerializeField] private float cellSpacing = 10f;

    [Header("Ritmo del aviso")]
    [SerializeField] private string activatedMessage = "SINERGIA ACTIVADA";
    [Tooltip("Pausa antes de que aparezca el primer texto.")]
    [SerializeField] private float leadIn = 0.3f;
    [Tooltip("Duración de la entrada (pequeño → grande) de cada texto.")]
    [SerializeField] private float textIn = 0.65f;
    [Tooltip("Tiempo que cada texto se mantiene a tamaño completo.")]
    [SerializeField] private float titleHold = 1.7f;
    [SerializeField] private float nameHold = 1.9f;
    [Tooltip("Duración de la salida (grande → pequeño) de cada texto.")]
    [SerializeField] private float textOut = 0.45f;
    [Tooltip("Pausa entre una fase y la siguiente.")]
    [SerializeField] private float phaseGap = 0.25f;
    [Tooltip("Duración del pop de entrada del icono.")]
    [SerializeField] private float iconIn = 0.55f;
    [Tooltip("Cuánto se mantiene el icono en el centro antes de volar a la tira.")]
    [SerializeField] private float iconHold = 0.7f;
    [SerializeField] private float iconFlight = 0.65f;
    [SerializeField] private Color titleColor = new Color(1f, 0.95f, 0.6f, 1f);
    [SerializeField] private Color cellBackdropColor = new Color(0.04f, 0.05f, 0.14f, 0.8f);

    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private RectTransform strip;
    private RectTransform banner;
    private RectTransform bannerIconRect;
    private Image bannerIcon;
    private TextMeshProUGUI bannerFallback;
    private TextMeshProUGUI titleText;
    private CanvasGroup titleGroup;
    private GlitchTextUI titleGlitch;
    private TextMeshProUGUI nameText;
    private CanvasGroup nameGroup;
    private GlitchTextUI nameGlitch;
    private TMP_FontAsset resolvedFont;

    private readonly Dictionary<SynergyData, RectTransform> cells = new Dictionary<SynergyData, RectTransform>();
    private readonly Queue<SynergyData> pending = new Queue<SynergyData>();
    private Coroutine playing;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        isQuitting = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (isQuitting || instance != null) return;

        GameObject go = new GameObject("SynergyActivationHUD");
        instance = go.AddComponent<SynergyActivationHUD>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        BuildUI();
        ApplyFont();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SynergyManager.EnsureExists();

        if (SynergyManager.Instance != null)
        {
            SynergyManager.Instance.OnSynergyActivated += OnSynergyActivated;
            SynergyManager.Instance.OnSynergyDeactivated += OnSynergyDeactivated;
        }
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (SynergyManager.Instance != null)
        {
            SynergyManager.Instance.OnSynergyActivated -= OnSynergyActivated;
            SynergyManager.Instance.OnSynergyDeactivated -= OnSynergyDeactivated;
        }
    }

    private void OnApplicationQuit()
    {
        isQuitting = true;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void Update()
    {
        if (canvasGroup == null) return;

        float target = Time.timeScale > 0f ? 1f : 0f;
        canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, target, Time.unscaledDeltaTime * 6f);

        if (playing == null && pending.Count > 0 && Time.timeScale > 0f)
            playing = StartCoroutine(PlayAnnouncement(pending.Dequeue()));
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ClearAll();
        ApplyFont();
    }

    private void OnSynergyActivated(SynergyData synergy)
    {
        if (synergy == null || cells.ContainsKey(synergy) || pending.Contains(synergy)) return;
        pending.Enqueue(synergy);
    }

    private void OnSynergyDeactivated(SynergyData synergy)
    {
        if (synergy == null) return;

        if (cells.TryGetValue(synergy, out RectTransform cell))
        {
            cells.Remove(synergy);
            if (cell != null) Destroy(cell.gameObject);
        }
    }

    private void ClearAll()
    {
        if (playing != null)
        {
            StopCoroutine(playing);
            playing = null;
        }

        pending.Clear();

        foreach (KeyValuePair<SynergyData, RectTransform> pair in cells)
        {
            if (pair.Value != null) Destroy(pair.Value.gameObject);
        }
        cells.Clear();

        if (banner != null)
            banner.gameObject.SetActive(false);
    }

    private void ApplyFont()
    {
        if (resolvedFont == null)
        {
            resolvedFont = Resources.FindObjectsOfTypeAll<TMP_FontAsset>()
                .FirstOrDefault(f => f != null && f.name.StartsWith(preferredFontName));
        }

        if (resolvedFont == null) return;

        StyleGameText(titleText);
        StyleGameText(nameText);
        StyleGameText(bannerFallback);

        foreach (RectTransform cell in cells.Values)
        {
            if (cell == null) continue;
            TextMeshProUGUI fallback = cell.GetComponentInChildren<TextMeshProUGUI>(true);
            if (fallback != null) StyleGameText(fallback);
        }
    }

    private void StyleGameText(TextMeshProUGUI tmp)
    {
        if (tmp == null || resolvedFont == null) return;

        tmp.font = resolvedFont;
        tmp.outlineWidth = outlineWidth;
        tmp.outlineColor = outlineColor;
    }

    private void BuildUI()
    {
        GameObject canvasObj = new GameObject("SynergyActivationCanvas");
        canvasObj.transform.SetParent(transform, false);

        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 150;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasGroup = canvasObj.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        strip = CreateRect("ActiveSynergiesStrip", canvasObj.transform, new Vector2(0.5f, 1f), new Vector2(0f, -stripTopOffset), new Vector2(0f, cellSize));
        HorizontalLayoutGroup layout = strip.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = cellSpacing;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = strip.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        BuildBanner(canvasObj.transform);
    }

    private void BuildBanner(Transform parent)
    {
        banner = CreateRect("ActivationBanner", parent, new Vector2(0.5f, 1f), new Vector2(0f, -bannerTopOffset), new Vector2(1200f, 140f));
        banner.pivot = new Vector2(0.5f, 0.5f);

        bannerIconRect = CreateRect("Icon", banner, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(96f, 96f));
        bannerIcon = bannerIconRect.gameObject.AddComponent<Image>();
        bannerIcon.preserveAspect = true;
        bannerIcon.raycastTarget = false;

        bannerFallback = CreateText("IconFallback", bannerIconRect, 64f, FontStyles.Bold, titleColor);
        Stretch(bannerFallback.rectTransform);
        bannerFallback.text = "?";

        titleText = CreateText("Title", banner, titleFontSize, FontStyles.Bold, titleColor);
        Stretch(titleText.rectTransform);
        titleGroup = titleText.gameObject.AddComponent<CanvasGroup>();
        titleGlitch = titleText.gameObject.AddComponent<GlitchTextUI>();

        nameText = CreateText("Name", banner, nameFontSize, FontStyles.Bold, Color.white);
        Stretch(nameText.rectTransform);
        nameGroup = nameText.gameObject.AddComponent<CanvasGroup>();
        nameGlitch = nameText.gameObject.AddComponent<GlitchTextUI>();

        bannerIconRect.gameObject.SetActive(false);
        titleText.gameObject.SetActive(false);
        nameText.gameObject.SetActive(false);
        banner.gameObject.SetActive(false);
    }

    private IEnumerator PlayAnnouncement(SynergyData synergy)
    {
        banner.gameObject.SetActive(true);
        banner.localScale = Vector3.one;

        yield return new WaitForSecondsRealtime(leadIn);

        yield return PlayTextPhase(titleText, titleGroup, titleGlitch, activatedMessage, titleHold);
        yield return new WaitForSecondsRealtime(phaseGap);

        yield return PlayTextPhase(nameText, nameGroup, nameGlitch, synergy.synergyName, nameHold);
        yield return new WaitForSecondsRealtime(phaseGap);

        ApplyIcon(bannerIcon, bannerFallback, synergy.icon);
        bannerIconRect.gameObject.SetActive(true);
        bannerIconRect.localScale = Vector3.zero;

        Sequence enterIcon = DOTween.Sequence().SetUpdate(true);
        enterIcon.Append(bannerIconRect.DOScale(1.3f, iconIn).SetEase(Ease.OutBack));
        enterIcon.Append(bannerIconRect.DOScale(1f, iconIn * 0.35f).SetEase(Ease.InOutSine));
        yield return enterIcon.WaitForCompletion();

        yield return new WaitForSecondsRealtime(iconHold);

        RectTransform cell = CreateCell(synergy);
        cell.localScale = Vector3.zero;
        Canvas.ForceUpdateCanvases();

        RectTransform ghost = CreateRect("IconGhost", canvas.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(cellSize, cellSize));
        Image ghostImage = ghost.gameObject.AddComponent<Image>();
        ghostImage.preserveAspect = true;
        ghostImage.raycastTarget = false;
        ghostImage.sprite = synergy.icon;
        ghostImage.enabled = synergy.icon != null;
        ghost.position = bannerIconRect.position;
        ghost.localScale = Vector3.one * (96f / cellSize);

        bannerIconRect.gameObject.SetActive(false);

        Sequence flight = DOTween.Sequence().SetUpdate(true);
        flight.Join(ghost.DOMove(cell.position, iconFlight).SetEase(Ease.InOutCubic));
        flight.Join(ghost.DOScale(1f, iconFlight));
        yield return flight.WaitForCompletion();

        Destroy(ghost.gameObject);
        banner.gameObject.SetActive(false);

        Sequence landing = DOTween.Sequence().SetUpdate(true);
        landing.Append(cell.DOScale(1.25f, 0.2f).SetEase(Ease.OutBack));
        landing.Append(cell.DOScale(1f, 0.15f));
        yield return landing.WaitForCompletion();

        PremiumUpgradeVisuals visuals = cell.GetComponent<PremiumUpgradeVisuals>();
        if (visuals != null)
        {
            visuals.SetPulseEnabled(false);
            visuals.SetPremium(true);
        }

        playing = null;
    }

    private IEnumerator PlayTextPhase(TextMeshProUGUI text, CanvasGroup group, GlitchTextUI glitch, string content, float hold)
    {
        RectTransform rect = text.rectTransform;

        group.alpha = 0f;
        rect.localScale = Vector3.one * 0.15f;
        text.gameObject.SetActive(true);
        glitch.SetText(content);

        Sequence enter = DOTween.Sequence().SetUpdate(true);
        enter.Join(group.DOFade(1f, textIn * 0.6f));
        enter.Join(rect.DOScale(1.1f, textIn).SetEase(Ease.OutBack));
        enter.Append(rect.DOScale(1f, textIn * 0.3f).SetEase(Ease.InOutSine));
        yield return enter.WaitForCompletion();

        yield return new WaitForSecondsRealtime(hold);

        Sequence exit = DOTween.Sequence().SetUpdate(true);
        exit.Join(rect.DOScale(0.15f, textOut).SetEase(Ease.InBack));
        exit.Join(group.DOFade(0f, textOut * 0.85f));
        yield return exit.WaitForCompletion();

        text.gameObject.SetActive(false);
        rect.localScale = Vector3.one;
    }

    private RectTransform CreateCell(SynergyData synergy)
    {
        if (cells.TryGetValue(synergy, out RectTransform existing) && existing != null)
            return existing;

        RectTransform cell = CreateRect(synergy.name, strip, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(cellSize, cellSize));

        Image backdrop = cell.gameObject.AddComponent<Image>();
        backdrop.color = cellBackdropColor;
        backdrop.raycastTarget = false;

        RectTransform iconRect = CreateRect("Icon", cell, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(cellSize - 12f, cellSize - 12f));
        Image icon = iconRect.gameObject.AddComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        TextMeshProUGUI fallback = CreateText("IconFallback", iconRect, 30f, FontStyles.Bold, titleColor);
        Stretch(fallback.rectTransform);
        fallback.text = "?";
        StyleGameText(fallback);

        ApplyIcon(icon, fallback, synergy.icon);

        PremiumUpgradeVisuals visuals = cell.gameObject.AddComponent<PremiumUpgradeVisuals>();
        visuals.SetPulseEnabled(false);

        cells[synergy] = cell;
        return cell;
    }

    private static void ApplyIcon(Image icon, TextMeshProUGUI fallback, Sprite sprite)
    {
        icon.sprite = sprite;
        icon.enabled = sprite != null;
        fallback.gameObject.SetActive(sprite == null);
    }

    private static RectTransform CreateRect(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, anchor.y >= 1f ? 1f : 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;

        return rt;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, float size, FontStyles style, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.raycastTarget = false;

        return tmp;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
