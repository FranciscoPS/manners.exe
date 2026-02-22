using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    private float maxHealth = 30f;
    private float currentHealth;
    private DamageTween damageTween;
    
    // Identificador del tipo de enemigo para buscar drops dinámicos
    private PoolManager.PoolType enemyPoolType = PoolManager.PoolType.BasicEnemy;
    
    // Valores por defecto si no hay config en GameBalanceConfig
    private int defaultMinOrbs = 1;
    private int defaultMaxOrbs = 3;
    private float defaultOrbSpawnRadius = 1f;
    private OrbConfiguration defaultOrbConfig;
    private float defaultCoinDropChance = 0.5f;
    private int defaultMinCoins = 1;
    private int defaultMaxCoins = 3;
    private float defaultDiamondDropChance = 0.1f;
    private int defaultMinDiamonds = 1;
    private int defaultMaxDiamonds = 1;

    public void SetConfiguration(float newMaxHealth, PoolManager.PoolType poolType,
                                OrbConfiguration fallbackOrbConfig, int fallbackMinOrbs, int fallbackMaxOrbs, float fallbackOrbRadius,
                                float fallbackCoinDropChance, int fallbackMinCoins, int fallbackMaxCoins,
                                float fallbackDiamondDropChance, int fallbackMinDiamonds, int fallbackMaxDiamonds)
    {
        maxHealth = newMaxHealth;
        currentHealth = maxHealth;
        enemyPoolType = poolType;
        
        // Guardar valores fallback por si GameBalanceConfig no tiene config para este enemigo/wave
        defaultOrbConfig = fallbackOrbConfig;
        defaultMinOrbs = fallbackMinOrbs;
        defaultMaxOrbs = fallbackMaxOrbs;
        defaultOrbSpawnRadius = fallbackOrbRadius;
        defaultCoinDropChance = fallbackCoinDropChance;
        defaultMinCoins = fallbackMinCoins;
        defaultMaxCoins = fallbackMaxCoins;
        defaultDiamondDropChance = fallbackDiamondDropChance;
        defaultMinDiamonds = fallbackMinDiamonds;
        defaultMaxDiamonds = fallbackMaxDiamonds;
        
        // Buscar DamageTween en este objeto o en hijos (para pooling)
        if (damageTween == null)
        {
            damageTween = GetComponentInChildren<DamageTween>();
        }
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
        if (damageTween != null)
        {
            damageTween.TweenFx();
        }
        
        // Mostrar número de daño flotante
        if (FloatingTextManager.Instance != null)
        {
            Vector3 textPosition = transform.position + Vector3.up * 1.5f;
            FloatingTextManager.Instance.ShowDamage(damage, textPosition);
        }
        
        currentHealth -= damage;

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
            return;
        }
        
        // Obtener configuración dinámica basada en wave actual
        int currentWave = GetCurrentWave();
        GameBalanceConfig.EnemyDropConfig dropConfig = null;
        
        if (GameBalanceConfig.Instance != null && GameBalanceConfig.Instance.HasDropConfigs())
        {
            dropConfig = GameBalanceConfig.Instance.GetEnemyDropConfig(enemyPoolType, currentWave);
        }
        
        // Usar valores dinámicos si hay config, sino usar defaults
        int minOrbs = dropConfig != null ? dropConfig.minOrbs : defaultMinOrbs;
        int maxOrbs = dropConfig != null ? dropConfig.maxOrbs : defaultMaxOrbs;
        float orbSpawnRadius = dropConfig != null ? 1f : defaultOrbSpawnRadius;
        OrbConfiguration orbConfig = dropConfig != null ? dropConfig.orbConfig : defaultOrbConfig;

        int orbCount = Random.Range(minOrbs, maxOrbs + 1);
        Vector3 spawnCenter = transform.position + Vector3.up * 0.5f;

        for (int i = 0; i < orbCount; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * orbSpawnRadius;
            Vector3 spawnPosition = spawnCenter + new Vector3(randomCircle.x, Random.Range(0f, 1f), randomCircle.y);
            
            ExperienceOrb orb = PoolManager.Instance.SpawnOrb(spawnPosition, orbConfig);
            if (orb != null && orbConfig == null)
            {
                orb.SetExperienceValue(10); // Fallback XP value
            }
        }
    }

    private void SpawnCollectibles()
    {
        if (PoolManager.Instance == null) return;
        
        // Obtener configuración dinámica basada en wave actual
        int currentWave = GetCurrentWave();
        GameBalanceConfig.EnemyDropConfig dropConfig = null;
        
        if (GameBalanceConfig.Instance != null && GameBalanceConfig.Instance.HasDropConfigs())
        {
            dropConfig = GameBalanceConfig.Instance.GetEnemyDropConfig(enemyPoolType, currentWave);
        }
        
        // Usar valores dinámicos si hay config, sino usar defaults
        float coinDropChance = dropConfig != null ? dropConfig.coinDropChance : defaultCoinDropChance;
        int minCoins = dropConfig != null ? dropConfig.minCoins : defaultMinCoins;
        int maxCoins = dropConfig != null ? dropConfig.maxCoins : defaultMaxCoins;
        float diamondDropChance = dropConfig != null ? dropConfig.diamondDropChance : defaultDiamondDropChance;
        int minDiamonds = dropConfig != null ? dropConfig.minDiamonds : defaultMinDiamonds;
        int maxDiamonds = dropConfig != null ? dropConfig.maxDiamonds : defaultMaxDiamonds;

        Vector3 spawnCenter = transform.position + Vector3.up * 0.5f;
        float spawnRadius = 1f;

        // Spawn coins based on dynamic configuration
        if (Random.value <= coinDropChance)
        {
            int coinCount = Random.Range(minCoins, maxCoins + 1);
            for (int i = 0; i < coinCount; i++)
            {
                Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
                Vector3 spawnPosition = spawnCenter + new Vector3(randomCircle.x, Random.Range(0f, 1f), randomCircle.y);
                PoolManager.Instance.SpawnCollectible(spawnPosition, Collectible.CollectibleType.Coin, 1);
            }
        }

        // Spawn diamonds based on dynamic configuration
        if (Random.value <= diamondDropChance)
        {
            int diamondCount = Random.Range(minDiamonds, maxDiamonds + 1);
            for (int i = 0; i < diamondCount; i++)
            {
                Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
                Vector3 spawnPosition = spawnCenter + new Vector3(randomCircle.x, Random.Range(0f, 1f), randomCircle.y);
                PoolManager.Instance.SpawnCollectible(spawnPosition, Collectible.CollectibleType.Diamond, 1);
            }
        }
    }
    
    /// <summary>
    /// Obtiene el número de wave actual (1-based)
    /// </summary>
    private int GetCurrentWave()
    {
        if (EnemySpawnManager.Instance != null)
        {
            return EnemySpawnManager.Instance.CurrentWaveNumber;
        }
        return 1; // Default a wave 1 si no hay spawn manager
    }
}
