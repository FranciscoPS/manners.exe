using System.Collections.Generic;
using UnityEngine;

public class CryoFieldEffect : MonoBehaviour, IOverrideEffect, IUpdateable
{
    private const float SlowRefreshWindow = 0.3f;

    private CryoFieldConfig config;
    private bool ownsConfig;
    private Transform player;
    private float tickTimer;
    private float repeatTimer;
    private int remainingExtraPulses;
    private GameObject fieldVisual;
    private Material generatedFieldMaterial;
    private readonly List<GameObject> echoVisuals = new List<GameObject>();
    private readonly OverrideCombatResolver combat = new OverrideCombatResolver();

    public bool IsActive => isActiveAndEnabled;

    private CryoFieldConfig Config
    {
        get
        {
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<CryoFieldConfig>();
                ownsConfig = true;
                Debug.LogWarning($"[OVERRIDE] {name} no tiene CryoFieldConfig asignado; usando valores por defecto.");
            }

            return config;
        }
    }

    public void Configure(CryoFieldConfig effectConfig)
    {
        ReleaseOwnedConfig();
        config = effectConfig;
    }

    public void Activate(Transform target)
    {
        ClearVisuals();
        player = target;
        transform.SetParent(player, false);
        transform.localPosition = Vector3.zero;

        BuildVisual();
        tickTimer = Config.tickInterval;
        remainingExtraPulses = 0;

        if (Config.activationSFX != null && MusicManager.Instance != null)
            MusicManager.Instance.PlaySFXOneShot(Config.activationSFX, Config.sfxVolume);
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
        if (player != null && fieldVisual == null)
            BuildVisual();
    }

    private void OnDisable()
    {
        UpdateManager.Instance?.Unregister(this);
        remainingExtraPulses = 0;
        tickTimer = config != null ? config.tickInterval : 1f;
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

        bool applyDamageThisFrame = false;
        bool isEcho = false;
        if (remainingExtraPulses > 0)
        {
            repeatTimer -= deltaTime;
            if (repeatTimer <= 0f)
            {
                remainingExtraPulses--;
                repeatTimer = Mathf.Max(0.01f, Config.repeatDelay);
                tickTimer = Mathf.Max(0.01f, Config.tickInterval);
                applyDamageThisFrame = true;
                isEcho = true;
            }
        }
        else
        {
            tickTimer -= deltaTime;
            if (tickTimer <= 0f)
            {
                tickTimer = Mathf.Max(0.01f, Config.tickInterval);
                remainingExtraPulses = OverrideCombatResolver.EmissionCount(Config.maxExtraPulses) - 1;
                repeatTimer = Mathf.Max(0.01f, Config.repeatDelay);
                applyDamageThisFrame = true;
            }
        }

        Vector3 center = player.position;
        List<EnemyHealth> enemies = EnemyHealth.ActiveEnemies;
        float radiusSqr = Config.radius * Config.radius;

        for (int i = echoVisuals.Count - 1; i >= 0; i--)
            if (echoVisuals[i] == null)
                echoVisuals.RemoveAt(i);

        if (applyDamageThisFrame)
            combat.BeginTick(OverrideAttackSource.Cryo, Config.damagePerTick, center, player.forward);

        // Direct damage may remove the current enemy immediately. Explosion
        // damage is resolved after this backwards pass by EndTick.
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            EnemyHealth enemy = enemies[i];
            if (enemy == null || !enemy.isActiveAndEnabled) continue;

            if ((enemy.transform.position - center).sqrMagnitude > radiusSqr) continue;

            EnemyController controller = enemy.Controller;
            if (controller != null)
                controller.ApplySlow(Config.slowMultiplier, SlowRefreshWindow);

            if (applyDamageThisFrame)
                combat.Hit(enemy);
        }

        if (applyDamageThisFrame)
        {
            combat.EndTick();
            if (isEcho)
            {
                GameObject visual = OverrideCombatResolver.ShowPulse(center, Config.radius, Config.visualColor);
                if (visual != null)
                    echoVisuals.Add(visual);
            }
        }
    }

    private void BuildVisual()
    {
        if (Config.visualPrefabOverride != null)
        {
            fieldVisual = Instantiate(Config.visualPrefabOverride, transform);
            fieldVisual.transform.localPosition = Vector3.zero;

            CryoFieldVisual animation = fieldVisual.GetComponent<CryoFieldVisual>();
            if (animation != null)
                animation.Play(Config.radius);

            return;
        }

        fieldVisual = OverrideVisualUtility.CreateSphere("CryoVisual", transform, Vector3.zero, Config.radius * 2f, Config.visualColor);
        generatedFieldMaterial = fieldVisual.GetComponent<Renderer>().sharedMaterial;
    }

    private void ClearVisuals()
    {
        DestroyOwned(fieldVisual);
        fieldVisual = null;
        DestroyOwned(generatedFieldMaterial);
        generatedFieldMaterial = null;

        for (int i = 0; i < echoVisuals.Count; i++)
            DestroyOwned(echoVisuals[i]);
        echoVisuals.Clear();
    }

    private static void DestroyOwned(Object ownedObject)
    {
        if (ownedObject == null) return;
        if (Application.isPlaying) Destroy(ownedObject);
        else DestroyImmediate(ownedObject);
    }
}
