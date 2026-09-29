using System.Collections.Generic;
using UnityEngine;

public class LaserBeamEffect : MonoBehaviour, IOverrideEffect, IUpdateable
{
    private static readonly int BeamLengthId = Shader.PropertyToID("_BeamLength");
    private const float ImpactGlowLift = 0.05f;
    private const int GroundPointAttempts = 12;

    private sealed class BeamVisual
    {
        public LineRenderer line;
        public Transform root;
        public ParticleSystem impactGlow;
        public LaserImpactVisual impactVisual;
        public readonly MaterialPropertyBlock properties = new MaterialPropertyBlock();
        public Vector3 groundOrigin;
        public Vector3 direction;
        public float startDistance;
        public float endDistance;
        public float baseWidth;
        public float damageTickTimer;
    }

    private LaserBeamConfig config;
    private Transform player;
    private readonly List<BeamVisual> beamVisuals = new List<BeamVisual>();
    private readonly OverrideCombatResolver combat = new OverrideCombatResolver();
    private readonly List<Vector3> claimedDirections = new List<Vector3>();
    private readonly List<EnemyHealth> claimedEnemies = new List<EnemyHealth>();
    private readonly List<BuildingsScript> claimedBuildings = new List<BuildingsScript>();
    private Material ownedBeamMaterial;
    private bool ownsConfig;
    private int activeBeamCount;

    private float cooldownTimer;
    private bool sweeping;
    private float sweepTimer;

    public bool IsActive => isActiveAndEnabled;

