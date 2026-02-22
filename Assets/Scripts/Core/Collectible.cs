using UnityEngine;

public class Collectible : MonoBehaviour, IPoolable
{
    public enum CollectibleType
    {
        Coin,
        Diamond
    }

    private CollectibleType type;
    private int value = 1;
    private float attractionRange;
    private float moveSpeed = 8f;
    private float acceleration = 15f;
    private float lifeTime;
    
    [Header("Warning Settings")]
    [SerializeField] private float warningTime = 10f; // Comienza a parpadear cuando quedan 6 segundos
    [SerializeField] private float normalBlinkSpeed = 4f; // 2 parpadeos completos por segundo (4 cambios de estado)
    [SerializeField] private float criticalTime = 2f; // Cuando quedan 3 segundos, parpadeo se acelera
    [SerializeField] private float criticalBlinkSpeed = 20f; // Parpadeo crítico muy rápido (10 parpadeos/segundo)
    
    [Header("Performance Settings")]
    [SerializeField] private float updateInterval = 0.1f;
    [SerializeField] private float distantCullDistance = 50f;
    
    private Transform player;
    private bool isMovingToPlayer = false;
    private float currentSpeed = 0f;
    private bool collected = false;
    private float lifetimeTimer;
    private Renderer collectibleRenderer;
    private bool isBlinking = false;
    private bool isCriticalBlink = false; // Para trackear cambio de velocidad
    private float blinkTimer = 0f; // Timer local para controlar parpadeo
    private bool isVisible = true; // Estado actual de visibilidad
    private Color originalColor;
    private Material materialInstance;
    
    private float cachedMagnetRange;
    private float nextUpdateTime;
    private float updateOffset;

    public void SetType(CollectibleType collectibleType)
    {
        type = collectibleType;
        
        // Forzar valores correctos de parpadeo
        warningTime = 10f;
        normalBlinkSpeed = 4f;
        criticalTime = 2f;
        criticalBlinkSpeed = 20f;
        
        UpdateCachedMagnetRange();
        
        if (GameBalanceConfig.Instance != null)
        {
            attractionRange = type == CollectibleType.Coin ? 
                GameBalanceConfig.Instance.CoinAttractionRange : 
                GameBalanceConfig.Instance.DiamondAttractionRange;
        }
        else
        {
            attractionRange = 5f;
        }
        
        if (GameBalanceConfig.Instance != null)
        {
            lifeTime = type == CollectibleType.Coin ? 
                GameBalanceConfig.Instance.CoinLifetime : 
                GameBalanceConfig.Instance.DiamondLifetime;
        }
        else
        {
            lifeTime = 30f;
        }
    }

    public void SetValue(int amount)
    {
        value = amount;
    }

