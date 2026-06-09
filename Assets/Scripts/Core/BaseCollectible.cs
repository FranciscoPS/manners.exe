using System.Collections.Generic;
using UnityEngine;

public abstract class BaseCollectible : MonoBehaviour, IPoolable, IUpdateable
{
    private static readonly List<BaseCollectible> activeCollectibles = new List<BaseCollectible>(256);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        activeCollectibles.Clear();
    }

    /// <summary>
    /// Fuerza a TODOS los pickups activos del mapa (orbes, monedas, diamantes)
    /// a volar hacia el jugador hasta ser recogidos. Usado por el ítem Imán Gigante.
    /// </summary>
    public static void AttractAllToPlayer()
    {
        for (int i = 0; i < activeCollectibles.Count; i++)
        {
            BaseCollectible c = activeCollectibles[i];
            if (c == null || c.collected) continue;
            c.isMovingToPlayer = true;
            c.currentSpeed = 0f;
        }
    }
    [Header("Movement Settings")]
    [SerializeField] protected float attractionRange = 5f;
    [SerializeField] protected float moveSpeed = 8f;
    [SerializeField] protected float acceleration = 15f;
    [SerializeField] protected float lifeTime = 30f;

    [Header("Warning Settings")]
    [SerializeField] protected float warningTime = 3f;
    [SerializeField] protected float blinkSpeed = 5f;
    [SerializeField] protected float finalWarningTime = 1f;
    [SerializeField] protected float blinkSpeedFast = 20f;

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

        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }

        InitializeRenderer();
        SetupPhysics();
        UpdateConfiguration(); // actualiza lifeTime desde GameBalanceConfig

        lifetimeTimer = lifeTime; // se setea DESPUÉS de UpdateConfiguration para usar el valor correcto

        updateOffset = Random.Range(0f, updateInterval);
        nextUpdateTime = Time.time + updateOffset;

        if (!activeCollectibles.Contains(this))
            activeCollectibles.Add(this);

        UpdateManager.Instance.Register(this);
    }

    protected virtual void OnDisable()
    {
        activeCollectibles.Remove(this);

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

    public void OnUpdate(float deltaTime)
    {
        if (collected) return;

        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        // El lifetime y parpadeo corren siempre, con o sin jugador
        lifetimeTimer -= deltaTime;

        if (lifetimeTimer <= warningTime && !isBlinking)
        {
            isBlinking = true;
        }

        if (isBlinking)
            HandleBlinking(deltaTime);

        if (lifetimeTimer <= 0f)
        {
            Despawn();
            return;
        }

        if (player == null) return; // movimiento y atracción requieren jugador

        if (isMovingToPlayer)
        {
            currentSpeed = Mathf.Min(currentSpeed + acceleration * deltaTime, moveSpeed);
            Vector3 direction = (player.position - transform.position).normalized;
            transform.position += direction * currentSpeed * deltaTime;
        }

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

        if (sqrDistance > distantCullDistance * distantCullDistance)
        {
            if (objectRenderer != null && objectRenderer.enabled)
                objectRenderer.enabled = false;
            return;
        }

        if (objectRenderer != null && !objectRenderer.enabled)
            objectRenderer.enabled = true;

        float sqrAttractionRange = attractionRange * attractionRange;

        if (!isMovingToPlayer && sqrDistance <= sqrAttractionRange)
        {
            isMovingToPlayer = true;
            currentSpeed = 0f;
        }
    }

    protected virtual void HandleBlinking(float deltaTime)
    {
        if (objectRenderer != null)
        {
            float speed = lifetimeTimer <= finalWarningTime ? blinkSpeedFast : blinkSpeed;
            objectRenderer.enabled = Mathf.PingPong(Time.time * speed, 1.0f) > 0.5f;
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
        if (SpawnFactory.Instance != null)
        {
            SpawnFactory.Instance.DestroyObject(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

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
