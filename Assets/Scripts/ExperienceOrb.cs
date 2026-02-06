using UnityEngine;

public class ExperienceOrb : MonoBehaviour, IPoolable
{
    [Header("Experience Settings")]
    [SerializeField] private int experienceValue = 10;
    
    [Header("Movement Settings")]
    [SerializeField] private float attractionRange = 5f;
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float acceleration = 15f;
    [SerializeField] private float lifeTime = 30f;
    
    private Transform player;
    private bool isMovingToPlayer = false;
    private float currentSpeed = 0f;
    private bool collected = false;
    private float lifetimeTimer;

    public void SetExperienceValue(int value)
    {
        experienceValue = value;
    }

    public void SetOrbColor(Color color)
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null && renderer.material != null)
        {
            renderer.material.color = color;
        }

        Light orbLight = GetComponent<Light>();
        if (orbLight != null)
        {
            orbLight.color = color;
        }
    }

    public void SetAttractionRange(float range)
    {
        attractionRange = range;
    }

    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
    }

    private void Start()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }
        
        SphereCollider collider = GetComponent<SphereCollider>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }
        
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        Light orbLight = GetComponent<Light>();
        if (orbLight != null)
        {
            orbLight.range = 10f;
            orbLight.intensity = 5f;
        }
    }

    private void Update()
    {
        if (player == null || collected) return;

        lifetimeTimer -= Time.deltaTime;
        if (lifetimeTimer <= 0f)
        {
            if (PoolManager.Instance != null)
            {
                PoolManager.Instance.Despawn(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= attractionRange)
        {
            isMovingToPlayer = true;
        }

        if (isMovingToPlayer)
        {
            currentSpeed = Mathf.Min(currentSpeed + acceleration * Time.deltaTime, moveSpeed);
            
            Vector3 direction = (player.position - transform.position).normalized;
            transform.position += direction * currentSpeed * Time.deltaTime;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected) return;

        if (other.CompareTag("Player"))
        {
            CollectExperience(other.transform);
        }
    }

    private void CollectExperience(Transform playerTransform)
    {
        collected = true;
        
        PlayerExperience playerExp = playerTransform.GetComponent<PlayerExperience>();
        if (playerExp != null)
        {
            playerExp.AddExperience(experienceValue);
        }
        
        if (PoolManager.Instance != null)
        {
            PoolManager.Instance.Despawn(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, attractionRange);
    }

    public void OnSpawn()
    {
        collected = false;
        isMovingToPlayer = false;
        currentSpeed = 0f;
        lifetimeTimer = lifeTime;

        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }
    }

    public void OnDespawn()
    {
        collected = false;
        isMovingToPlayer = false;
        currentSpeed = 0f;
    }
}
