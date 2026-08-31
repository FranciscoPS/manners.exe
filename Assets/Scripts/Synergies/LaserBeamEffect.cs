using System.Collections.Generic;
using UnityEngine;

public class LaserBeamEffect : MonoBehaviour, ISynergyEffect, IUpdateable
{
    private LaserBeamConfig config;
    private Transform player;
    private float fireTimer;
    private LineRenderer line;
    private float beamVisibleTimer;

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
        fireTimer = Config.interval;
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

        fireTimer -= deltaTime;
        if (fireTimer <= 0f)
        {
            fireTimer = Config.interval;
            Fire();
        }

        if (beamVisibleTimer > 0f)
        {
            beamVisibleTimer -= deltaTime;
            if (beamVisibleTimer <= 0f && line != null)
                line.enabled = false;
        }
    }

    private void Fire()
    {
        Vector3 origin = player.position;
        Vector3 direction = player.forward;
        Vector3 end = origin + direction * Config.range;

        List<EnemyHealth> enemies = EnemyHealth.ActiveEnemies;

        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyHealth enemy = enemies[i];
            if (enemy == null) continue;

            Vector3 point = enemy.transform.position;
            float distanceAlong = Vector3.Dot(point - origin, direction);
            if (distanceAlong < 0f || distanceAlong > Config.range) continue;

            Vector3 closestPointOnLine = origin + direction * distanceAlong;
            float lateralDistance = Vector3.Distance(point, closestPointOnLine);
            if (lateralDistance > Config.width) continue;

            enemy.TakeDamage(Config.damage);
        }

        ShowBeam(origin, end);
    }

    private void ShowBeam(Vector3 origin, Vector3 end)
    {
        if (line == null) return;

        line.SetPosition(0, origin + Vector3.up * 1f);
        line.SetPosition(1, end + Vector3.up * 1f);
        line.enabled = true;
        beamVisibleTimer = Config.beamDuration;
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
