using UnityEngine;

public class Projectile : MonoBehaviour, IPoolable, IUpdateable
{
    [SerializeField] private Transform visualRoot;

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
    private GameObject visualInstance;

    private static readonly Collider[] _explosionBuffer    = new Collider[64];
    private static readonly Collider[] _chainKnockbackBuffer = new Collider[32];

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

    public void SetVisualPrefab(GameObject visualPrefab)
    {
        if (visualInstance != null)
        {
            Destroy(visualInstance);
            visualInstance = null;
        }

        if (visualPrefab != null)
        {
            visualInstance = Instantiate(visualPrefab, transform);

            visualInstance.transform.localPosition = Vector3.zero;
            visualInstance.transform.localRotation = Quaternion.identity;
            visualInstance.transform.localScale = Vector3.one;
        }
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

        if (direction != Vector3.zero)
        {
            visualRoot.rotation = Quaternion.LookRotation(direction);
        }

        rb.linearVelocity = direction * speed;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            if (isExplosive)
            {

                Explode(other.transform.position);
            }
            else
            {

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

        int hitCount = Physics.OverlapSphereNonAlloc(impactPoint, explosionRadius, _explosionBuffer, EnemyLayerMask);

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

        if (UpdateManager.Instance != null)
        {
            UpdateManager.Instance.Register(this);
        }
    }

    public void OnDespawn()
    {
        rb.linearVelocity = Vector3.zero;
        direction = Vector3.zero;

        if (visualInstance != null)
        {
            Destroy(visualInstance);
            visualInstance = null;
        }

        if (UpdateManager.Instance != null)
        {
            UpdateManager.Instance.Unregister(this);
        }
    }
}
