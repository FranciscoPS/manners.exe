using UnityEngine;

[CreateAssetMenu(fileName = "EnemyConfig", menuName = "Game/Enemy Configuration")]
public class EnemyConfiguration : ScriptableObject
{
    [Header("Pool Settings")]
    [Tooltip("Tipo de pool a usar para este enemigo")]
    public PoolManager.PoolType enemyPoolType = PoolManager.PoolType.BasicEnemy;
    
    [Header("Stats")]
    public float maxHealth = 30f;
    public float moveSpeed = 3f;
    public float contactDamage = 10f;
    
    [Header("Experience Drop")]
    public OrbConfiguration orbConfig;
    public int minOrbs = 1;
    public int maxOrbs = 3;
    public float orbSpawnRadius = 1f;
    
    [Header("Currency Drops")]
    [Tooltip("Chance to drop coins (0-1)")]
    [Range(0f, 1f)] public float coinDropChance = 0.5f;
    public int minCoins = 1;
    public int maxCoins = 3;
    
    [Tooltip("Chance to drop diamonds (0-1)")]
    [Range(0f, 1f)] public float diamondDropChance = 0.1f;
    public int minDiamonds = 1;
    public int maxDiamonds = 1;
    
    public void ApplyToEnemy(GameObject enemyObject)
    {
        EnemyController controller = enemyObject.GetComponent<EnemyController>();
        if (controller != null)
        {
            controller.SetStats(moveSpeed, contactDamage);
        }
        
        EnemyHealth health = enemyObject.GetComponent<EnemyHealth>();
        if (health != null)
        {
            // Pasar el poolType y valores fallback al EnemyHealth
            // EnemyHealth ahora consulta GameBalanceConfig dinámicamente usando estos como fallback
            health.SetConfiguration(maxHealth, enemyPoolType,
                                   orbConfig, minOrbs, maxOrbs, orbSpawnRadius,
                                   coinDropChance, minCoins, maxCoins,
                                   diamondDropChance, minDiamonds, maxDiamonds);
        }
    }
}
