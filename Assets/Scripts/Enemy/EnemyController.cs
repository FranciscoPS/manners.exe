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
        // Intentar obtener player (puede que no exista aún; también buscamos en OnUpdate)
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        
        // Limpiar estado de knockback cuando se reactiva desde el pool
        isKnockedBack = false;
        knockbackTimer = 0f;
        knockbackVelocity = Vector3.zero;
        
        // Re-configurar velocidad si tenemos NavMeshAgent
        if (agent != null && agent.isOnNavMesh)
        {
            agent.enabled = true;
            agent.speed = moveSpeed;
        }
        
        // Asegurar que el Rigidbody esté en el estado correcto
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
        // Si no tenemos player buscarlo (robusto frente al orden de inicialización)
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (player == null) return; // sigue sin existir el player en esta frame
        }
        
        // Si está en knockback, reducir el timer
        if (isKnockedBack)
        {
            knockbackTimer -= deltaTime;
            if (knockbackTimer <= 0f)
            {
                EndKnockback();
            }
            return; // No seguir al player mientras está en knockback
        }

        if (useNavMesh && agent != null && agent.isOnNavMesh)
        {
            agent.SetDestination(player.position);
        }

        // Rotación independiente para cada visual (solo si están asignados y marcados desde el inspector)
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

    // IFixedUpdateable implementation
    public void OnFixedUpdate(float fixedDeltaTime)
    {
        // Aplicar velocidad de knockback si está activo
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
        
        // Desactivar NavMeshAgent temporalmente
        if (agent != null && agent.enabled)
        {
            agent.enabled = false;
        }
        
        // Si usamos Rigidbody directo, hacerlo no-kinematic temporalmente
        if (rb != null && rb.isKinematic)
        {
            rb.isKinematic = false;
        }
        
        // Configurar knockback
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
        
        // Restaurar configuración
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
