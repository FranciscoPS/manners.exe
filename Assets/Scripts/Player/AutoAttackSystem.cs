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

    private float targetSearchTimer = 0f;
    private const float TARGET_SEARCH_INTERVAL = 0.15f;
    private Collider[] enemyBuffer = new Collider[50];

    public bool IsActive => gameObject.activeInHierarchy && enabled;

    private void Awake()
    {

        if (GameBalanceConfig.Instance != null)
        {
            attackRange = GameBalanceConfig.Instance.PlayerAttackRange;
            attackCooldown = GameBalanceConfig.Instance.PlayerAttackCooldown;
        }
        else
        {

            attackRange = 10f;
            attackCooldown = 0.5f;
        }
    }

    private void OnEnable()
    {

        if (UpdateManager.Instance != null)
        {
            UpdateManager.Instance.Register(this);
        }
    }

    private void OnDisable()
    {

        if (UpdateManager.Instance != null)
        {
            UpdateManager.Instance.Unregister(this);
        }
    }

    public void OnUpdate(float deltaTime)
    {
        cooldownTimer -= deltaTime;
        targetSearchTimer -= deltaTime;

        if (targetSearchTimer <= 0f)
        {
            FindClosestEnemy();
            targetSearchTimer = TARGET_SEARCH_INTERVAL;
        }

        if (currentTarget != null && !currentTarget.gameObject.activeInHierarchy)
        {
            currentTarget = null;
        }

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

        int enemyCount = Physics.OverlapSphereNonAlloc(transform.position, currentRange, enemyBuffer, enemyLayer);

        if (enemyCount == 0)
        {
            currentTarget = null;
            return;
        }

        float closestDistance = Mathf.Infinity;
        Transform closestEnemy = null;

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
            return;
        }

        if (projectileConfig == null)
        {
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
        int knockbackChainJumps = 0;

        if (PlayerStatsManager.Instance != null)
        {
            multiShotProb = PlayerStatsManager.Instance.GetMultiShotProbability();
            extraBullets = PlayerStatsManager.Instance.GetMultiShotExtraBullets();
            explosiveProb = PlayerStatsManager.Instance.GetExplosiveShotProbability();
            explosionRadius = PlayerStatsManager.Instance.GetExplosionRadius();
            knockbackProb = PlayerStatsManager.Instance.GetKnockbackProbability();
            knockbackForce = PlayerStatsManager.Instance.GetKnockbackForce();
            knockbackChainJumps = PlayerStatsManager.Instance.GetKnockbackChainJumps();
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
                projectile.SetKnockback(bulletKnockback, hasKnockback, knockbackChainJumps);
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

        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2, $"Attack Range: {currentRange:F2}");
        #endif
    }
}
