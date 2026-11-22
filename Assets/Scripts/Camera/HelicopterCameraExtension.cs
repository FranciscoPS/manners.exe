using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
[RequireComponent(typeof(CinemachinePositionComposer))]
public sealed class HelicopterCameraExtension : CinemachineExtension
{
    private const float DriftSeed = 7.1f;
    private const float PitchPhaseOffset = 5.3f;
    private const float AltitudePhaseOffset = 2.9f;
    private const float LowHealthRearmMargin = 0.1f;

    [Tooltip("Todos los valores de la cámara de helicóptero y su lente (sandbox: HelicopterCameraConfig_Sandbox).")]
    [SerializeField] private HelicopterCameraConfig config;
    [Tooltip("Volume de la escena que muestra la lente de TV. Su perfil, peso y prioridad salen del config.")]
    [SerializeField] private Volume lensVolume;
    [Tooltip("Vida del jugador, para el acercamiento con vida baja. Si queda vacío se busca la del objetivo que sigue la cámara.")]
    [SerializeField] private PlayerHealth playerHealth;

    private CinemachinePositionComposer composer;
    private float composerDistance;
    private Vector3 composerDamping;
    private ScreenComposerSettings composerComposition;
    private Quaternion restRotation;
    private bool hasRestPose;
    private bool composerApplied;
    private VolumeProfile lensSource;
    private DepthOfField lensDepth;

    private float flightTime;
    private float punchElapsed = float.PositiveInfinity;
    private HelicopterZoomPunch punch;
    private float zoom;
    private float crowd;
    private float zoneScale = 1f;
    private float idleTime;
    private Vector3 lastTargetPosition;
    private bool hasTargetSample;
    private bool lowHealthArmed = true;
    private bool subscribed;

    public HelicopterCameraConfig Config => config;

    public float CurrentFieldOfView { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        CaptureRestPose();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        if (!Application.isPlaying) return;

        CaptureRestPose();
        Subscribe();
        BroadcastMode.Changed += HandleModeChanged;
        HandleModeChanged();
    }

    private void Start()
    {
        if (Application.isPlaying) Subscribe();
    }

    private void OnDisable()
    {
        if (!Application.isPlaying) return;

        BroadcastMode.Changed -= HandleModeChanged;
        Unsubscribe();
        Restore();
    }

    public void Punch(HelicopterZoomPunch value)
    {
        if (config == null || !config.cameraMotion || value.strength <= 0f) return;

        punch = value;
        punchElapsed = 0f;
    }

    public override void PrePipelineMutateCameraStateCallback(CinemachineVirtualCameraBase vcam, ref CameraState curState, float deltaTime)
    {
        if (!Application.isPlaying || config == null) return;

        if (!BroadcastMode.Enabled)
        {
            if (hasRestPose) curState.RawOrientation = restRotation;
            return;
        }

        Advance(Mathf.Max(0f, deltaTime), vcam.Follow);
        ApplyComposer();
        ApplyLens();

        float yaw = config.heading;
        float pitch = config.pitch;
        if (config.cameraMotion)
        {
            yaw += HelicopterCameraMath.Orbit(flightTime, config.orbitAmplitude, config.orbitPeriod);
            pitch += HelicopterCameraMath.Orbit(flightTime + PitchPhaseOffset, config.pitchAmplitude, config.pitchPeriod);
        }
        curState.RawOrientation = Quaternion.Euler(pitch, yaw, 0f);

        LensSettings lens = curState.Lens;
        lens.FieldOfView = HelicopterCameraMath.ZoomedFieldOfView(config.fieldOfView, config.crowdWiden, crowd, zoom);
        curState.Lens = lens;
        CurrentFieldOfView = lens.FieldOfView;
    }

    protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
    {
        if (stage != CinemachineCore.Stage.Noise || !Application.isPlaying || config == null) return;
        if (!BroadcastMode.Enabled || !config.cameraMotion) return;

        Vector2 drift = HelicopterCameraMath.Drift(flightTime, config.driftFrequency, config.driftAmplitude, DriftSeed);
        state.OrientationCorrection = state.OrientationCorrection * Quaternion.Euler(drift.y, drift.x, 0f);
    }

    private void Advance(float deltaTime, Transform followTarget)
    {
        flightTime += deltaTime;
        if (!float.IsPositiveInfinity(punchElapsed)) punchElapsed += deltaTime;

        if (followTarget != null && deltaTime > 0f)
        {
            Vector3 position = followTarget.position;
            float speed = hasTargetSample ? (position - lastTargetPosition).magnitude / deltaTime : 0f;
            lastTargetPosition = position;
            hasTargetSample = true;
            idleTime = HelicopterCameraMath.IdleTime(idleTime, speed, config.idleSpeed, deltaTime);
        }
        zoneScale = HelicopterCameraMath.RecenterZone(zoneScale, idleTime, config.recenterDelay, config.recenterBlend, deltaTime);

        float target = 0f;
        if (config.cameraMotion && punchElapsed < punch.Duration)
            target = punch.strength * HelicopterCameraMath.PunchEnvelope(punchElapsed, punch.attack, punch.hold, punch.release);
        zoom = Mathf.Lerp(zoom, target, 1f - Mathf.Exp(-config.zoomSmoothing * deltaTime));

        float crowdTarget = HelicopterCameraMath.CrowdFactor(EnemyHealth.ActiveEnemies.Count, config.crowdStart, config.crowdFull);
        crowd = Mathf.MoveTowards(crowd, crowdTarget, deltaTime / config.crowdResponse);
    }

