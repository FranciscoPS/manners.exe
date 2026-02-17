using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{
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
        // Buscar player cada vez que se activa (importante para pooling)
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
    }

    private void Update()
    {
        if (player == null) return;
        
        // Si está en knockback, reducir el timer
        if (isKnockedBack)
        {
            knockbackTimer -= Time.deltaTime;
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
    }

    private void FixedUpdate()
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
