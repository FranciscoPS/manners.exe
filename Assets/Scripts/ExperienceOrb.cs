using UnityEngine;

public class ExperienceOrb : MonoBehaviour, IPoolable
{
    private int experienceValue = 10;
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
    private Renderer orbRenderer;
    private bool isBlinking = false;
    private Color originalColor;
    private Material materialInstance;
    
    private float cachedMagnetRange;
    private float nextUpdateTime;
    private float updateOffset;

    public void SetExperienceValue(int value)
    {
        experienceValue = value;
    }

    public void SetOrbColor(Color color)
    {
        originalColor = color;
        
        if (orbRenderer == null)
            orbRenderer = GetComponent<Renderer>();
            
        if (orbRenderer != null)
        {
            if (materialInstance == null && orbRenderer.material != null)
            {
                materialInstance = orbRenderer.material;
            }
            
            if (materialInstance != null)
            {
                materialInstance.color = color;
                
                if (materialInstance.HasProperty("_BaseColor"))
                    materialInstance.SetColor("_BaseColor", color);
                if (materialInstance.HasProperty("_Color"))
                    materialInstance.SetColor("_Color", color);
                if (materialInstance.HasProperty("_EmissionColor"))
                    materialInstance.SetColor("_EmissionColor", color);
            }
        }
    }

    public void SetAttractionRange(float range)
    {
        attractionRange = range;
    }

    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
    }

    public void SetVisuals(Mesh mesh, Material material, Color color, float scale)
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter != null && mesh != null)
        {
            meshFilter.mesh = mesh;
        }

        if (orbRenderer == null)
            orbRenderer = GetComponent<Renderer>();

        if (orbRenderer != null)
        {
            if (material != null)
            {
                materialInstance = new Material(material);
                orbRenderer.material = materialInstance;
            }
            else if (materialInstance == null)
            {
                materialInstance = new Material(orbRenderer.sharedMaterial);
                orbRenderer.material = materialInstance;
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

    public void SetEmission(float emissionIntensity, float fresnelPower)
    {
        if (materialInstance == null && orbRenderer != null)
            materialInstance = orbRenderer.material;
            
        if (materialInstance != null)
        {
            if (materialInstance.HasProperty("_EmissionIntensity"))
                materialInstance.SetFloat("_EmissionIntensity", emissionIntensity);
            if (materialInstance.HasProperty("_FresnelPower"))
                materialInstance.SetFloat("_FresnelPower", fresnelPower);
                
            materialInstance.EnableKeyword("_EMISSION");
        }
    }

    private void Start()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }
        
        orbRenderer = GetComponent<Renderer>();
        
        if (orbRenderer != null && materialInstance == null)
        {
            materialInstance = orbRenderer.material;
            
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
                    originalColor = Color.green;
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
        
        UpdateCachedMagnetRange();
        
        updateOffset = Random.Range(0f, updateInterval);
        nextUpdateTime = Time.time + updateOffset;
    }
    
    private void UpdateCachedMagnetRange()
    {
        if (PlayerStatsManager.Instance != null)
        {
            cachedMagnetRange = PlayerStatsManager.Instance.GetModifiedMagnetRange();
            attractionRange = cachedMagnetRange;
        }
        else if (GameBalanceConfig.Instance != null)
        {
            attractionRange = GameBalanceConfig.Instance.OrbAttractionRange;
            cachedMagnetRange = attractionRange;
        }
        else
        {
            attractionRange = 5f;
            cachedMagnetRange = 5f;
        }
        
        if (GameBalanceConfig.Instance != null)
        {
            lifeTime = GameBalanceConfig.Instance.OrbLifetime;
        }
        else
        {
            lifeTime = 30f;
        }
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
            if (orbRenderer != null)
                orbRenderer.enabled = false;
            return;
        }
        
        if (orbRenderer != null)
            orbRenderer.enabled = true;
        
        float attractionRangeSqr = cachedMagnetRange * cachedMagnetRange;
        
        if (sqrDistance <= attractionRangeSqr)
        {
            isMovingToPlayer = true;
        }
    }
    

    
    private void HandleBlinking()
    {
        float blinkValue = Mathf.PingPong(Time.time * blinkSpeed, 1f);
        
        if (orbRenderer != null)
        {
            orbRenderer.enabled = blinkValue > 0.5f;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected) return;

        if (other.CompareTag("Player"))
        {
            CollectExperience(other.transform);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (collected) return;

        if (other.CompareTag("Player"))
        {
            CollectExperience(other.transform);
        }
    }

    private void CollectExperience(Transform playerTransform)
    {
        collected = true;
        
        PlayerExperience playerExp = playerTransform.GetComponent<PlayerExperience>();
        if (playerExp != null)
        {
            playerExp.AddExperience(experienceValue);
        }
        
        // Mostrar número de experiencia flotante
        if (FloatingTextManager.Instance != null)
        {
            Vector3 textPosition = transform.position + Vector3.up * 0.5f;
            FloatingTextManager.Instance.ShowExperience(experienceValue, textPosition);
        }
        
        if (PickupAudioManager.Instance != null)
        {
            PickupAudioManager.Instance.PlayExperienceOrbSound(experienceValue);
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, attractionRange);
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
        
        if (orbRenderer != null)
        {
            orbRenderer.enabled = true;
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
        
        if (orbRenderer != null)
        {
            orbRenderer.enabled = true;
        }
    }
}
