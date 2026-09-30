using System.Collections.Generic;
using UnityEngine;

public class EmpPulseEffect : MonoBehaviour, IOverrideEffect, IUpdateable
{
    private EmpPulseConfig config;
    private bool ownsConfig;
    private Transform player;
    private float pulseTimer;
    private float repeatTimer;
    private int remainingExtraPulses;

    private bool expanding;
    private float expandTimer;
    private float effectiveFreezeDuration;
    private GameObject proceduralVisual;

    private readonly OverrideCombatResolver combat = new OverrideCombatResolver();
    private readonly List<Vector3> chainOrigins = new List<Vector3>();
    private readonly HashSet<EnemyHealth> frozenSet = new HashSet<EnemyHealth>();
    private readonly List<EnemyHealth> waveTargets = new List<EnemyHealth>();
    private readonly EnemyProximityGrid chainGrid = new EnemyProximityGrid();
    private readonly List<EnemyHealth> chainCandidates = new List<EnemyHealth>();
    private readonly List<EnemyHealth> chainNeighbors = new List<EnemyHealth>();
    private readonly List<GameObject> spawnedVisuals = new List<GameObject>();
    private readonly List<Material> generatedMaterials = new List<Material>();

    public bool IsActive => isActiveAndEnabled;

    private EmpPulseConfig Config
    {
        get
        {
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<EmpPulseConfig>();
                ownsConfig = true;
                Debug.LogWarning($"[OVERRIDE] {name} no tiene EmpPulseConfig asignado; usando valores por defecto.");
            }

            return config;
        }
    }

    public void Configure(EmpPulseConfig effectConfig)
    {
        ReleaseOwnedConfig();
        config = effectConfig;
    }

    public void Activate(Transform target)
    {
        ClearVisuals();
        player = target;
        pulseTimer = Config.interval;
        remainingExtraPulses = 0;
        expanding = false;
    }

    public void Deactivate()
    {
        player = null;
        ClearVisuals();
        DestroyOwned(gameObject);
    }

    private void OnEnable()
    {
        UpdateManager.Instance?.Register(this);
    }

    private void OnDisable()
    {
        UpdateManager.Instance?.Unregister(this);
        expanding = false;
        remainingExtraPulses = 0;
        pulseTimer = config != null ? config.interval : 5f;
        ClearVisuals();
    }

    private void OnDestroy()
    {
        ClearVisuals();
        ReleaseOwnedConfig();
    }

    private void ReleaseOwnedConfig()
    {
        if (!ownsConfig) return;
        DestroyOwned(config);
        ownsConfig = false;
        config = null;
    }

    public void OnUpdate(float deltaTime)
    {
        if (player == null) return;

        PruneVisuals();

        if (expanding)
        {
            UpdateExpansion(deltaTime);
            return;
        }

        if (remainingExtraPulses > 0)
        {
            repeatTimer -= deltaTime;
            if (repeatTimer <= 0f)
            {
                remainingExtraPulses--;
                StartPulse();
            }
            return;
        }

        pulseTimer -= deltaTime;
        if (pulseTimer > 0f) return;

        remainingExtraPulses = OverrideCombatResolver.EmissionCount(Config.maxExtraPulses) - 1;
        StartPulse();
    }

    private void StartPulse()
    {
        frozenSet.Clear();
        chainOrigins.Clear();
        effectiveFreezeDuration = Config.freezeDuration;
        if (OverrideManager.Instance != null && OverrideManager.Instance.GetActiveConfig<CryoFieldConfig>() != null)
            effectiveFreezeDuration *= Mathf.Max(1f, Config.cryoFreezeDurationMultiplier);
        combat.BeginTick(OverrideAttackSource.Emp, 0f, player.position, player.forward);

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
            combat.EndTick();
            FinishVisual();

            if (remainingExtraPulses > 0)
                repeatTimer = Mathf.Max(0.01f, Config.repeatDelay);
            else
                pulseTimer = Mathf.Max(0.01f, Config.interval);
        }
    }

    private void CatchWavefront(float radius)
    {
        Vector3 center = player.position;
        List<EnemyHealth> enemies = EnemyHealth.ActiveEnemies;
        float radiusSqr = radius * radius;
        waveTargets.Clear();

        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyHealth enemy = enemies[i];
            if (enemy == null || !enemy.isActiveAndEnabled || frozenSet.Contains(enemy)) continue;

            if ((enemy.transform.position - center).sqrMagnitude <= radiusSqr)
                waveTargets.Add(enemy);
        }

        // Damage can immediately return an enemy to its pool and remove it from
        // ActiveEnemies. Finish collection before applying any combat effects.
        for (int i = 0; i < waveTargets.Count; i++)
            Freeze(waveTargets[i]);
    }

    private void PropagateChain()
    {
        if (Config.maxChainHops <= 0 || Config.chainRadius <= 0f || chainOrigins.Count == 0) return;

        // Wave victims cannot be infected twice. Exclude them before indexing,
        // so a dense horde already covered by the wave needs no chain queries.
        chainCandidates.Clear();
        List<EnemyHealth> enemies = EnemyHealth.ActiveEnemies;
        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyHealth enemy = enemies[i];
            if (enemy != null && enemy.isActiveAndEnabled && !frozenSet.Contains(enemy))
                chainCandidates.Add(enemy);
        }

        int remainingCandidates = chainCandidates.Count;
        if (remainingCandidates == 0) return;
        chainGrid.Build(chainCandidates, Config.chainRadius);

        int hopStart = 0;
        int hopEnd = chainOrigins.Count;

        for (int hop = 0; hop < Config.maxChainHops && hopStart < hopEnd && remainingCandidates > 0; hop++)
        {
            for (int s = hopStart; s < hopEnd && remainingCandidates > 0; s++)
            {
                chainNeighbors.Clear();
                // Keep the impact position even when a laser/explosion combo
                // kills a frozen seed and its transform returns to a pool.
                chainGrid.CollectWithin(chainOrigins[s], Config.chainRadius, chainNeighbors);

                for (int i = 0; i < chainNeighbors.Count && remainingCandidates > 0; i++)
                    if (Freeze(chainNeighbors[i]))
                        remainingCandidates--;
            }

            hopStart = hopEnd;
            hopEnd = chainOrigins.Count;
        }
    }

    private bool Freeze(EnemyHealth enemy)
    {
        if (enemy == null || !enemy.isActiveAndEnabled || !frozenSet.Add(enemy)) return false;
        chainOrigins.Add(enemy.transform.position);

        EnemyController controller = enemy.Controller;
        if (controller != null)
            controller.ApplySlow(0f, effectiveFreezeDuration);

        combat.Hit(enemy);
        return true;
    }

    private void SpawnVisual()
    {
        if (Config.visualPrefabOverride != null)
        {
            GameObject visual = Instantiate(Config.visualPrefabOverride, player.position, Quaternion.identity);
            spawnedVisuals.Add(visual);
            EmpPulseVisual pulseVisual = visual.GetComponent<EmpPulseVisual>();
            if (pulseVisual != null)
                pulseVisual.Play(player, Config.radius, Config.expandDuration, 1);

            proceduralVisual = null;
            return;
        }

        proceduralVisual = OverrideVisualUtility.CreateFlatDisc("EmpRingVisual", null, player.position + Vector3.up * 0.05f, 0.02f, Config.ringColor);
        spawnedVisuals.Add(proceduralVisual);
        generatedMaterials.Add(proceduralVisual.GetComponent<Renderer>().sharedMaterial);
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

        float lifetime = Mathf.Max(0f, Config.ringLifetime);
        Material material = proceduralVisual.GetComponent<Renderer>().sharedMaterial;
        if (Application.isPlaying)
        {
            Destroy(material, lifetime);
            Destroy(proceduralVisual, lifetime);
        }
        else
        {
            DestroyOwned(material);
            DestroyOwned(proceduralVisual);
        }
        proceduralVisual = null;
    }

    private void PruneVisuals()
    {
        for (int i = spawnedVisuals.Count - 1; i >= 0; i--)
            if (spawnedVisuals[i] == null)
                spawnedVisuals.RemoveAt(i);

        for (int i = generatedMaterials.Count - 1; i >= 0; i--)
            if (generatedMaterials[i] == null)
                generatedMaterials.RemoveAt(i);
    }

    private void ClearVisuals()
    {
        for (int i = 0; i < spawnedVisuals.Count; i++)
            DestroyOwned(spawnedVisuals[i]);
        spawnedVisuals.Clear();
        for (int i = 0; i < generatedMaterials.Count; i++)
            DestroyOwned(generatedMaterials[i]);
        generatedMaterials.Clear();
        proceduralVisual = null;
    }

    private static void DestroyOwned(Object ownedObject)
    {
        if (ownedObject == null) return;
        if (Application.isPlaying) Destroy(ownedObject);
        else DestroyImmediate(ownedObject);
    }
}
