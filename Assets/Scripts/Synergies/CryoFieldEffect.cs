using System.Collections.Generic;
using UnityEngine;

public class CryoFieldEffect : MonoBehaviour, ISynergyEffect, IUpdateable
{
    private CryoFieldConfig config;
    private Transform player;
    private float tickTimer;

    public bool IsActive => isActiveAndEnabled;

    private CryoFieldConfig Config
    {
        get
        {
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<CryoFieldConfig>();
                Debug.LogWarning($"[SYNERGY] {name} no tiene CryoFieldConfig asignado; usando valores por defecto.");
            }

            return config;
        }
    }

    public void Configure(CryoFieldConfig effectConfig)
    {
        config = effectConfig;
    }

    public void Activate(Transform target)
    {
        player = target;
        transform.SetParent(player, false);
        transform.localPosition = Vector3.zero;

        BuildVisual();
        tickTimer = Config.tickInterval;
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

        tickTimer -= deltaTime;
        if (tickTimer > 0f) return;
        tickTimer = Config.tickInterval;

        Vector3 center = player.position;
        List<EnemyHealth> enemies = EnemyHealth.ActiveEnemies;
        float radiusSqr = Config.radius * Config.radius;

        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyHealth enemy = enemies[i];
            if (enemy == null) continue;

            if ((enemy.transform.position - center).sqrMagnitude > radiusSqr) continue;

            EnemyController controller = enemy.GetComponent<EnemyController>();
            if (controller != null)
                controller.ApplySlow(Config.slowMultiplier, Config.tickInterval * 1.5f);

            enemy.TakeDamage(Config.damagePerTick);
        }
    }

    private void BuildVisual()
    {
        if (Config.visualPrefabOverride != null)
        {
            GameObject visual = Instantiate(Config.visualPrefabOverride, transform);
            visual.transform.localPosition = Vector3.zero;
            return;
        }

        SynergyVisualUtility.CreateFlatDisc("CryoVisual", transform, new Vector3(0f, 0.05f, 0f), Config.radius * 2f, Config.visualColor);
    }
}
