using UnityEngine;
using UnityEngine.UI;

public class MinimapSystem : MonoBehaviour
{
    public static MinimapSystem Instance { get; private set; }

    [Header("Camera")]
    [SerializeField] private Camera minimapCamera;
    [SerializeField] private RenderTexture minimapRenderTexture;
    [SerializeField] private float cameraHeight = 80f;

    [Header("UI References")]
    [SerializeField] private RawImage mapDisplay;
    [SerializeField] private RectTransform playerIcon;
    [SerializeField] private RectTransform shopIcon;

    private Transform playerTransform;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (minimapRenderTexture != null && mapDisplay != null)
            mapDisplay.texture = minimapRenderTexture;

        if (minimapCamera != null)
            minimapCamera.targetTexture = minimapRenderTexture;

        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
            playerTransform = playerGO.transform;

        if (playerIcon != null)
        {
            playerIcon.anchorMin = new Vector2(0.5f, 0.5f);
            playerIcon.anchorMax = new Vector2(0.5f, 0.5f);
            playerIcon.anchoredPosition = Vector2.zero;
        }

        if (shopIcon != null)
            shopIcon.gameObject.SetActive(false);

        GameEvents.OnShopLocationChanged += OnShopLocationChanged;
        RefreshShopIcon();
    }

    private void OnDestroy()
    {
        GameEvents.OnShopLocationChanged -= OnShopLocationChanged;
    }

    private void LateUpdate()
    {
        if (playerTransform == null || minimapCamera == null) return;

        Vector3 playerPos = playerTransform.position;
        minimapCamera.transform.position = new Vector3(playerPos.x, playerPos.y + cameraHeight, playerPos.z);

        RefreshShopIcon();
    }

    private void PlaceIcon(RectTransform icon, Vector3 worldPos)
    {
        if (icon == null || playerTransform == null || minimapCamera == null) return;

        Vector3 offset = worldPos - playerTransform.position;
        float orthoSize = minimapCamera.orthographicSize;
        float halfSize = orthoSize * 2f;

        float u = Mathf.Clamp01(0.5f + offset.x / halfSize);
        float v = Mathf.Clamp01(0.5f + offset.z / halfSize);

        icon.anchorMin = new Vector2(u, v);
        icon.anchorMax = new Vector2(u, v);
        icon.anchoredPosition = Vector2.zero;
    }

    private void OnShopLocationChanged(int newIndex)
    {
        RefreshShopIcon();
    }

    private void RefreshShopIcon()
    {
        if (shopIcon == null || ShopManager.Instance == null) return;

        ShopScript activeShop = ShopManager.Instance.GetActiveShop();
        if (activeShop == null)
        {
            shopIcon.gameObject.SetActive(false);
            return;
        }

        shopIcon.gameObject.SetActive(true);
        PlaceIcon(shopIcon, activeShop.transform.position);
    }
}
