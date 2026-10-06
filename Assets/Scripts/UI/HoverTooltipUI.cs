using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class HoverTooltipUI : MonoBehaviour, IUpdateable
{
    private static HoverTooltipUI instance;

    private RectTransform canvasRect;
    private Canvas ownerCanvas;
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI bodyText;

    [SerializeField] private Vector2 cursorOffset = new Vector2(24f, -24f);

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

        HoverTooltipUI prefab = Resources.Load<RuntimeUIPrefabs>("UI/RuntimeUIPrefabs_Production")?.hoverTooltip;
        if (prefab == null) return null;
        instance = Instantiate(prefab, canvas.transform, false);
        instance.ownerCanvas = canvas;
        instance.canvasRect = canvas.transform as RectTransform;

        return instance;
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

    public bool IsActive => isActiveAndEnabled;
    private void OnEnable() { UpdateManager.Instance?.Register(this); GameLocalization.LanguageChanged += Hide; }
    private void OnDisable() { UpdateManager.Instance?.Unregister(this); GameLocalization.LanguageChanged -= Hide; }
    public void OnUpdate(float deltaTime)
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

        panelRect.anchoredPosition = ClampToCanvas(localPoint + cursorOffset);
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
