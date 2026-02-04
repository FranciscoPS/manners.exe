using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 30f;
    
    [Header("Experience Orb Settings")]
    [SerializeField] private GameObject experienceOrbPrefab;
    [SerializeField] private int minOrbs = 1;
    [SerializeField] private int maxOrbs = 3;
    [SerializeField] private float orbSpawnRadius = 1f;

    private float currentHealth;

    private void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        SpawnExperienceOrbs();
        Destroy(gameObject);
    }

    private void SpawnExperienceOrbs()
    {
        if (experienceOrbPrefab == null) return;

        int orbCount = Random.Range(minOrbs, maxOrbs + 1);
        Vector3 spawnCenter = transform.position + Vector3.up * 0.5f;

        for (int i = 0; i < orbCount; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * orbSpawnRadius;
            Vector3 spawnPosition = spawnCenter + new Vector3(randomCircle.x, Random.Range(0f, 1f), randomCircle.y);
            
            GameObject orb = Instantiate(experienceOrbPrefab, spawnPosition, Quaternion.identity);
            orb.transform.SetParent(null);
        }
    }
}
