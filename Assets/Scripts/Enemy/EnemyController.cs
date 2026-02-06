using UnityEngine;

public class EnemyController : MonoBehaviour
{
    private float moveSpeed = 3f;
    private float contactDamage = 10f;

    private Transform player;
    private Rigidbody rb;

    public float ContactDamage => contactDamage;

    public void SetStats(float newMoveSpeed, float newContactDamage)
    {
        moveSpeed = newMoveSpeed;
        contactDamage = newContactDamage;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
    }

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    private void FixedUpdate()
    {
        if (player != null)
        {
            Vector3 direction = (player.position - transform.position).normalized;
            rb.linearVelocity = new Vector3(direction.x * moveSpeed, rb.linearVelocity.y, direction.z * moveSpeed);
        }
    }
}
