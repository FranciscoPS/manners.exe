using UnityEngine;

public class AutoAttackSystem : MonoBehaviour
{
    [SerializeField] private Transform firePoint;
    [SerializeField] private float attackRange = 10f;
    [SerializeField] private float attackCooldown = 0.5f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private ProjectileConfiguration projectileConfig;

    private float cooldownTimer = 0f;
    private Transform currentTarget;

    private void Update()
    {
        cooldownTimer -= Time.deltaTime;

        FindClosestEnemy();

        if (currentTarget != null && cooldownTimer <= 0f)
        {
            Shoot();
            cooldownTimer = attackCooldown;
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

        Projectile projectile = PoolManager.Instance.SpawnProjectile(firePoint.position, Quaternion.identity, projectileConfig);
        
        if (projectile != null)
        {
            Vector3 direction = (currentTarget.position - firePoint.position).normalized;
            projectile.SetDirection(direction);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
