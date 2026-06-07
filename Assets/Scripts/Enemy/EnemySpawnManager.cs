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

    [Header("Enemy Cap")]
    [Tooltip("Máximo de enemigos activos en escena a la vez. Impide los 10M de polígonos.")]
    [SerializeField] private int maxConcurrentEnemies = 40;

    private List<SpawnPoint> allSpawnPoints = new List<SpawnPoint>();
    private int currentWaveIndex = 0;
    private int waveLoopCount = 0;
    private bool isSpawningWave = false;
    private float continuousSpawnTimer = 0f;
    private bool spawnBlocked = false;
    private bool suppressContinuousSpawn = false;

    public int CurrentWaveIndex => currentWaveIndex;
    public int CurrentWaveNumber => currentWaveIndex + 1;

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

        if (UpdateManager.Instance != null)
        {
            UpdateManager.Instance.Register(this);
        }
    }

    private void OnDestroy()
    {

        if (UpdateManager.Instance != null)
        {
            UpdateManager.Instance.Unregister(this);
        }
    }

    private void Start()
    {
        DetectSpawnPoints();
        continuousSpawnTimer = continuousSpawnInterval;
        StartCoroutine(ScatterClusteredEnemies());

        if (waveQueue != null && waveQueue.Length > 0)
        {
            StartCoroutine(WaveSequence());
        }
        else
        {
            LogDebug("No waves configured in Wave Queue!");
        }
    }

    public void OnUpdate(float deltaTime)
    {
        if (spawnBlocked) return;

        if (enableContinuousSpawn && !isSpawningWave && !suppressContinuousSpawn)
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

        int slots = maxConcurrentEnemies - EnemyHealth.ActiveEnemyCount;
        if (slots <= 0)
        {
            PerformanceMonitor.Instance?.LogEvent($"[CAP] Spawn bloqueado — activos: {EnemyHealth.ActiveEnemyCount}/{maxConcurrentEnemies}");
            return;
        }

        for (int i = allSpawnPoints.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            SpawnPoint temp = allSpawnPoints[i];
            allSpawnPoints[i] = allSpawnPoints[j];
            allSpawnPoints[j] = temp;
        }

        int enemiesSpawned = 0;
        for (int i = 0; i < allSpawnPoints.Count; i++)
        {
            if (enemiesSpawned >= continuousEnemiesPerSpawn)
                break;

            SpawnPoint point = allSpawnPoints[i];
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

            while (spawnBlocked) yield return null;

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
                    waveLoopCount++;
                    LogDebug($"Looping waves... (vuelta {waveLoopCount})");
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
        suppressContinuousSpawn = wave.isRestWave;
        int enemiesSpawned = 0;
        int totalEnemies = wave.totalEnemies;
        string waveTag = $"{wave.waveName} [vuelta {waveLoopCount + 1}, índice {currentWaveIndex + 1}/{waveQueue.Length}]";

        PerformanceMonitor.Instance?.LogEvent($"Wave START: {waveTag} | totalEnemies={totalEnemies} | batchSize={wave.enemiesPerBatch}");

        while (enemiesSpawned < totalEnemies)
        {

            while (EnemyHealth.ActiveEnemyCount >= maxConcurrentEnemies)
                yield return new WaitForSeconds(0.5f);

            int remainingEnemies = totalEnemies - enemiesSpawned;
            int enemiesToSpawnThisBatch = Mathf.Min(wave.enemiesPerBatch, remainingEnemies);

            int actuallySpawned = SpawnBatch(enemiesToSpawnThisBatch, wave);
            enemiesSpawned += actuallySpawned;

            if (enemiesSpawned < totalEnemies)
                yield return new WaitForSeconds(wave.spawnInterval);
        }

        isSpawningWave = false;
        PerformanceMonitor.Instance?.LogEvent($"Wave END: {waveTag} | spawned={enemiesSpawned}");
        LogDebug($"Wave {waveTag} completed: {enemiesSpawned} enemies spawned");
    }

    private int SpawnBatch(int count, WaveData wave)
    {
        if (allSpawnPoints.Count == 0)
        {
            LogDebug("No spawn points available!");
            return 0;
        }

        int slots = maxConcurrentEnemies - EnemyHealth.ActiveEnemyCount;
        if (slots <= 0) return 0;
        count = Mathf.Min(count, slots);

        for (int i = allSpawnPoints.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            SpawnPoint tmp = allSpawnPoints[i];
            allSpawnPoints[i] = allSpawnPoints[j];
            allSpawnPoints[j] = tmp;
        }

        int enemiesRemaining = count;
        int pointIndex = 0;
        int maxIterations = allSpawnPoints.Count * 2;

        while (enemiesRemaining > 0 && pointIndex < maxIterations)
        {
            SpawnPoint currentPoint = allSpawnPoints[pointIndex % allSpawnPoints.Count];
            EnemyConfiguration config = wave.GetRandomEnemyConfig();

            if (config != null && currentPoint.IsReady)
            {
                int enemiesToSpawn = Mathf.Min(currentPoint.MaxEnemiesPerSpawn, enemiesRemaining);
                currentPoint.SpawnEnemies(enemiesToSpawn, config);
                enemiesRemaining -= enemiesToSpawn;
            }

            pointIndex++;
        }

        return count - enemiesRemaining;
    }

    private const float ScatterCheckInterval         = 4f;
    private const float ScatterClusterRadius         = 2.5f;
    // Minimum cluster size to scatter off-screen enemies.
    private const int   ScatterThresholdOffScreen    = 10;
    // Minimum cluster size to scatter even on-screen enemies (extreme pileup).
    private const int   ScatterThresholdOnScreen     = 20;
    // Max enemies allowed to remain per cluster after scattering.
    private const int   ScatterMaxPerCluster         = 5;
    // Enemies within this distance of the player are never scattered (active combat).
    private const float ScatterMinPlayerDist         = 8f;

    private IEnumerator ScatterClusteredEnemies()
    {
        Transform playerTransform = null;

        while (true)
        {
            yield return new WaitForSeconds(ScatterCheckInterval);

            if (playerTransform == null)
                playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;

            if (isSpawningWave) continue;

            if (allSpawnPoints.Count == 0) continue;

            Camera cam = Camera.main;
            var enemies = new List<EnemyHealth>(EnemyHealth.ActiveEnemies);
            int count = enemies.Count;
            if (count < ScatterThresholdOffScreen) continue;

            var alreadyScattered = new HashSet<EnemyHealth>();
            int totalScattered = 0;

            for (int i = 0; i < count; i++)
            {
                if (enemies[i] == null || alreadyScattered.Contains(enemies[i])) continue;

                Vector3 pos = enemies[i].transform.position;
                var cluster = new List<EnemyHealth>();

                for (int j = 0; j < count; j++)
                {
                    if (i == j || enemies[j] == null || alreadyScattered.Contains(enemies[j])) continue;
                    if (Vector3.Distance(pos, enemies[j].transform.position) <= ScatterClusterRadius)
                        cluster.Add(enemies[j]);
                }

                // Not big enough to do anything with at all.
                if (cluster.Count < ScatterThresholdOffScreen) continue;

                bool extremePileup = cluster.Count >= ScatterThresholdOnScreen;

                // Separate cluster members by eligibility:
                //  - Off-screen + far from player  → always eligible (threshold 10).
                //  - On-screen                     → only eligible on extreme pileup (threshold 20).
                //  - Within ScatterMinPlayerDist   → never eligible (active combat).
                var candidates = new List<EnemyHealth>();
                foreach (var e in cluster)
                {
                    if (e == null) continue;

                    // Never scatter enemies in direct melee range.
                    if (playerTransform != null &&
                        Vector3.Distance(e.transform.position, playerTransform.position) < ScatterMinPlayerDist)
                        continue;

                    bool onScreen = false;
                    if (cam != null)
                    {
                        Vector3 vp = cam.WorldToViewportPoint(e.transform.position);
                        onScreen = vp.z > 0f && vp.x > -0.05f && vp.x < 1.05f &&
                                   vp.y > -0.05f && vp.y < 1.05f;
                    }

                    if (onScreen && !extremePileup) continue;  // On-screen: only touch when massive.

                    candidates.Add(e);
                }

                // Scatter only the excess; seed enemy (enemies[i]) counts as 1 of the 5 kept.
                int toScatter = Mathf.Max(0, candidates.Count - (ScatterMaxPerCluster - 1));
                if (toScatter == 0) continue;

                Debug.Log($"[Scatter] Cluster {cluster.Count} (extremo={extremePileup}) — candidatos {candidates.Count} — dispersando {toScatter}");

                // Pre-filter spawn points so enemies don't warp directly onto the player.
                var safeSpawnPoints = playerTransform != null
                    ? allSpawnPoints.FindAll(sp =>
                        Vector3.Distance(sp.transform.position, playerTransform.position) >= ScatterMinPlayerDist)
                    : allSpawnPoints;
                if (safeSpawnPoints.Count == 0) safeSpawnPoints = allSpawnPoints;

                for (int k = 0; k < toScatter; k++)
                {
                    if (candidates[k] == null) continue;
                    SpawnPoint target = safeSpawnPoints[Random.Range(0, safeSpawnPoints.Count)];
                    EnemyController ctrl = candidates[k].GetComponent<EnemyController>();
                    if (ctrl != null)
                    {
                        target.WarnThenWarp(ctrl);
                        totalScattered++;
                    }
                    alreadyScattered.Add(candidates[k]);
                }
            }

            if (totalScattered > 0)
                Debug.Log($"[Scatter] Dispersados: {totalScattered}");
        }
    }

    private void LogDebug(string message)
    {

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

    public void SetSpawnBlocked(bool blocked)
    {
        spawnBlocked = blocked;
    }

    public void SetWaveMultiplier(float multiplier)
    {
    }
}
