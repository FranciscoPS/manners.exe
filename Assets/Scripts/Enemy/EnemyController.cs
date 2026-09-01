using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour, IUpdateable, IFixedUpdateable
{
    [Header("Visuals")]
    [SerializeField] private Transform visual1;
    [SerializeField] private Transform visual2;

    [Header("Rotation Speeds")]
    [Tooltip("Velocidad de rotación para el primer visual")]
    [SerializeField] private float rotationSpeed1 = 10f;
    [Tooltip("Velocidad de rotación para el segundo visual")]
    [SerializeField] private float rotationSpeed2 = 10f;

    [Header("Apply To Visuals")]
    [Tooltip("Si está activado, se aplicará la rotación al primer visual si está asignado")]
    [SerializeField] private bool applyToVisual1 = true;
    [Tooltip("Si está activado, se aplicará la rotación al segundo visual si está asignado")]
    [SerializeField] private bool applyToVisual2 = false;

    private float moveSpeed = 3f;
    private float contactDamage = 10f;

    private Transform player;
    private NavMeshAgent agent;
    private bool useNavMesh = true;
    private Rigidbody rb;

    private bool isKnockedBack = false;
    private float knockbackTimer = 0f;
    private Vector3 knockbackVelocity = Vector3.zero;

    private Vector3 separation = Vector3.zero;

    private float slowMultiplier = 1f;
    private float slowEndTime = -1f;

    [Header("Movement")]
    [Tooltip("Distancia a la que el enemigo se detiene alrededor del jugador. Evita que todos converjan en el mismo punto.")]
    [SerializeField] private float attackRadius = 1.5f;

    private const float StuckCheckInterval = 5f;
    private const float StuckMoveThreshold = 0.8f;
    private const float DetourDistance     = 15f;

    private bool    isDetouring       = false;
    private float   stuckTimer        = 0f;
    private Vector3 lastCheckPosition;

    public float ContactDamage => contactDamage;

    public bool IsActive => gameObject.activeInHierarchy && enabled;

    public bool WantsSeparation => IsActive && !isKnockedBack;

    public void SetSeparation(Vector3 value) => separation = value;

    public void ApplySlow(float multiplier, float duration)
    {
        slowMultiplier = Mathf.Clamp01(multiplier);
        slowEndTime = Time.time + duration;
    }

    private float CurrentSpeedMultiplier()
    {
        return Time.time < slowEndTime ? slowMultiplier : 1f;
    }

    public void SetStats(float newMoveSpeed, float newContactDamage)
    {
        moveSpeed = newMoveSpeed;
        contactDamage = newContactDamage;

        if (agent != null)
        {
            agent.speed = moveSpeed;
        }
    }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();

        if (agent != null)
        {
            useNavMesh = true;
            agent.speed = moveSpeed;
            agent.acceleration = 8f;
            agent.angularSpeed = 120f;
            agent.stoppingDistance = 0.5f;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;

            if (rb != null)
            {
                rb.isKinematic = true;
            }
        }
        else if (rb != null)
        {
            useNavMesh = false;
            rb.freezeRotation = true;
            rb.isKinematic = false;
        }
    }

    private void OnEnable()
    {

        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        isKnockedBack = false;
        knockbackTimer = 0f;
        knockbackVelocity = Vector3.zero;

        separation = Vector3.zero;

        slowMultiplier = 1f;
        slowEndTime = -1f;

        isDetouring       = false;
        stuckTimer        = 0f;
        lastCheckPosition = transform.position;

        if (agent != null)
        {
            agent.enabled = true;
            agent.speed = moveSpeed;
            agent.avoidancePriority = Random.Range(1, 100);
        }

        if (rb != null)
        {
            rb.isKinematic = useNavMesh;
            rb.linearVelocity = Vector3.zero;
        }

        if (UpdateManager.Instance != null)
        {
            UpdateManager.Instance.Register(this as IUpdateable);
            UpdateManager.Instance.Register(this as IFixedUpdateable);
        }

        EnemySeparationManager.Register(this);
    }

    private void OnDisable()
    {

        if (UpdateManager.Instance != null)
        {
            UpdateManager.Instance.Unregister(this as IUpdateable);
            UpdateManager.Instance.Unregister(this as IFixedUpdateable);
        }

        EnemySeparationManager.Unregister(this);
    }

    public void OnUpdate(float deltaTime)
    {

        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (player == null) return;
        }

        if (isKnockedBack)
        {
            knockbackTimer -= deltaTime;
            if (knockbackTimer <= 0f)
            {
                EndKnockback();
            }
            return;
        }

        if (useNavMesh && agent != null && agent.isOnNavMesh)
        {
            agent.speed = moveSpeed * CurrentSpeedMultiplier();

            stuckTimer += deltaTime;
            if (stuckTimer >= StuckCheckInterval)
            {
                stuckTimer = 0f;
                if (Vector3.Distance(transform.position, lastCheckPosition) < StuckMoveThreshold)
                    TryDetour();
                lastCheckPosition = transform.position;
            }

            if (isDetouring)
            {
                if (!agent.pathPending && agent.remainingDistance < 1.2f)
                    isDetouring = false;
            }
            else
            {
                Vector3 toPlayer = player.position - transform.position;
                toPlayer.y = 0f;
                Vector3 destination = toPlayer.magnitude > attackRadius
                    ? player.position - toPlayer.normalized * attackRadius
                    : transform.position;
                agent.SetDestination(destination);
            }

            if (separation.sqrMagnitude > 0.0001f)
            {
                agent.Move(new Vector3(separation.x, 0f, separation.z) * deltaTime);
            }
        }

        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            if (applyToVisual1 && visual1 != null)
            {
                visual1.rotation = Quaternion.Slerp(
                    visual1.rotation,
                    targetRotation,
                    rotationSpeed1 * deltaTime
                );
            }

            if (applyToVisual2 && visual2 != null)
            {
                visual2.rotation = Quaternion.Slerp(
                    visual2.rotation,
                    targetRotation,
                    rotationSpeed2 * deltaTime
                );
            }
        }
    }

    public void OnFixedUpdate(float fixedDeltaTime)
    {

        if (isKnockedBack && rb != null)
        {
            rb.linearVelocity = knockbackVelocity;
            return;
        }

        if (!useNavMesh && rb != null && player != null)
        {
            float currentSpeed = moveSpeed * CurrentSpeedMultiplier();
            Vector3 direction = (player.position - transform.position).normalized;
            rb.linearVelocity = new Vector3(
                direction.x * currentSpeed + separation.x,
                rb.linearVelocity.y,
                direction.z * currentSpeed + separation.z);
        }
    }

    public void WarpTo(Vector3 position)
    {
        isKnockedBack = false;
        knockbackTimer = 0f;
        knockbackVelocity = Vector3.zero;
        isDetouring = false;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.Warp(position);
        }
        else
        {
            transform.position = position;
        }
    }

    private void TryDetour()
    {
        if (player == null || agent == null || !agent.isOnNavMesh)
        {
            Debug.Log($"[Detour:{name}] IGNORADO — player={player != null} agent={agent != null} onNavMesh={agent?.isOnNavMesh}");
            return;
        }

        Vector3 awayFromPlayer = transform.position - player.position;
        awayFromPlayer.y = 0f;
        if (awayFromPlayer.sqrMagnitude < 0.01f)
            awayFromPlayer = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
        awayFromPlayer.Normalize();

        Vector3 perp   = Vector3.Cross(awayFromPlayer, Vector3.up) * Random.Range(-0.8f, 0.8f);
        Vector3 target = player.position + (awayFromPlayer + perp).normalized * DetourDistance;

        UnityEngine.AI.NavMeshHit hit;
        if (UnityEngine.AI.NavMesh.SamplePosition(target, out hit, 8f, UnityEngine.AI.NavMesh.AllAreas))
        {
            isDetouring = true;
            agent.SetDestination(hit.position);
            Debug.Log($"[Detour:{name}] Desviando a {hit.position} (dist al jugador={Vector3.Distance(transform.position, player.position):F1}m)");
        }
        else
        {
            Debug.Log($"[Detour:{name}] SamplePosition FALLÓ — no hay NavMesh cerca de {target}");
        }
    }

    public void ApplyKnockback(Vector3 direction, float force, float duration)
    {
        if (force <= 0f) return;

        if (agent != null && agent.enabled)
        {
            agent.enabled = false;
        }

        if (rb != null && rb.isKinematic)
        {
            rb.isKinematic = false;
        }

        isKnockedBack = true;
        knockbackTimer = duration;
        knockbackVelocity = new Vector3(direction.x * force, 0f, direction.z * force);

        if (rb != null)
        {
            rb.linearVelocity = knockbackVelocity;
        }
    }

    private void EndKnockback()
    {
        isKnockedBack = false;
        knockbackVelocity = Vector3.zero;

        if (useNavMesh && agent != null)
        {
            agent.enabled = true;
            if (rb != null)
            {
                rb.isKinematic = true;
            }
        }
        else if (rb != null)
        {
            rb.isKinematic = false;
        }
    }
}
