using UnityEngine;

public class Projectile : MonoBehaviour, IPoolable
{
    private float speed = 15f;
    private float damage = 10f;
    private float lifetime = 5f;

    private Vector3 direction;
    private Rigidbody rb;
    private float lifetimeTimer;
    private GameObject trailInstance;
    private Light projectileLight;
    private Material materialInstance;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        projectileLight = GetComponent<Light>();
    }

    public void SetStats(float newSpeed, float newDamage, float newLifetime)
    {
        speed = newSpeed;
        damage = newDamage;
        lifetime = newLifetime;
    }

    public void SetVisuals(Mesh mesh, Material material, Color color, Vector3 scale)
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter != null && mesh != null)
        {
            meshFilter.mesh = mesh;
        }

        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            if (material != null)
            {
                materialInstance = new Material(material);
                renderer.material = materialInstance;
            }
            else if (materialInstance == null)
            {
                materialInstance = new Material(renderer.sharedMaterial);
                renderer.material = materialInstance;
            }
            
            materialInstance.color = color;
            
            if (materialInstance.HasProperty("_BaseColor"))
                materialInstance.SetColor("_BaseColor", color);
            if (materialInstance.HasProperty("_Color"))
                materialInstance.SetColor("_Color", color);
            if (materialInstance.HasProperty("_EmissionColor"))
                materialInstance.SetColor("_EmissionColor", color * 0.5f);
        }

        transform.localScale = scale;
    }

    public void SetEffects(GameObject trail, GameObject hitEffect, bool hasLight, Color lightColor, float lightIntensity)
    {
        if (trail != null && trailInstance == null)
        {
            trailInstance = Instantiate(trail, transform);
        }

        if (hasLight)
        {
            if (projectileLight == null)
            {
                projectileLight = gameObject.AddComponent<Light>();
                projectileLight.type = LightType.Point;
                projectileLight.range = 3f;
            }
            projectileLight.color = lightColor;
            projectileLight.intensity = lightIntensity;
            projectileLight.enabled = true;
        }
        else if (projectileLight != null)
        {
            projectileLight.enabled = false;
        }
    }

    private void Update()
    {
        lifetimeTimer -= Time.deltaTime;
        if (lifetimeTimer <= 0f)
        {
            if (PoolManager.Instance != null)
            {
                PoolManager.Instance.Despawn(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }

    public void SetDirection(Vector3 dir)
    {
        direction = dir.normalized;
        rb.linearVelocity = direction * speed;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            EnemyHealth enemyHealth = other.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage);
            }
            
            if (PoolManager.Instance != null)
            {
                PoolManager.Instance.Despawn(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }

    public void OnSpawn()
    {
        lifetimeTimer = lifetime;
        rb.linearVelocity = Vector3.zero;
    }

    public void OnDespawn()
    {
        rb.linearVelocity = Vector3.zero;
        direction = Vector3.zero;
    }
}
