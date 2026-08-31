using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(fileName = "SandboxConfig", menuName = "Game/Sandbox/Sandbox Config")]
public class SandboxConfig : ScriptableObject
{
    public enum EnvironmentMode
    {
        ProceduralArena,
        EnvironmentPrefab,
        AdditiveScene
    }

    [System.Serializable]
    public class ObstacleRing
    {
        [Tooltip("Prefabs que se reparten en el anillo (edificios, props). Se eligen al azar.")]
        public GameObject[] prefabs;
        [Tooltip("Cuántos objetos se colocan en total.")]
        public int count = 20;
        [Tooltip("Distancia mínima al centro. Deja libre la zona de juego cercana al jugador.")]
        public float innerRadius = 12f;
        [Tooltip("Distancia máxima al centro.")]
        public float outerRadius = 42f;
        [Tooltip("Escala uniforme aplicada a cada instancia.")]
        public float uniformScale = 2f;
        public bool randomRotation = true;
    }

    [System.Serializable]
    public class StartingUpgrade
    {
        public UpgradeData upgrade;
        [Tooltip("Cuántos niveles de esta mejora se conceden al arrancar la partida.")]
        [Range(1, 99)] public int levels = 1;
    }

    [System.Serializable]
    public class EnemyScaling
    {
        [Tooltip("Multiplica la vida de TODOS los enemigos del sandbox. 1 = valores originales del EnemyConfiguration.")]
        [Range(0.05f, 50f)] public float healthMultiplier = 1f;
        [Range(0.05f, 10f)] public float moveSpeedMultiplier = 1f;
        [Range(0f, 10f)] public float contactDamageMultiplier = 1f;
        [Tooltip("Multiplica la cantidad de orbes de experiencia que sueltan.")]
        [Range(0f, 20f)] public float orbDropMultiplier = 1f;

        [Tooltip("Si está activo, sustituye la probabilidad de drop de monedas de todos los enemigos por el valor de abajo.")]
        public bool overrideCoinDropChance = false;
        [Range(0f, 1f)] public float coinDropChance = 0.5f;

        [Tooltip("Si está activo, sustituye la probabilidad de drop de diamantes de todos los enemigos por el valor de abajo.")]
        public bool overrideDiamondDropChance = false;
        [Range(0f, 1f)] public float diamondDropChance = 0.1f;
    }

    [System.Serializable]
    public class WaveScaling
    {
        [Tooltip("Multiplica 'totalEnemies' de cada wave.")]
        [Range(0.05f, 20f)] public float sizeMultiplier = 1f;
        [Tooltip("Multiplica el ritmo: 2 = spawnea el doble de rápido (divide spawnInterval entre 2).")]
        [Range(0.05f, 20f)] public float paceMultiplier = 1f;
        [Tooltip("Multiplica 'enemiesPerBatch' de cada wave.")]
        [Range(0.05f, 20f)] public float batchMultiplier = 1f;
    }

    [System.Serializable]
    public class SpawnRing
    {
        [Tooltip("Cuántos SpawnPoint se generan alrededor del centro del mapa.")]
        [Range(1, 16)] public int count = 8;
        [Tooltip("Distancia de los SpawnPoint al centro.")]
        public float radius = 30f;
        public int maxEnemiesPerSpawn = 4;
        public float spawnCooldown = 2f;
        [Tooltip("Radio de dispersión alrededor del propio SpawnPoint.")]
        public float spawnRadius = 3f;
        [Tooltip("Actívalo solo si el entorno tiene NavMesh horneado (modo AdditiveScene con un nivel real).")]
        public bool useNavMesh = false;
        public bool showSpawnWarning = true;
        public float warningDuration = 1f;
    }

    [System.Serializable]
    public class Hotkeys
    {
        public bool enabled = true;
        public Key statusReport = Key.F1;
        public Key grantLevel = Key.F2;
        public Key grantRandomUpgrade = Key.F3;
        public Key grantRandomPremiumUpgrade = Key.F4;
        public Key spawnEnemyBurst = Key.F5;
        public Key killAllEnemies = Key.F6;
        public Key addCurrency = Key.F7;
        public Key toggleInvulnerable = Key.F8;
        public Key toggleSpawning = Key.F9;
        public Key forceFinalRush = Key.F10;
        public Key cycleTimeScale = Key.F11;
        public Key spawnChest = Key.F12;
        public Key reloadSandbox = Key.Backspace;

        [Tooltip("Cuántos enemigos spawnea la tecla de ráfaga manual.")]
        public int burstAmount = 10;
        [Tooltip("Distancia a la que aparece la ráfaga manual alrededor del jugador.")]
        public float burstRadius = 12f;
        public int currencyPerPress = 500;
        [Tooltip("Valores por los que rota la tecla de time scale.")]
        public float[] timeScaleSteps = { 1f, 0.25f, 2f, 4f };
    }

