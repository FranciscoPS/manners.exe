using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class EnemySpawnManager : MonoBehaviour
{
    public static EnemySpawnManager Instance { get; private set; }

    [Header("Wave Configuration")]
    [SerializeField] private WaveData[] waveQueue;
    [SerializeField] private float timeBetweenWaves = 30f;
    [SerializeField] private bool autoLoopWaves = true;

    [Header("Continuous Spawn")]
    [SerializeField] private bool enableContinuousSpawn = true;
    [SerializeField] private float continuousSpawnInterval = 3f;
    [SerializeField] private int continuousEnemiesPerSpawn = 2;
    [SerializeField] private EnemyConfiguration[] continuousEnemyTypes;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private List<SpawnPoint> allSpawnPoints = new List<SpawnPoint>();
    private int currentWaveIndex = 0;
    private bool isSpawningWave = false;
    private float continuousSpawnTimer = 0f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        DetectSpawnPoints();
        continuousSpawnTimer = continuousSpawnInterval;
        
        if (waveQueue != null && waveQueue.Length > 0)
        {
            StartCoroutine(WaveSequence());
        }
        else
        {
            LogDebug("No waves configured in Wave Queue!");
        }
    }

    private void Update()
    {
        if (enableContinuousSpawn && !isSpawningWave)
        {
            continuousSpawnTimer -= Time.deltaTime;
            
            if (continuousSpawnTimer <= 0f)
            {
                SpawnContinuousEnemies();
                continuousSpawnTimer = continuousSpawnInterval;
            }
        }
    }

    private void SpawnContinuousEnemies()
    {
        if (continuousEnemyTypes == null || continuousEnemyTypes.Length == 0)
            return;

        if (allSpawnPoints.Count == 0)
            return;

        List<SpawnPoint> shuffledPoints = new List<SpawnPoint>(allSpawnPoints);
        for (int i = 0; i < shuffledPoints.Count; i++)
        {
            int randomIndex = Random.Range(i, shuffledPoints.Count);
            SpawnPoint temp = shuffledPoints[i];
            shuffledPoints[i] = shuffledPoints[randomIndex];
            shuffledPoints[randomIndex] = temp;
        }

        int enemiesSpawned = 0;
        foreach (var point in shuffledPoints)
        {
            if (enemiesSpawned >= continuousEnemiesPerSpawn)
                break;

            if (point.IsReady)
            {
                EnemyConfiguration config = continuousEnemyTypes[Random.Range(0, continuousEnemyTypes.Length)];
                if (config != null)
                {
                    point.SpawnEnemies(1, config);
                    enemiesSpawned++;
                }
            }
        }
    }

    private void DetectSpawnPoints()
    {
        SpawnPoint[] foundPoints = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);
        allSpawnPoints = new List<SpawnPoint>(foundPoints);
        LogDebug($"Detected {allSpawnPoints.Count} spawn points");
    }

    private IEnumerator WaveSequence()
    {
        while (true)
        {
            if (waveQueue == null || waveQueue.Length == 0)
                yield break;

            WaveData currentWave = waveQueue[currentWaveIndex];
            
            if (currentWave != null)
            {
                LogDebug($"Starting {currentWave.waveName}");
                yield return StartCoroutine(ExecuteWave(currentWave));
            }

            currentWaveIndex++;

            if (currentWaveIndex >= waveQueue.Length)
            {
                if (autoLoopWaves)
                {
                    currentWaveIndex = 0;
                    LogDebug("Looping waves...");
                }
                else
                {
                    LogDebug("All waves completed");
                    yield break;
                }
            }

            yield return new WaitForSeconds(timeBetweenWaves);
        }
    }

    private IEnumerator ExecuteWave(WaveData wave)
    {
        isSpawningWave = true;
        int enemiesSpawned = 0;
        int totalEnemies = wave.totalEnemies;

        while (enemiesSpawned < totalEnemies)
        {
            int remainingEnemies = totalEnemies - enemiesSpawned;
            int enemiesToSpawnThisBatch = Mathf.Min(wave.enemiesPerBatch, remainingEnemies);

            SpawnBatch(enemiesToSpawnThisBatch, wave);
            enemiesSpawned += enemiesToSpawnThisBatch;

            if (enemiesSpawned < totalEnemies)
            {
                yield return new WaitForSeconds(wave.spawnInterval);
            }
        }

        isSpawningWave = false;
        LogDebug($"Wave {wave.waveName} completed: {enemiesSpawned} enemies spawned");
    }

    private void SpawnBatch(int count, WaveData wave)
    {
        if (allSpawnPoints.Count == 0)
        {
            LogDebug("No spawn points available!");
            return;
        }

        List<SpawnPoint> availablePoints = new List<SpawnPoint>(allSpawnPoints);
        
        for (int i = 0; i < availablePoints.Count; i++)
        {
            int randomIndex = Random.Range(i, availablePoints.Count);
            SpawnPoint temp = availablePoints[i];
            availablePoints[i] = availablePoints[randomIndex];
            availablePoints[randomIndex] = temp;
        }

        int enemiesRemaining = count;
        int pointIndex = 0;

        while (enemiesRemaining > 0 && availablePoints.Count > 0)
        {
            SpawnPoint currentPoint = availablePoints[pointIndex % availablePoints.Count];
            EnemyConfiguration config = wave.GetRandomEnemyConfig();

            if (config != null && currentPoint.CanSpawnEnemyType(config))
            {
                int enemiesToSpawn = Mathf.Min(currentPoint.MaxEnemiesPerSpawn, enemiesRemaining);
                currentPoint.SpawnEnemies(enemiesToSpawn, config);
                enemiesRemaining -= enemiesToSpawn;
            }

            pointIndex++;

            if (pointIndex >= availablePoints.Count * 2)
            {
                break;
            }
        }
    }

    private void LogDebug(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[EnemySpawnManager] {message}");
        }
    }

    public void TriggerWave(int waveIndex)
    {
        if (isSpawningWave) return;
        
        if (waveIndex >= 0 && waveIndex < waveQueue.Length)
        {
            StopAllCoroutines();
            currentWaveIndex = waveIndex;
            StartCoroutine(WaveSequence());
        }
    }

    public void SetWaveMultiplier(float multiplier)
    {
    }
}
