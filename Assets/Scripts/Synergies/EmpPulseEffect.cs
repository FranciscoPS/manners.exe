using System.Collections.Generic;
using UnityEngine;

public class EmpPulseEffect : MonoBehaviour, ISynergyEffect, IUpdateable
{
    private EmpPulseConfig config;
    private Transform player;
    private float pulseTimer;

    private readonly List<EnemyHealth> frozenBuffer = new List<EnemyHealth>();
    private readonly HashSet<EnemyHealth> frozenSet = new HashSet<EnemyHealth>();

    public bool IsActive => isActiveAndEnabled;

    private EmpPulseConfig Config
    {
        get
        {
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<EmpPulseConfig>();
                Debug.LogWarning($"[SYNERGY] {name} no tiene EmpPulseConfig asignado; usando valores por defecto.");
            }

            return config;
        }
    }

    public void Configure(EmpPulseConfig effectConfig)
    {
        config = effectConfig;
    }

    public void Activate(Transform target)
    {
        player = target;
        pulseTimer = Config.interval;
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

        pulseTimer -= deltaTime;
        if (pulseTimer > 0f) return;
        pulseTimer = Config.interval;

        Pulse();
    }

    private void Pulse()
    {
        frozenSet.Clear();
        frozenBuffer.Clear();

        Vector3 center = player.position;
        List<EnemyHealth> enemies = EnemyHealth.ActiveEnemies;
        float radiusSqr = Config.radius * Config.radius;
        float chainRadiusSqr = Config.chainRadius * Config.chainRadius;

        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyHealth enemy = enemies[i];
            if (enemy == null) continue;

            if ((enemy.transform.position - center).sqrMagnitude <= radiusSqr)
                Freeze(enemy);
        }

        int cursor = 0;
        while (cursor < frozenBuffer.Count)
        {
            EnemyHealth source = frozenBuffer[cursor];
            cursor++;
            if (source == null) continue;

            Vector3 sourcePosition = source.transform.position;

            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyHealth candidate = enemies[i];
                if (candidate == null || frozenSet.Contains(candidate)) continue;

                if ((candidate.transform.position - sourcePosition).sqrMagnitude <= chainRadiusSqr)
                    Freeze(candidate);
            }
        }

        SpawnVisual(center);
    }

    private void Freeze(EnemyHealth enemy)
    {
        if (!frozenSet.Add(enemy)) return;
        frozenBuffer.Add(enemy);

        EnemyController controller = enemy.GetComponent<EnemyController>();
        if (controller != null)
            controller.ApplySlow(0f, Config.freezeDuration);
    }

    private void SpawnVisual(Vector3 center)
    {
        if (Config.visualPrefabOverride != null)
        {
            Instantiate(Config.visualPrefabOverride, center, Quaternion.identity);
            return;
        }

        GameObject ring = SynergyVisualUtility.CreateFlatDisc("EmpRingVisual", null, center + Vector3.up * 0.05f, Config.radius * 2f, Config.ringColor);
        Destroy(ring, Config.ringLifetime);
    }
}
