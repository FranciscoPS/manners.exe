using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SandboxDebugMonitor : MonoBehaviour, IUpdateable
{
    [Header("UI")]
    [SerializeField] private GameObject panelRoot;
    [Header("Texto del panel por idioma")]
    [SerializeField] private LocalizedString liveHeader = new LocalizedString("FPS: {0:F0} | Time scale: x{1:F2}\nTime left: {2}\nPlayer: {3:F0}/{4:F0} HP | Level {5}\nWallet: {6} coins | {7} gems\nDamage: {8:F1} | Fire interval: {9:F3}s\nRange: {10:F1} | Magnet: {11:F1}", "FPS: {0:F0} | Escala de tiempo: x{1:F2}\nTiempo restante: {2}\nJugador: {3:F0}/{4:F0} HP | Nivel {5}\nMonedero: {6} monedas | {7} gemas\nDaño: {8:F1} | Intervalo de disparo: {9:F3}s\nAlcance: {10:F1} | Imán: {11:F1}");
    [SerializeField] private LocalizedString liveFooter = new LocalizedString("Wave {0} | Active enemies: {1}\nSpawn blocked: {2}", "Oleada {0} | Enemigos activos: {1}\nSpawn bloqueado: {2}");
    [SerializeField] private LocalizedString overridesTitle = new LocalizedString("OVERRIDES", "SOBRECARGAS");
    [SerializeField] private LocalizedString overridesDisabled = new LocalizedString("OVERRIDES (disabled)", "SOBRECARGAS (desactivadas)");
    [SerializeField] private LocalizedString activeLabel = new LocalizedString("• ACTIVE", "• ACTIVA");
    [SerializeField] private LocalizedString levelLabel = new LocalizedString("Lv {0}", "Nv {0}");

    [Header("Refresco")]
    [SerializeField] private float livePanelRefreshInterval = 0.25f;
    [SerializeField] private float consoleReportInterval = 15f;

    [Header("Eventos por consola")]
    [SerializeField] private bool logLevelUps = true;
    [SerializeField] private bool logUpgrades = true;
    [SerializeField] private bool logWaveEvents = true;
    [SerializeField] private bool logChests = true;

    private static readonly Color ColorInactive = new Color(0.55f, 0.55f, 0.55f);
    private static readonly Color ColorPartial = new Color(0.95f, 0.75f, 0.25f);
    private static readonly Color ColorActive = new Color(0.35f, 0.9f, 0.4f);
    private static readonly Color ColorSectionTitle = new Color(0.5f, 0.75f, 1f);
    private static readonly Color ColorLabelDim = new Color(0.65f, 0.65f, 0.65f);

    [System.Serializable]
    private class UpgradeRow
    {
        public UpgradeType type;
        public TextMeshProUGUI label;
        public TextMeshProUGUI value;
    }

    [System.Serializable]
    private class OverrideRow
    {
        public OverrideData overrideData;
        public TextMeshProUGUI label;
        public TextMeshProUGUI value;
    }

    [SerializeField] private TextMeshProUGUI headerText;
    [SerializeField] private TextMeshProUGUI footerText;
    [SerializeField] private TextMeshProUGUI overridesSectionTitle;
    [SerializeField] private List<UpgradeRow> upgradeRows = new List<UpgradeRow>();
    [SerializeField] private List<OverrideRow> overrideRows = new List<OverrideRow>();

    private PlayerHealth cachedPlayerHealth;
    private PlayerExperience cachedPlayerExperience;

    private float panelTimer;
    private float consoleTimer;
    private float smoothedFps;

    public bool IsActive => isActiveAndEnabled;

    private void Awake()
    {
        consoleTimer = consoleReportInterval;

    }

    private void OnEnable()
    {
        UpdateManager.Instance?.Register(this);
        GameLocalization.LanguageChanged += RefreshPanel;

        if (logLevelUps) GameEvents.OnLevelUp += HandleLevelUp;
        if (logChests) GameEvents.OnChestSpawned += HandleChestSpawned;
        if (logWaveEvents) GameEvents.OnMatchTimeExpired += HandleMatchTimeExpired;

        if (logUpgrades && PlayerStatsManager.Instance != null)
            PlayerStatsManager.Instance.OnUpgradeApplied += HandleUpgradeApplied;

        if (OverrideManager.Instance != null)
        {
            OverrideManager.Instance.OnOverrideActivated += HandleOverrideActivated;
            OverrideManager.Instance.OnOverrideDeactivated += HandleOverrideDeactivated;
        }
    }

    private void OnDisable()
    {
        UpdateManager.Instance?.Unregister(this);
        GameLocalization.LanguageChanged -= RefreshPanel;

        GameEvents.OnLevelUp -= HandleLevelUp;
        GameEvents.OnChestSpawned -= HandleChestSpawned;
        GameEvents.OnMatchTimeExpired -= HandleMatchTimeExpired;

        if (PlayerStatsManager.Instance != null)
            PlayerStatsManager.Instance.OnUpgradeApplied -= HandleUpgradeApplied;

        if (OverrideManager.Instance != null)
        {
            OverrideManager.Instance.OnOverrideActivated -= HandleOverrideActivated;
            OverrideManager.Instance.OnOverrideDeactivated -= HandleOverrideDeactivated;
        }
    }

    public void OnUpdate(float deltaTime)
    {
        deltaTime = Time.unscaledDeltaTime;
        if (deltaTime > 0f)
        {
            float instantFps = 1f / deltaTime;
            smoothedFps = smoothedFps <= 0f ? instantFps : Mathf.Lerp(smoothedFps, instantFps, 0.1f);
        }

        EnsurePlayerCache();

        panelTimer -= deltaTime;
        if (panelTimer <= 0f)
        {
            panelTimer = livePanelRefreshInterval;
            RefreshPanel();
        }

        consoleTimer -= deltaTime;
        if (consoleTimer <= 0f)
        {
            consoleTimer = consoleReportInterval;
            LogFullReport();
        }
    }

    public void TogglePanel()
    {
        if (panelRoot == null) return;

        panelRoot.SetActive(!panelRoot.activeSelf);
        SandboxLog.Command($"Panel de debug: {(panelRoot.activeSelf ? "visible" : "oculto")}");
    }

    private void EnsurePlayerCache()
    {
        if (cachedPlayerHealth != null) return;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null) return;

        cachedPlayerHealth = playerObject.GetComponent<PlayerHealth>();
        cachedPlayerExperience = playerObject.GetComponent<PlayerExperience>();
    }

    private void RefreshPanel()
    {
        PlayerStatsManager liveStats = PlayerStatsManager.Instance;
        CurrencyManager wallet = CurrencyManager.Instance;
        if (headerText != null) headerText.text = liveHeader.Format(smoothedFps, Time.timeScale, GameTimeManager.Instance != null ? GameTimeManager.Instance.GetFormattedCountdown() : "--:--", cachedPlayerHealth != null ? cachedPlayerHealth.CurrentHealth : 0, cachedPlayerHealth != null ? cachedPlayerHealth.MaxHealth : 0, cachedPlayerExperience != null ? cachedPlayerExperience.GetCurrentLevel() : 0, wallet != null ? wallet.CurrentCoins : 0, wallet != null ? wallet.CurrentDiamonds : 0, liveStats != null ? liveStats.GetModifiedDamage() : 0, liveStats != null ? liveStats.GetModifiedAttackCooldown() : 0, liveStats != null ? liveStats.GetModifiedAttackRange() : 0, liveStats != null ? liveStats.GetModifiedMagnetRange() : 0);
        EnemySpawnManager spawner = EnemySpawnManager.Instance;
        if (footerText != null) footerText.text = liveFooter.Format(spawner != null ? spawner.CurrentWaveNumber : 0, EnemyHealth.ActiveEnemyCount, spawner != null && spawner.IsSpawnBlocked);

        OverrideManager overrideManager = OverrideManager.Instance;

        if (overridesSectionTitle != null)
        {
            bool enabled = overrideManager == null || overrideManager.OverridesEnabled;
            overridesSectionTitle.text = enabled ? overridesTitle.Value : overridesDisabled.Value;
            overridesSectionTitle.color = enabled ? ColorSectionTitle : ColorInactive;
        }

        PlayerStatsManager stats = PlayerStatsManager.Instance;
        for (int i = 0; i < upgradeRows.Count; i++)
        {
            UpgradeRow row = upgradeRows[i];
            UpgradeData data = FindUpgrade(row.type);
            row.label.text = data != null ? data.upgradeName : row.type.ToString();
            int level = stats != null ? stats.GetUpgradeLevel(row.type) : 0;

            row.value.text = level > 0 ? levelLabel.Format(level) : "—";
            row.value.color = level > 0 ? ColorActive : ColorInactive;
            row.label.color = level > 0 ? Color.white : ColorLabelDim;
        }

        for (int i = 0; i < overrideRows.Count; i++)
        {
            OverrideRow row = overrideRows[i];
            OverrideData overrideData = row.overrideData;
            if (overrideData == null) continue;
            row.label.text = overrideData.overrideName;
            bool active = overrideManager != null && overrideManager.IsOverrideActive(overrideData);

            if (active)
            {
                row.value.text = activeLabel.Value;
                row.value.color = ColorActive;
                row.label.color = Color.white;
            }
            else
            {
                int levelA = stats != null ? stats.GetUpgradeLevel(overrideData.requiredUpgradeA) : 0;
                int levelB = stats != null ? stats.GetUpgradeLevel(overrideData.requiredUpgradeB) : 0;
                bool anyProgress = levelA > 0 || levelB > 0;

                row.value.text = $"{FormatUpgradeName(overrideData.requiredUpgradeA)} {levelA}/{overrideData.requiredLevelA}   {FormatUpgradeName(overrideData.requiredUpgradeB)} {levelB}/{overrideData.requiredLevelB}";
                row.value.color = anyProgress ? ColorPartial : ColorInactive;
                row.label.color = ColorLabelDim;
            }
        }
    }

    private void LogFullReport()
    {
        string header = SandboxReportBuilder.BuildHeader(smoothedFps, cachedPlayerHealth, cachedPlayerExperience).Replace("\n", $"\n{SandboxLog.Prefix} ").TrimEnd();
        string footer = SandboxReportBuilder.BuildFooter().Replace("\n", $"\n{SandboxLog.Prefix} ").TrimEnd();

        Debug.Log($"{SandboxLog.Prefix} ══ INFORME ══\n{SandboxLog.Prefix} {header}\n" +
                   $"{SandboxLog.Prefix} Mejoras:  {SandboxReportBuilder.BuildUpgradesLine()}\n" +
                   $"{SandboxLog.Prefix} Sobrecargas: {SandboxReportBuilder.BuildOverridesLine()}\n" +
                   $"{SandboxLog.Prefix} {footer}");
    }

    private void HandleLevelUp(int level)
    {
        SandboxLog.Info($"NIVEL {level} alcanzado.");
    }

    private void HandleUpgradeApplied(UpgradeType type, int level)
    {
        UpgradeData data = FindUpgrade(type);
        string value = data != null ? data.GetFormattedValue(level) : level.ToString();

        SandboxLog.Info($"MEJORA aplicada: {type} → nivel {level} ({value}).");
    }

    private void HandleOverrideActivated(OverrideData overrideData)
    {
        SandboxLog.Info($"SOBRECARGA desbloqueada: {overrideData.overrideName}");
    }

    private void HandleOverrideDeactivated(OverrideData overrideData)
    {
        SandboxLog.Info($"SOBRECARGA desactivada: {overrideData.overrideName}");
    }

    private void HandleChestSpawned()
    {
        SandboxLog.Info("COFRE generado en el mapa.");
    }

    private void HandleMatchTimeExpired()
    {
        SandboxLog.Info("TIEMPO AGOTADO: comienza la oleada final.");
    }

    private static UpgradeData FindUpgrade(UpgradeType type)
    {
        UpgradeDatabase database = UpgradeDatabase.Instance;
        if (database == null || database.allUpgrades == null) return null;

        return database.allUpgrades.Find(u => u != null && u.upgradeType == type);
    }

    private static string FormatUpgradeName(UpgradeType type)
    {
        return GameSessionStats.Instance != null ? GameSessionStats.Instance.GetUpgradeDisplayName(type) : type.ToString();
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }

}
