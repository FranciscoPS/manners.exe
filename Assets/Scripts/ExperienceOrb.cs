using UnityEngine;

public class ExperienceOrb : MonoBehaviour
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

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        Destroy(gameObject, lifeTime);
        
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
        
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, attractionRange);
    }
}
