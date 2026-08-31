using System.Collections.Generic;
using UnityEngine;

public class CryoFieldEffect : MonoBehaviour, ISynergyEffect, IUpdateable
{
    [Header("Área")]
    [SerializeField] private float radius = 4f;
    [Range(0f, 1f)][SerializeField] private float slowMultiplier = 0.5f;
    [SerializeField] private float damagePerTick = 2f;
    [SerializeField] private float tickInterval = 1f;

    [Header("Visual")]
    [SerializeField] private Color visualColor = new Color(0.4f, 0.85f, 1f, 0.35f);

    private Transform player;
    private float tickTimer;

    public bool IsActive => isActiveAndEnabled;

    public void Activate(Transform target, SynergyData source)
    {
        player = target;
        transform.SetParent(player, false);
        transform.localPosition = Vector3.zero;

        SynergyVisualUtility.CreateFlatDisc("CryoVisual", transform, new Vector3(0f, 0.05f, 0f), radius * 2f, visualColor);

        tickTimer = tickInterval;
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
        tickTimer = tickInterval;

        Vector3 center = player.position;
        List<EnemyHealth> enemies = EnemyHealth.ActiveEnemies;
        float radiusSqr = radius * radius;

        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyHealth enemy = enemies[i];
            if (enemy == null) continue;

            if ((enemy.transform.position - center).sqrMagnitude > radiusSqr) continue;

            EnemyController controller = enemy.GetComponent<EnemyController>();
            if (controller != null)
                controller.ApplySlow(slowMultiplier, tickInterval * 1.5f);

            enemy.TakeDamage(damagePerTick);
        }
    }
}
