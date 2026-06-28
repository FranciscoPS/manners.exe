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

    [Header("Early Game Ramp (arranque fácil)")]
    [Tooltip("Suaviza SOLO el arranque: durante los primeros segundos los enemigos salen más lento y en lotes más pequeños, subiendo gradualmente hasta la dificultad normal. NO toca la curva después de la rampa (factor=1).")]
    [SerializeField] private bool enableEarlyRamp = true;
    [Tooltip("Segundo de partida en el que el arranque alcanza la dificultad NORMAL (factor=1). Ej 150 = 2:30: el primer minuto es muy fácil, sobre el min 2 ya se nota la subida y al 2:30 corre la curva normal.")]
    [SerializeField] private float earlyRampSeconds = 150f;
    [Range(0.05f, 1f)]
    [Tooltip("Factor de dificultad en el segundo 0. Más bajo = arranque más fácil/lento. 0.2 = ~1/5 del ritmo normal al empezar.")]
    [SerializeField] private float earlyRampStartFactor = 0.2f;
    [Range(1f, 3f)]
    [Tooltip("Qué tan 'marcada' es la curva del arranque. 1 = lineal. 2 = se queda fácil más tiempo y sube fuerte cerca del final de la rampa.")]
    [SerializeField] private float earlyRampCurvePower = 2f;

    [Header("Final Rush (fin de partida)")]
    [Header("Final Rush - Ráfagas (spawn por tandas)")]
    [Tooltip("Tiempo (seg) entre ráfagas al INICIO. Grande = tandas espaciadas para no abrumar de golpe.")]
    [SerializeField] private float finalRushBurstInterval = 6f;
    [Tooltip("Cuanto se reduce el intervalo entre ráfagas por cada escalon (mas frecuentes con el tiempo).")]
    [SerializeField] private float finalRushBurstIntervalReductionPerTier = 1f;
    [Tooltip("Intervalo MINIMO entre ráfagas (no baja de aqui por mas que escale).")]
    [SerializeField] private float finalRushBurstIntervalMin = 1.5f;
    [Tooltip("Enemigos por spawn point en la PRIMERA ráfaga (pocos al inicio para que sea sobrevivible).")]
    [SerializeField] private int finalRushBurstPerSpawnPoint = 2;
    [Tooltip("Cuantos enemigos extra por spawn point se suman a la ráfaga por cada escalon.")]
    [SerializeField] private int finalRushBurstGrowthPerTier = 3;

    [Header("Final Rush - Escalado por escalones")]
    [Tooltip("Cada cuantos segundos los enemigos suben de nivel (mas vida y velocidad).")]
    [SerializeField] private float finalRushTierInterval = 30f;
    [Tooltip("Vida de los enemigos en el primer escalon (nivel 0). Empieza en la vida normal.")]
    [SerializeField] private float finalRushBaseHealth = 30f;
    [Tooltip("Multiplicador EXPONENCIAL de vida por escalon: vida = base * mult^escalon. Ej 4 -> 30,120,480,1920...")]
    [SerializeField] private float finalRushHealthTierMultiplier = 4f;
    [Tooltip("Velocidad de los enemigos en el primer escalon (nivel 0).")]
    [SerializeField] private float finalRushBaseSpeed = 6f;
    [Tooltip("Cuanta velocidad se suma por cada escalon.")]
    [SerializeField] private float finalRushSpeedPerTier = 2f;
    [Tooltip("Velocidad maxima que pueden alcanzar los enemigos al escalar.")]
    [SerializeField] private float finalRushMaxSpeed = 24f;
    [Tooltip("Dano de contacto en el primer escalon (bajo para que el jugador sobreviva el inicio).")]
    [SerializeField] private float finalRushBaseContactDamage = 12f;
    [Tooltip("Cuanto dano de contacto se suma por cada escalon (sube con el tiempo).")]
    [SerializeField] private float finalRushContactDamagePerTier = 18f;

    [Header("TEST")]
    [Tooltip("TEST: dispara la oleada final inmediatamente al iniciar la partida (quitar antes de publicar).")]
    [SerializeField] private bool testFinalRushFromStart = false;

    private List<SpawnPoint> allSpawnPoints = new List<SpawnPoint>();
    private int currentWaveIndex = 0;
    private int waveLoopCount = 0;
    private bool isSpawningWave = false;
    private float continuousSpawnTimer = 0f;
    private bool spawnBlocked = false;
    private bool suppressContinuousSpawn = false;
    private bool finalRushActive = false;

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

        GameEvents.OnMatchTimeExpired -= HandleMatchTimeExpired;

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

        GameEvents.OnMatchTimeExpired += HandleMatchTimeExpired;

        if (waveQueue != null && waveQueue.Length > 0)
        {
            StartCoroutine(WaveSequence());
        }
        else
        {
            LogDebug("No waves configured in Wave Queue!");
        }

        if (testFinalRushFromStart)
        {
            HandleMatchTimeExpired();
        }
    }

    public void OnUpdate(float deltaTime)
    {
        if (spawnBlocked) return;

        if (enableContinuousSpawn && !isSpawningWave && !suppressContinuousSpawn && !finalRushActive)
        {
            continuousSpawnTimer -= deltaTime;

            if (continuousSpawnTimer <= 0f)
            {
                SpawnContinuousEnemies();
                continuousSpawnTimer = EarlyScaledInterval(continuousSpawnInterval);
            }
        }
    }

    private void HandleMatchTimeExpired()
    {
        if (finalRushActive) return;
        finalRushActive = true;
        suppressContinuousSpawn = true;
        LogDebug("Tiempo de partida agotado: iniciando oleada final imposible.");
        PerformanceMonitor.Instance?.LogEvent("[FINAL RUSH] Tiempo agotado: oleada final imposible iniciada.");
        StartCoroutine(FinalRushRoutine());
    }

    private IEnumerator FinalRushRoutine()
    {

        EnemyConfiguration buffed = CreateFinalRushConfig();
        if (buffed == null)
        {
            LogDebug("[FINAL RUSH] No hay configuracion de enemigo base; abortando rush.");
            yield break;
        }

        float startTime = Time.time;
        int lastTier = -1;

        while (true)
        {
            float elapsed = Time.time - startTime;

            int tier = Mathf.FloorToInt(elapsed / finalRushTierInterval);

            buffed.maxHealth = finalRushBaseHealth * Mathf.Pow(finalRushHealthTierMultiplier, tier);
            buffed.moveSpeed = Mathf.Min(finalRushMaxSpeed, finalRushBaseSpeed + finalRushSpeedPerTier * tier);

            buffed.contactDamage = finalRushBaseContactDamage + finalRushContactDamagePerTier * tier;

            int perSpawnPoint = finalRushBurstPerSpawnPoint + finalRushBurstGrowthPerTier * tier;

            float burstInterval = Mathf.Max(finalRushBurstIntervalMin,
                finalRushBurstInterval - finalRushBurstIntervalReductionPerTier * tier);

            if (tier != lastTier)
            {
                lastTier = tier;
                PerformanceMonitor.Instance?.LogEvent(
                    $"[FINAL RUSH] Escalon {tier}: vida={buffed.maxHealth:F0} vel={buffed.moveSpeed:F1} " +
                    $"x{perSpawnPoint}/punto cada {burstInterval:F1}s");
            }

            for (int p = 0; p < allSpawnPoints.Count; p++)
            {
                allSpawnPoints[p].ForceSpawn(perSpawnPoint, buffed);
            }

            yield return new WaitForSeconds(burstInterval);
        }
    }

    private EnemyConfiguration CreateFinalRushConfig()
    {
        EnemyConfiguration baseConfig = null;

        if (continuousEnemyTypes != null && continuousEnemyTypes.Length > 0)
            baseConfig = continuousEnemyTypes[0];

        if (baseConfig == null && waveQueue != null && waveQueue.Length > 0)
        {
            WaveData wave = waveQueue[Mathf.Clamp(currentWaveIndex, 0, waveQueue.Length - 1)];
            if (wave != null)
                baseConfig = wave.GetRandomEnemyConfig();
        }

        if (baseConfig == null)
            return null;

        EnemyConfiguration clone = Instantiate(baseConfig);
        clone.maxHealth = finalRushBaseHealth;
        clone.moveSpeed = finalRushBaseSpeed;
        clone.contactDamage = finalRushBaseContactDamage;

        clone.coinDropChance = 0f;
        clone.diamondDropChance = 0f;
        clone.minOrbs = 0;
        clone.maxOrbs = 0;
        return clone;
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
            if (finalRushActive) yield break;

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

    private float EarlyDifficultyFactor()
    {
        if (!enableEarlyRamp || earlyRampSeconds <= 0f) return 1f;

        GameTimeManager gtm = GameTimeManager.Instance;
        float t = gtm != null ? gtm.GetGameTime() : 0f;
        if (t >= earlyRampSeconds) return 1f;

        float p = Mathf.Clamp01(t / earlyRampSeconds);
        float eased = Mathf.Pow(p, Mathf.Max(1f, earlyRampCurvePower));
        return Mathf.Lerp(Mathf.Clamp(earlyRampStartFactor, 0.05f, 1f), 1f, eased);
    }

    private float EarlyScaledInterval(float baseInterval)
    {
        return baseInterval / Mathf.Max(0.05f, EarlyDifficultyFactor());
    }

    private int EarlyScaledBatch(int baseBatch)
    {
        return Mathf.Max(1, Mathf.RoundToInt(baseBatch * EarlyDifficultyFactor()));
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
            if (finalRushActive)
            {
                isSpawningWave = false;
                yield break;
            }

            while (EnemyHealth.ActiveEnemyCount >= maxConcurrentEnemies)
                yield return new WaitForSeconds(0.5f);

            int remainingEnemies = totalEnemies - enemiesSpawned;
            int enemiesToSpawnThisBatch = Mathf.Min(EarlyScaledBatch(wave.enemiesPerBatch), remainingEnemies);

            int actuallySpawned = SpawnBatch(enemiesToSpawnThisBatch, wave);
            enemiesSpawned += actuallySpawned;

            if (enemiesSpawned < totalEnemies)
                yield return new WaitForSeconds(EarlyScaledInterval(wave.spawnInterval));
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

    private const int   ScatterThresholdOffScreen    = 10;

    private const int   ScatterThresholdOnScreen     = 20;

    private const int   ScatterMaxPerCluster         = 5;

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

                if (cluster.Count < ScatterThresholdOffScreen) continue;

                bool extremePileup = cluster.Count >= ScatterThresholdOnScreen;

                var candidates = new List<EnemyHealth>();
                foreach (var e in cluster)
                {
                    if (e == null) continue;

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

                    if (onScreen && !extremePileup) continue;

                    candidates.Add(e);
                }

                int toScatter = Mathf.Max(0, candidates.Count - (ScatterMaxPerCluster - 1));
                if (toScatter == 0) continue;

                Debug.Log($"[Scatter] Cluster {cluster.Count} (extremo={extremePileup}) — candidatos {candidates.Count} — dispersando {toScatter}");

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
