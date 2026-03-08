using System.Collections.Generic;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{

    public static int ActiveEnemyCount { get; private set; }
    public static readonly List<EnemyHealth> ActiveEnemies = new List<EnemyHealth>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ClearStatics()
    {
        ActiveEnemyCount = 0;
        ActiveEnemies.Clear();
    }

    [Header("Death VFX")]
    [SerializeField] private GameObject explosionPrefab;

    private Transform dropPoint;
    private float maxHealth = 30f;
    private float currentHealth;
    private DamageTween damageTween;

    private PoolManager.PoolType enemyPoolType = PoolManager.PoolType.BasicEnemy;

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

        if (damageTween == null)
        {
            damageTween = GetComponentInChildren<DamageTween>();
        }

        if (dropPoint == null)
        {
            Transform found = transform.Find("DropPoint");
            dropPoint = found != null ? found : transform;
        }
    }

    private void Start()
    {
        currentHealth = maxHealth;
    }

    private void OnEnable()
    {
        ActiveEnemyCount++;
        ActiveEnemies.Add(this);
    }

    private void OnDisable()
    {
        ActiveEnemyCount = Mathf.Max(0, ActiveEnemyCount - 1);
        ActiveEnemies.Remove(this);
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

        if (FloatingTextManager.Instance != null)
        {
            Vector3 textPosition = transform.position + Vector3.up * 1.5f;
            FloatingTextManager.Instance.ShowDamage(damage, textPosition);
        }

        currentHealth -= damage;
        GameEvents.TriggerEnemyDamaged(damage);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {

        if (GameSessionStats.Instance != null)
        {
            GameSessionStats.Instance.RegisterEnemyKill();
        }

        if (explosionPrefab != null)
        {

            Vector3 explosionPos = transform.position + Vector3.up * 2f;
            Instantiate(explosionPrefab, explosionPos, Quaternion.identity);
        }

        int currentWave = GetCurrentWave();
        GameBalanceConfig.EnemyDropConfig dropConfig = null;
        if (GameBalanceConfig.Instance != null && GameBalanceConfig.Instance.HasDropConfigs())
        {
            dropConfig = GameBalanceConfig.Instance.GetEnemyDropConfig(enemyPoolType, currentWave);
        }

        SpawnExperienceOrbs(dropConfig);
        SpawnCollectibles(dropConfig);

        if (SpawnFactory.Instance != null)
        {
            SpawnFactory.Instance.DestroyObject(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void SpawnExperienceOrbs(GameBalanceConfig.EnemyDropConfig dropConfig)
    {
        if (SpawnFactory.Instance == null) return;

        int minOrbs          = dropConfig != null ? dropConfig.minOrbs      : defaultMinOrbs;
        int maxOrbs          = dropConfig != null ? dropConfig.maxOrbs      : defaultMaxOrbs;
        float orbSpawnRadius = dropConfig != null ? 1f                      : defaultOrbSpawnRadius;
        OrbConfiguration orbConfig = dropConfig != null ? dropConfig.orbConfig : defaultOrbConfig;

        int orbCount = Random.Range(minOrbs, maxOrbs + 1);
        Vector3 spawnCenter = dropPoint != null ? dropPoint.position : transform.position;

        DropSpawner spawner = DropSpawner.GetOrCreate();
        for (int i = 0; i < orbCount; i++)
            spawner.EnqueueOrb(spawnCenter, orbSpawnRadius, orbConfig, 10);
    }

    private void SpawnCollectibles(GameBalanceConfig.EnemyDropConfig dropConfig)
    {
        if (SpawnFactory.Instance == null) return;

        float coinDropChance    = dropConfig != null ? dropConfig.coinDropChance    : defaultCoinDropChance;
        int minCoins            = dropConfig != null ? dropConfig.minCoins          : defaultMinCoins;
        int maxCoins            = dropConfig != null ? dropConfig.maxCoins          : defaultMaxCoins;
        float diamondDropChance = dropConfig != null ? dropConfig.diamondDropChance : defaultDiamondDropChance;
        int minDiamonds         = dropConfig != null ? dropConfig.minDiamonds       : defaultMinDiamonds;
        int maxDiamonds         = dropConfig != null ? dropConfig.maxDiamonds       : defaultMaxDiamonds;

        Vector3 spawnCenter = dropPoint != null ? dropPoint.position : transform.position;
        const float spawnRadius = 1f;

        DropSpawner spawner = DropSpawner.GetOrCreate();

        if (Random.value <= coinDropChance)
        {
            int coinCount = Random.Range(minCoins, maxCoins + 1);
            for (int i = 0; i < coinCount; i++)
                spawner.EnqueueCoin(spawnCenter, spawnRadius);
        }

        if (Random.value <= diamondDropChance)
        {
            int diamondCount = Random.Range(minDiamonds, maxDiamonds + 1);
            for (int i = 0; i < diamondCount; i++)
                spawner.EnqueueDiamond(spawnCenter, spawnRadius);
        }
    }

    private int GetCurrentWave()
    {
        if (EnemySpawnManager.Instance != null)
        {
            return EnemySpawnManager.Instance.CurrentWaveNumber;
        }
        return 1;
    }
}
