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

    [Header("Chest Icon Pulse")]
    [Tooltip("Cuántas veces por segundo pulsa el icono del cofre (grande/pequeño). Más bajo = más lento.")]
    [SerializeField] private float chestPulseFrequency = 0.7f;
    [Tooltip("Escala mínima y máxima del pulso del icono del cofre.")]
    [SerializeField] private float chestPulseMinScale = 0.85f;
    [SerializeField] private float chestPulseMaxScale = 1.25f;

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

        // Hide the template — only clones will be used
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

        // Grow pool if needed, cloned under the same parent as the template (MinimapRoot)
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

    private void PlaceIcon(RectTransform icon, Vector3 worldPos)
    {
        if (icon == null || playerTransform == null || minimapCamera == null) return;

        Vector3 offset = worldPos - playerTransform.position;
        float orthoSize = minimapCamera.orthographicSize;
        float halfSize = orthoSize * 2f;

        float u = 0.5f + offset.x / halfSize;
        float v = 0.5f + offset.z / halfSize;

        Vector2 dir = new Vector2(u - 0.5f, v - 0.5f);
        float maxRadius = 0.5f - iconEdgePadding;
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
        PlaceIcon(shopIcon, activeShop.transform.position);
    }

    private void RefreshChestIcon()
    {
        if (chestIcon == null) return;

        if (ChestSpawner.TryGetActiveChestPosition(out Vector3 chestPos))
        {
            chestIcon.gameObject.SetActive(true);
            PlaceIcon(chestIcon, chestPos);

            // Pulso lento grande/pequeño para llamar la atención.
            float t = (Mathf.Sin(Time.unscaledTime * chestPulseFrequency * Mathf.PI * 2f) + 1f) * 0.5f;
            float scale = Mathf.Lerp(chestPulseMinScale, chestPulseMaxScale, t);
            chestIcon.localScale = new Vector3(scale, scale, 1f);
        }
        else
        {
            chestIcon.gameObject.SetActive(false);
        }
    }
}
