using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class EnemySpawnManager : MonoBehaviour, IUpdateable
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

    [Header("Proximity Spawn Settings")]
    [Tooltip("Enable distance-based spawn point selection (recommended for large maps)")]
    [SerializeField] private bool useProximitySpawning = true;
    [Tooltip("Minimum distance from player to spawn (prevents spawning on player)")]
    [SerializeField] private float minSpawnDistance = 15f;
    [Tooltip("Maximum distance from player to spawn (keeps pressure on player)")]
    [SerializeField] private float maxSpawnDistance = 40f;
    [Tooltip("If no points in range, use closest N points as fallback")]
    [SerializeField] private int fallbackPointCount = 4;

    private List<SpawnPoint> allSpawnPoints = new List<SpawnPoint>();
    private int currentWaveIndex = 0;
    private bool isSpawningWave = false;
    private float continuousSpawnTimer = 0f;
    private Transform playerTransform;
    
    // Propiedad pública para que otros sistemas consulten la wave actual
    public int CurrentWaveIndex => currentWaveIndex;
    public int CurrentWaveNumber => currentWaveIndex + 1; // 1-based para UI/balance

    // IUpdateable implementation
    public bool IsActive => this != null && enabled && gameObject.activeInHierarchy;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // Register con UpdateManager
        if (UpdateManager.Instance != null)
        {
            UpdateManager.Instance.Register(this);
        }
    }

    private void OnDestroy()
    {
        // Unregister del UpdateManager
        if (UpdateManager.Instance != null)
        {
            UpdateManager.Instance.Unregister(this);
        }
    }

    private void Start()
    {
        DetectSpawnPoints();
        FindPlayer();
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

    private void FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            Debug.LogWarning("EnemySpawnManager: Player not found! Proximity spawning will be disabled.");
            useProximitySpawning = false;
        }
    }

    // IUpdateable implementation
    public void OnUpdate(float deltaTime)
    {
        if (enableContinuousSpawn && !isSpawningWave)
        {
            continuousSpawnTimer -= deltaTime;
            
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

        // Obtener spawn points válidos basados en proximidad al jugador
        List<SpawnPoint> validPoints = GetValidSpawnPoints();
        
        // Shuffle de los puntos válidos
        List<SpawnPoint> shuffledPoints = new List<SpawnPoint>(validPoints);
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

    /// <summary>
    /// Obtiene spawn points válidos basados en la proximidad al jugador
    /// Implementa el patrón usado en Vampire Survivors y similares
    /// </summary>
    private List<SpawnPoint> GetValidSpawnPoints()
    {
        // Si proximity spawning está desactivado, usar todos los puntos
        if (!useProximitySpawning || playerTransform == null)
        {
            return new List<SpawnPoint>(allSpawnPoints);
        }

        Vector3 playerPosition = playerTransform.position;
        List<SpawnPoint> validPoints = new List<SpawnPoint>();
        
        // Filtrar puntos dentro del rango de distancia
        foreach (var point in allSpawnPoints)
        {
            float distance = Vector3.Distance(playerPosition, point.transform.position);
            
            // Punto está en el "anillo" de spawn (donut shape)
            if (distance >= minSpawnDistance && distance <= maxSpawnDistance)
            {
                validPoints.Add(point);
            }
        }

        // Fallback: Si no hay puntos en rango, usar los N puntos más cercanos
        // Esto previene que el juego se "rompa" si el jugador está muy lejos
        if (validPoints.Count == 0)
        {
            validPoints = GetClosestSpawnPoints(playerPosition, fallbackPointCount);
        }

        return validPoints;
    }

    /// <summary>
    /// Obtiene los N spawn points más cercanos al jugador (fallback)
    /// </summary>
    private List<SpawnPoint> GetClosestSpawnPoints(Vector3 position, int count)
    {
        // Crear lista con distancias
        List<(SpawnPoint point, float distance)> pointsWithDistance = new List<(SpawnPoint, float)>();
        
        foreach (var point in allSpawnPoints)
        {
            float distance = Vector3.Distance(position, point.transform.position);
            pointsWithDistance.Add((point, distance));
        }

        // Ordenar por distancia (más cercano primero)
        pointsWithDistance.Sort((a, b) => a.distance.CompareTo(b.distance));

        // Tomar los N más cercanos
        int pointsToTake = Mathf.Min(count, pointsWithDistance.Count);
        List<SpawnPoint> result = new List<SpawnPoint>();
        
        for (int i = 0; i < pointsToTake; i++)
        {
            result.Add(pointsWithDistance[i].point);
        }

        return result;
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

        // Obtener spawn points válidos basados en proximidad al jugador
        List<SpawnPoint> availablePoints = GetValidSpawnPoints();
        
        // Shuffle de los puntos válidos
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

            if (config != null)
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
        // Logs removed for optimization
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

    // ===== DEBUGGING & VISUALIZATION =====

    private void OnDrawGizmosSelected()
    {
        if (!useProximitySpawning || playerTransform == null)
            return;

        Vector3 playerPos = playerTransform.position;

        // Distancia mínima (rojo) - No spawnar aquí
        Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
        DrawCircleGizmo(playerPos, minSpawnDistance);

        // Zona de spawn válida (verde) - Spawnar aquí
        Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
        DrawRingGizmo(playerPos, minSpawnDistance, maxSpawnDistance);

        // Distancia máxima (amarillo)
        Gizmos.color = Color.yellow;
        DrawCircleGizmo(playerPos, maxSpawnDistance, true);

        // Mostrar spawn points válidos
        List<SpawnPoint> validPoints = GetValidSpawnPoints();
        foreach (var point in validPoints)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(playerPos, point.transform.position);
            Gizmos.DrawWireSphere(point.transform.position, 1f);
        }
    }

    private void DrawCircleGizmo(Vector3 center, float radius, bool wireOnly = false)
    {
        int segments = 32;
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(Mathf.Cos(0) * radius, 0, Mathf.Sin(0) * radius);

        for (int i = 1; i <= segments; i++)
        {
            float angle = Mathf.Deg2Rad * angleStep * i;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }

    private void DrawRingGizmo(Vector3 center, float innerRadius, float outerRadius)
    {
        int segments = 32;
        float angleStep = 360f / segments;

        for (int i = 0; i < segments; i++)
        {
            float angle1 = Mathf.Deg2Rad * angleStep * i;
            float angle2 = Mathf.Deg2Rad * angleStep * (i + 1);

            Vector3 inner1 = center + new Vector3(Mathf.Cos(angle1) * innerRadius, 0, Mathf.Sin(angle1) * innerRadius);
            Vector3 inner2 = center + new Vector3(Mathf.Cos(angle2) * innerRadius, 0, Mathf.Sin(angle2) * innerRadius);
            Vector3 outer1 = center + new Vector3(Mathf.Cos(angle1) * outerRadius, 0, Mathf.Sin(angle1) * outerRadius);
            Vector3 outer2 = center + new Vector3(Mathf.Cos(angle2) * outerRadius, 0, Mathf.Sin(angle2) * outerRadius);

            Gizmos.DrawLine(inner1, inner2);
            Gizmos.DrawLine(outer1, outer2);
            
            if (i % 4 == 0)
            {
                Gizmos.DrawLine(inner1, outer1);
            }
        }
    }
}
