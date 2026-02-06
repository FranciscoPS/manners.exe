using UnityEngine;

public class ExperienceOrb : MonoBehaviour, IPoolable
{
    private int experienceValue = 10;
    private float attractionRange = 5f;
    private float moveSpeed = 8f;
    private float acceleration = 15f;
    private float lifeTime = 30f;
    
    [Header("Warning Settings")]
    [SerializeField] private float warningTime = 3f;
    [SerializeField] private float blinkSpeed = 5f;
    
    private Transform player;
    private bool isMovingToPlayer = false;
    private float currentSpeed = 0f;
    private bool collected = false;
    private float lifetimeTimer;
    private Renderer orbRenderer;
    private Light orbLight;
    private bool isBlinking = false;
    private Color originalColor;
    private Material materialInstance;

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
                    materialInstance.SetColor("_EmissionColor", color * 0.5f);
            }
        }

        if (orbLight == null)
            orbLight = GetComponent<Light>();
            
        if (orbLight != null)
        {
            orbLight.color = color;
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
                materialInstance.SetColor("_EmissionColor", color * 0.5f);
        }

        transform.localScale = Vector3.one * scale;
    }

    public void SetLight(bool hasLight, float intensity, float range, Color color)
    {
        if (orbLight == null)
            orbLight = GetComponent<Light>();

        if (hasLight)
        {
            if (orbLight == null)
            {
                orbLight = gameObject.AddComponent<Light>();
                orbLight.type = LightType.Point;
            }
            orbLight.intensity = intensity;
            orbLight.range = range;
            orbLight.color = color;
            orbLight.enabled = true;
        }
        else if (orbLight != null)
        {
            orbLight.enabled = false;
        }
    }

    private void Start()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }
        
        orbRenderer = GetComponent<Renderer>();
        orbLight = GetComponent<Light>();
        
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

        if (orbLight != null)
        {
            orbLight.range = 10f;
            orbLight.intensity = 5f;
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

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= attractionRange)
        {
            isMovingToPlayer = true;
        }

        if (isMovingToPlayer)
        {
            currentSpeed = Mathf.Min(currentSpeed + acceleration * Time.deltaTime, moveSpeed);
            
            Vector3 direction = (player.position - transform.position).normalized;
            transform.position += direction * currentSpeed * Time.deltaTime;
        }
    }
    
    private void HandleBlinking()
    {
        float blinkValue = Mathf.PingPong(Time.time * blinkSpeed, 1f);
        
        if (orbRenderer != null)
        {
            orbRenderer.enabled = blinkValue > 0.5f;
        }
        
        if (orbLight != null)
        {
            orbLight.enabled = blinkValue > 0.5f;
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

    private void CollectExperience(Transform playerTransform)
    {
        collected = true;
        
        PlayerExperience playerExp = playerTransform.GetComponent<PlayerExperience>();
        if (playerExp != null)
        {
            playerExp.AddExperience(experienceValue);
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
        
        if (orbLight != null)
        {
            orbLight.enabled = true;
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
        
        if (orbLight != null)
        {
            orbLight.enabled = true;
        }
    }
}
