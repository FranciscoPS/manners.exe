using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class EnemyController : MonoBehaviour, IUpdateable, IFixedUpdateable
{
    [Header("Visuals")]
    [SerializeField] private Transform visual1;
    [SerializeField] private Transform visual2;

    [Header("Rotation Speeds")]
    [SerializeField] private float rotationSpeed1 = 10f;
    [SerializeField] private float rotationSpeed2 = 10f;

    [Header("Apply To Visuals")]
    [SerializeField] private bool applyToVisual1 = true;
    [SerializeField] private bool applyToVisual2 = false;

    [Header("Movement")]
    [Tooltip("Radio suave usado por el flocking para mantener espacio entre enemigos.")]
    [SerializeField] private float agentRadius = 0.55f;
    [Tooltip("Aceleracion maxima al cambiar de direccion.")]
    [SerializeField] private float acceleration = 18f;
    [Tooltip("Mantiene compatibilidad con valores anteriores; el flocking lo usa como distancia minima al objetivo si se desea tunear despues.")]
    [SerializeField] private float attackRadius = 1.5f;
    [Tooltip("Que tan rapido cambia de direccion. Valores altos responden rapido; valores bajos evitan zigzag pero abren las curvas.")]
    [SerializeField] private float steeringResponsiveness = 10f;
    [Tooltip("Tiempo de movimiento lateral sin progreso antes de forzar salida de una orbita local.")]
    [SerializeField] private float orbitBreakDelay = 0.35f;
    [SerializeField] private bool useFlocking = true;

    private const float TargetRefreshInterval = 0.35f;

    private float moveSpeed = 3f;
    private float contactDamage = 10f;
    private float targetRefreshTimer;
    private float orbitTimer;
    private float lastTargetDistance;
    private Vector3 smoothedMoveDirection;

    private Transform player;
    private Rigidbody rb;

    private bool isKnockedBack;
    private float knockbackTimer;
    private Vector3 knockbackVelocity;
    private Vector3 planarVelocity;

    public float ContactDamage => contactDamage;
    public bool IsActive => gameObject.activeInHierarchy && enabled;
    public float AgentRadius => Mathf.Max(0.1f, agentRadius);
    public float AttackRadius => attackRadius;
    public Vector3 PlanarVelocity => planarVelocity;
    public Transform Target => player;

    public void SetStats(float newMoveSpeed, float newContactDamage)
    {
        moveSpeed = Mathf.Max(0f, newMoveSpeed);
        contactDamage = newContactDamage;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.constraints |= RigidbodyConstraints.FreezePositionY;
        rb.isKinematic = false;
    }

    private void OnEnable()
    {
        ResolvePlayer();

        isKnockedBack = false;
        knockbackTimer = 0f;
        knockbackVelocity = Vector3.zero;
        planarVelocity = Vector3.zero;
        smoothedMoveDirection = Vector3.zero;
        orbitTimer = 0f;
        lastTargetDistance = -1f;
        targetRefreshTimer = 0f;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        EnemyFlockManager.Instance.Register(this);

        if (UpdateManager.Instance != null)
        {
            UpdateManager.Instance.Register(this as IUpdateable);
            UpdateManager.Instance.Register(this as IFixedUpdateable);
        }
    }

    private void OnDisable()
    {
        if (EnemyFlockManager.HasInstance)
        {
            EnemyFlockManager.Instance.Unregister(this);
        }

        if (UpdateManager.Instance != null)
        {
            UpdateManager.Instance.Unregister(this as IUpdateable);
            UpdateManager.Instance.Unregister(this as IFixedUpdateable);
        }
    }

    public void OnUpdate(float deltaTime)
    {
        targetRefreshTimer -= deltaTime;
        if (player == null || targetRefreshTimer <= 0f)
        {
            ResolvePlayer();
            targetRefreshTimer = TargetRefreshInterval;
        }

        if (isKnockedBack)
        {
            knockbackTimer -= deltaTime;
            if (knockbackTimer <= 0f)
            {
                EndKnockback();
            }
        }

        RotateVisuals(deltaTime);
    }

    public void OnFixedUpdate(float fixedDeltaTime)
    {
        if (rb == null)
        {
            return;
        }

        if (isKnockedBack)
        {
            planarVelocity = ResolveObstacleVelocity(knockbackVelocity, fixedDeltaTime);
            rb.linearVelocity = new Vector3(planarVelocity.x, 0f, planarVelocity.z);
            return;
        }

        if (player == null)
        {
            ResolvePlayer();
            if (player == null)
            {
                planarVelocity = Vector3.MoveTowards(planarVelocity, Vector3.zero, acceleration * fixedDeltaTime);
                planarVelocity = ResolveObstacleVelocity(planarVelocity, fixedDeltaTime);
                rb.linearVelocity = new Vector3(planarVelocity.x, 0f, planarVelocity.z);
                return;
            }
        }

        Vector3 desiredDirection = useFlocking && EnemyFlockManager.HasInstance
            ? EnemyFlockManager.Instance.GetDesiredDirection(this, player.position)
            : GetDirectDirectionTo(player.position);

        desiredDirection = BreakLocalOrbit(desiredDirection, fixedDeltaTime);

        Vector3 moveDirection = SmoothDirection(desiredDirection, fixedDeltaTime);
        Vector3 desiredVelocity = moveDirection * moveSpeed;
        planarVelocity = Vector3.MoveTowards(planarVelocity, desiredVelocity, acceleration * fixedDeltaTime);
        planarVelocity = ResolveObstacleVelocity(planarVelocity, fixedDeltaTime);
        rb.linearVelocity = new Vector3(planarVelocity.x, 0f, planarVelocity.z);
    }

    public void WarpTo(Vector3 position)
    {
        isKnockedBack = false;
        knockbackTimer = 0f;
        knockbackVelocity = Vector3.zero;
        planarVelocity = Vector3.zero;
        smoothedMoveDirection = Vector3.zero;
        orbitTimer = 0f;
        lastTargetDistance = -1f;

        if (rb != null)
        {
            rb.position = position;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        else
        {
            transform.position = position;
        }
    }

    public void ApplyKnockback(Vector3 direction, float force, float duration)
    {
        if (force <= 0f || duration <= 0f)
        {
            return;
        }

        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
        {
            direction = -transform.forward;
        }

        direction.Normalize();

        isKnockedBack = true;
        knockbackTimer = duration;
        knockbackVelocity = direction * force;
        planarVelocity = knockbackVelocity;
        smoothedMoveDirection = direction;
        orbitTimer = 0f;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = new Vector3(knockbackVelocity.x, 0f, knockbackVelocity.z);
        }
    }

    private void EndKnockback()
    {
        isKnockedBack = false;
        knockbackVelocity = Vector3.zero;
        planarVelocity = Vector3.zero;
        smoothedMoveDirection = Vector3.zero;
        orbitTimer = 0f;
    }

    private void ResolvePlayer()
    {
        if (EnemyFlockManager.HasInstance && EnemyFlockManager.Instance.Target != null)
        {
            player = EnemyFlockManager.Instance.Target;
            return;
        }

        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    private Vector3 GetDirectDirectionTo(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;
        return direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.zero;
    }

    private Vector3 ResolveObstacleVelocity(Vector3 velocity, float fixedDeltaTime)
    {
        velocity.y = 0f;
        if (EnemyFlockManager.HasInstance)
        {
            velocity = EnemyFlockManager.Instance.ResolveObstacleVelocity(this, velocity, fixedDeltaTime);
            velocity.y = 0f;
        }

        return velocity;
    }

    private Vector3 SmoothDirection(Vector3 desiredDirection, float fixedDeltaTime)
    {
        desiredDirection.y = 0f;
        if (desiredDirection.sqrMagnitude <= 0.001f)
        {
            smoothedMoveDirection = Vector3.MoveTowards(
                smoothedMoveDirection,
                Vector3.zero,
                steeringResponsiveness * fixedDeltaTime);
            return smoothedMoveDirection;
        }

        desiredDirection.Normalize();
        if (smoothedMoveDirection.sqrMagnitude <= 0.001f)
        {
            smoothedMoveDirection = desiredDirection;
        }
        else
        {
            smoothedMoveDirection = Vector3.Slerp(
                smoothedMoveDirection,
                desiredDirection,
                Mathf.Clamp01(steeringResponsiveness * fixedDeltaTime)).normalized;
        }

        return smoothedMoveDirection;
    }

    private Vector3 BreakLocalOrbit(Vector3 desiredDirection, float fixedDeltaTime)
    {
        if (player == null)
        {
            return desiredDirection;
        }

        Vector3 directDirection = GetDirectDirectionTo(player.position);
        if (directDirection.sqrMagnitude <= 0.001f)
        {
            orbitTimer = 0f;
            return desiredDirection;
        }

        float targetDistance = Vector3.Distance(
            new Vector3(transform.position.x, 0f, transform.position.z),
            new Vector3(player.position.x, 0f, player.position.z));

        bool hasPreviousDistance = lastTargetDistance >= 0f;
        bool notApproaching = hasPreviousDistance && targetDistance > lastTargetDistance - 0.02f;

        Vector3 velocityDirection = planarVelocity;
        velocityDirection.y = 0f;
        bool movingSideways = velocityDirection.sqrMagnitude > 0.05f &&
                              Vector3.Dot(velocityDirection.normalized, directDirection) < 0.15f;

        if (notApproaching && movingSideways && targetDistance > attackRadius + 0.75f)
        {
            orbitTimer += fixedDeltaTime;
        }
        else
        {
            orbitTimer = Mathf.Max(0f, orbitTimer - fixedDeltaTime * 2f);
        }

        lastTargetDistance = targetDistance;

        if (orbitTimer < orbitBreakDelay)
        {
            return desiredDirection;
        }

        Vector3 corrected = desiredDirection + directDirection * 1.75f;
        corrected.y = 0f;
        return corrected.sqrMagnitude > 0.001f ? corrected.normalized : directDirection;
    }

    private void RotateVisuals(float deltaTime)
    {
        Vector3 lookDirection = planarVelocity;
        lookDirection.y = 0f;

        if (lookDirection.sqrMagnitude < 0.01f && player != null)
        {
            lookDirection = player.position - transform.position;
            lookDirection.y = 0f;
        }

        if (lookDirection.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(lookDirection.normalized);

        if (applyToVisual1 && visual1 != null)
        {
            visual1.rotation = Quaternion.Slerp(visual1.rotation, targetRotation, rotationSpeed1 * deltaTime);
        }

        if (applyToVisual2 && visual2 != null)
        {
            visual2.rotation = Quaternion.Slerp(visual2.rotation, targetRotation, rotationSpeed2 * deltaTime);
        }
    }
}
