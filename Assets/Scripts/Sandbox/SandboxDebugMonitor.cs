using UnityEngine;
using TMPro;

public class SandboxDebugMonitor : MonoBehaviour, IUpdateable
{
    [Header("UI")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI debugText;

    [Header("Refresco")]
    [SerializeField] private float livePanelRefreshInterval = 0.25f;
    [SerializeField] private float consoleReportInterval = 15f;

    [Header("Eventos por consola")]
    [SerializeField] private bool logLevelUps = true;
    [SerializeField] private bool logUpgrades = true;
    [SerializeField] private bool logWaveEvents = true;
    [SerializeField] private bool logChests = true;

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

        if (logLevelUps) GameEvents.OnLevelUp += HandleLevelUp;
        if (logChests) GameEvents.OnChestSpawned += HandleChestSpawned;
        if (logWaveEvents) GameEvents.OnMatchTimeExpired += HandleMatchTimeExpired;

        if (logUpgrades && PlayerStatsManager.Instance != null)
            PlayerStatsManager.Instance.OnUpgradeApplied += HandleUpgradeApplied;
    }

    private void OnDisable()
    {
        UpdateManager.Instance?.Unregister(this);

        GameEvents.OnLevelUp -= HandleLevelUp;
        GameEvents.OnChestSpawned -= HandleChestSpawned;
        GameEvents.OnMatchTimeExpired -= HandleMatchTimeExpired;

        if (PlayerStatsManager.Instance != null)
            PlayerStatsManager.Instance.OnUpgradeApplied -= HandleUpgradeApplied;
    }

    public void OnUpdate(float deltaTime)
    {
        if (deltaTime > 0f)
        {
            float instantFps = 1f / deltaTime;
            smoothedFps = smoothedFps <= 0f ? instantFps : Mathf.Lerp(smoothedFps, instantFps, 0.1f);
        }

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

    private void RefreshPanel()
    {
        if (debugText == null) return;
        debugText.text = SandboxReportBuilder.Build(smoothedFps);
    }

    private void LogFullReport()
    {
        Debug.Log($"{SandboxLog.Prefix} ══ INFORME ══\n{SandboxLog.Prefix} " + SandboxReportBuilder.Build(smoothedFps).Replace("\n", $"\n{SandboxLog.Prefix} ").TrimEnd());
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
}
