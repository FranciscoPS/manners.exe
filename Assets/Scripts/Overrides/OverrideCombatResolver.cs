using System.Collections.Generic;
using UnityEngine;

public enum OverrideAttackSource
{
    Laser,
    Cryo,
    Emp
}

public sealed class OverrideCombatResolver
{
    private const float ChainRadius = 2.5f;
    private const int MaxChainJumps = 32;
    private const int MaxExtraEmissions = 8;
    private readonly HashSet<EnemyHealth> hitEnemies = new HashSet<EnemyHealth>();
    private readonly HashSet<EnemyHealth> pushedEnemies = new HashSet<EnemyHealth>();
    private readonly List<EnemyHealth> explosionTargets = new List<EnemyHealth>();
    private readonly List<EnemyHealth> chainTargets = new List<EnemyHealth>();
    private readonly EnemyProximityGrid chainGrid = new EnemyProximityGrid();

    private OverrideAttackSource source;
    private CryoFieldConfig cryo;
    private EmpPulseConfig emp;
    private Vector3 origin;
    private Vector3 direction;
    private Vector3 firstImpact;
    private float damage;
    private float explosionDamage;
    private float explosionRadius;
    private float knockbackForce;
    private int chainJumps;
    private bool tickOpen;

    public static int EmissionCount(int maxExtraEmissions = 2)
    {
        PlayerStatsManager stats = PlayerStatsManager.Instance;
        if (maxExtraEmissions <= 0 || !Owns(stats, UpgradeType.MultiShot)) return 1;
        return 1 + Mathf.Clamp(stats.GetMultiShotExtraBullets(), 0, Mathf.Min(maxExtraEmissions, MaxExtraEmissions));
    }

    private static bool Owns(PlayerStatsManager stats, UpgradeType type)
    {
        return stats != null && stats.GetUpgradeLevel(type) > 0;
    }

    public void BeginTick(OverrideAttackSource attackSource, float baseDamage, Vector3 attackOrigin, Vector3 attackDirection)
    {
        source = attackSource;
        origin = attackOrigin;
        direction = attackDirection;
        direction.y = 0f;
        direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
        hitEnemies.Clear();
        pushedEnemies.Clear();
        tickOpen = true;

        OverrideManager manager = OverrideManager.Instance;
        cryo = manager != null ? manager.GetActiveConfig<CryoFieldConfig>() : null;
        emp = manager != null ? manager.GetActiveConfig<EmpPulseConfig>() : null;
        damage = Mathf.Max(0f, baseDamage);
        if (source == OverrideAttackSource.Emp && manager != null)
        {
            LaserBeamConfig laser = manager.GetActiveConfig<LaserBeamConfig>();
            if (laser != null)
                damage += Mathf.Max(0f, laser.damage * laser.empDamageMultiplier);
        }

        PlayerStatsManager stats = PlayerStatsManager.Instance;
        explosionRadius = Owns(stats, UpgradeType.ExplosiveShot) ? Mathf.Max(0f, stats.GetExplosionRadius()) : 0f;
        explosionDamage = damage;
        if (source == OverrideAttackSource.Emp && damage <= 0f && stats != null)
            explosionDamage = Mathf.Max(0f, stats.GetModifiedDamage());
        knockbackForce = Owns(stats, UpgradeType.Knockback) ? Mathf.Max(0f, stats.GetKnockbackForce()) : 0f;
        chainJumps = knockbackForce > 0f ? Mathf.Clamp(stats.GetKnockbackChainJumps(), 0, MaxChainJumps) : 0;
    }

    public void Hit(EnemyHealth enemy)
    {
        if (!tickOpen || !IsLive(enemy) || !hitEnemies.Add(enemy)) return;
        if (hitEnemies.Count == 1) firstImpact = enemy.transform.position;
        ApplyHit(enemy, damage, origin, false);
    }

    public void EndTick()
    {
        if (!tickOpen) return;
        tickOpen = false;
        if (hitEnemies.Count == 0) return;

        if (explosionRadius > 0f && explosionDamage > 0f)
        {
            explosionTargets.Clear();
            float radiusSqr = explosionRadius * explosionRadius;
            List<EnemyHealth> enemies = EnemyHealth.ActiveEnemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyHealth enemy = enemies[i];
                if (IsLive(enemy) && (enemy.transform.position - firstImpact).sqrMagnitude <= radiusSqr)
                    explosionTargets.Add(enemy);
            }

            ShowPulse(firstImpact, explosionRadius, new Color(1f, 0.42f, 0.08f));
            for (int i = 0; i < explosionTargets.Count; i++)
                if (IsLive(explosionTargets[i]))
                    ApplyHit(explosionTargets[i], explosionDamage, firstImpact, true);
        }

        ApplyChain();
    }

    private void ApplyHit(EnemyHealth enemy, float amount, Vector3 hitOrigin, bool secondary)
    {
        EnemyController controller = enemy.Controller;
        float multiplier = 1f;
        if (source == OverrideAttackSource.Laser && controller != null)
        {
            if (cryo != null && controller.IsSlowed)
                multiplier = Mathf.Max(multiplier, cryo.laserDamageMultiplier);
            if (emp != null && controller.IsFrozen)
                multiplier = Mathf.Max(multiplier, emp.laserDamageMultiplier);
        }

        if (controller != null)
        {
            if (cryo != null && (source == OverrideAttackSource.Laser || secondary))
                controller.ApplySlow(cryo.slowMultiplier, Mathf.Max(0.3f, cryo.tickInterval));
            Push(enemy, hitOrigin, knockbackForce);
        }

        if (amount > 0f) enemy.TakeDamage(amount * multiplier);
    }

    private void Push(EnemyHealth enemy, Vector3 hitOrigin, float force)
    {
        if (force <= 0f || enemy.Controller == null || !pushedEnemies.Add(enemy)) return;
        Vector3 pushDirection = enemy.transform.position - hitOrigin;
        pushDirection.y = 0f;
        if (pushDirection.sqrMagnitude < 0.0001f) pushDirection = direction;
        enemy.Controller.ApplyKnockback(pushDirection.normalized, force, 0.3f);
    }

    private void ApplyChain()
    {
        if (knockbackForce <= 0f || chainJumps <= 0) return;
        chainGrid.Build(EnemyHealth.ActiveEnemies, ChainRadius);
        Vector3 currentPosition = firstImpact;
        float force = knockbackForce;
        for (int jump = 0; jump < chainJumps; jump++)
        {
            chainTargets.Clear();
            chainGrid.CollectWithin(currentPosition, ChainRadius, chainTargets);
            EnemyHealth nearest = null;
            float nearestSqr = float.MaxValue;
            for (int i = 0; i < chainTargets.Count; i++)
            {
                EnemyHealth candidate = chainTargets[i];
                if (!IsLive(candidate) || candidate.Controller == null || pushedEnemies.Contains(candidate)) continue;
                float distanceSqr = (candidate.transform.position - currentPosition).sqrMagnitude;
                if (distanceSqr >= nearestSqr) continue;
                nearest = candidate;
                nearestSqr = distanceSqr;
            }
            if (nearest == null) break;
            Push(nearest, currentPosition, force);
            currentPosition = nearest.transform.position;
            force *= 0.95f;
        }
    }

    private static bool IsLive(EnemyHealth enemy)
    {
        return enemy != null && enemy.isActiveAndEnabled;
    }

    public static GameObject ShowPulse(Vector3 center, float radius, Color color)
    {
        return OverrideProcVisual.Show(center, radius, color);
    }
}
