using System.Collections.Generic;
using UnityEngine;

public class LaserBeamEffect : MonoBehaviour, ISynergyEffect, IUpdateable
{
    private static readonly int BeamLengthId = Shader.PropertyToID("_BeamLength");
    private const float ImpactGlowLift = 0.05f;

    private LaserBeamConfig config;
    private Transform player;
    private LineRenderer line;
    private Transform visualRoot;
    private ParticleSystem impactGlow;
    private MaterialPropertyBlock beamProperties;
    private float baseWidth;

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

        SetVisualActive(true);
        line.widthMultiplier = 0f;
        UpdateBeamPositions(sweepStartDistance);

        if (Config.fireSFX != null && MusicManager.Instance != null)
            MusicManager.Instance.PlaySFXOneShot(Config.fireSFX, Config.sfxVolume);
    }

    private Vector3? FindRandomBuildingPosition()
    {
        List<BuildingsScript> buildings = BuildingsScript.ActiveBuildings;
        float rangeSqr = Config.range * Config.range;
        int inRangeCount = 0;

        for (int i = 0; i < buildings.Count; i++)
        {
            if (buildings[i] == null) continue;
            if ((buildings[i].transform.position - player.position).sqrMagnitude <= rangeSqr)
                inRangeCount++;
        }

        if (inRangeCount == 0) return null;

        int pick = Random.Range(0, inRangeCount);
        for (int i = 0; i < buildings.Count; i++)
        {
            if (buildings[i] == null) continue;
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

        line.widthMultiplier = baseWidth * BeamEnvelope();
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
            SetVisualActive(false);
        }
    }

    private float BeamEnvelope()
    {
        float fadeIn = Mathf.Clamp01(sweepTimer / Mathf.Max(0.001f, Config.beamFadeIn));
        float fadeOut = Mathf.Clamp01((Config.sweepDuration - sweepTimer) / Mathf.Max(0.001f, Config.beamFadeOut));
        return Mathf.Min(fadeIn, fadeOut);
    }

    private Vector3 UpdateBeamPositions(float distance)
    {
        Vector3 groundPoint = sweepGroundOrigin + sweepDirection * distance;
        Vector3 origin = player.position + Vector3.up * Config.beamOriginHeight;
        Vector3 beam = groundPoint - origin;

        line.SetPosition(0, origin);
        line.SetPosition(1, groundPoint);

        beamProperties.SetFloat(BeamLengthId, beam.magnitude);
        line.SetPropertyBlock(beamProperties);

        if (visualRoot != null)
            visualRoot.SetPositionAndRotation(origin, beam.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(beam) : visualRoot.rotation);

        if (impactGlow != null)
            impactGlow.transform.position = groundPoint + Vector3.up * ImpactGlowLift;

        return groundPoint;
    }

    private void SetVisualActive(bool active)
    {
        if (visualRoot != null)
            visualRoot.gameObject.SetActive(active);
        else if (line != null)
            line.enabled = active;

        if (impactGlow != null)
            impactGlow.gameObject.SetActive(active);
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

            Vector3 closestOnBeam = ClosestPointOnSegment(sweepGroundOrigin, groundPoint, flat);
            if ((flat - closestOnBeam).sqrMagnitude <= radiusSqr)
                enemy.TakeDamage(Config.damage);
        }

        List<BuildingsScript> buildings = BuildingsScript.ActiveBuildings;
        for (int i = 0; i < buildings.Count; i++)
        {
            BuildingsScript building = buildings[i];
            if (building == null) continue;

            if (building.IsSegmentWithinHitRange(sweepGroundOrigin, groundPoint, Config.impactRadius))
                building.DestroyByHit(groundPoint);
        }
    }

    private static Vector3 ClosestPointOnSegment(Vector3 a, Vector3 b, Vector3 p)
    {
        Vector3 ab = b - a;
        float sqrLen = ab.sqrMagnitude;
        if (sqrLen < 0.0001f) return a;

        float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / sqrLen);
        return a + ab * t;
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
        if (Config.visualPrefabOverride != null)
            BuildPrefabVisual();
        else
            BuildDefaultVisual();

        line.positionCount = 2;
        line.useWorldSpace = true;
        baseWidth = line.widthMultiplier;
        beamProperties = new MaterialPropertyBlock();

        SetVisualActive(false);
    }

    private void BuildPrefabVisual()
    {
        GameObject visual = Instantiate(Config.visualPrefabOverride, transform);
        visual.name = Config.visualPrefabOverride.name;
        visualRoot = visual.transform;

        line = visual.GetComponentInChildren<LineRenderer>(true);
        if (line == null)
        {
            line = visual.AddComponent<LineRenderer>();
            ApplyDefaultLineStyle();
        }

        line.widthMultiplier = Config.lineWidth;

        if (Config.beamMaterialOverride != null)
        {
            line.sharedMaterial = Config.beamMaterialOverride;
            line.textureMode = LineTextureMode.Stretch;
            line.textureScale = Vector2.one;
        }

        ParticleSystem originGlow = visual.GetComponentInChildren<ParticleSystem>(true);
        if (originGlow != null)
        {
            impactGlow = Instantiate(originGlow, transform);
            impactGlow.name = "ImpactGlow";
        }
    }

    private void BuildDefaultVisual()
    {
        line = gameObject.AddComponent<LineRenderer>();
        ApplyDefaultLineStyle();
    }

    private void ApplyDefaultLineStyle()
    {
        line.widthCurve = new AnimationCurve(new Keyframe(0f, 0.9f), new Keyframe(1f, 0.6f));
        line.widthMultiplier = Config.lineWidth;
        line.alignment = LineAlignment.View;
        line.textureMode = LineTextureMode.Stretch;
        line.textureScale = Vector2.one;
        line.numCapVertices = 4;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;

        if (Config.beamMaterialOverride != null)
        {
            line.sharedMaterial = Config.beamMaterialOverride;
            line.startColor = Color.white;
            line.endColor = Color.white;
        }
        else
        {
            line.material = new Material(SynergyVisualUtility.FindUnlitShader());
            line.startColor = Color.Lerp(Config.beamColor, Color.white, 0.5f);
            line.endColor = Config.beamColor;
        }
    }
}