    [Header("=== OVERRIDES DE BALANCE (duplica los assets originales) ===")]
    [Tooltip("GameBalanceConfig alternativo SOLO para el sandbox. Duplica Assets/Resources/GameBalanceConfig.asset, edítalo y arrástralo aquí. Si se deja vacío se usa el de producción.")]
    [SerializeField] private GameBalanceConfig balanceOverride;

    [Tooltip("UpgradeDatabase alternativa SOLO para el sandbox. Aquí es donde probarás mejoras y sinergias nuevas sin tocar la base real.")]
    [SerializeField] private UpgradeDatabase upgradeDatabaseOverride;

    [Header("=== ENTORNO ===")]
    [Tooltip("ProceduralArena: genera suelo, muros y obstáculos. EnvironmentPrefab: instancia un prefab tuyo. AdditiveScene: carga un nivel real de forma aditiva y apaga sus managers.")]
    [SerializeField] private EnvironmentMode environmentMode = EnvironmentMode.ProceduralArena;

    [Header("Arena procedural")]
    [SerializeField] private float arenaSize = 120f;
    [SerializeField] private Material groundMaterial;
    [SerializeField] private bool createInvisibleWalls = true;
    [SerializeField] private float wallHeight = 12f;
    [SerializeField] private ObstacleRing obstacles = new ObstacleRing();
    [Tooltip("Semilla del generador de obstáculos. Mismo valor = mismo mapa cada vez.")]
    [SerializeField] private int arenaSeed = 12345;

    [Header("Prefab de entorno")]
    [SerializeField] private GameObject environmentPrefab;

    [Header("Escena aditiva")]
    [Tooltip("Nombre de la escena a cargar (debe estar en Build Settings). Ej: CityTest")]
    [SerializeField] private string additiveSceneName = "CityTest";
    [Tooltip("Objetos raíz de esa escena que se CONSERVAN. Todo lo demás (managers, canvas, cámaras) se apaga para que mande el sandbox.")]
    [SerializeField] private string[] additiveKeepRoots = { "MAP", "Directional Light", "Global Volume" };

