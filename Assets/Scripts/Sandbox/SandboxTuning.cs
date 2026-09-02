using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-5000)]
public class SandboxTuning : MonoBehaviour
{
    public enum TutorialMode
    {
        UsarConfigDeProduccion,
        Desactivado,
        ForzarSiempre
    }

    [System.Serializable]
    public class UpgradeTypeLevel
    {
        public UpgradeType upgradeType;
        [Range(0, 20)] public int level = 0;
    }

    [Header("=== BALANCE INDEPENDIENTE ===")]
    [Tooltip("GameBalanceConfig del sandbox (Assets/Configurations/Sandbox/GameBalanceConfig_Sandbox.asset). Se inyecta ANTES que cualquier otro script lea GameBalanceConfig.Instance.")]
    [SerializeField] private GameBalanceConfig balanceOverride;
    [Tooltip("UpgradeDatabase del sandbox. Aquí es donde se prueban mejoras y sinergias nuevas sin tocar la base real.")]
    [SerializeField] private UpgradeDatabase upgradeDatabaseOverride;
    [Tooltip("SynergyDatabase del sandbox. Ajusta aquí los requisitos y números de cada sinergia sin tocar producción.")]
    [SerializeField] private SynergyDatabase synergyDatabaseOverride;
    [Tooltip("ChestOpeningConfig del sandbox. Ajusta aquí los tiempos, colores y sacudidas de la cinemática de apertura de cofre sin tocar producción.")]
    [SerializeField] private ChestOpeningConfig chestOpeningConfigOverride;

    [Header("=== PARTIDA ===")]
    [SerializeField] private float matchDurationMinutes = 3f;
    [SerializeField] private float startingTimeScale = 1f;

    [Header("=== JUGADOR ===")]
    [SerializeField] private bool startInvulnerable = false;
    [Tooltip("Vida extra añadida al máximo del jugador definido en GameBalanceConfig (ajuste rápido sin tener que abrir ese asset).")]
    [SerializeField] private float startingHealthBonus = 0f;

    [Header("=== TUTORIAL ===")]
    [Tooltip("Usar config de producción: se comporta igual que en CityTest (respeta si ya lo completaste antes).\nDesactivado: nunca se muestra en el sandbox.\nForzar siempre: se muestra siempre, aunque ya lo hayas completado antes.")]
    [SerializeField] private TutorialMode tutorialMode = TutorialMode.Desactivado;

    [Header("=== SINERGIAS ===")]
    [Tooltip("Apaga esto para probar el juego sin ninguna sinergia, aunque se alcancen los niveles requeridos.")]
    [SerializeField] private bool synergiesEnabled = true;
    [Tooltip("Sinergias que se activan directamente al arrancar, sin comprobar ni subir los niveles requeridos. Útil para probar solo el efecto de una en aislado.")]
    [SerializeField] private List<SynergyData> forceActiveSynergies;

    [Header("=== PROGRESIÓN INICIAL ===")]
    [SerializeField] private int startingCoins = 0;
    [SerializeField] private int startingDiamonds = 0;
    [SerializeField] private int startingPlayerLevels = 0;
    [Tooltip("Nivel inicial de cada mejora. Súbelas a los niveles requeridos por una sinergia para empezar la partida con ella ya activa.")]
    [SerializeField] private List<UpgradeTypeLevel> startingUpgradeLevels;

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

        if (synergyDatabaseOverride != null)
        {
            SynergyDatabase.OverrideInstance(synergyDatabaseOverride);
            SandboxLog.Ok($"SynergyDatabase sobrescrita con '{synergyDatabaseOverride.name}'.");
        }
        else
        {
            SandboxLog.Skipped("SynergyDatabase: usando el asset de producción (Resources/SynergyDatabase).");
        }

        if (chestOpeningConfigOverride != null)
        {
            ChestOpeningConfig.OverrideInstance(chestOpeningConfigOverride);
            SandboxLog.Ok($"ChestOpeningConfig sobrescrito con '{chestOpeningConfigOverride.name}'.");
        }
        else
        {
            SandboxLog.Skipped("ChestOpeningConfig: usando el asset de producción (Resources/ChestOpeningConfig).");
        }

        ConfigureTutorial();
    }

    private void ConfigureTutorial()
    {
        if (tutorialMode == TutorialMode.UsarConfigDeProduccion)
        {
            SandboxLog.Skipped("Tutorial: usando TutorialConfig de producción (respeta si ya lo completaste antes).");
            return;
        }

        TutorialConfig overrideConfig = ScriptableObject.CreateInstance<TutorialConfig>();
        overrideConfig.SetTutorialEnabled(tutorialMode == TutorialMode.ForzarSiempre);
        overrideConfig.SetForceShowEveryRun(tutorialMode == TutorialMode.ForzarSiempre);

        TutorialConfig.OverrideInstance(overrideConfig);
        SandboxLog.Ok($"Tutorial: {tutorialMode}.");
    }

    private void Start()
    {
        Time.timeScale = Mathf.Max(0.01f, startingTimeScale);

        ConfigureMatch();
        ConfigurePlayer();
        ConfigureSynergies();
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

    private void ConfigureSynergies()
    {
        SynergyManager.EnsureExists();
        SynergyManager.Instance?.SetEnabled(synergiesEnabled);

        if (forceActiveSynergies == null) return;

        for (int i = 0; i < forceActiveSynergies.Count; i++)
        {
            SynergyData synergy = forceActiveSynergies[i];
            if (synergy == null) continue;

            SynergyManager.Instance?.ForceActivate(synergy);
            SandboxLog.Ok($"Sinergia forzada: {synergy.synergyName}");
        }
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

        if (startingHealthBonus != 0f)
        {
            PlayerHealth health = FindFirstObjectByType<PlayerHealth>();
            health?.AddMaxHealth(startingHealthBonus);
        }

        int grantedUpgrades = ApplyStartingUpgradeLevels();

        if (startingPlayerLevels > 0)
            SandboxCommands.GrantLevels(startingPlayerLevels);

        SandboxLog.Ok($"Progresión inicial: {startingCoins} monedas, {startingDiamonds} diamantes, +{startingHealthBonus:F0} HP, {grantedUpgrades} mejoras precargadas, +{startingPlayerLevels} niveles.");
    }

    private int ApplyStartingUpgradeLevels()
    {
        if (startingUpgradeLevels == null || PlayerStatsManager.Instance == null || UpgradeDatabase.Instance == null)
            return 0;

        int granted = 0;

        for (int i = 0; i < startingUpgradeLevels.Count; i++)
        {
            UpgradeTypeLevel entry = startingUpgradeLevels[i];
            if (entry == null || entry.level <= 0) continue;

            UpgradeData data = UpgradeDatabase.Instance.allUpgrades.Find(u => u != null && u.upgradeType == entry.upgradeType);
            if (data == null)
            {
                SandboxLog.Warn($"No se encontró un UpgradeData para {entry.upgradeType} en la UpgradeDatabase activa.");
                continue;
            }

            for (int level = 0; level < entry.level; level++)
                PlayerStatsManager.Instance.ApplyUpgrade(data);

            granted++;
        }

        return granted;
    }
}
