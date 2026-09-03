using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class HoverTooltipUI : MonoBehaviour
{
    private static HoverTooltipUI instance;

    private RectTransform selfRect;
    private RectTransform canvasRect;
    private Canvas ownerCanvas;
    private RectTransform panelRect;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI bodyText;

    private static readonly Vector2 CursorOffset = new Vector2(24f, -24f);
    private static readonly Color PanelColor = new Color(0.04f, 0.05f, 0.14f, 0.96f);
    private static readonly Color TitleColor = Color.white;
    private static readonly Color BodyColor = new Color(0.82f, 0.85f, 0.95f, 1f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    public static void Show(Canvas canvas, string title, string body)
    {
        HoverTooltipUI tooltip = EnsureInstance(canvas);
        tooltip?.ShowInternal(title, body);
    }

    public static void Hide()
    {
        if (instance != null)
            instance.gameObject.SetActive(false);
    }

    private static HoverTooltipUI EnsureInstance(Canvas canvas)
    {
        if (instance != null) return instance;
        if (canvas == null) return null;

        GameObject root = new GameObject("HoverTooltip", typeof(RectTransform));
        root.transform.SetParent(canvas.transform, false);
        root.transform.SetAsLastSibling();

        instance = root.AddComponent<HoverTooltipUI>();
        instance.Build(canvas);

        return instance;
    }

    private void Build(Canvas canvas)
    {
        ownerCanvas = canvas;
        canvasRect = canvas.transform as RectTransform;

        selfRect = (RectTransform)transform;
        selfRect.anchorMin = Vector2.zero;
        selfRect.anchorMax = Vector2.one;
        selfRect.offsetMin = Vector2.zero;
        selfRect.offsetMax = Vector2.zero;

        GameObject panelObj = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        panelObj.transform.SetParent(transform, false);

        panelRect = (RectTransform)panelObj.transform;
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.sizeDelta = new Vector2(340f, 0f);

        Image background = panelObj.GetComponent<Image>();
        background.color = PanelColor;
        background.raycastTarget = false;

        VerticalLayoutGroup layout = panelObj.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 12, 12);
        layout.spacing = 4f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = panelObj.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        titleText = CreateText(panelObj.transform, "Title", 22f, FontStyles.Bold, TitleColor);
        bodyText = CreateText(panelObj.transform, "Body", 17f, FontStyles.Normal, BodyColor);

        gameObject.SetActive(false);
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name, float size, FontStyles style, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.raycastTarget = false;

        return tmp;
    }

    private void ShowInternal(string title, string body)
    {
        gameObject.SetActive(true);

        bool hasTitle = !string.IsNullOrEmpty(title);
        titleText.gameObject.SetActive(hasTitle);
        titleText.text = title;
        bodyText.text = body;

        UpdatePosition();
    }

    private void Update()
    {
        if (gameObject.activeSelf)
            UpdatePosition();
    }

    private void UpdatePosition()
    {
        if (canvasRect == null || panelRect == null || Mouse.current == null) return;

        Vector2 screenPoint = Mouse.current.position.ReadValue();
        Camera eventCamera = ownerCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : ownerCanvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, eventCamera, out Vector2 localPoint);

        panelRect.anchoredPosition = ClampToCanvas(localPoint + CursorOffset);
    }

    private Vector2 ClampToCanvas(Vector2 position)
    {
        Vector2 canvasSize = canvasRect.rect.size;
        Vector2 panelSize = panelRect.rect.size;

        float minX = -canvasSize.x * 0.5f;
        float maxX = canvasSize.x * 0.5f - panelSize.x;
        float minY = -canvasSize.y * 0.5f + panelSize.y;
        float maxY = canvasSize.y * 0.5f;

        return new Vector2(
            Mathf.Clamp(position.x, minX, maxX),
            Mathf.Clamp(position.y, minY, maxY));
    }
}
