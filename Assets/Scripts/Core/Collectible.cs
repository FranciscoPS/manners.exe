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
    [SerializeField] private float warningTime = 3f;
    [SerializeField] private float blinkSpeed = 5f;
    
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
    private Color originalColor;
    private Material materialInstance;
    
    private float cachedMagnetRange;
    private float nextUpdateTime;
    private float updateOffset;

    public void SetType(CollectibleType collectibleType)
    {
        type = collectibleType;
        
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
        float blinkValue = Mathf.PingPong(Time.time * blinkSpeed, 1f);
        
        if (collectibleRenderer != null)
        {
            collectibleRenderer.enabled = blinkValue > 0.5f;
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
                
                if (PickupAudioManager.Instance != null)
                {
                    PickupAudioManager.Instance.PlayCoinSound();
                }
            }
            else if (type == CollectibleType.Diamond)
            {
                CurrencyManager.Instance.AddDiamonds(value);
                
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
