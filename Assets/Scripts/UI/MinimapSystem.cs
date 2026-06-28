using System.Collections.Generic;
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
    [SerializeField] private RectTransform chestIcon;
    [SerializeField] private RectTransform enemyIconTemplate;

    [Header("Icon Settings")]
    [SerializeField] private float iconEdgePadding = 0.08f;
    [Tooltip("Padding para iconos importantes (tienda/cofre). Más ALTO = más adentro (más visible); más BAJO = más pegado al borde (se esconde más). 0.13 = asoma ~70%, se esconde ~30% en el borde para que notes su dirección.")]
    [SerializeField] private float landmarkIconEdgePadding = 0.13f;

    [Header("Chest Icon Pulse")]
    [Tooltip("Cuántas veces por segundo pulsa el icono del cofre (grande/pequeño). Más bajo = más lento.")]
    [SerializeField] private float chestPulseFrequency = 0.8f;
    [Tooltip("Escala mínima (tamaño normal) del pulso del icono del cofre.")]
    [SerializeField] private float chestPulseMinScale = 1f;
    [Tooltip("Escala máxima (tamaño grande) del pulso del icono del cofre.")]
    [SerializeField] private float chestPulseMaxScale = 1.6f;

    private Transform playerTransform;
    private readonly List<RectTransform> _enemyIconPool = new List<RectTransform>();

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

        if (chestIcon != null)
            chestIcon.gameObject.SetActive(false);

        if (enemyIconTemplate != null)
            enemyIconTemplate.gameObject.SetActive(false);

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
        RefreshChestIcon();
        UpdateEnemyIcons();
    }

    private void UpdateEnemyIcons()
    {
        if (enemyIconTemplate == null) return;

        var enemies = EnemyHealth.ActiveEnemies;
        int count = enemies.Count;

        while (_enemyIconPool.Count < count)
        {
            RectTransform dot = Instantiate(enemyIconTemplate, enemyIconTemplate.parent);
            dot.gameObject.SetActive(false);
            _enemyIconPool.Add(dot);
        }

        for (int i = 0; i < count; i++)
        {
            if (enemies[i] == null) continue;
            _enemyIconPool[i].gameObject.SetActive(true);
            PlaceIcon(_enemyIconPool[i], enemies[i].transform.position);
        }

        for (int i = count; i < _enemyIconPool.Count; i++)
            _enemyIconPool[i].gameObject.SetActive(false);
    }

    private void PlaceIcon(RectTransform icon, Vector3 worldPos, float edgePaddingOverride = -1f)
    {
        if (icon == null || playerTransform == null || minimapCamera == null) return;

        Vector3 offset = worldPos - playerTransform.position;
        float orthoSize = minimapCamera.orthographicSize;
        float halfSize = orthoSize * 2f;

        float u = 0.5f + offset.x / halfSize;
        float v = 0.5f + offset.z / halfSize;

        Vector2 dir = new Vector2(u - 0.5f, v - 0.5f);
        float padding = edgePaddingOverride >= 0f ? edgePaddingOverride : iconEdgePadding;
        float maxRadius = 0.5f - padding;
        if (dir.magnitude > maxRadius)
            dir = dir.normalized * maxRadius;

        icon.anchorMin = new Vector2(0.5f + dir.x, 0.5f + dir.y);
        icon.anchorMax = new Vector2(0.5f + dir.x, 0.5f + dir.y);
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
        PlaceIcon(shopIcon, activeShop.transform.position, landmarkIconEdgePadding);
    }

    private void RefreshChestIcon()
    {
        if (chestIcon == null) return;

        if (ChestSpawner.TryGetActiveChestPosition(out Vector3 chestPos))
        {
            chestIcon.gameObject.SetActive(true);
            PlaceIcon(chestIcon, chestPos, landmarkIconEdgePadding);

            float freq = chestPulseFrequency > 0f ? chestPulseFrequency : 0.8f;
            float minScale = chestPulseMinScale > 0f ? chestPulseMinScale : 1f;
            float maxScale = chestPulseMaxScale > minScale ? chestPulseMaxScale : minScale + 0.6f;

            float t = (Mathf.Sin(Time.unscaledTime * freq * Mathf.PI * 2f) + 1f) * 0.5f;
            float scale = Mathf.Lerp(minScale, maxScale, t);
            chestIcon.localScale = new Vector3(scale, scale, 1f);
        }
        else
        {
            chestIcon.gameObject.SetActive(false);
        }
    }
}
