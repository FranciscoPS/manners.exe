using UnityEngine;

public class AutoAttackSystem : MonoBehaviour
{
    [SerializeField] private Transform firePoint;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private ProjectileConfiguration projectileConfig;

    private float attackRange;
    private float attackCooldown;
    private float cooldownTimer = 0f;
    private Transform currentTarget;

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

    private void Update()
    {
        cooldownTimer -= Time.deltaTime;

        FindClosestEnemy();

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
        Collider[] enemies = Physics.OverlapSphere(transform.position, attackRange, enemyLayer);
        
        if (enemies.Length == 0)
        {
            currentTarget = null;
            return;
        }

        float closestDistance = Mathf.Infinity;
        Transform closestEnemy = null;

        foreach (Collider enemy in enemies)
        {
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
        
        // Obtener número de proyectiles del PlayerStatsManager
        int projectileCount = 1;
        bool isExplosive = false;
        float knockbackForce = 0f;
        
        if (PlayerStatsManager.Instance != null)
        {
            projectileCount = PlayerStatsManager.Instance.GetProjectileCount();
            isExplosive = PlayerStatsManager.Instance.IsExplosiveShot();
            knockbackForce = PlayerStatsManager.Instance.GetKnockbackForce();
        }
        
        // Calcular ángulos de disparo para spread
        float angleStep = projectileCount > 1 ? 15f : 0f;
        float startAngle = -(angleStep * (projectileCount - 1)) / 2f;
        
        Vector3 baseDirection = (currentTarget.position - firePoint.position).normalized;
        
        for (int i = 0; i < projectileCount; i++)
        {
            Projectile projectile = PoolManager.Instance.SpawnProjectile(firePoint.position, Quaternion.identity, projectileConfig);
            
            if (projectile != null)
            {
                // Calcular dirección con spread
                float angle = startAngle + (angleStep * i);
                Vector3 direction = Quaternion.Euler(0, angle, 0) * baseDirection;
                
                projectile.SetDirection(direction);
                projectile.SetExplosive(isExplosive, 3f);
                projectile.SetKnockback(knockbackForce);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
