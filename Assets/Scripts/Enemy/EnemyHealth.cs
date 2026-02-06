using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 30f;
    
    [Header("Experience Orb Settings")]
    [SerializeField] private int minOrbs = 1;
    [SerializeField] private int maxOrbs = 3;
    [SerializeField] private float orbSpawnRadius = 1f;
    [SerializeField] private OrbConfiguration orbConfig;
    [SerializeField] private int defaultExperienceValue = 10;

    private float currentHealth;

    private void Start()
    {
        currentHealth = maxHealth;
    }

    public void ResetHealth()
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
        
        if (PoolManager.Instance != null)
        {
            PoolManager.Instance.Despawn(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void SpawnExperienceOrbs()
    {
        if (PoolManager.Instance == null)
        {
            Debug.LogWarning("PoolManager not initialized!");
            return;
        }

        int orbCount = Random.Range(minOrbs, maxOrbs + 1);
        Vector3 spawnCenter = transform.position + Vector3.up * 0.5f;

        for (int i = 0; i < orbCount; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * orbSpawnRadius;
            Vector3 spawnPosition = spawnCenter + new Vector3(randomCircle.x, Random.Range(0f, 1f), randomCircle.y);
            
            PoolManager.Instance.Spawn<ExperienceOrb>(
                PoolManager.PoolType.ExperienceOrb, 
                spawnPosition, 
                Quaternion.identity,
                orb =>
                {
                    if (orbConfig != null)
                    {
                        orbConfig.ApplyToOrb(orb);
                    }
                    else
                    {
                        orb.SetExperienceValue(defaultExperienceValue);
                    }
                }
            );
        }
    }
}
