using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform player;
    [SerializeField] private float spawnRadius = 15f;
    [SerializeField] private float initialSpawnInterval = 3f;
    [SerializeField] private int enemiesPerWave = 5;
    [SerializeField] private float waveInterval = 20f;
    [SerializeField] private int enemyIncreasePerWave = 2;

    private float spawnTimer;
    private float waveTimer;
    private int currentWave = 1;
    private int currentEnemiesPerWave;

    private void Start()
    {
        spawnTimer = initialSpawnInterval;
        waveTimer = waveInterval;
        currentEnemiesPerWave = enemiesPerWave;
    }

    private void Update()
    {
        spawnTimer -= Time.deltaTime;
        waveTimer -= Time.deltaTime;

        if (spawnTimer <= 0f)
        {
            SpawnEnemies(1);
            spawnTimer = initialSpawnInterval;
        }

        if (waveTimer <= 0f)
        {
            StartNewWave();
            waveTimer = waveInterval;
        }
    }

    private void StartNewWave()
    {
        currentWave++;
        currentEnemiesPerWave += enemyIncreasePerWave;
        Debug.Log($"Wave {currentWave}");
        SpawnEnemies(currentEnemiesPerWave);
    }

    private void SpawnEnemies(int count)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPosition = GetRandomSpawnPosition();
            Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        }
    }

    private Vector3 GetRandomSpawnPosition()
    {
        Vector2 randomCircle = Random.insideUnitCircle.normalized * spawnRadius;
        Vector3 spawnPos = player.position + new Vector3(randomCircle.x, 0f, randomCircle.y);
        spawnPos.y = 0.5f;
        return spawnPos;
    }

    private void OnDrawGizmosSelected()
    {
        if (player != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(player.position, spawnRadius);
        }
    }
}
