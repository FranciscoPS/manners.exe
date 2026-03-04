using UnityEngine;

public class Projectile : MonoBehaviour, IPoolable, IUpdateable
{
    private float speed = 15f;
    private float damage = 10f;
    private float lifetime = 5f;
    private bool isExplosive = false;
    private float explosionRadius = 3f;
    private float knockbackForce = 0f;
    private bool isChainKnockback = false;
    private float chainKnockbackRadius = 2f;

    private Vector3 direction;
    private Rigidbody rb;
    private float lifetimeTimer;
    private GameObject trailInstance;
    private Light projectileLight;
    private Material materialInstance;

    // Buffer estático compartido: CERO allocations en explosiones.
    // Tamaño 64 cubre cualquier escenario realista de radio de explosión.
    private static readonly Collider[] _explosionBuffer    = new Collider[64];
    private static readonly Collider[] _chainKnockbackBuffer = new Collider[32];
    
    // LayerMask cacheada: GetMask hace una búsqueda de string cada vez.
    // Al ser static se inicializa una sola vez para todas las instancias.
    private static int _enemyLayerMask = -1;
    private static int EnemyLayerMask
    {
        get
        {
            if (_enemyLayerMask == -1)
                _enemyLayerMask = LayerMask.GetMask("Enemy");
            return _enemyLayerMask;
        }
    }

    // IUpdateable implementation
    public bool IsActive => this != null && gameObject != null && gameObject.activeInHierarchy && lifetimeTimer > 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        projectileLight = GetComponent<Light>();
    }

    public void SetStats(float newSpeed, float newDamage, float newLifetime)
    {
        speed = newSpeed;
        damage = newDamage;
        lifetime = newLifetime;
    }
    
    public void SetExplosive(bool explosive, float radius = 3f)
    {
        isExplosive = explosive;
        explosionRadius = radius;
    }
    
    public void SetKnockback(float force, bool isChain = false)
    {
        knockbackForce = force;
        isChainKnockback = isChain;
    }

    public void SetVisuals(Mesh mesh, Material material, Color color, Vector3 scale)
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter != null && mesh != null)
        {
            meshFilter.mesh = mesh;
        }

        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            if (material != null)
            {
                materialInstance = new Material(material);
                renderer.material = materialInstance;
            }
            else if (materialInstance == null)
            {
                materialInstance = new Material(renderer.sharedMaterial);
                renderer.material = materialInstance;
            }
            
            materialInstance.color = color;
            
            if (materialInstance.HasProperty("_BaseColor"))
                materialInstance.SetColor("_BaseColor", color);
            if (materialInstance.HasProperty("_Color"))
                materialInstance.SetColor("_Color", color);
            if (materialInstance.HasProperty("_EmissionColor"))
                materialInstance.SetColor("_EmissionColor", color * 0.5f);
        }

        transform.localScale = scale;
    }

    public void SetEffects(GameObject trail, GameObject hitEffect, bool hasLight, Color lightColor, float lightIntensity)
    {
        if (trail != null && trailInstance == null)
        {
            trailInstance = Instantiate(trail, transform);
        }

        if (hasLight)
        {
            if (projectileLight == null)
            {
                projectileLight = gameObject.AddComponent<Light>();
                projectileLight.type = LightType.Point;
                projectileLight.range = 3f;
            }
            projectileLight.color = lightColor;
            projectileLight.intensity = lightIntensity;
            projectileLight.enabled = true;
        }
        else if (projectileLight != null)
        {
            projectileLight.enabled = false;
        }
    }

    // IUpdateable implementation
    public void OnUpdate(float deltaTime)
    {
        lifetimeTimer -= deltaTime;
        if (lifetimeTimer <= 0f)
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
    }

    public void SetDirection(Vector3 dir)
    {
        direction = dir.normalized;
        rb.linearVelocity = direction * speed;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            if (isExplosive)
            {
                // Explosión de área
                Explode(other.transform.position);
            }
            else
            {
                // Daño directo
                DealDamageToEnemy(other.gameObject, other.transform.position);
            }
            
            if (SpawnFactory.Instance != null)
            {
                SpawnFactory.Instance.DestroyObject(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
    
    private void Explode(Vector3 impactPoint)
    {
        // OverlapSphereNonAlloc: rellena el buffer estático, sin heap allocation.
        int hitCount = Physics.OverlapSphereNonAlloc(impactPoint, explosionRadius, _explosionBuffer, EnemyLayerMask);

        // Log cuando una sola explosión mata muchos enemigos — correlaciona con spikes de FPS
        if (hitCount >= 5)
        {
            PerformanceMonitor.Instance?.LogEvent($"EXPLOSION masiva | {hitCount} enemies en radio {explosionRadius:F1}m");
        }

        for (int i = 0; i < hitCount; i++)
        {
            DealDamageToEnemy(_explosionBuffer[i].gameObject, impactPoint);
        }
    }
    
    private void DealDamageToEnemy(GameObject enemy, Vector3 impactPoint)
    {
        EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(damage);
        }
        
        if (knockbackForce > 0f)
        {
            EnemyController enemyController = enemy.GetComponent<EnemyController>();
            if (enemyController != null)
            {
                Vector3 knockbackDirection = (enemy.transform.position - impactPoint).normalized;
                float duration = 0.3f;
                enemyController.ApplyKnockback(knockbackDirection, knockbackForce, duration);
                
                if (isChainKnockback)
                {
                    ApplyChainKnockback(enemy.transform.position, knockbackDirection);
                }
            }
        }
    }
    
    private void ApplyChainKnockback(Vector3 knockedEnemyPosition, Vector3 knockbackDirection)
    {
        int hitCount = Physics.OverlapSphereNonAlloc(knockedEnemyPosition, chainKnockbackRadius, _chainKnockbackBuffer, EnemyLayerMask);
        
        for (int i = 0; i < hitCount; i++)
        {
            Collider enemyCollider = _chainKnockbackBuffer[i];
            if (enemyCollider.transform.position == knockedEnemyPosition)
                continue;
            
            EnemyController chainEnemy = enemyCollider.GetComponent<EnemyController>();
            if (chainEnemy != null)
            {
                Vector3 chainDirection = (enemyCollider.transform.position - knockedEnemyPosition).normalized;
                float chainForce = knockbackForce * 0.7f;
                float duration = 0.25f;
                chainEnemy.ApplyKnockback(chainDirection, chainForce, duration);
            }
        }
    }

    public void OnSpawn()
    {
        lifetimeTimer = lifetime;
        rb.linearVelocity = Vector3.zero;
        
        // Registrar con UpdateManager
        if (UpdateManager.Instance != null)
        {
            UpdateManager.Instance.Register(this);
        }
    }

    public void OnDespawn()
    {
        rb.linearVelocity = Vector3.zero;
        direction = Vector3.zero;
        
        // Unregister del UpdateManager
        if (UpdateManager.Instance != null)
        {
            UpdateManager.Instance.Unregister(this);
        }
    }
}
