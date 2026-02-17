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
        
        // Obtener probabilidades de premium upgrades
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
        
        // Calcular cuántas balas disparar (1 + balas extra por MultiShot)
        int totalBullets = 1;
        
        // Tirar probabilidad para MultiShot
        if (multiShotProb > 0f && Random.Range(0f, 100f) < multiShotProb)
        {
            totalBullets += extraBullets;
        }
        
        // Calcular spread para múltiples balas
        float angleStep = totalBullets > 1 ? 15f : 0f;
        float startAngle = -(angleStep * (totalBullets - 1)) / 2f;
        
        Vector3 baseDirection = (currentTarget.position - firePoint.position).normalized;
        
        for (int i = 0; i < totalBullets; i++)
        {
            Projectile projectile = PoolManager.Instance.SpawnProjectile(firePoint.position, Quaternion.identity, projectileConfig);
            
            if (projectile != null)
            {
                // Calcular dirección con spread
                float angle = startAngle + (angleStep * i);
                Vector3 direction = Quaternion.Euler(0, angle, 0) * baseDirection;
                
                projectile.SetDirection(direction);
                
                // Determinar si esta bala específica es explosiva (probabilidad)
                bool isExplosive = explosiveProb > 0f && Random.Range(0f, 100f) < explosiveProb;
                projectile.SetExplosive(isExplosive, explosionRadius);
                
                // Determinar si esta bala específica tiene knockback (probabilidad)
                float bulletKnockback = (knockbackProb > 0f && Random.Range(0f, 100f) < knockbackProb) ? knockbackForce : 0f;
                projectile.SetKnockback(bulletKnockback);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        float currentRange = attackRange;
        
        // Si está en play mode, usar el valor actual
        if (Application.isPlaying && GameBalanceConfig.Instance != null)
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
