using System.Collections.Generic;
using UnityEngine;

public class LaserBeamEffect : MonoBehaviour, ISynergyEffect, IUpdateable
{
    [Header("Disparo")]
    [SerializeField] private float interval = 3f;
    [SerializeField] private float damage = 15f;
    [SerializeField] private float range = 14f;
    [SerializeField] private float width = 0.6f;

    [Header("Visual")]
    [SerializeField] private float beamDuration = 0.2f;
    [SerializeField] private Color beamColor = new Color(1f, 0.2f, 0.2f);
    [SerializeField] private float lineWidth = 0.15f;

    private Transform player;
    private float fireTimer;
    private LineRenderer line;
    private float beamVisibleTimer;

    public bool IsActive => isActiveAndEnabled;

    public void Activate(Transform target, SynergyData source)
    {
        player = target;
        fireTimer = interval;
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
            fireTimer = interval;
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
        Vector3 end = origin + direction * range;

        List<EnemyHealth> enemies = EnemyHealth.ActiveEnemies;

        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyHealth enemy = enemies[i];
            if (enemy == null) continue;

            Vector3 point = enemy.transform.position;
            float distanceAlong = Vector3.Dot(point - origin, direction);
            if (distanceAlong < 0f || distanceAlong > range) continue;

            Vector3 closestPointOnLine = origin + direction * distanceAlong;
            float lateralDistance = Vector3.Distance(point, closestPointOnLine);
            if (lateralDistance > width) continue;

            enemy.TakeDamage(damage);
        }

        ShowBeam(origin, end);
    }

    private void ShowBeam(Vector3 origin, Vector3 end)
    {
        if (line == null) return;

        line.SetPosition(0, origin + Vector3.up * 1f);
        line.SetPosition(1, end + Vector3.up * 1f);
        line.enabled = true;
        beamVisibleTimer = beamDuration;
    }

    private void BuildVisual()
    {
        line = gameObject.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;
        line.material = new Material(SynergyVisualUtility.FindUnlitShader());
        line.startColor = beamColor;
        line.endColor = beamColor;
        line.useWorldSpace = true;
        line.enabled = false;
    }
}
