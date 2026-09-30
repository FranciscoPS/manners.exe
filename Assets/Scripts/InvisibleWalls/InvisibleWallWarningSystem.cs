using UnityEngine;

public class InvisibleWallWarningSystem : MonoBehaviour, IUpdateable
{
    public readonly struct Frame
    {
        public readonly InvisibleWallWarningConfig config;
        public readonly Vector3 playerCenter;
        public readonly Vector3 playerVelocity;
        public readonly float playerRadius;
        public readonly float zoneBottom;
        public readonly Vector2 zoneSize;
        public readonly Vector3 cameraPosition;
        public readonly bool hasCamera;
        public readonly bool suppressed;

        public Frame(InvisibleWallWarningConfig config, Vector3 playerCenter, Vector3 playerVelocity, float playerRadius,
            float zoneBottom, Vector2 zoneSize, Vector3 cameraPosition, bool hasCamera, bool suppressed)
        {
            this.config = config;
            this.playerCenter = playerCenter;
            this.playerVelocity = playerVelocity;
            this.playerRadius = playerRadius;
            this.zoneBottom = zoneBottom;
            this.zoneSize = zoneSize;
            this.cameraPosition = cameraPosition;
            this.hasCamera = hasCamera;
            this.suppressed = suppressed;
        }
    }

    [Header("Avisos")]
    [Tooltip("Un aviso por muro invisible. Los coloca y conecta 'Tools > Manners > Muros invisibles > 2. Colocar avisos en la escena abierta'.")]
    [SerializeField] private InvisibleWallWarning[] warnings;

    [Header("Jugador")]
    [Tooltip("Cada cuántos segundos se vuelve a buscar al jugador (tag 'Player') mientras no exista en la escena.")]
    [SerializeField, Min(0.05f)] private float playerSearchInterval = 0.5f;

    private Collider playerCollider;
    private Rigidbody playerBody;
    private PlayerHealth playerHealth;
    private Transform cameraTransform;
    private float searchTimer;

    public bool IsActive => isActiveAndEnabled;

    private void Awake()
    {
        if (warnings == null) return;

        for (int i = 0; i < warnings.Length; i++)
        {
            if (warnings[i] != null)
                warnings[i].Initialize();
        }
    }

    private void OnEnable()
    {
        if (UpdateManager.Instance != null)
            UpdateManager.Instance.Register(this as IUpdateable);
    }

    private void OnDisable()
    {
        if (UpdateManager.Instance != null)
            UpdateManager.Instance.Unregister(this as IUpdateable);
    }

    public void OnUpdate(float deltaTime)
    {
        if (deltaTime <= 0f || warnings == null) return;

        InvisibleWallWarningConfig config = InvisibleWallWarningConfig.Instance;
        Frame frame = BuildFrame(config, deltaTime);

        for (int i = 0; i < warnings.Length; i++)
        {
            if (warnings[i] != null)
                warnings[i].Tick(in frame, deltaTime);
        }
    }

    private Frame BuildFrame(InvisibleWallWarningConfig config, float deltaTime)
    {
        if (!ResolvePlayer(deltaTime))
            return new Frame(config, Vector3.zero, Vector3.zero, 0f, 0f, Vector2.zero, Vector3.zero, false, true);

        Bounds bounds = playerCollider.bounds;
        float radius = Mathf.Max(bounds.extents.x, bounds.extents.z);
        Vector2 zoneSize = new Vector2(
            radius * 2f + config.sidePadding * 2f,
            bounds.size.y + config.topPadding + config.bottomExtension);

        Vector3 velocity = playerBody != null ? playerBody.linearVelocity : Vector3.zero;
        velocity.y = 0f;

        bool suppressed = config.hideWhenPlayerDies && playerHealth != null && playerHealth.IsDead;
        bool hasCamera = ResolveCamera();
        Vector3 cameraPosition = hasCamera ? cameraTransform.position : Vector3.zero;

        return new Frame(config, bounds.center, velocity, radius, bounds.min.y - config.bottomExtension,
            zoneSize, cameraPosition, hasCamera, suppressed);
    }

    private bool ResolvePlayer(float deltaTime)
    {
        if (playerCollider != null && playerCollider.enabled) return true;

        searchTimer -= deltaTime;
        if (searchTimer > 0f) return false;
        searchTimer = playerSearchInterval;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return false;

        playerCollider = player.GetComponent<Collider>();
        playerBody = player.GetComponent<Rigidbody>();
        playerHealth = player.GetComponent<PlayerHealth>();

        return playerCollider != null && playerCollider.enabled;
    }

    private bool ResolveCamera()
    {
        if (cameraTransform != null) return true;

        Camera main = Camera.main;
        if (main == null) return false;

        cameraTransform = main.transform;
        return true;
    }
}