    public void SetVisuals(Mesh mesh, Material material, Color color, float scale)
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter != null && mesh != null)
        {
            meshFilter.mesh = mesh;
        }

        if (collectibleRenderer == null)
            collectibleRenderer = GetComponent<Renderer>();

        if (collectibleRenderer != null)
        {
            if (material != null)
            {
                materialInstance = new Material(material);
                collectibleRenderer.material = materialInstance;
            }
            else if (materialInstance == null)
            {
                materialInstance = new Material(collectibleRenderer.sharedMaterial);
                collectibleRenderer.material = materialInstance;
            }
            
            originalColor = color;
            materialInstance.color = color;
            
            if (materialInstance.HasProperty("_BaseColor"))
                materialInstance.SetColor("_BaseColor", color);
            if (materialInstance.HasProperty("_Color"))
                materialInstance.SetColor("_Color", color);
            if (materialInstance.HasProperty("_EmissionColor"))
                materialInstance.SetColor("_EmissionColor", color);
            
            if (materialInstance.HasProperty("_RandomOffset"))
                materialInstance.SetFloat("_RandomOffset", Random.Range(0f, 100f));
        }

        transform.localScale = Vector3.one * scale;
    }

    public void SetAttractionRange(float range)
    {
        attractionRange = range;
    }

    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
    }

    private void Start()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }
        
        collectibleRenderer = GetComponent<Renderer>();
        
        if (collectibleRenderer != null && materialInstance == null)
        {
            materialInstance = collectibleRenderer.material;
            
            if (materialInstance.HasProperty("_RandomOffset"))
                materialInstance.SetFloat("_RandomOffset", Random.Range(0f, 100f));
            
            if (originalColor == Color.clear || originalColor == Color.black)
            {
                if (materialInstance.HasProperty("_BaseColor"))
                    originalColor = materialInstance.GetColor("_BaseColor");
                else if (materialInstance.HasProperty("_Color"))
                    originalColor = materialInstance.GetColor("_Color");
                else
                    originalColor = materialInstance.color;
                    
                if (originalColor == Color.clear || originalColor == Color.black)
                    originalColor = Color.yellow;
            }
        }
        
        SphereCollider collider = GetComponent<SphereCollider>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }
        
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        
        updateOffset = Random.Range(0f, updateInterval);
        nextUpdateTime = Time.time + updateOffset;
        UpdateCachedMagnetRange();
    }

    private void Update()
    {
        if (player == null || collected) return;

        lifetimeTimer -= Time.deltaTime;
        
        if (lifetimeTimer <= warningTime && !isBlinking)
        {
            isBlinking = true;
        }
        
        if (isBlinking)
        {
            HandleBlinking();
        }
        
        if (lifetimeTimer <= 0f)
        {
            if (PoolManager.Instance != null)
            {
                PoolManager.Instance.Despawn(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
            return;
        }

        if (isMovingToPlayer)
        {
            currentSpeed = Mathf.Min(currentSpeed + acceleration * Time.deltaTime, moveSpeed);
            
            Vector3 direction = (player.position - transform.position).normalized;
            transform.position += direction * currentSpeed * Time.deltaTime;
        }
        
        if (Time.time >= nextUpdateTime)
        {
            nextUpdateTime = Time.time + updateInterval + updateOffset;
            UpdateDistanceCheck();
        }
    }
    
    private void UpdateDistanceCheck()
    {
        if (player == null || isMovingToPlayer) return;
        
        float sqrDistance = (transform.position - player.position).sqrMagnitude;
        float cullDistanceSqr = distantCullDistance * distantCullDistance;
        
        if (sqrDistance > cullDistanceSqr)
        {
            if (collectibleRenderer != null)
                collectibleRenderer.enabled = false;
            return;
        }
        
        if (collectibleRenderer != null)
            collectibleRenderer.enabled = true;
        
        float attractionRangeSqr = cachedMagnetRange * cachedMagnetRange;
        
        if (sqrDistance <= attractionRangeSqr)
        {
            isMovingToPlayer = true;
        }
    }
    
    private void UpdateCachedMagnetRange()
    {
        if (PlayerStatsManager.Instance != null)
        {
            cachedMagnetRange = PlayerStatsManager.Instance.GetModifiedMagnetRange();
        }
        else
        {
            cachedMagnetRange = attractionRange;
        }
    }
    
    private void HandleBlinking()
    {
        // Detectar cambio a parpadeo crítico
        bool shouldBeCritical = lifetimeTimer <= criticalTime;
        
        if (shouldBeCritical && !isCriticalBlink)
        {
            isCriticalBlink = true;
            blinkTimer = 0f; // Reset timer al cambiar a crítico
        }
        
        // Determinar velocidad según tiempo restante
        float currentBlinkSpeed = shouldBeCritical ? criticalBlinkSpeed : normalBlinkSpeed;
        
        // Calcular intervalo de parpadeo (tiempo entre cada cambio on/off)
        float blinkInterval = 1f / currentBlinkSpeed;
        
        blinkTimer += Time.deltaTime;
        
        // Alternar visibilidad según el intervalo
        if (blinkTimer >= blinkInterval)
        {
            blinkTimer -= blinkInterval;
            isVisible = !isVisible;
            
            if (collectibleRenderer != null)
            {
                collectibleRenderer.enabled = isVisible;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected) return;

        if (other.CompareTag("Player"))
        {
            CollectItem();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (collected) return;

        if (other.CompareTag("Player"))
        {
            CollectItem();
        }
    }

    private void CollectItem()
    {
        collected = true;
        
        if (CurrencyManager.Instance != null)
        {
            if (type == CollectibleType.Coin)
            {
                CurrencyManager.Instance.AddCoins(value);
                
                // Mostrar número de monedas flotante
                if (FloatingTextManager.Instance != null)
                {
                    Vector3 textPosition = transform.position + Vector3.up * 0.5f;
                    FloatingTextManager.Instance.ShowCoins(value, textPosition);
                }
                
                if (PickupAudioManager.Instance != null)
                {
                    PickupAudioManager.Instance.PlayCoinSound();
                }
            }
            else if (type == CollectibleType.Diamond)
            {
                CurrencyManager.Instance.AddDiamonds(value);
                
                // Mostrar número de diamantes flotante
                if (FloatingTextManager.Instance != null)
                {
                    Vector3 textPosition = transform.position + Vector3.up * 0.5f;
                    FloatingTextManager.Instance.ShowDiamonds(value, textPosition);
                }
                
                if (PickupAudioManager.Instance != null)
                {
                    PickupAudioManager.Instance.PlayDiamondSound();
                }
            }
        }
        
        if (PoolManager.Instance != null)
        {
            PoolManager.Instance.Despawn(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    public void OnSpawn()
    {
        collected = false;
        isMovingToPlayer = false;
        currentSpeed = 0f;
        lifetimeTimer = lifeTime;
        isBlinking = false;
        isCriticalBlink = false; // Reset flag de parpadeo crítico
        blinkTimer = 0f; // Reset timer de parpadeo
        isVisible = true; // Empezar visible

        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }
        
        if (collectibleRenderer != null)
        {
            collectibleRenderer.enabled = true;
        }
        
        if (materialInstance != null)
        {
            materialInstance.color = originalColor;
            if (materialInstance.HasProperty("_BaseColor"))
                materialInstance.SetColor("_BaseColor", originalColor);
            if (materialInstance.HasProperty("_Color"))
                materialInstance.SetColor("_Color", originalColor);
        }
    }

    public void OnDespawn()
    {
        collected = false;
        isMovingToPlayer = false;
        currentSpeed = 0f;
        isBlinking = false;
        
        if (collectibleRenderer != null)
        {
            collectibleRenderer.enabled = true;
        }
    }
}
