using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(-5000)]
public class SandboxTuning : MonoBehaviour
{
    [System.Serializable]
    public class StartingUpgrade
    {
        public UpgradeData upgrade;
        [Range(1, 99)] public int levels = 1;
    }

    [Header("=== BALANCE INDEPENDIENTE ===")]
    [Tooltip("GameBalanceConfig del sandbox (Assets/Configurations/Sandbox/GameBalanceConfig_Sandbox.asset). Se inyecta ANTES que cualquier otro script lea GameBalanceConfig.Instance.")]
    [SerializeField] private GameBalanceConfig balanceOverride;
    [Tooltip("UpgradeDatabase del sandbox. Aquí es donde se prueban mejoras y sinergias nuevas sin tocar la base real.")]
    [SerializeField] private UpgradeDatabase upgradeDatabaseOverride;

    [Header("=== PARTIDA ===")]
    [SerializeField] private float matchDurationMinutes = 3f;
    [SerializeField] private float startingTimeScale = 1f;

    [Header("=== JUGADOR ===")]
    [SerializeField] private bool startInvulnerable = false;

    [Header("=== PROGRESIÓN INICIAL ===")]
    [SerializeField] private int startingCoins = 0;
    [SerializeField] private int startingDiamonds = 0;
    [SerializeField] private int startingPlayerLevels = 0;
    [Tooltip("Mejoras concedidas automáticamente al arrancar. Ideal para entrar directo a probar una build o una sinergia concreta.")]
    [SerializeField] private StartingUpgrade[] startingUpgrades;

    [Header("=== COFRES ===")]
    [SerializeField] private bool overrideChestTiming = false;
    [SerializeField] private float chestFirstSpawnDelay = 20f;
    [SerializeField] private float chestSpawnInterval = 45f;
    [SerializeField] private float chestMinRadius = 3f;
    [SerializeField] private float chestMaxRadius = 10f;

    [Header("=== SEPARACIÓN DE ENEMIGOS ===")]
    [SerializeField] private bool overrideSeparation = false;
    [SerializeField] private float separationRadius = 1.1f;
    [SerializeField] private float separationMaxSpeed = 2.5f;
    [SerializeField] private float separationPushStrength = 3.5f;
    [SerializeField] private float separationRecalcInterval = 0.12f;
    [SerializeField] private int separationMaxNeighbors = 12;

    private void Awake()
    {
        if (balanceOverride != null)
        {
            GameBalanceConfig.OverrideInstance(balanceOverride);
            SandboxLog.Ok($"GameBalanceConfig sobrescrito con '{balanceOverride.name}'.");
        }
        else
        {
            SandboxLog.Skipped("GameBalanceConfig: usando el asset de producción (Resources/GameBalanceConfig).");
        }

        if (upgradeDatabaseOverride != null)
        {
            UpgradeDatabase.OverrideInstance(upgradeDatabaseOverride);
            SandboxLog.Ok($"UpgradeDatabase sobrescrita con '{upgradeDatabaseOverride.name}'.");
        }
        else
        {
            SandboxLog.Skipped("UpgradeDatabase: usando el asset de producción (Resources/UpgradeDatabase).");
        }
    }

    private void Start()
    {
        Time.timeScale = Mathf.Max(0.01f, startingTimeScale);

        ConfigureMatch();
        ConfigurePlayer();
        ConfigureChests();
        ConfigureSeparation();

        StartCoroutine(ApplyStartingProgressionNextFrame());
    }

    private void ConfigureMatch()
    {
        GameTimeManager timeManager = GameTimeManager.Instance;
        if (timeManager == null)
        {
            SandboxLog.Warn("GameTimeManager no disponible.");
            return;
        }

        timeManager.SetMatchDuration(matchDurationMinutes);
        timeManager.ResetGame();

        SandboxLog.Ok($"Partida: {matchDurationMinutes} min hasta la oleada final. Time scale inicial x{Time.timeScale}.");
    }

    private void ConfigurePlayer()
    {
        PlayerHealth health = FindFirstObjectByType<PlayerHealth>();
        if (health == null)
        {
            SandboxLog.Warn("No se encontró PlayerHealth en la escena.");
            return;
        }

        if (startInvulnerable)
            health.SetInvulnerable(true);

        SandboxLog.Ok($"Jugador: {health.CurrentHealth:F0}/{health.MaxHealth:F0} HP. Invulnerable={startInvulnerable}");
    }

    private void ConfigureChests()
    {
        if (!overrideChestTiming)
        {
            SandboxLog.Skipped("Cofres: valores del nivel duplicado (sin override).");
            return;
        }

        ChestSpawner spawner = ChestSpawner.Instance;
        if (spawner == null)
        {
            SandboxLog.Warn("ChestSpawner no disponible.");
            return;
        }

        Transform player = FindFirstObjectByType<PlayerHealth>()?.transform;
        Vector3 center = player != null ? player.position : Vector3.zero;

        spawner.SetSpawnTiming(chestSpawnInterval, chestFirstSpawnDelay);
        spawner.SetSpawnArea(center, chestMinRadius, chestMaxRadius);

        SandboxLog.Ok($"Cofres: primero a los {chestFirstSpawnDelay}s, luego cada {chestSpawnInterval}s, radio {chestMinRadius}-{chestMaxRadius}.");
    }

    private void ConfigureSeparation()
    {
        if (!overrideSeparation)
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

        separation.Configure(separationRadius, separationMaxSpeed, separationPushStrength, separationRecalcInterval, separationMaxNeighbors);

        SandboxLog.Ok($"Separación de enemigos: radio={separationRadius}, empuje={separationPushStrength}, vel máx={separationMaxSpeed}.");
    }

    private IEnumerator ApplyStartingProgressionNextFrame()
    {
        yield return null;

        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.ResetSessionCurrency();

            if (startingCoins > 0) CurrencyManager.Instance.AddCoins(startingCoins);
            if (startingDiamonds > 0) CurrencyManager.Instance.AddDiamonds(startingDiamonds);
        }

        int grantedUpgrades = 0;
        if (startingUpgrades != null && PlayerStatsManager.Instance != null)
        {
            for (int i = 0; i < startingUpgrades.Length; i++)
            {
                StartingUpgrade entry = startingUpgrades[i];
                if (entry == null || entry.upgrade == null) continue;

                for (int level = 0; level < entry.levels; level++)
                    PlayerStatsManager.Instance.ApplyUpgrade(entry.upgrade);

                grantedUpgrades++;
            }
        }

        if (startingPlayerLevels > 0)
            SandboxCommands.GrantLevels(startingPlayerLevels);

        SandboxLog.Ok($"Progresión inicial: {startingCoins} monedas, {startingDiamonds} diamantes, {grantedUpgrades} mejoras precargadas, +{startingPlayerLevels} niveles.");
    }
}
