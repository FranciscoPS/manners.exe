using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour, IUpdateable, IFixedUpdateable
{
    [SerializeField] private Transform visual1;
    [SerializeField] private Transform visual2;

    [Header("Visual Rotation")]
    [SerializeField] private float rotationSpeed1 = 10f;
    [SerializeField] private float rotationSpeed2 = 10f;
    [SerializeField] private bool applyToVisual1 = true;
    [SerializeField] private bool applyToVisual2 = true;

    private float moveSpeed = 3f;
    private float contactDamage = 10f;

    private Transform player;
    private NavMeshAgent agent;
    private bool useNavMesh = true;
    private Rigidbody rb;
    
    // Knockback system
    private bool isKnockedBack = false;
    private float knockbackTimer = 0f;
    private Vector3 knockbackVelocity = Vector3.zero;

    public float ContactDamage => contactDamage;

    // IUpdateable implementation
    public bool IsActive => gameObject.activeInHierarchy && enabled;

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
        
        if (agent != null && agent.isOnNavMesh)
        {
            agent.enabled = true;
            agent.speed = moveSpeed;
        }
        
        if (rb != null)
        {
            rb.isKinematic = useNavMesh;
            rb.linearVelocity = Vector3.zero;
        }
        
        // Registrar con UpdateManager
        if (UpdateManager.Instance != null)
        {
            UpdateManager.Instance.Register(this as IUpdateable);
            UpdateManager.Instance.Register(this as IFixedUpdateable);
        }
    }
    
    private void OnDisable()
    {
        // Unregister del UpdateManager
        if (UpdateManager.Instance != null)
        {
            UpdateManager.Instance.Unregister(this as IUpdateable);
            UpdateManager.Instance.Unregister(this as IFixedUpdateable);
        }
    }

    // IUpdateable implementation
    public void OnUpdate(float deltaTime)
    {
        if (player == null) return;

        if ((visual1 != null && applyToVisual1) || (visual2 != null && applyToVisual2))
        {
            Vector3 direction = player.position - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);

                if (visual1 != null && applyToVisual1)
                {
                    visual1.rotation = Quaternion.Slerp(
                        visual1.rotation,
                        targetRotation,
                        rotationSpeed1 * Time.deltaTime
                    );
                }

                if (visual2 != null && applyToVisual2)
                {
                    visual2.rotation = Quaternion.Slerp(
                        visual2.rotation,
                        targetRotation,
                        rotationSpeed2 * Time.deltaTime
                    );
                }
            }
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
            agent.SetDestination(player.position);
        }
    }

    // IFixedUpdateable implementation
    public void OnFixedUpdate(float fixedDeltaTime)
    {
        if (isKnockedBack && rb != null)
        {
            rb.linearVelocity = knockbackVelocity;
            return;
        }
        
        if (!useNavMesh && rb != null && player != null)
        {
            Vector3 direction = (player.position - transform.position).normalized;
            rb.linearVelocity = new Vector3(direction.x * moveSpeed, rb.linearVelocity.y, direction.z * moveSpeed);
        }
    }
    
    /// <summary>
    /// Aplica knockback al enemigo
    /// </summary>
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
    
    /// <summary>
    /// Termina el knockback y restaura el comportamiento normal
    /// </summary>
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
