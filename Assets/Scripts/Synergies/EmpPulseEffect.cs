using System.Collections.Generic;
using UnityEngine;

public class EmpPulseEffect : MonoBehaviour, ISynergyEffect, IUpdateable
{
    [Header("Pulso")]
    [SerializeField] private float interval = 5f;
    [SerializeField] private float radius = 5f;
    [SerializeField] private float freezeDuration = 2f;
    [Tooltip("Distancia a la que el congelamiento se contagia de un enemigo congelado a otro.")]
    [SerializeField] private float chainRadius = 2.5f;

    [Header("Visual")]
    [SerializeField] private Color ringColor = new Color(0.6f, 0.9f, 1f, 0.5f);
    [SerializeField] private float ringLifetime = 0.4f;

    private Transform player;
    private float pulseTimer;

    private readonly List<EnemyHealth> frozenBuffer = new List<EnemyHealth>();
    private readonly HashSet<EnemyHealth> frozenSet = new HashSet<EnemyHealth>();

    public bool IsActive => isActiveAndEnabled;

    public void Activate(Transform target, SynergyData source)
    {
        player = target;
        pulseTimer = interval;
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
        pulseTimer = interval;

        Pulse();
    }

    private void Pulse()
    {
        frozenSet.Clear();
        frozenBuffer.Clear();

        Vector3 center = player.position;
        List<EnemyHealth> enemies = EnemyHealth.ActiveEnemies;
        float radiusSqr = radius * radius;
        float chainRadiusSqr = chainRadius * chainRadius;

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

        SpawnRing(center);
    }

    private void Freeze(EnemyHealth enemy)
    {
        if (!frozenSet.Add(enemy)) return;
        frozenBuffer.Add(enemy);

        EnemyController controller = enemy.GetComponent<EnemyController>();
        if (controller != null)
            controller.ApplySlow(0f, freezeDuration);
    }

    private void SpawnRing(Vector3 center)
    {
        GameObject ring = SynergyVisualUtility.CreateFlatDisc("EmpRingVisual", null, center + Vector3.up * 0.05f, radius * 2f, ringColor);
        Object.Destroy(ring, ringLifetime);
    }
}