    private LaserBeamConfig Config
    {
        get
        {
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<LaserBeamConfig>();
                ownsConfig = true;
                Debug.LogWarning($"[OVERRIDE] {name} no tiene LaserBeamConfig asignado; usando valores por defecto.");
            }

            return config;
        }
    }

    private float MinSeparationDot => Mathf.Cos(Mathf.Clamp(Config.minBeamSeparationAngle, 0f, 180f) * Mathf.Deg2Rad);

    public void Configure(LaserBeamConfig effectConfig)
    {
        ReleaseOwnedConfig();
        config = effectConfig;
    }

    public void Activate(Transform target)
    {
        player = target;
        cooldownTimer = 0f;
        sweeping = false;
        EnsureBeamVisuals(1);
        SetVisualActive(false);
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
        sweeping = false;
        cooldownTimer = config != null ? config.interval : 3f;
        SetVisualActive(false);
    }

    private void OnDestroy()
    {
        FlipbookMaterialUtility.Release(ref ownedBeamMaterial);
        ReleaseOwnedConfig();
    }

    private void ReleaseOwnedConfig()
    {
        if (!ownsConfig) return;
        if (config != null)
        {
            if (Application.isPlaying) Destroy(config);
            else DestroyImmediate(config);
        }
        ownsConfig = false;
        config = null;
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
            BeginSweep();
        }
    }

    private void BeginSweep()
    {
        sweeping = true;
        sweepTimer = 0f;
        activeBeamCount = OverrideCombatResolver.EmissionCount(Config.maxExtraBeams);
        EnsureBeamVisuals(activeBeamCount);
        claimedDirections.Clear();
        claimedEnemies.Clear();
        claimedBuildings.Clear();

        for (int i = 0; i < beamVisuals.Count; i++)
        {
            BeamVisual beam = beamVisuals[i];
            bool active = i < activeBeamCount;
            SetVisualActive(beam, active);
            if (!active) continue;

            AimBeam(beam, PickTarget());
            beam.damageTickTimer = 0f;
            beam.line.widthMultiplier = 0f;
            UpdateBeamPositions(beam, beam.startDistance);
            if (beam.impactVisual != null) beam.impactVisual.SetIntensity(0f);
        }

        if (Config.fireShake > 0f && CameraShakeManager.Instance != null)
            CameraShakeManager.Instance.Shake(Config.fireShake);

        if (Config.fireSFX != null && MusicManager.Instance != null)
            MusicManager.Instance.PlaySFXOneShot(Config.fireSFX, Config.sfxVolume);
    }

    private Vector3 PickTarget()
    {
        if (!TryPickEnemy(out Vector3 target) && !TryPickBuilding(out target))
            target = PickGroundPoint();

        claimedDirections.Add(FlatDirection(target));
        return target;
    }

    private void AimBeam(BeamVisual beam, Vector3 target)
    {
        beam.groundOrigin = new Vector3(player.position.x, target.y, player.position.z);
        beam.direction = FlatDirection(target);
        beam.startDistance = Vector3.Distance(beam.groundOrigin, target);
        beam.endDistance = beam.startDistance + Config.extendDistance;
    }

    private bool TryPickEnemy(out Vector3 target)
    {
        List<EnemyHealth> enemies = EnemyHealth.ActiveEnemies;
        EnemyHealth nearest = null;
        float nearestSqr = Config.range * Config.range;

        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyHealth enemy = enemies[i];
            if (enemy == null || claimedEnemies.Contains(enemy)) continue;

            Vector3 position = enemy.transform.position;
            float distSqr = (position - player.position).sqrMagnitude;
            if (distSqr > nearestSqr || !IsSeparated(FlatDirection(position))) continue;

            nearestSqr = distSqr;
            nearest = enemy;
        }

        target = nearest != null ? nearest.transform.position : Vector3.zero;
        if (nearest == null) return false;

        claimedEnemies.Add(nearest);
        return true;
    }

    private bool TryPickBuilding(out Vector3 target)
    {
        List<BuildingsScript> buildings = BuildingsScript.ActiveBuildings;
        int candidateCount = 0;

        for (int i = 0; i < buildings.Count; i++)
            if (IsBuildingCandidate(buildings[i]))
                candidateCount++;

        target = Vector3.zero;
        if (candidateCount == 0) return false;

        int pick = Random.Range(0, candidateCount);
        for (int i = 0; i < buildings.Count; i++)
        {
            if (!IsBuildingCandidate(buildings[i])) continue;
            if (pick-- > 0) continue;

            claimedBuildings.Add(buildings[i]);
            target = buildings[i].transform.position;
            return true;
        }

        return false;
    }

    private bool IsBuildingCandidate(BuildingsScript building)
    {
        if (building == null || claimedBuildings.Contains(building)) return false;

        Vector3 position = building.transform.position;
        return (position - player.position).sqrMagnitude <= Config.range * Config.range && IsSeparated(FlatDirection(position));
    }

    private Vector3 PickGroundPoint()
    {
        Vector3 best = RandomGroundPoint();
        float bestDot = ClosestClaimedDot(FlatDirection(best));

        for (int attempt = 1; attempt < GroundPointAttempts && bestDot > MinSeparationDot; attempt++)
        {
            Vector3 candidate = RandomGroundPoint();
            float dot = ClosestClaimedDot(FlatDirection(candidate));
            if (dot >= bestDot) continue;

            best = candidate;
            bestDot = dot;
        }

        return best;
    }

    private Vector3 RandomGroundPoint()
    {
        Vector2 offset = Random.insideUnitCircle.normalized * Config.range * Random.Range(0.3f, 1f);
        return player.position + new Vector3(offset.x, 0f, offset.y);
    }

    private bool IsSeparated(Vector3 direction)
    {
        return ClosestClaimedDot(direction) <= MinSeparationDot;
    }

    private float ClosestClaimedDot(Vector3 direction)
    {
        float closest = -1f;
        for (int i = 0; i < claimedDirections.Count; i++)
            closest = Mathf.Max(closest, Vector3.Dot(direction, claimedDirections[i]));
        return closest;
    }

    private Vector3 FlatDirection(Vector3 target)
    {
        Vector3 flat = target - player.position;
        flat.y = 0f;
        if (flat.sqrMagnitude > 0.0001f) return flat.normalized;

        Vector3 forward = Vector3.ProjectOnPlane(player.forward, Vector3.up);
        return forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
    }

    private void UpdateSweep(float deltaTime)
    {
        sweepTimer += deltaTime;
        float t = Config.sweepDuration > 0f ? Mathf.Clamp01(sweepTimer / Config.sweepDuration) : 1f;

        float envelope = BeamEnvelope();
        float width = envelope * IgnitionKick() * WidthPulse();
        for (int i = 0; i < activeBeamCount; i++)
        {
            BeamVisual beam = beamVisuals[i];
            beam.line.widthMultiplier = beam.baseWidth * width;
            Vector3 groundPoint = UpdateBeamPositions(beam, Mathf.Lerp(beam.startDistance, beam.endDistance, t));
            if (beam.impactVisual != null) beam.impactVisual.SetIntensity(envelope);

            beam.damageTickTimer -= deltaTime;
            if (beam.damageTickTimer <= 0f)
            {
                beam.damageTickTimer = Mathf.Max(0.01f, Config.damageTickInterval);
                DamageNear(beam.groundOrigin, groundPoint);
            }
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

    private float IgnitionKick()
    {
        if (Config.ignitionDuration <= 0f || Config.ignitionKick <= 0f) return 1f;

        float remaining = 1f - Mathf.Clamp01(sweepTimer / Config.ignitionDuration);
        return 1f + Config.ignitionKick * remaining * remaining;
    }

    private float WidthPulse()
    {
        return 1f + Config.pulseAmount * Mathf.Sin(sweepTimer * 2f * Mathf.PI * Config.pulseFrequency);
    }

    private Vector3 UpdateBeamPositions(BeamVisual visual, float distance)
    {
        Vector3 groundPoint = visual.groundOrigin + visual.direction * distance;
        Vector3 origin = player.position + Vector3.up * Config.beamOriginHeight;
        Vector3 beam = groundPoint - origin;

        visual.line.SetPosition(0, origin);
        visual.line.SetPosition(1, groundPoint);

        visual.properties.SetFloat(BeamLengthId, beam.magnitude);
        visual.line.SetPropertyBlock(visual.properties);

        visual.root.SetPositionAndRotation(origin, beam.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(beam) : visual.root.rotation);

        if (visual.impactGlow != null)
            visual.impactGlow.transform.position = groundPoint + Vector3.up * ImpactGlowLift;

        if (visual.impactVisual != null)
            visual.impactVisual.Follow(new Vector3(groundPoint.x, player.position.y + Config.groundOffset, groundPoint.z));

        return groundPoint;
    }

    private void SetVisualActive(bool active)
    {
        for (int i = 0; i < beamVisuals.Count; i++)
            SetVisualActive(beamVisuals[i], active && i < activeBeamCount);
    }

    private static void SetVisualActive(BeamVisual visual, bool active)
    {
        visual.root.gameObject.SetActive(active);

        if (visual.impactGlow != null)
            visual.impactGlow.gameObject.SetActive(active);

        if (visual.impactVisual != null)
            visual.impactVisual.SetVisible(active);
    }

    private void DamageNear(Vector3 groundOrigin, Vector3 groundPoint)
    {
        combat.BeginTick(OverrideAttackSource.Laser, Config.damage, groundOrigin, groundPoint - groundOrigin);
        List<EnemyHealth> enemies = EnemyHealth.ActiveEnemies;
        float radiusSqr = Config.impactRadius * Config.impactRadius;

        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            EnemyHealth enemy = enemies[i];
            if (enemy == null) continue;

            Vector3 flat = enemy.transform.position;
            flat.y = groundPoint.y;

            Vector3 closestOnBeam = ClosestPointOnSegment(groundOrigin, groundPoint, flat);
            if ((flat - closestOnBeam).sqrMagnitude <= radiusSqr)
                combat.Hit(enemy);
        }

        combat.EndTick();

        List<BuildingsScript> buildings = BuildingsScript.ActiveBuildings;
        for (int i = buildings.Count - 1; i >= 0; i--)
        {
            BuildingsScript building = buildings[i];
            if (building == null) continue;

            if (building.IsSegmentWithinHitRange(groundOrigin, groundPoint, Config.impactRadius))
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

    private void EnsureBeamVisuals(int count)
    {
        while (beamVisuals.Count < count)
        {
            BeamVisual visual = new BeamVisual();
            if (Config.visualPrefabOverride != null)
                BuildPrefabVisual(visual);
            else
                BuildDefaultVisual(visual);

            visual.line.positionCount = 2;
            visual.line.useWorldSpace = true;
            visual.baseWidth = visual.line.widthMultiplier;
            SetVisualActive(visual, false);
            beamVisuals.Add(visual);
        }
    }

    private void BuildPrefabVisual(BeamVisual beam)
    {
        GameObject visual = Instantiate(Config.visualPrefabOverride, transform);
        visual.name = Config.visualPrefabOverride.name;
        beam.root = visual.transform;

        beam.impactVisual = visual.GetComponentInChildren<LaserImpactVisual>(true);
        if (beam.impactVisual != null)
        {
            beam.impactVisual.transform.SetParent(transform, false);
            beam.impactVisual.Configure(Config.impactRadius);
        }

        beam.line = visual.GetComponentInChildren<LineRenderer>(true);
        if (beam.line == null)
        {
            beam.line = visual.AddComponent<LineRenderer>();
            ApplyDefaultLineStyle(beam.line);
        }

        beam.line.widthMultiplier = Config.lineWidth;

        if (Config.beamMaterialOverride != null)
        {
            beam.line.sharedMaterial = Config.beamMaterialOverride;
            beam.line.textureMode = LineTextureMode.Stretch;
            beam.line.textureScale = Vector2.one;
        }

        ParticleSystem originGlow = visual.GetComponentInChildren<ParticleSystem>(true);
        if (originGlow != null)
        {
            beam.impactGlow = Instantiate(originGlow, transform);
            beam.impactGlow.name = "ImpactGlow";
        }
    }

    private void BuildDefaultVisual(BeamVisual beam)
    {
        GameObject visual = new GameObject("LaserBeam");
        visual.transform.SetParent(transform, false);
        beam.root = visual.transform;
        beam.line = visual.AddComponent<LineRenderer>();
        ApplyDefaultLineStyle(beam.line);
    }

    private void ApplyDefaultLineStyle(LineRenderer line)
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
            if (ownedBeamMaterial == null)
                ownedBeamMaterial = new Material(OverrideVisualUtility.FindUnlitShader());
            line.sharedMaterial = ownedBeamMaterial;
            line.startColor = Color.Lerp(Config.beamColor, Color.white, 0.5f);
            line.endColor = Config.beamColor;
        }
    }
}
