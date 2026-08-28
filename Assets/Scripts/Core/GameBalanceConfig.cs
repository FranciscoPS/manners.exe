using UnityEngine;
using System;

[CreateAssetMenu(fileName = "GameBalanceConfig", menuName = "Game/Game Balance Configuration")]
public class GameBalanceConfig : ScriptableObject
{

    // Cada entrada reemplaza (no complementa) los drops definidos en el EnemyConfiguration
    // de ese enemyType mientras currentWave caiga en [fromWave, toWave]. Si ningún entry
    // matchea, EnemyHealth usa los valores del EnemyConfiguration del enemigo como fallback.
    [System.Serializable]
    public class EnemyDropConfig
    {
        [Header("Wave Range")]
        [Tooltip("Desde qué wave se aplica esta config (1-based)")]
        public int fromWave = 1;
        [Tooltip("Hasta qué wave se aplica (999 = infinito)")]
        public int toWave = 999;

        [Header("Enemy Type")]
        [Tooltip("Tipo de pool del enemigo (BasicEnemy, FastEnemy, etc.)")]
        public PoolManager.PoolType enemyType = PoolManager.PoolType.BasicEnemy;

        [Header("Experience Orbs")]
        public OrbConfiguration orbConfig;
        public int minOrbs = 1;
        public int maxOrbs = 3;

        [Header("Coins")]
        [Range(0f, 1f)] public float coinDropChance = 0.5f;
        public int minCoins = 1;
        public int maxCoins = 3;

        [Header("Diamonds")]
        [Range(0f, 1f)] public float diamondDropChance = 0.1f;
        public int minDiamonds = 1;
        public int maxDiamonds = 1;
    }
    private static GameBalanceConfig instance;
    public static GameBalanceConfig Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<GameBalanceConfig>("GameBalanceConfig");
            }
            return instance;
        }
    }

    [Header("=== INITIAL PLAYER STATS ===")]
    [SerializeField] private float playerMaxHealth = 100f;
    [SerializeField] private float playerBaseDamage = 10f;
    [SerializeField] private float playerMoveSpeed = 5f;
    [SerializeField] private float playerAttackRange = 10f;
    [SerializeField] private float playerAttackCooldown = 0.5f;

    [Header("=== EXPERIENCE SYSTEM ===")]
    [SerializeField] private int baseExperienceRequired = 100;
    [SerializeField] private float experienceMultiplier = 1.5f;

    [Header("=== BUILDING DROP RATES ===")]
    [Tooltip("Chance for buildings to drop coins (0-1)")]
    [SerializeField] private float buildingCoinDropChance = 0.7f;
    [SerializeField] private int buildingMinCoins = 2;
    [SerializeField] private int buildingMaxCoins = 5;

    [Tooltip("Chance for buildings to drop diamonds (0-1)")]
    [SerializeField] private float buildingDiamondDropChance = 0.15f;
    [SerializeField] private int buildingMinDiamonds = 1;
    [SerializeField] private int buildingMaxDiamonds = 2;

    [Header("=== BUILDING EXPERIENCE ===")]
    [SerializeField] private int buildingMinOrbs = 3;
    [SerializeField] private int buildingMaxOrbs = 7;
    [SerializeField] private float buildingOrbSpawnRadius = 2f;
    [SerializeField] private int buildingDefaultExperienceValue = 15;

    [Header("=== PICKUP ATTRACTION ===")]
    [SerializeField] private float coinAttractionRange = 5f;
    [SerializeField] private float diamondAttractionRange = 5f;
    [SerializeField] private float orbAttractionRange = 5f;

    [Header("=== PICKUP LIFETIME ===")]
    [SerializeField] private float coinLifetime = 30f;
    [SerializeField] private float diamondLifetime = 30f;
    [SerializeField] private float orbLifetime = 30f;

    [Header("=== ENEMY DROPS BY WAVE ===")]
    [Tooltip("Configuraciones de drops por tipo de enemigo y wave. Se busca la primera que matchee el wave actual.")]
    [SerializeField] private EnemyDropConfig[] enemyDropConfigs = new EnemyDropConfig[0];

    public float PlayerMaxHealth => playerMaxHealth;
    public float PlayerBaseDamage => playerBaseDamage;
    public float PlayerMoveSpeed => playerMoveSpeed;
    public float PlayerAttackRange => playerAttackRange;
    public float PlayerAttackCooldown => playerAttackCooldown;

    public int BaseExperienceRequired => baseExperienceRequired;
    public float ExperienceMultiplier => experienceMultiplier;

    public float BuildingCoinDropChance => buildingCoinDropChance;
    public int BuildingMinCoins => buildingMinCoins;
    public int BuildingMaxCoins => buildingMaxCoins;
    public float BuildingDiamondDropChance => buildingDiamondDropChance;
    public int BuildingMinDiamonds => buildingMinDiamonds;
    public int BuildingMaxDiamonds => buildingMaxDiamonds;

    public int BuildingMinOrbs => buildingMinOrbs;
    public int BuildingMaxOrbs => buildingMaxOrbs;
    public float BuildingOrbSpawnRadius => buildingOrbSpawnRadius;
    public int BuildingDefaultExperienceValue => buildingDefaultExperienceValue;

    public float CoinAttractionRange => coinAttractionRange;
    public float DiamondAttractionRange => diamondAttractionRange;
    public float OrbAttractionRange => orbAttractionRange;

    public float CoinLifetime => coinLifetime;
    public float DiamondLifetime => diamondLifetime;
    public float OrbLifetime => orbLifetime;

    public int CalculateExperienceForLevel(int level)
    {
        return Mathf.RoundToInt(baseExperienceRequired * Mathf.Pow(experienceMultiplier, level - 1));
    }

    public EnemyDropConfig GetEnemyDropConfig(PoolManager.PoolType enemyType, int currentWave)
    {

        foreach (var config in enemyDropConfigs)
        {
            if (config.enemyType == enemyType &&
                currentWave >= config.fromWave &&
                currentWave <= config.toWave)
            {
                return config;
            }
        }

        return null;
    }

    public bool HasDropConfigs()
    {
        return enemyDropConfigs != null && enemyDropConfigs.Length > 0;
    }
}
