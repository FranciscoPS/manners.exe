using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    private float maxHealth = 30f;
    
    private int minOrbs = 1;
    private int maxOrbs = 3;
    private float orbSpawnRadius = 1f;
    private OrbConfiguration orbConfig;
    private int defaultExperienceValue = 10;
    
    // Currency drop settings
    private float coinDropChance = 0.5f;
    private int minCoins = 1;
    private int maxCoins = 3;
    private float diamondDropChance = 0.1f;
    private int minDiamonds = 1;
    private int maxDiamonds = 1;

    private float currentHealth;

    public void SetConfiguration(float newMaxHealth, OrbConfiguration newOrbConfig, int newMinOrbs, int newMaxOrbs, float newOrbRadius,
                                float newCoinDropChance, int newMinCoins, int newMaxCoins,
                                float newDiamondDropChance, int newMinDiamonds, int newMaxDiamonds)
    {
        maxHealth = newMaxHealth;
        currentHealth = maxHealth;
        orbConfig = newOrbConfig;
        minOrbs = newMinOrbs;
        maxOrbs = newMaxOrbs;
        orbSpawnRadius = newOrbRadius;
        
        coinDropChance = newCoinDropChance;
        minCoins = newMinCoins;
        maxCoins = newMaxCoins;
        diamondDropChance = newDiamondDropChance;
        minDiamonds = newMinDiamonds;
        maxDiamonds = newMaxDiamonds;
    }

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
        float previousHealth = currentHealth;
        currentHealth -= damage;
        Debug.Log($"[EnemyHealth] Took {damage} damage. Health: {previousHealth} -> {currentHealth} (Max: {maxHealth})");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        SpawnExperienceOrbs();
        SpawnCollectibles();
        
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
            
            ExperienceOrb orb = PoolManager.Instance.SpawnOrb(spawnPosition, orbConfig);
            if (orb != null && orbConfig == null)
            {
                orb.SetExperienceValue(defaultExperienceValue);
            }
        }
    }

    private void SpawnCollectibles()
    {
        if (PoolManager.Instance == null) return;

        Vector3 spawnCenter = transform.position + Vector3.up * 0.5f;

        // Spawn coins based on this enemy's configuration
        if (Random.value <= coinDropChance)
        {
            int coinCount = Random.Range(minCoins, maxCoins + 1);
            for (int i = 0; i < coinCount; i++)
            {
                Vector2 randomCircle = Random.insideUnitCircle * orbSpawnRadius;
                Vector3 spawnPosition = spawnCenter + new Vector3(randomCircle.x, Random.Range(0f, 1f), randomCircle.y);
                PoolManager.Instance.SpawnCollectible(spawnPosition, Collectible.CollectibleType.Coin, 1);
            }
        }

        // Spawn diamonds based on this enemy's configuration
        if (Random.value <= diamondDropChance)
        {
            int diamondCount = Random.Range(minDiamonds, maxDiamonds + 1);
            for (int i = 0; i < diamondCount; i++)
            {
                Vector2 randomCircle = Random.insideUnitCircle * orbSpawnRadius;
                Vector3 spawnPosition = spawnCenter + new Vector3(randomCircle.x, Random.Range(0f, 1f), randomCircle.y);
                PoolManager.Instance.SpawnCollectible(spawnPosition, Collectible.CollectibleType.Diamond, 1);
            }
        }
    }
}
