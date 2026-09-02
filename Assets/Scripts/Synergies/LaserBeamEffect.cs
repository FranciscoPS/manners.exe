using System.Collections.Generic;
using UnityEngine;

public class LaserBeamEffect : MonoBehaviour, ISynergyEffect, IUpdateable
{
    private LaserBeamConfig config;
    private Transform player;
    private LineRenderer line;

    private float cooldownTimer;
    private bool sweeping;
    private float sweepTimer;
    private float damageTickTimer;

    private Vector3 sweepGroundOrigin;
    private Vector3 sweepDirection;
    private float sweepStartDistance;
    private float sweepEndDistance;

    public bool IsActive => isActiveAndEnabled;

    private LaserBeamConfig Config
    {
        get
        {
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<LaserBeamConfig>();
                Debug.LogWarning($"[SYNERGY] {name} no tiene LaserBeamConfig asignado; usando valores por defecto.");
            }

            return config;
        }
    }

    public void Configure(LaserBeamConfig effectConfig)
    {
        config = effectConfig;
    }

    public void Activate(Transform target)
    {
        player = target;
        cooldownTimer = 0f;
        BuildVisual();
    }

    public void Deactivate()
    {
        Destroy(gameObject);
    }

    private void OnEnable()
    {
        UpdateManager.Instance?.Register(this);
    }

    private void OnDisable()
    {
        UpdateManager.Instance?.Unregister(this);
    }

    public void OnUpdate(float deltaTime)
    {
        if (player == null) return;

        if (sweeping)
        {
            UpdateSweep(deltaTime);
            return;
        }

        cooldownTimer -= deltaTime;
        if (cooldownTimer <= 0f)
        {
            cooldownTimer = Config.interval;
            TryStartSweep();
        }
    }

    private void TryStartSweep()
    {
        Vector3 targetPoint;

        EnemyHealth nearest = FindNearestEnemy();
        if (nearest != null)
        {
            targetPoint = nearest.transform.position;
        }
        else
        {
            Vector3? fallback = FindRandomBuildingPosition();
            targetPoint = fallback ?? RandomGroundPoint();
        }

        BeginSweep(targetPoint);
    }

    private void BeginSweep(Vector3 targetGroundPoint)
    {
        Vector3 playerGround = new Vector3(player.position.x, targetGroundPoint.y, player.position.z);
        Vector3 toTarget = targetGroundPoint - playerGround;
        toTarget.y = 0f;

        sweepDirection = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : player.forward;
        sweepGroundOrigin = playerGround;
        sweepStartDistance = toTarget.magnitude;
        sweepEndDistance = sweepStartDistance + Config.extendDistance;

        sweeping = true;
        sweepTimer = 0f;
        damageTickTimer = 0f;
        line.enabled = true;

        UpdateBeamPositions(sweepStartDistance);

        if (Config.fireSFX != null && MusicManager.Instance != null)
            MusicManager.Instance.PlaySFXOneShot(Config.fireSFX, Config.sfxVolume);
    }

    private Vector3? FindRandomBuildingPosition()
    {
        BuildingsScript[] buildings = Object.FindObjectsByType<BuildingsScript>(FindObjectsSortMode.None);
        if (buildings.Length == 0) return null;

        float rangeSqr = Config.range * Config.range;
        int inRangeCount = 0;

        for (int i = 0; i < buildings.Length; i++)
        {
            if ((buildings[i].transform.position - player.position).sqrMagnitude <= rangeSqr)
                inRangeCount++;
        }

        if (inRangeCount == 0) return null;

        int pick = Random.Range(0, inRangeCount);
        for (int i = 0; i < buildings.Length; i++)
        {
            if ((buildings[i].transform.position - player.position).sqrMagnitude > rangeSqr) continue;

            if (pick == 0) return buildings[i].transform.position;
            pick--;
        }

        return null;
    }

    private Vector3 RandomGroundPoint()
    {
        Vector2 offset = Random.insideUnitCircle.normalized * Config.range * Random.Range(0.3f, 1f);
        return player.position + new Vector3(offset.x, 0f, offset.y);
    }

    private void UpdateSweep(float deltaTime)
    {
        sweepTimer += deltaTime;
        float t = Config.sweepDuration > 0f ? Mathf.Clamp01(sweepTimer / Config.sweepDuration) : 1f;
        float currentDistance = Mathf.Lerp(sweepStartDistance, sweepEndDistance, t);

        Vector3 groundPoint = UpdateBeamPositions(currentDistance);

        damageTickTimer -= deltaTime;
        if (damageTickTimer <= 0f)
        {
            damageTickTimer = Config.damageTickInterval;
            DamageNear(groundPoint);
        }

        if (t >= 1f)
        {
            sweeping = false;
            line.enabled = false;
        }
    }

    private Vector3 UpdateBeamPositions(float distance)
    {
        Vector3 groundPoint = sweepGroundOrigin + sweepDirection * distance;
        Vector3 origin = player.position + Vector3.up * Config.beamOriginHeight;

        line.SetPosition(0, origin);
        line.SetPosition(1, groundPoint);

        return groundPoint;
    }

    private void DamageNear(Vector3 groundPoint)
    {
        List<EnemyHealth> enemies = EnemyHealth.ActiveEnemies;
        float radiusSqr = Config.impactRadius * Config.impactRadius;

        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyHealth enemy = enemies[i];
            if (enemy == null) continue;

            Vector3 flat = enemy.transform.position;
            flat.y = groundPoint.y;

            if ((flat - groundPoint).sqrMagnitude <= radiusSqr)
                enemy.TakeDamage(Config.damage);
        }
    }

    private EnemyHealth FindNearestEnemy()
    {
        List<EnemyHealth> enemies = EnemyHealth.ActiveEnemies;
        EnemyHealth nearest = null;
        float nearestSqr = Config.range * Config.range;

        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyHealth enemy = enemies[i];
            if (enemy == null) continue;

            float distSqr = (enemy.transform.position - player.position).sqrMagnitude;
            if (distSqr <= nearestSqr)
            {
                nearestSqr = distSqr;
                nearest = enemy;
            }
        }

        return nearest;
    }

    private void BuildVisual()
    {
        line = gameObject.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.startWidth = Config.lineWidth;
        line.endWidth = Config.lineWidth;
        line.useWorldSpace = true;
        line.enabled = false;

        if (Config.beamMaterialOverride != null)
        {
            line.material = Config.beamMaterialOverride;
        }
        else
        {
            line.material = new Material(SynergyVisualUtility.FindUnlitShader());
            line.startColor = Config.beamColor;
            line.endColor = Config.beamColor;
        }
    }
}
