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

    [Header("Chain Knockback (empuje en cadena)")]
    [Tooltip("Cuántos enemigos EXTRA encadena el empuje (saltos de la cadena). 0 = sin cadena.")]
    [SerializeField] private int chainKnockbackJumps = 5;
    [Tooltip("Radio para buscar el siguiente enemigo en cada salto de la cadena.")]
    [SerializeField] private float chainKnockbackRadius = 2.5f;
    [Tooltip("Fuerza del primer eslabón respecto a la fuerza base del impacto (0..1+).")]
    [SerializeField] private float chainKnockbackForceMultiplier = 1f;
    [Tooltip("Cuánto se debilita el empuje en cada salto (0..1). 0.95 = pierde solo 5% por salto, así toda la cadena empuja de verdad.")]
    [SerializeField] private float chainKnockbackFalloff = 0.95f;
    [Tooltip("Duración del empuje de cada eslabón encadenado.")]
    [SerializeField] private float chainKnockbackDuration = 0.25f;

    private TrailRenderer[] trailRenderers;

    private Vector3 direction;
    private Rigidbody rb;
    private float lifetimeTimer;
    private GameObject trailInstance;
    private Light projectileLight;
    private GameObject visualInstance;

    private static readonly Collider[] _explosionBuffer    = new Collider[64];
    private static readonly Collider[] _chainKnockbackBuffer = new Collider[64];
    private static readonly Transform[] _chainVisited = new Transform[64];

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

        trailRenderers = GetComponentsInChildren<TrailRenderer>(true);
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

    public void SetKnockback(float force, bool isChain = false, int chainJumps = -1)
    {
        knockbackForce = force;
        isChainKnockback = isChain;
        if (chainJumps >= 0) chainKnockbackJumps = chainJumps;
    }

    public void SetVisualPrefab(GameObject visualPrefab)
    {
        if (visualInstance != null)
        {
            Destroy(visualInstance);
            visualInstance = null;
        }

        if (visualPrefab == null) return;

        if (visualPrefab.GetComponent<Projectile>() != null)
        {

            return;
        }

        visualInstance = Instantiate(visualPrefab, transform);
        visualInstance.transform.localPosition = Vector3.zero;
        visualInstance.transform.localRotation = Quaternion.identity;
        visualInstance.transform.localScale = Vector3.one;
    }

    public void SetEffects(GameObject hitEffect, bool hasLight, Color lightColor, float lightIntensity)
    {

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

                if (isChainKnockback && chainKnockbackJumps > 0)
                {
                    ApplyChainKnockback(enemy.transform, knockbackDirection);
                }
            }
        }
    }

    private void ApplyChainKnockback(Transform firstEnemy, Vector3 initialDir)
    {
        int visitedCount = 0;
        if (firstEnemy != null) _chainVisited[visitedCount++] = firstEnemy;

        Vector3 currentPos = firstEnemy != null ? firstEnemy.position : transform.position;
        float currentForce = knockbackForce * chainKnockbackForceMultiplier;

        for (int jump = 0; jump < chainKnockbackJumps; jump++)
        {
            int hitCount = Physics.OverlapSphereNonAlloc(currentPos, chainKnockbackRadius, _chainKnockbackBuffer, EnemyLayerMask);

            EnemyController nearestCtrl = null;
            Transform nearestT = null;
            float nearestSqr = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                Transform t = _chainKnockbackBuffer[i].transform;

                bool visited = false;
                for (int v = 0; v < visitedCount; v++)
                {
                    if (_chainVisited[v] == t) { visited = true; break; }
                }
                if (visited) continue;

                float dSqr = (t.position - currentPos).sqrMagnitude;
                if (dSqr < nearestSqr)
                {
                    EnemyController ctrl = _chainKnockbackBuffer[i].GetComponent<EnemyController>();
                    if (ctrl != null)
                    {
                        nearestSqr = dSqr;
                        nearestCtrl = ctrl;
                        nearestT = t;
                    }
                }
            }

            if (nearestCtrl == null) break;

            Vector3 dir = nearestT.position - currentPos;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) dir = initialDir;
            dir.Normalize();

            nearestCtrl.ApplyKnockback(dir, currentForce, chainKnockbackDuration);

            if (visitedCount < _chainVisited.Length) _chainVisited[visitedCount++] = nearestT;
            currentPos = nearestT.position;
            currentForce *= chainKnockbackFalloff;
        }
    }

    public void OnSpawn()
    {
        lifetimeTimer = lifetime;
        rb.linearVelocity = Vector3.zero;

        foreach (TrailRenderer trail in trailRenderers)
        {
            trail.Clear();
            trail.emitting = false;
            trail.emitting = true;
        }

        if (UpdateManager.Instance != null)
        {
            UpdateManager.Instance.Register(this);
        }
    }

    public void OnDespawn()
    {
        rb.linearVelocity = Vector3.zero;
        direction = Vector3.zero;
        if (UpdateManager.Instance != null)
        {
            UpdateManager.Instance.Unregister(this);
        }
    }
}
