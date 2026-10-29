using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class OverrideActivationHUD : MonoBehaviour, IUpdateable
{
    private const float BannerIconSize = 96f;

    private static OverrideActivationHUD instance;
    private static bool isQuitting;

    public static bool Exists => instance != null;

    [SerializeField] private float cellSize = 54f;

    [Header("Ritmo del aviso")]
    [SerializeField] private LocalizedString activatedMessage = new LocalizedString("OVERRIDE ACTIVATED", "SOBRECARGA ACTIVADA");
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

    [SerializeField] private Canvas canvas;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform strip;
    [SerializeField] private RectTransform banner;
    [SerializeField] private RectTransform bannerIconRect;
    [SerializeField] private Image bannerIcon;
    [SerializeField] private TextMeshProUGUI bannerFallback;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private CanvasGroup titleGroup;
    [SerializeField] private GlitchTextUI titleGlitch;
    [SerializeField] private Image titlePlate;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private CanvasGroup nameGroup;
    [SerializeField] private GlitchTextUI nameGlitch;
    [SerializeField] private Image namePlate;
    [SerializeField] private RectTransform iconGhost;
    [SerializeField] private RectTransform cellPrefab;

    private readonly Dictionary<OverrideData, RectTransform> cells = new Dictionary<OverrideData, RectTransform>();
    private readonly HashSet<OverrideData> announced = new HashSet<OverrideData>();
    private readonly Queue<OverrideData> pending = new Queue<OverrideData>();
    private OverrideData current;
    private Coroutine playing;

    public static bool WillAnnounce(OverrideData overrideData)
    {
        return instance != null && overrideData != null && (instance.current == overrideData || instance.pending.Contains(overrideData));
    }

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

        instance = RuntimeUIPrefabs.Spawn(p => p.overrideActivationHUD);
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

    }

    private void OnEnable()
    {
        UpdateManager.Instance?.Register(this);
        SceneManager.sceneLoaded += OnSceneLoaded;
        OverrideManager.EnsureExists();

        if (OverrideManager.Instance != null)
        {
            OverrideManager.Instance.OnOverrideActivated += OnOverrideActivated;
            OverrideManager.Instance.OnOverrideDeactivated += OnOverrideDeactivated;
        }
    }

    private void OnDisable()
    {
        UpdateManager.Instance?.Unregister(this);
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (OverrideManager.Instance != null)
        {
            OverrideManager.Instance.OnOverrideActivated -= OnOverrideActivated;
            OverrideManager.Instance.OnOverrideDeactivated -= OnOverrideDeactivated;
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

    public bool IsActive => isActiveAndEnabled;

    public void OnUpdate(float deltaTime)
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

    }

    private void OnOverrideActivated(OverrideData overrideData)
    {
        if (overrideData == null || announced.Contains(overrideData)) return;

        announced.Add(overrideData);
        pending.Enqueue(overrideData);
    }

    private void OnOverrideDeactivated(OverrideData overrideData)
    {
        if (overrideData == null) return;

        announced.Remove(overrideData);

        if (cells.TryGetValue(overrideData, out RectTransform cell))
        {
            cells.Remove(overrideData);
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
        announced.Clear();
        current = null;

        foreach (KeyValuePair<OverrideData, RectTransform> pair in cells)
        {
            if (pair.Value != null) Destroy(pair.Value.gameObject);
        }
        cells.Clear();

        if (banner != null)
            banner.gameObject.SetActive(false);
    }

    private IEnumerator PlayAnnouncement(OverrideData overrideData)
    {
        current = overrideData;
        banner.gameObject.SetActive(true);
        banner.localScale = Vector3.one;

        yield return new WaitForSecondsRealtime(leadIn);

        yield return PlayTextPhase(titleText, titleGroup, titleGlitch, titlePlate, activatedMessage.Value, titleHold);
        yield return new WaitForSecondsRealtime(phaseGap);

        yield return PlayTextPhase(nameText, nameGroup, nameGlitch, namePlate, overrideData.overrideName, nameHold);
        yield return new WaitForSecondsRealtime(phaseGap);

        ApplyIcon(bannerIcon, bannerFallback, overrideData.icon);
        bannerIconRect.gameObject.SetActive(true);
        bannerIconRect.localScale = Vector3.zero;

        Sequence enterIcon = DOTween.Sequence().SetUpdate(true);
        enterIcon.Append(bannerIconRect.DOScale(1.3f, iconIn).SetEase(Ease.OutBack));
        enterIcon.Append(bannerIconRect.DOScale(1f, iconIn * 0.35f).SetEase(Ease.InOutSine));
        yield return enterIcon.WaitForCompletion();

        yield return new WaitForSecondsRealtime(iconHold);

        OverrideHudPanel panel = OverrideHudPanel.Instance;
        RectTransform slot = panel != null ? panel.GetResultSlot(overrideData) : null;

        if (slot != null)
            yield return FlyIconToPanel(overrideData, panel, slot);
        else
            yield return FlyIconToStrip(overrideData);

        current = null;
        playing = null;
    }

    private IEnumerator FlyIconToPanel(OverrideData overrideData, OverrideHudPanel panel, RectTransform slot)
    {
        Canvas.ForceUpdateCanvases();

        float slotSize = Mathf.Max(1f, slot.rect.width);
        RectTransform iconRect = panel.GetResultIconRect(overrideData);
        float iconSize = iconRect != null ? iconRect.rect.width : slotSize;

        RectTransform ghost = CreateGhost(overrideData.icon, slotSize);
        ghost.position = bannerIconRect.position;
        ghost.localScale = Vector3.one * (BannerIconSize / slotSize);

        bannerIconRect.gameObject.SetActive(false);

        Sequence flight = DOTween.Sequence().SetUpdate(true);
        flight.Join(ghost.DOMove(ToOverlayPosition(slot), iconFlight).SetEase(Ease.InOutCubic));
        flight.Join(ghost.DOScale(iconSize / slotSize, iconFlight));
        yield return flight.WaitForCompletion();

        ghost.gameObject.SetActive(false);
        banner.gameObject.SetActive(false);

        panel.Land(overrideData);
    }

    private IEnumerator FlyIconToStrip(OverrideData overrideData)
    {
        RectTransform cell = CreateCell(overrideData);
        cell.localScale = Vector3.zero;
        Canvas.ForceUpdateCanvases();

        RectTransform ghost = CreateGhost(overrideData.icon, cellSize);
        ghost.position = bannerIconRect.position;
        ghost.localScale = Vector3.one * (BannerIconSize / cellSize);

        bannerIconRect.gameObject.SetActive(false);

        Sequence flight = DOTween.Sequence().SetUpdate(true);
        flight.Join(ghost.DOMove(cell.position, iconFlight).SetEase(Ease.InOutCubic));
        flight.Join(ghost.DOScale(1f, iconFlight));
        yield return flight.WaitForCompletion();

        ghost.gameObject.SetActive(false);
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
    }

    private RectTransform CreateGhost(Sprite sprite, float size)
    {
        if (iconGhost == null) return null;
        Image image = iconGhost.GetComponent<Image>();
        image.sprite = sprite;
        image.enabled = sprite != null;
        iconGhost.gameObject.SetActive(true);
        return iconGhost;
    }

    private Vector3 ToOverlayPosition(RectTransform target)
    {
        Canvas targetCanvas = target.GetComponentInParent<Canvas>();
        Canvas rootCanvas = targetCanvas != null ? targetCanvas.rootCanvas : null;
        Camera targetCamera = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? rootCanvas.worldCamera : null;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(targetCamera, target.position);
        RectTransformUtility.ScreenPointToWorldPointInRectangle((RectTransform)canvas.transform, screenPoint, null, out Vector3 world);

        return world;
    }

    private IEnumerator PlayTextPhase(TextMeshProUGUI text, CanvasGroup group, GlitchTextUI glitch, Image plate, string content, float hold)
    {
        RectTransform rect = (RectTransform)group.transform;

        group.alpha = 0f;
        rect.localScale = Vector3.one * 0.15f;
        group.gameObject.SetActive(true);
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

        group.gameObject.SetActive(false);
        rect.localScale = Vector3.one;
    }

    private RectTransform CreateCell(OverrideData overrideData)
    {
        if (cells.TryGetValue(overrideData, out RectTransform existing) && existing != null) return existing;
        if (cellPrefab == null) return null;
        RectTransform cell = Instantiate(cellPrefab, strip, false);
        cell.GetComponent<OverrideStripCellUI>().SetIcon(overrideData.icon);
        cells[overrideData] = cell;
        return cell;
    }

    private static void ApplyIcon(Image icon, TextMeshProUGUI fallback, Sprite sprite)
    {
        icon.sprite = sprite;
        icon.enabled = sprite != null;
        fallback.gameObject.SetActive(sprite == null);
    }

}
