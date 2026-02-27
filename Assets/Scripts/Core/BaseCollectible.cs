using UnityEngine;

/// <summary>
/// Clase base para todos los colectables (Experience, Coins, Diamonds)
/// Elimina código duplicado y usa UpdateManager para mejor rendimiento
/// </summary>
public abstract class BaseCollectible : MonoBehaviour, IPoolable, IUpdateable
{
    [Header("Movement Settings")]
    [SerializeField] protected float attractionRange = 5f;
    [SerializeField] protected float moveSpeed = 8f;
    [SerializeField] protected float acceleration = 15f;
    [SerializeField] protected float lifeTime = 30f;
    
    [Header("Warning Settings")]
    [SerializeField] protected float warningTime = 3f;
    [SerializeField] protected float blinkSpeed = 5f;
    
    [Header("Performance Settings")]
    [SerializeField] protected float updateInterval = 0.1f;
    [SerializeField] protected float distantCullDistance = 50f;
    
    protected Transform player;
    protected bool isMovingToPlayer = false;
    protected float currentSpeed = 0f;
    protected bool collected = false;
    protected float lifetimeTimer;
    protected Renderer objectRenderer;
    protected bool isBlinking = false;
    protected Color originalColor;
    protected Material materialInstance;
    
    protected float nextUpdateTime;
    protected float updateOffset;
    
    public bool IsActive => gameObject.activeInHierarchy && !collected;
    
    protected virtual void Awake()
    {
        objectRenderer = GetComponent<Renderer>();
    }
    
    protected virtual void OnEnable()
    {
        collected = false;
        isMovingToPlayer = false;
        currentSpeed = 0f;
        isBlinking = false;
        lifetimeTimer = lifeTime;
        
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }
        
        InitializeRenderer();
        SetupPhysics();
        UpdateConfiguration();
        
        updateOffset = Random.Range(0f, updateInterval);
        nextUpdateTime = Time.time + updateOffset;
        
        // Registrarse en UpdateManager
        UpdateManager.Instance.Register(this);
    }
    
    protected virtual void OnDisable()
    {
        // Desregistrarse del UpdateManager
        if (UpdateManager.Instance != null)
        {
            UpdateManager.Instance.Unregister(this);
        }
    }
    
    protected virtual void InitializeRenderer()
    {
        if (objectRenderer != null && materialInstance == null)
        {
            materialInstance = objectRenderer.material;
            
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
            }
        }
    }
    
    protected virtual void SetupPhysics()
    {
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
    }
    
    protected abstract void UpdateConfiguration();
    
    // IUpdateable implementation - llamado por UpdateManager
    public void OnUpdate(float deltaTime)
    {
        if (player == null || collected) return;
        
        // Lifetime
        lifetimeTimer -= deltaTime;
        
        if (lifetimeTimer <= warningTime && !isBlinking)
        {
            isBlinking = true;
        }
        
        if (isBlinking)
        {
            HandleBlinking(deltaTime);
        }
        
        if (lifetimeTimer <= 0f)
        {
            Despawn();
            return;
        }
        
        // Movement
        if (isMovingToPlayer)
        {
            currentSpeed = Mathf.Min(currentSpeed + acceleration * deltaTime, moveSpeed);
            
            Vector3 direction = (player.position - transform.position).normalized;
            transform.position += direction * currentSpeed * deltaTime;
        }
        
        // Distance check (optimized with interval)
        if (Time.time >= nextUpdateTime)
        {
            nextUpdateTime = Time.time + updateInterval + updateOffset;
            UpdateDistanceCheck();
        }
    }
    
    protected virtual void UpdateDistanceCheck()
    {
        if (player == null) return;
        
        float sqrDistance = (transform.position - player.position).sqrMagnitude;
        
        // Culling lejano
        if (sqrDistance > distantCullDistance * distantCullDistance)
        {
            if (objectRenderer != null && objectRenderer.enabled)
                objectRenderer.enabled = false;
            return;
        }
        
        if (objectRenderer != null && !objectRenderer.enabled)
            objectRenderer.enabled = true;
        
        // Check atracción
        float sqrAttractionRange = attractionRange * attractionRange;
        
        if (!isMovingToPlayer && sqrDistance <= sqrAttractionRange)
        {
            isMovingToPlayer = true;
            currentSpeed = 0f;
        }
    }
    
    protected virtual void HandleBlinking(float deltaTime)
    {
        if (materialInstance != null)
        {
            float alpha = Mathf.PingPong(Time.time * blinkSpeed, 1.0f);
            Color blinkColor = originalColor;
            blinkColor.a = alpha;
            materialInstance.color = blinkColor;
            
            if (materialInstance.HasProperty("_BaseColor"))
                materialInstance.SetColor("_BaseColor", blinkColor);
            if (materialInstance.HasProperty("_Color"))
                materialInstance.SetColor("_Color", blinkColor);
        }
    }
    
    protected virtual void OnTriggerEnter(Collider other)
    {
        if (collected) return;
        
        if (other.CompareTag("Player"))
        {
            collected = true;
            OnCollected(other.gameObject);
            Despawn();
        }
    }
    
    protected abstract void OnCollected(GameObject playerObject);
    
    protected virtual void Despawn()
    {
        if (PoolManager.Instance != null)
        {
            PoolManager.Instance.Despawn(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
    
    // IPoolable implementation
    public virtual void OnSpawn()
    {
        collected = false;
        isMovingToPlayer = false;
        currentSpeed = 0f;
        isBlinking = false;
        lifetimeTimer = lifeTime;
        
        if (materialInstance != null && originalColor != Color.clear)
        {
            materialInstance.color = originalColor;
            if (materialInstance.HasProperty("_BaseColor"))
                materialInstance.SetColor("_BaseColor", originalColor);
            if (materialInstance.HasProperty("_Color"))
                materialInstance.SetColor("_Color", originalColor);
        }
        
        if (objectRenderer != null)
            objectRenderer.enabled = true;
    }
    
    public virtual void OnDespawn()
    {
        collected = false;
        isMovingToPlayer = false;
    }
    
    // Setters comunes
    public virtual void SetVisuals(Mesh mesh, Material material, Color color, float scale)
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter != null && mesh != null)
        {
            meshFilter.mesh = mesh;
        }
        
        if (objectRenderer == null)
            objectRenderer = GetComponent<Renderer>();
        
        if (objectRenderer != null)
        {
            if (material != null)
            {
                materialInstance = new Material(material);
                objectRenderer.material = materialInstance;
            }
            else if (materialInstance == null)
            {
                materialInstance = new Material(objectRenderer.sharedMaterial);
                objectRenderer.material = materialInstance;
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
    
    public virtual void SetAttractionRange(float range)
    {
        attractionRange = range;
    }
    
    public virtual void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
    }
    
    public virtual void SetEmission(float emissionIntensity, float fresnelPower)
    {
        if (materialInstance == null && objectRenderer != null)
            materialInstance = objectRenderer.material;
        
        if (materialInstance != null)
        {
            if (materialInstance.HasProperty("_EmissionIntensity"))
                materialInstance.SetFloat("_EmissionIntensity", emissionIntensity);
            if (materialInstance.HasProperty("_FresnelPower"))
                materialInstance.SetFloat("_FresnelPower", fresnelPower);
            
            materialInstance.EnableKeyword("_EMISSION");
        }
    }
}
