using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SandboxBootstrapper : MonoBehaviour
{
    public static SandboxBootstrapper Instance { get; private set; }

    [SerializeField] private SandboxConfig config;

    private readonly List<SpawnPoint> spawnPoints = new List<SpawnPoint>();
    private readonly Dictionary<EnemyConfiguration, EnemyConfiguration> enemyClones = new Dictionary<EnemyConfiguration, EnemyConfiguration>();
    private readonly List<Object> runtimeAssets = new List<Object>();

    private Transform environmentRoot;
    private GameObject player;
    private PlayerHealth playerHealth;
    private PlayerExperience playerExperience;
    private bool ready;

    public SandboxConfig Config => config;
    public GameObject Player => player;
    public PlayerHealth PlayerHealth => playerHealth;
    public PlayerExperience PlayerExperience => playerExperience;
    public IReadOnlyList<SpawnPoint> SpawnPoints => spawnPoints;
    public bool IsReady => ready;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (config == null)
        {
            SandboxLog.Error("No hay SandboxConfig asignado. Asigna uno en el inspector de [SANDBOX].");
            enabled = false;
            return;
        }

        ApplyDatabaseOverrides();
    }

    private void OnDestroy()
    {
        if (Instance != this) return;

        for (int i = 0; i < runtimeAssets.Count; i++)
        {
            if (runtimeAssets[i] != null)
                Destroy(runtimeAssets[i]);
        }

        runtimeAssets.Clear();
        enemyClones.Clear();
        Instance = null;
    }

    private void Start()
    {
        StartCoroutine(BuildRoutine());
    }

    private void ApplyDatabaseOverrides()
    {
        if (config.BalanceOverride != null)
        {
            GameBalanceConfig.OverrideInstance(config.BalanceOverride);
            SandboxLog.Ok($"GameBalanceConfig sobrescrito con '{config.BalanceOverride.name}'.");
        }
        else
        {
            SandboxLog.Skipped("GameBalanceConfig: usando el asset de producción (Resources/GameBalanceConfig).");
        }

        if (config.UpgradeDatabaseOverride != null)
        {
            UpgradeDatabase.OverrideInstance(config.UpgradeDatabaseOverride);
            SandboxLog.Ok($"UpgradeDatabase sobrescrita con '{config.UpgradeDatabaseOverride.name}'.");
        }
        else
        {
            SandboxLog.Skipped("UpgradeDatabase: usando el asset de producción (Resources/UpgradeDatabase).");
        }
    }

    private IEnumerator BuildRoutine()
    {
        SandboxLog.Info($"═══ Arrancando sandbox con config '{config.name}' ═══");

        Time.timeScale = Mathf.Max(0.01f, config.StartingTimeScale);

        environmentRoot = new GameObject("[SANDBOX ENVIRONMENT]").transform;
        yield return SandboxEnvironmentBuilder.Build(config, environmentRoot);

        if (config.Environment == SandboxConfig.EnvironmentMode.AdditiveScene)
        {
            int purged = SandboxEnvironmentBuilder.PurgeImportedManagers();
            yield return null;
            SandboxLog.Ok($"Managers del nivel importado eliminados: {purged}. A partir de aquí manda el sandbox.");
        }

        CreateCoreManagers();
        CreatePools();

        CreatePlayer();
        CreateCamera();
        CollectSpawnPoints();
        CreateSpawnManager();
        ConfigureSeparation();
        ConfigureChests();
        ConfigureMatchTimer();

        yield return null;

        ApplyStartingProgression();
        CreateTooling();

        ready = true;
        SandboxLog.Info("═══ Sandbox listo ═══");

        SandboxStatusLogger.ReportNow();
    }

    private void CreateCoreManagers()
    {
        GameObject managers = new GameObject("[SANDBOX MANAGERS]");

        if (UpdateManager.Instance == null)
            SandboxLog.Warn("UpdateManager no disponible.");

        if (CurrencyManager.Instance == null)
            new GameObject("CurrencyManager").AddComponent<CurrencyManager>();

        if (ExperienceManager.Instance == null)
            new GameObject("ExperienceManager").AddComponent<ExperienceManager>();

        if (PlayerStatsManager.Instance != null)
            PlayerStatsManager.Instance.ResetUpgrades();

        if (SpawnFactory.Instance == null)
            SandboxLog.Warn("SpawnFactory no disponible.");

        if (config.EnablePerformanceMonitor && PerformanceMonitor.Instance == null)
        {
            GameObject monitor = new GameObject("PerformanceMonitor");
            monitor.transform.SetParent(managers.transform);
            monitor.AddComponent<PerformanceMonitor>();
        }

        SandboxLog.Ok("Managers base creados (UpdateManager, SpawnFactory, CurrencyManager, ExperienceManager, PlayerStatsManager).");
    }

    private void CreatePools()
    {
        if (config.Pools == null || config.Pools.Count == 0)
        {
            SandboxLog.Warn("El SandboxConfig no tiene pools configurados: no se podrá spawnear nada. Usa Tools > Manners > Sandbox > Rellenar Config desde el nivel 1.");
            return;
        }

        PoolManager manager = PoolManager.Instance;

        if (manager == null)
        {
            GameObject poolObject = new GameObject("PoolManager");
            manager = poolObject.AddComponent<PoolManager>();
        }

        manager.AddPoolConfigs(config.Pools);

        int prewarmTotal = 0;
        for (int i = 0; i < config.Pools.Count; i++)
        {
            if (config.Pools[i] != null)
                prewarmTotal += config.Pools[i].prewarmCount;
        }

        SandboxLog.Ok($"Pools: {config.Pools.Count} tipos registrados, {prewarmTotal} instancias en precarga.");
    }

    private void CreatePlayer()
    {
        if (config.PlayerPrefab == null)
        {
            SandboxLog.Error("No hay Player Prefab en el SandboxConfig: el sandbox no tendrá jugador.");
            return;
        }

        player = Instantiate(config.PlayerPrefab, config.PlayerSpawnPosition, Quaternion.identity);
        player.name = "Player";

        playerHealth = player.GetComponent<PlayerHealth>();
        playerExperience = player.GetComponent<PlayerExperience>();

        if (playerHealth != null && config.StartInvulnerable)
            playerHealth.SetInvulnerable(true);

        SandboxLog.Ok($"Jugador instanciado en {config.PlayerSpawnPosition}. Invulnerable={config.StartInvulnerable}");
    }

    private void CreateCamera()
    {
        if (!config.CreateCamera)
        {
            SandboxLog.Skipped("Cámara: desactivada en el config (usa la cámara que ya haya en la escena).");
            return;
        }

        if (Camera.main != null)
        {
            SandboxLog.Skipped($"Cámara: ya existe '{Camera.main.name}' con tag MainCamera, no se crea otra.");
            return;
        }

        GameObject cameraObject = new GameObject("SandboxCamera");
        cameraObject.tag = "MainCamera";

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = config.CameraFieldOfView;
        camera.farClipPlane = 500f;

        cameraObject.AddComponent<AudioListener>();

        SandboxCameraRig rig = cameraObject.AddComponent<SandboxCameraRig>();
        rig.Configure(player != null ? player.transform : null, config.CameraOffset, config.CameraPitch, config.CameraDamping);

        SandboxLog.Ok($"Cámara creada siguiendo al jugador con offset {config.CameraOffset}.");
    }

    private void CollectSpawnPoints()
    {
        spawnPoints.Clear();

        SpawnPoint[] existing = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);
        if (existing.Length > 0)
        {
            spawnPoints.AddRange(existing);
            SandboxLog.Ok($"SpawnPoints: reutilizando los {existing.Length} que trae el entorno importado.");
            return;
        }

        SandboxConfig.SpawnRing ring = config.Spawns;
        GameObject container = new GameObject("SpawnPoints");
        container.transform.SetParent(environmentRoot, false);

        int count = Mathf.Max(1, ring.count);
        int sectorCount = System.Enum.GetValues(typeof(SpawnPoint.SpawnSector)).Length;

        for (int i = 0; i < count; i++)
        {
            float angle = (i / (float)count) * Mathf.PI * 2f;
            Vector3 position = new Vector3(Mathf.Cos(angle) * ring.radius, 0f, Mathf.Sin(angle) * ring.radius);

            GameObject pointObject = new GameObject($"SpawnPoint_{i:00}");
            pointObject.SetActive(false);
            pointObject.transform.SetParent(container.transform, false);
            pointObject.transform.localPosition = position;

            SpawnPoint point = pointObject.AddComponent<SpawnPoint>();
            point.Configure((SpawnPoint.SpawnSector)(i % sectorCount), ring.maxEnemiesPerSpawn, ring.spawnCooldown,
                            ring.spawnRadius, ring.useNavMesh, ring.showSpawnWarning, ring.warningDuration);

            pointObject.SetActive(true);
            spawnPoints.Add(point);
        }

        SandboxLog.Ok($"SpawnPoints: {spawnPoints.Count} generados en un anillo de radio {ring.radius}.");
    }

    private void CreateSpawnManager()
    {
        WaveData[] waves = ResolveWaves(config.WaveQueue);
        EnemyConfiguration[] continuous = ResolveEnemies(config.ContinuousEnemyTypes);

        GameObject managerObject = new GameObject("EnemySpawnManager");
        managerObject.SetActive(false);

        EnemySpawnManager manager = managerObject.AddComponent<EnemySpawnManager>();
        manager.SetWaveQueue(waves, config.TimeBetweenWaves, config.AutoLoopWaves);
        manager.SetContinuousSpawn(config.EnableContinuousSpawn, config.ContinuousSpawnInterval, config.ContinuousEnemiesPerSpawn, continuous);
        manager.SetSpawnLimits(config.MaxConcurrentEnemies, config.EnableEarlyRamp);

        managerObject.SetActive(true);

        string scalingTag = config.ApplyEnemyScaling
            ? $"vida x{config.Enemies.healthMultiplier}, vel x{config.Enemies.moveSpeedMultiplier}, daño x{config.Enemies.contactDamageMultiplier}"
            : "sin escalado";

        SandboxLog.Ok($"EnemySpawnManager: {waves.Length} waves, continuo={config.EnableContinuousSpawn} ({config.ContinuousEnemiesPerSpawn} cada {config.ContinuousSpawnInterval}s), cap={config.MaxConcurrentEnemies}, rampa inicial={config.EnableEarlyRamp}, enemigos {scalingTag}.");
    }

    private void ConfigureSeparation()
    {
        if (!config.OverrideSeparation)
        {
            SandboxLog.Skipped("Separación de enemigos: valores por defecto.");
            return;
        }

        if (EnemySeparationManager.Instance == null)
            new GameObject("[EnemySeparationManager]").AddComponent<EnemySeparationManager>();

        EnemySeparationManager separation = EnemySeparationManager.Instance;
        if (separation == null)
        {
            SandboxLog.Warn("Separación de enemigos: no se pudo crear el manager.");
            return;
        }

        separation.Configure(config.SeparationRadius, config.SeparationMaxSpeed, config.SeparationPushStrength,
                             config.SeparationRecalcInterval, config.SeparationMaxNeighbors);

        SandboxLog.Ok($"Separación de enemigos: radio={config.SeparationRadius}, empuje={config.SeparationPushStrength}, vel máx={config.SeparationMaxSpeed}.");
    }

    private void ConfigureChests()
    {
        ChestSpawner spawner = ChestSpawner.Instance;

        if (spawner == null)
        {
            SandboxLog.Warn("ChestSpawner no encontrado.");
            return;
        }

        if (!config.EnableChests)
        {
            spawner.gameObject.SetActive(false);
            SandboxLog.Skipped("Cofres: desactivados en el config.");
            return;
        }

        spawner.SetSpawnTiming(config.ChestSpawnInterval, config.ChestFirstSpawnDelay);
        spawner.SetSpawnArea(config.PlayerSpawnPosition, config.ChestSpawnRadius * 0.35f, config.ChestSpawnRadius);

        SandboxLog.Ok($"Cofres: primero a los {config.ChestFirstSpawnDelay}s, luego cada {config.ChestSpawnInterval}s, radio {config.ChestSpawnRadius}.");
    }

    private void ConfigureMatchTimer()
    {
        GameTimeManager timeManager = GameTimeManager.Instance;

        if (timeManager == null)
        {
            SandboxLog.Warn("GameTimeManager no disponible.");
            return;
        }

        timeManager.SetMatchDuration(config.MatchDurationMinutes);
        timeManager.ResetGame();

        SandboxLog.Ok($"Partida: {config.MatchDurationMinutes} min hasta la oleada final. Time scale inicial x{Time.timeScale}.");
    }

    private void ApplyStartingProgression()
    {
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.ResetSessionCurrency();

            if (config.StartingCoins > 0) CurrencyManager.Instance.AddCoins(config.StartingCoins);
            if (config.StartingDiamonds > 0) CurrencyManager.Instance.AddDiamonds(config.StartingDiamonds);
        }

        int grantedUpgrades = 0;
        if (config.StartingUpgrades != null && PlayerStatsManager.Instance != null)
        {
            for (int i = 0; i < config.StartingUpgrades.Length; i++)
            {
                SandboxConfig.StartingUpgrade entry = config.StartingUpgrades[i];
                if (entry == null || entry.upgrade == null) continue;

                for (int level = 0; level < entry.levels; level++)
                    PlayerStatsManager.Instance.ApplyUpgrade(entry.upgrade);

                grantedUpgrades++;
            }
        }

        if (config.StartingPlayerLevels > 0)
            SandboxCommands.GrantLevels(config.StartingPlayerLevels);

        SandboxLog.Ok($"Progresión inicial: {config.StartingCoins} monedas, {config.StartingDiamonds} diamantes, {grantedUpgrades} mejoras precargadas, +{config.StartingPlayerLevels} niveles.");
    }

    private void CreateTooling()
    {
        GameObject tooling = new GameObject("[SANDBOX TOOLING]");
        tooling.transform.SetParent(transform, false);

        if (config.Keys != null && config.Keys.enabled)
        {
            SandboxHotkeys hotkeys = tooling.AddComponent<SandboxHotkeys>();
            SandboxLog.Ok($"Teclas de prueba: {hotkeys.BuildHelpText()}");
        }
        else
        {
            SandboxLog.Skipped("Teclas de prueba: desactivadas en el config.");
        }

        if (config.EnableStatusLogger)
        {
            tooling.AddComponent<SandboxStatusLogger>();
        }
        else
        {
            SandboxLog.Skipped("Logger de estado: desactivado en el config.");
        }
    }

    public EnemyConfiguration ResolveEnemy(EnemyConfiguration source)
    {
        if (source == null) return null;
        if (!config.ApplyEnemyScaling) return source;

        if (enemyClones.TryGetValue(source, out EnemyConfiguration cached))
            return cached;

        SandboxConfig.EnemyScaling scaling = config.Enemies;
        EnemyConfiguration clone = Instantiate(source);
        clone.name = $"{source.name} (Sandbox)";

        clone.maxHealth = Mathf.Max(1f, source.maxHealth * scaling.healthMultiplier);
        clone.moveSpeed = Mathf.Max(0.1f, source.moveSpeed * scaling.moveSpeedMultiplier);
        clone.contactDamage = Mathf.Max(0f, source.contactDamage * scaling.contactDamageMultiplier);
        clone.minOrbs = Mathf.RoundToInt(source.minOrbs * scaling.orbDropMultiplier);
        clone.maxOrbs = Mathf.RoundToInt(source.maxOrbs * scaling.orbDropMultiplier);

        if (scaling.overrideCoinDropChance)
            clone.coinDropChance = scaling.coinDropChance;

        if (scaling.overrideDiamondDropChance)
            clone.diamondDropChance = scaling.diamondDropChance;

        enemyClones[source] = clone;
        runtimeAssets.Add(clone);
        return clone;
    }

    private EnemyConfiguration[] ResolveEnemies(EnemyConfiguration[] sources)
    {
        if (sources == null) return new EnemyConfiguration[0];

        EnemyConfiguration[] result = new EnemyConfiguration[sources.Length];
        for (int i = 0; i < sources.Length; i++)
            result[i] = ResolveEnemy(sources[i]);

        return result;
    }

    private WaveData[] ResolveWaves(WaveData[] sources)
    {
        if (sources == null) return new WaveData[0];

        List<WaveData> result = new List<WaveData>(sources.Length);

        for (int i = 0; i < sources.Length; i++)
        {
            WaveData source = sources[i];
            if (source == null) continue;

            bool needsClone = config.ApplyWaveScaling || config.ApplyEnemyScaling;
            if (!needsClone)
            {
                result.Add(source);
                continue;
            }

            WaveData clone = Instantiate(source);
            clone.name = $"{source.name} (Sandbox)";

            if (config.ApplyWaveScaling)
            {
                SandboxConfig.WaveScaling scaling = config.Waves;
                clone.totalEnemies = Mathf.Max(1, Mathf.RoundToInt(source.totalEnemies * scaling.sizeMultiplier));
                clone.enemiesPerBatch = Mathf.Max(1, Mathf.RoundToInt(source.enemiesPerBatch * scaling.batchMultiplier));
                clone.spawnInterval = Mathf.Max(0.05f, source.spawnInterval / Mathf.Max(0.01f, scaling.paceMultiplier));
            }

            if (clone.enemyDistribution != null)
            {
                for (int e = 0; e < clone.enemyDistribution.Length; e++)
                {
                    if (clone.enemyDistribution[e] == null) continue;
                    clone.enemyDistribution[e].enemyConfig = ResolveEnemy(clone.enemyDistribution[e].enemyConfig);
                }
            }

            runtimeAssets.Add(clone);
            result.Add(clone);
        }

        return result.ToArray();
    }

    public EnemyConfiguration GetManualBurstEnemy()
    {
        if (config.ManualBurstEnemy != null)
            return ResolveEnemy(config.ManualBurstEnemy);

        if (config.ContinuousEnemyTypes != null && config.ContinuousEnemyTypes.Length > 0)
            return ResolveEnemy(config.ContinuousEnemyTypes[0]);

        if (config.WaveQueue != null)
        {
            for (int i = 0; i < config.WaveQueue.Length; i++)
            {
                if (config.WaveQueue[i] == null) continue;

                EnemyConfiguration fromWave = config.WaveQueue[i].GetRandomEnemyConfig();
                if (fromWave != null) return ResolveEnemy(fromWave);
            }
        }

        return null;
    }
}
