using UnityEngine;
using UnityEngine.UI;

public class MinimapSystem : MonoBehaviour
{
    public static MinimapSystem Instance { get; private set; }

    [Header("Camera")]
    [SerializeField] private Camera minimapCamera;
    [SerializeField] private RenderTexture minimapRenderTexture;

    [Header("UI References")]
    [SerializeField] private RawImage mapDisplay;
    [SerializeField] private RawImage fogOverlay;
    [SerializeField] private RectTransform playerIcon;
    [SerializeField] private RectTransform shopIcon;

    [Header("World Bounds (XZ)")]
    [SerializeField] private Vector2 worldMin = new Vector2(-100f, -100f);
    [SerializeField] private Vector2 worldMax = new Vector2(100f, 100f);

    [Header("Fog Settings")]
    [SerializeField] private int fogTextureSize = 256;
    [SerializeField] private float revealRadius = 18f;
    [SerializeField] private float fogUpdateInterval = 0.1f;

    private Texture2D fogTexture;
    private Color32[] fogPixels;
    private Transform playerTransform;
    private float fogTimer;
    private bool fogDirty;

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
        InitFog();

        if (minimapRenderTexture != null && mapDisplay != null)
            mapDisplay.texture = minimapRenderTexture;

        if (minimapCamera != null)
            minimapCamera.targetTexture = minimapRenderTexture;

        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
            playerTransform = playerGO.transform;

        if (shopIcon != null)
            shopIcon.gameObject.SetActive(false);

        GameEvents.OnShopLocationChanged += OnShopLocationChanged;
        RefreshShopIcon();
    }

    private void OnDestroy()
    {
        GameEvents.OnShopLocationChanged -= OnShopLocationChanged;

        if (fogTexture != null)
            Destroy(fogTexture);
    }

    private void Update()
    {
        if (playerTransform == null) return;

        UpdateIconPosition(playerIcon, playerTransform.position);

        fogTimer += Time.deltaTime;
        if (fogTimer >= fogUpdateInterval)
        {
            fogTimer = 0f;
            RevealAround(playerTransform.position);
            if (fogDirty) ApplyFog();
        }
    }

    private void InitFog()
    {
        fogTexture = new Texture2D(fogTextureSize, fogTextureSize, TextureFormat.RGBA32, false);
        fogTexture.filterMode = FilterMode.Bilinear;
        fogPixels = new Color32[fogTextureSize * fogTextureSize];

        for (int i = 0; i < fogPixels.Length; i++)
            fogPixels[i] = new Color32(0, 0, 0, 230);

        fogTexture.SetPixels32(fogPixels);
        fogTexture.Apply();

        if (fogOverlay != null)
            fogOverlay.texture = fogTexture;
    }

    private void RevealAround(Vector3 worldPos)
    {
        Vector2 uv = WorldToUV(worldPos);
        int cx = Mathf.RoundToInt(uv.x * (fogTextureSize - 1));
        int cy = Mathf.RoundToInt(uv.y * (fogTextureSize - 1));

        float worldWidth = worldMax.x - worldMin.x;
        int pixelRadius = Mathf.RoundToInt((revealRadius / worldWidth) * fogTextureSize);
        int radiusSq = pixelRadius * pixelRadius;

        int x0 = Mathf.Max(cx - pixelRadius, 0);
        int x1 = Mathf.Min(cx + pixelRadius, fogTextureSize - 1);
        int y0 = Mathf.Max(cy - pixelRadius, 0);
        int y1 = Mathf.Min(cy + pixelRadius, fogTextureSize - 1);

        for (int y = y0; y <= y1; y++)
        {
            for (int x = x0; x <= x1; x++)
            {
                int dx = x - cx;
                int dy = y - cy;
                if (dx * dx + dy * dy <= radiusSq)
                {
                    int idx = y * fogTextureSize + x;
                    if (fogPixels[idx].a > 0)
                    {
                        fogPixels[idx].a = 0;
                        fogDirty = true;
                    }
                }
            }
        }
    }

    private void ApplyFog()
    {
        fogTexture.SetPixels32(fogPixels);
        fogTexture.Apply();
        fogDirty = false;
    }

    private void UpdateIconPosition(RectTransform icon, Vector3 worldPos)
    {
        if (icon == null) return;
        Vector2 uv = WorldToUV(worldPos);
        icon.anchorMin = uv;
        icon.anchorMax = uv;
        icon.anchoredPosition = Vector2.zero;
    }

    private Vector2 WorldToUV(Vector3 worldPos)
    {
        float u = Mathf.Clamp01(Mathf.InverseLerp(worldMin.x, worldMax.x, worldPos.x));
        float v = Mathf.Clamp01(Mathf.InverseLerp(worldMin.y, worldMax.y, worldPos.z));
        return new Vector2(u, v);
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
        UpdateIconPosition(shopIcon, activeShop.transform.position);
    }

    public void RevealAll()
    {
        for (int i = 0; i < fogPixels.Length; i++)
            fogPixels[i].a = 0;
        ApplyFog();
    }
}