    [Header("=== JUGADOR ===")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Vector3 playerSpawnPosition = new Vector3(0f, 1f, 0f);
    [Tooltip("Arranca la partida con el jugador invulnerable (se puede alternar con la tecla configurada).")]
    [SerializeField] private bool startInvulnerable = false;

    [Header("=== CÁMARA ===")]
    [SerializeField] private bool createCamera = true;
    [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 26f, -13f);
    [SerializeField] private float cameraPitch = 62f;
    [SerializeField] private float cameraFieldOfView = 55f;
    [SerializeField] private float cameraDamping = 8f;

    [Header("=== POOLS ===")]
    [Tooltip("Mismos campos que el PoolManager de un nivel real. Cada tipo que vayas a spawnear necesita su entrada aquí.")]
    [SerializeField] private List<PoolManager.PoolConfig> pools = new List<PoolManager.PoolConfig>();

    [Header("=== SPAWNS ===")]
    [SerializeField] private SpawnRing spawnRing = new SpawnRing();
    [SerializeField] private WaveData[] waveQueue;
    [SerializeField] private float timeBetweenWaves = 10f;
    [SerializeField] private bool autoLoopWaves = true;
    [SerializeField] private bool enableContinuousSpawn = true;
    [SerializeField] private float continuousSpawnInterval = 3f;
    [SerializeField] private int continuousEnemiesPerSpawn = 2;
    [SerializeField] private EnemyConfiguration[] continuousEnemyTypes;
    [SerializeField] private int maxConcurrentEnemies = 120;
    [Tooltip("Rampa de dificultad inicial del EnemySpawnManager. En el sandbox suele interesar apagarla para medir el ritmo real desde el segundo 0.")]
    [SerializeField] private bool enableEarlyRamp = false;
    [Tooltip("Enemigo usado por la tecla de ráfaga manual. Si se deja vacío se usa el primero de 'continuousEnemyTypes'.")]
    [SerializeField] private EnemyConfiguration manualBurstEnemy;

    [Header("=== ESCALADO DE PRUEBA ===")]
    [SerializeField] private bool applyEnemyScaling = false;
    [SerializeField] private EnemyScaling enemyScaling = new EnemyScaling();
    [SerializeField] private bool applyWaveScaling = false;
    [SerializeField] private WaveScaling waveScaling = new WaveScaling();

    [Header("Separación de enemigos")]
    [SerializeField] private bool overrideSeparation = false;
    [SerializeField] private float separationRadius = 1.1f;
    [SerializeField] private float separationMaxSpeed = 2.5f;
    [SerializeField] private float separationPushStrength = 3.5f;
    [SerializeField] private float separationRecalcInterval = 0.12f;
    [SerializeField] private int separationMaxNeighbors = 12;

    [Header("=== PARTIDA ===")]
    [Tooltip("Duración de la partida en minutos. Al llegar a 0 se dispara la oleada final.")]
    [SerializeField] private float matchDurationMinutes = 3f;
    [SerializeField] private float startingTimeScale = 1f;

    [Header("=== PROGRESIÓN INICIAL ===")]
    [SerializeField] private int startingCoins = 0;
    [SerializeField] private int startingDiamonds = 0;
    [Tooltip("Mejoras concedidas automáticamente al arrancar. Ideal para entrar directo a probar una build o una sinergia concreta.")]
    [SerializeField] private StartingUpgrade[] startingUpgrades;
    [Tooltip("Niveles de jugador concedidos al arrancar (concede la experiencia necesaria).")]
    [SerializeField] private int startingPlayerLevels = 0;

    [Header("=== COFRES ===")]
    [SerializeField] private bool enableChests = true;
    [SerializeField] private float chestFirstSpawnDelay = 20f;
    [SerializeField] private float chestSpawnInterval = 45f;
    [SerializeField] private float chestSpawnRadius = 10f;

    [Header("=== LOGS ===")]
    [SerializeField] private bool enableStatusLogger = true;
    [Tooltip("Cada cuántos segundos se imprime el informe de estado completo.")]
    [SerializeField] private float statusReportInterval = 15f;
    [SerializeField] private bool logLevelUps = true;
    [SerializeField] private bool logUpgrades = true;
    [SerializeField] private bool logWaveEvents = true;
    [SerializeField] private bool logChests = true;
    [Tooltip("Añade el PerformanceMonitor del juego para vigilar FPS y objetos activos.")]
    [SerializeField] private bool enablePerformanceMonitor = true;

    [Header("=== TECLAS ===")]
    [SerializeField] private Hotkeys hotkeys = new Hotkeys();

    public GameBalanceConfig BalanceOverride => balanceOverride;
    public UpgradeDatabase UpgradeDatabaseOverride => upgradeDatabaseOverride;

    public EnvironmentMode Environment => environmentMode;
    public float ArenaSize => arenaSize;
    public Material GroundMaterial => groundMaterial;
    public bool CreateInvisibleWalls => createInvisibleWalls;
    public float WallHeight => wallHeight;
    public ObstacleRing Obstacles => obstacles;
    public int ArenaSeed => arenaSeed;
    public GameObject EnvironmentPrefab => environmentPrefab;
    public string AdditiveSceneName => additiveSceneName;
    public string[] AdditiveKeepRoots => additiveKeepRoots;

    public GameObject PlayerPrefab => playerPrefab;
    public Vector3 PlayerSpawnPosition => playerSpawnPosition;
    public bool StartInvulnerable => startInvulnerable;

    public bool CreateCamera => createCamera;
    public Vector3 CameraOffset => cameraOffset;
    public float CameraPitch => cameraPitch;
    public float CameraFieldOfView => cameraFieldOfView;
    public float CameraDamping => cameraDamping;

    public List<PoolManager.PoolConfig> Pools => pools;

    public SpawnRing Spawns => spawnRing;
    public WaveData[] WaveQueue => waveQueue;
    public float TimeBetweenWaves => timeBetweenWaves;
    public bool AutoLoopWaves => autoLoopWaves;
    public bool EnableContinuousSpawn => enableContinuousSpawn;
    public float ContinuousSpawnInterval => continuousSpawnInterval;
    public int ContinuousEnemiesPerSpawn => continuousEnemiesPerSpawn;
    public EnemyConfiguration[] ContinuousEnemyTypes => continuousEnemyTypes;
    public int MaxConcurrentEnemies => maxConcurrentEnemies;
    public bool EnableEarlyRamp => enableEarlyRamp;
    public EnemyConfiguration ManualBurstEnemy => manualBurstEnemy;

    public bool ApplyEnemyScaling => applyEnemyScaling;
    public EnemyScaling Enemies => enemyScaling;
    public bool ApplyWaveScaling => applyWaveScaling;
    public WaveScaling Waves => waveScaling;

    public bool OverrideSeparation => overrideSeparation;
    public float SeparationRadius => separationRadius;
    public float SeparationMaxSpeed => separationMaxSpeed;
    public float SeparationPushStrength => separationPushStrength;
    public float SeparationRecalcInterval => separationRecalcInterval;
    public int SeparationMaxNeighbors => separationMaxNeighbors;

    public float MatchDurationMinutes => matchDurationMinutes;
    public float StartingTimeScale => startingTimeScale;

    public int StartingCoins => startingCoins;
    public int StartingDiamonds => startingDiamonds;
    public StartingUpgrade[] StartingUpgrades => startingUpgrades;
    public int StartingPlayerLevels => startingPlayerLevels;

    public bool EnableChests => enableChests;
    public float ChestFirstSpawnDelay => chestFirstSpawnDelay;
    public float ChestSpawnInterval => chestSpawnInterval;
    public float ChestSpawnRadius => chestSpawnRadius;

    public bool EnableStatusLogger => enableStatusLogger;
    public float StatusReportInterval => statusReportInterval;
    public bool LogLevelUps => logLevelUps;
    public bool LogUpgrades => logUpgrades;
    public bool LogWaveEvents => logWaveEvents;
    public bool LogChests => logChests;
    public bool EnablePerformanceMonitor => enablePerformanceMonitor;

    public Hotkeys Keys => hotkeys;
}