    private void CaptureRestPose()
    {
        if (composer == null) composer = GetComponent<CinemachinePositionComposer>();
        if (hasRestPose) return;

        restRotation = transform.rotation;
        if (composer != null)
        {
            composerDistance = composer.CameraDistance;
            composerDamping = composer.Damping;
            composerComposition = composer.Composition;
        }
        hasRestPose = true;
    }

    private void ApplyComposer()
    {
        if (composer == null) return;

        float altitude = config.cameraMotion
            ? HelicopterCameraMath.AltitudeScale(flightTime + AltitudePhaseOffset, config.altitudeAmplitude, config.altitudePeriod)
            : 1f;
        composer.CameraDistance = HelicopterCameraMath.DistanceForFramedHeight(config.framedHeight, config.fieldOfView) * altitude;
        composer.Damping = new Vector3(config.followDamping, config.followDamping, config.followDamping);

        ScreenComposerSettings composition = composer.Composition;
        Vector2 zone = new Vector2(Mathf.Max(0f, config.deadZone.x), Mathf.Max(0f, config.deadZone.y)) * zoneScale;
        composition.DeadZone.Enabled = zone.x > 0f || zone.y > 0f;
        composition.DeadZone.Size = zone;
        composition.HardLimits.Enabled = config.hardLimits.x > 0f && config.hardLimits.y > 0f;
        if (composition.HardLimits.Enabled) composition.HardLimits.Size = config.hardLimits;
        composition.Validate();
        composer.Composition = composition;
        composerApplied = true;
    }

    private void ApplyLens()
    {
        if (lensVolume == null) return;

        if (lensSource != config.lensProfile || lensVolume.sharedProfile != config.lensProfile)
        {
            lensSource = config.lensProfile;
            lensVolume.sharedProfile = lensSource;
            lensVolume.profile = null;
            lensDepth = null;
            if (lensSource != null) lensVolume.profile.TryGet(out lensDepth);
        }

        if (!Mathf.Approximately(lensVolume.priority, config.lensPriority)) lensVolume.priority = config.lensPriority;
        float weight = config.lensProfile != null ? config.lensWeight : 0f;
        if (!Mathf.Approximately(lensVolume.weight, weight)) lensVolume.weight = weight;

        if (lensDepth != null && composer != null)
        {
            lensDepth.gaussianStart.Override(composer.CameraDistance * config.depthOfFieldStart);
            lensDepth.gaussianEnd.Override(composer.CameraDistance * config.depthOfFieldEnd);
        }
    }

    private void Restore()
    {
        if (composer != null && composerApplied)
        {
            composer.CameraDistance = composerDistance;
            composer.Damping = composerDamping;
            composer.Composition = composerComposition;
        }
        composerApplied = false;
        if (lensVolume != null) lensVolume.weight = 0f;
        zoom = 0f;
        punchElapsed = float.PositiveInfinity;
        ResetFollowTracking();
    }

    private void ResetFollowTracking()
    {
        hasTargetSample = false;
        idleTime = 0f;
        zoneScale = 1f;
    }

    private void HandleModeChanged()
    {
        if (BroadcastMode.Enabled)
        {
            if (config == null) return;
            ApplyComposer();
            ApplyLens();
        }
        else
        {
            Restore();
        }
    }

    private void Subscribe()
    {
        if (subscribed) return;

        GameEvents.OnLevelUp += HandleLevelUp;
        GameEvents.OnChestSpawned += HandleChestSpawned;
        GameEvents.OnGameStarted += HandleGameStarted;

        OverrideManager.EnsureExists();
        if (OverrideManager.Instance != null) OverrideManager.Instance.OnOverrideActivated += HandleOverrideActivated;

        if (playerHealth == null && ComponentOwner != null && ComponentOwner.Follow != null)
            playerHealth = ComponentOwner.Follow.GetComponentInParent<PlayerHealth>();
        if (playerHealth != null) playerHealth.OnHealthChanged += HandleHealthChanged;

        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed) return;

        GameEvents.OnLevelUp -= HandleLevelUp;
        GameEvents.OnChestSpawned -= HandleChestSpawned;
        GameEvents.OnGameStarted -= HandleGameStarted;
        if (OverrideManager.Instance != null) OverrideManager.Instance.OnOverrideActivated -= HandleOverrideActivated;
        if (playerHealth != null) playerHealth.OnHealthChanged -= HandleHealthChanged;

        subscribed = false;
    }

    private void HandleGameStarted()
    {
        lowHealthArmed = true;
        punchElapsed = float.PositiveInfinity;
        ResetFollowTracking();
    }

    private void HandleLevelUp(int level)
    {
        if (config != null) Punch(config.levelUp);
    }

    private void HandleChestSpawned()
    {
        if (config != null) Punch(config.chestSpawned);
    }

    private void HandleOverrideActivated(OverrideData overrideData)
    {
        if (config != null) Punch(config.overrideActivated);
    }

    private void HandleHealthChanged(float current, float max)
    {
        if (config == null || max <= 0f) return;

        float ratio = current / max;
        if (lowHealthArmed && current > 0f && ratio < config.lowHealthThreshold)
        {
            lowHealthArmed = false;
            Punch(config.lowHealth);
        }
        else if (!lowHealthArmed && ratio > config.lowHealthThreshold + LowHealthRearmMargin)
        {
            lowHealthArmed = true;
        }
    }
}
