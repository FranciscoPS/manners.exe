using UnityEngine;

public class AutoAttackSystem : MonoBehaviour, IUpdateable
{
    [SerializeField] private Transform firePoint;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private ProjectileConfiguration projectileConfig;

    private float attackRange;
    private float attackCooldown;
    private float cooldownTimer = 0f;
    private Transform currentTarget;
    
    // Target search optimization - reduce Physics.OverlapSphere calls
    private float targetSearchTimer = 0f;
    private const float TARGET_SEARCH_INTERVAL = 0.15f; // Only search every 150ms instead of every frame
    private Collider[] enemyBuffer = new Collider[50]; // Reusable buffer - ZERO allocations

    // IUpdateable implementation
    public bool IsActive => gameObject.activeInHierarchy && enabled;

    private void Awake()
    {
        // Load attack values from GameBalanceConfig
        if (GameBalanceConfig.Instance != null)
        {
            attackRange = GameBalanceConfig.Instance.PlayerAttackRange;
            attackCooldown = GameBalanceConfig.Instance.PlayerAttackCooldown;
        }
        else
        {
            // Fallback values if config is missing
            attackRange = 10f;
            attackCooldown = 0.5f;
        }
    }

    private void OnEnable()
    {
        // Registrar con UpdateManager
        if (UpdateManager.Instance != null)
        {
            UpdateManager.Instance.Register(this);
        }
    }
    
    private void OnDisable()
    {
        // Unregister del UpdateManager
        if (UpdateManager.Instance != null)
        {
            UpdateManager.Instance.Unregister(this);
        }
    }

    // IUpdateable implementation
    public void OnUpdate(float deltaTime)
    {
        cooldownTimer -= deltaTime;
        targetSearchTimer -= deltaTime;

        // Solo buscar enemigos cada TARGET_SEARCH_INTERVAL (no cada frame)
        // Esto reduce llamadas a Physics de 60/s a ~7/s = 88% reducción!
        if (targetSearchTimer <= 0f)
        {
            FindClosestEnemy();
            targetSearchTimer = TARGET_SEARCH_INTERVAL;
        }

        // Verificar si el target actual sigue siendo válido
        if (currentTarget != null && !currentTarget.gameObject.activeInHierarchy)
        {
            currentTarget = null;
        }

        // Usar cooldown modificado del PlayerStatsManager
        float currentCooldown = PlayerStatsManager.Instance != null 
            ? PlayerStatsManager.Instance.GetModifiedAttackCooldown() 
            : attackCooldown;

        if (currentTarget != null && cooldownTimer <= 0f)
        {
            Shoot();
            cooldownTimer = currentCooldown;
        }
    }

    private void FindClosestEnemy()
    {
        float currentRange = PlayerStatsManager.Instance != null 
            ? PlayerStatsManager.Instance.GetModifiedAttackRange() 
            : attackRange;
        
        // Usa buffer reutilizable - ZERO allocations (el array ya existe)
        // Physics.OverlapSphereNonAlloc no asigna memoria nueva
        int enemyCount = Physics.OverlapSphereNonAlloc(transform.position, currentRange, enemyBuffer, enemyLayer);
        
        if (enemyCount == 0)
        {
            currentTarget = null;
            return;
        }

        float closestDistance = Mathf.Infinity;
        Transform closestEnemy = null;

        // Usar for en lugar de foreach (más eficiente, evita iterator allocations)
        for (int i = 0; i < enemyCount; i++)
        {
            Collider enemy = enemyBuffer[i];
            if (enemy == null || !enemy.gameObject.activeInHierarchy) continue;
            
            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = enemy.transform;
            }
        }

        currentTarget = closestEnemy;
    }

    private void Shoot()
    {
        if (PoolManager.Instance == null)
        {
            Debug.LogWarning("PoolManager not initialized!");
            return;
        }

        if (projectileConfig == null)
        {
            Debug.LogError("[AutoAttackSystem] projectileConfig is NULL! Assign a ProjectileConfiguration in the Inspector!");
            return;
        }

        if (MusicManager.Instance != null && SFXDatabase.Instance != null && SFXDatabase.Instance.shootSFX != null)
        {
            MusicManager.Instance.PlaySFXOneShot(SFXDatabase.Instance.shootSFX, SFXDatabase.Instance.shootVolume);
        }
        
        float multiShotProb = 0f;
        int extraBullets = 0;
        float explosiveProb = 0f;
        float explosionRadius = 0f;
        float knockbackProb = 0f;
        float knockbackForce = 0f;
        
        if (PlayerStatsManager.Instance != null)
        {
            multiShotProb = PlayerStatsManager.Instance.GetMultiShotProbability();
            extraBullets = PlayerStatsManager.Instance.GetMultiShotExtraBullets();
            explosiveProb = PlayerStatsManager.Instance.GetExplosiveShotProbability();
            explosionRadius = PlayerStatsManager.Instance.GetExplosionRadius();
            knockbackProb = PlayerStatsManager.Instance.GetKnockbackProbability();
            knockbackForce = PlayerStatsManager.Instance.GetKnockbackForce();
        }
        
        int totalBullets = 1;
        
        if (multiShotProb > 0f && Random.Range(0f, 100f) < multiShotProb)
        {
            totalBullets += extraBullets;
        }
        
        float angleStep = totalBullets > 1 ? 20f : 0f;
        float startAngle = -(angleStep * (totalBullets - 1)) / 2f;
        
        Vector3 baseDirection = (currentTarget.position - firePoint.position).normalized;
        
        for (int i = 0; i < totalBullets; i++)
        {
            Projectile projectile = SpawnFactory.Instance.CreateProjectile(firePoint.position, Quaternion.identity, projectileConfig);
            
            if (projectile != null)
            {
                float angle = startAngle + (angleStep * i);
                Vector3 direction = Quaternion.Euler(0, angle, 0) * baseDirection;
                
                projectile.SetDirection(direction);
                
                bool isExplosive = explosiveProb > 0f && Random.Range(0f, 100f) < explosiveProb;
                projectile.SetExplosive(isExplosive, explosionRadius);
                
                bool hasKnockback = knockbackProb > 0f && Random.Range(0f, 100f) < knockbackProb;
                float bulletKnockback = hasKnockback ? knockbackForce : 0f;
                projectile.SetKnockback(bulletKnockback, hasKnockback);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        float currentRange = attackRange;
        
        if (Application.isPlaying && PlayerStatsManager.Instance != null)
        {
            currentRange = PlayerStatsManager.Instance.GetModifiedAttackRange();
        }
        else if (GameBalanceConfig.Instance != null)
        {
            currentRange = GameBalanceConfig.Instance.PlayerAttackRange;
        }
        
        Gizmos.DrawWireSphere(transform.position, currentRange);
        
        // Mostrar el valor en Scene view
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2, $"Attack Range: {currentRange:F2}");
        #endif
    }
}
