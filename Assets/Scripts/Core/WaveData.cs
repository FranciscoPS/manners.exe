using UnityEngine;

[System.Serializable]
public class EnemySpawnEntry
{
    public EnemyConfiguration enemyConfig;
    [Range(0f, 100f)] public float percentage = 100f;
}

[CreateAssetMenu(fileName = "WaveData", menuName = "Game/Wave Data")]
public class WaveData : ScriptableObject
{
    [Header("Wave Info")]
    public string waveName = "Wave";
    public int totalEnemies = 10;

    [Header("Enemy Distribution")]
    public EnemySpawnEntry[] enemyDistribution;

    [Header("Spawn Settings")]
    public float spawnInterval = 3f;
    public int enemiesPerBatch = 4;

    public EnemyConfiguration GetRandomEnemyConfig()
    {
        if (enemyDistribution == null || enemyDistribution.Length == 0)
            return null;

        float totalPercentage = 0f;
        foreach (var entry in enemyDistribution)
        {
            totalPercentage += entry.percentage;
        }

        float randomValue = Random.Range(0f, totalPercentage);
        float currentSum = 0f;

        foreach (var entry in enemyDistribution)
        {
            currentSum += entry.percentage;
            if (randomValue <= currentSum && entry.enemyConfig != null)
            {
                return entry.enemyConfig;
            }
        }

        return enemyDistribution[0].enemyConfig;
    }
}
