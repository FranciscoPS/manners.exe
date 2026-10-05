using System.Collections;
using UnityEngine;
using TMPro;

public class ChestAnnouncement : MonoBehaviour
{
    private static ChestAnnouncement instance;
    private static bool isQuitting = false;

    [Header("Timing")]
    [Tooltip("Cuánto tiempo (seg) permanece visible el mensaje.")]
    [SerializeField] private float displayDuration = 2.5f;
    [Tooltip("Cuántas veces por segundo pulsa (grande/pequeño). Más bajo = más lento.")]
    [SerializeField] private float pulseFrequency = 0.8f;
    [Tooltip("Escala mínima y máxima del pulso.")]
    [SerializeField] private float pulseMinScale = 0.9f;
    [SerializeField] private float pulseMaxScale = 1.15f;

    [Header("Style")]
    [SerializeField] private int fontSize = 54;
    [SerializeField] private Color textColor = new Color(1f, 0.85f, 0.2f);
    [SerializeField] private float verticalAnchor = 0.78f;

    private TMP_Text text;
    private RectTransform textRect;
    private CanvasGroup group;
    private UnityEngine.UI.Image plate;
    private Coroutine routine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        isQuitting = false;
    }

    public static void Show(string message)
    {
        if (isQuitting) return;
        EnsureExists();
        instance.ShowInternal(message);
    }

    private static void EnsureExists()
    {
        if (instance != null) return;

        GameObject go = new GameObject("ChestAnnouncement");
        instance = go.AddComponent<ChestAnnouncement>();
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

    private void BuildUI()
    {
        GameObject canvasObj = new GameObject("ChestAnnouncementCanvas");
        canvasObj.transform.SetParent(transform, false);

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        var scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        UIStyle style = UIStyle.Instance;

        GameObject bannerObj = new GameObject("AnnouncementBanner", typeof(RectTransform));
        bannerObj.transform.SetParent(canvasObj.transform, false);
        group = bannerObj.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;

        textRect = (RectTransform)bannerObj.transform;
        textRect.anchorMin = new Vector2(0.5f, verticalAnchor);
        textRect.anchorMax = new Vector2(0.5f, verticalAnchor);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = new Vector2(1400f, 200f);

        if (style != null)
        {
            plate = style.CreatePlate("Plate", bannerObj.transform, style.panel, style.skew, true, null);
            plate.GetComponent<UnityEngine.UI.Shadow>().effectColor = style.yellow;
            plate.rectTransform.anchorMin = plate.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        }

        GameObject textObj = new GameObject("AnnouncementText");
        textObj.transform.SetParent(bannerObj.transform, false);

        text = textObj.AddComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.fontSize = fontSize;
        text.color = textColor;
        text.fontStyle = FontStyles.Bold;
        text.raycastTarget = false;

        if (style != null)
        {
            style.ApplyText(text, UITextRole.Title);
            text.color = style.yellow;
            text.characterSpacing = 3f;
            text.extraPadding = true;
        }

        RectTransform labelRect = text.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        bannerObj.SetActive(false);
    }

    private void ShowInternal(string message)
    {
        if (text == null) return;

        text.text = message;

        if (plate != null)
        {
            textRect.gameObject.SetActive(true);
            text.ForceMeshUpdate();
            Vector2 size = text.GetPreferredValues(message, textRect.sizeDelta.x - 120f, 0f);
            plate.rectTransform.sizeDelta = new Vector2(size.x + fontSize * 2.2f, size.y + fontSize * 0.7f);
        }

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(AnimateRoutine());
    }

    private IEnumerator AnimateRoutine()
    {
        textRect.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < displayDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = (Mathf.Sin(elapsed * pulseFrequency * Mathf.PI * 2f) + 1f) * 0.5f;
            float scale = Mathf.Lerp(pulseMinScale, pulseMaxScale, t);
            textRect.localScale = new Vector3(scale, scale, 1f);

            float fadeStart = displayDuration * 0.75f;
            float alpha = elapsed < fadeStart
                ? 1f
                : Mathf.Clamp01(1f - (elapsed - fadeStart) / (displayDuration - fadeStart));
            group.alpha = alpha;

            yield return null;
        }

        textRect.localScale = Vector3.one;
        group.alpha = 1f;
        textRect.gameObject.SetActive(false);
        routine = null;
    }
}
