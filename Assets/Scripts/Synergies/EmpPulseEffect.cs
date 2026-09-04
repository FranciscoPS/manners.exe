using System.Collections.Generic;
using UnityEngine;

public class EmpPulseEffect : MonoBehaviour, ISynergyEffect, IUpdateable
{
    private EmpPulseConfig config;
    private Transform player;
    private float pulseTimer;

    private bool expanding;
    private float expandTimer;
    private GameObject proceduralVisual;

    private readonly List<EnemyHealth> frozenBuffer = new List<EnemyHealth>();
    private readonly HashSet<EnemyHealth> frozenSet = new HashSet<EnemyHealth>();
    private readonly EnemyProximityGrid chainGrid = new EnemyProximityGrid();
    private readonly List<EnemyHealth> chainNeighbors = new List<EnemyHealth>();

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

        if (expanding)
        {
            UpdateExpansion(deltaTime);
            return;
        }

        pulseTimer -= deltaTime;
        if (pulseTimer > 0f) return;
        pulseTimer = Config.interval;

        StartPulse();
    }

    private void StartPulse()
    {
        frozenSet.Clear();
        frozenBuffer.Clear();

        expanding = true;
        expandTimer = 0f;

        SpawnVisual();

        if (Config.pulseSFX != null && MusicManager.Instance != null)
            MusicManager.Instance.PlaySFXOneShot(Config.pulseSFX, Config.sfxVolume);
    }

    private void UpdateExpansion(float deltaTime)
    {
        expandTimer += deltaTime;
        float t = Config.expandDuration > 0f ? Mathf.Clamp01(expandTimer / Config.expandDuration) : 1f;
        float currentRadius = Mathf.Lerp(0f, Config.radius, t);

        UpdateVisualScale(currentRadius);
        CatchWavefront(currentRadius);

        if (t >= 1f)
        {
            expanding = false;
            PropagateChain();
            FinishVisual();
        }
    }

    private void CatchWavefront(float radius)
    {
        Vector3 center = player.position;
        List<EnemyHealth> enemies = EnemyHealth.ActiveEnemies;
        float radiusSqr = radius * radius;

        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyHealth enemy = enemies[i];
            if (enemy == null) continue;

            if ((enemy.transform.position - center).sqrMagnitude <= radiusSqr)
                Freeze(enemy);
        }
    }

    private void PropagateChain()
    {
        if (Config.maxChainHops <= 0 || frozenBuffer.Count == 0) return;

        chainGrid.Build(EnemyHealth.ActiveEnemies, Config.chainRadius);

        int hopStart = 0;
        int hopEnd = frozenBuffer.Count;

        for (int hop = 0; hop < Config.maxChainHops && hopStart < hopEnd; hop++)
        {
            for (int s = hopStart; s < hopEnd; s++)
            {
                EnemyHealth source = frozenBuffer[s];
                if (source == null) continue;

                chainNeighbors.Clear();
                chainGrid.CollectWithin(source.transform.position, Config.chainRadius, chainNeighbors);

                for (int i = 0; i < chainNeighbors.Count; i++)
                    Freeze(chainNeighbors[i]);
            }

            hopStart = hopEnd;
            hopEnd = frozenBuffer.Count;
        }
    }

    private void Freeze(EnemyHealth enemy)
    {
        if (!frozenSet.Add(enemy)) return;
        frozenBuffer.Add(enemy);

        EnemyController controller = enemy.Controller;
        if (controller != null)
            controller.ApplySlow(0f, Config.freezeDuration);
    }

    private void SpawnVisual()
    {
        if (Config.visualPrefabOverride != null)
        {
            GameObject visual = Instantiate(Config.visualPrefabOverride, player.position, Quaternion.identity);
            EmpPulseVisual pulseVisual = visual.GetComponent<EmpPulseVisual>();
            if (pulseVisual != null)
                pulseVisual.Play(player, Config.radius, Config.expandDuration);

            proceduralVisual = null;
            return;
        }

        proceduralVisual = SynergyVisualUtility.CreateFlatDisc("EmpRingVisual", null, player.position + Vector3.up * 0.05f, 0.02f, Config.ringColor);
    }

    private void UpdateVisualScale(float radius)
    {
        if (proceduralVisual == null) return;

        proceduralVisual.transform.position = new Vector3(player.position.x, player.position.y + 0.05f, player.position.z);

        float diameter = Mathf.Max(0.02f, radius * 2f);
        proceduralVisual.transform.localScale = new Vector3(diameter, proceduralVisual.transform.localScale.y, diameter);
    }

    private void FinishVisual()
    {
        if (proceduralVisual == null) return;

        Destroy(proceduralVisual, Config.ringLifetime);
        proceduralVisual = null;
    }
}
