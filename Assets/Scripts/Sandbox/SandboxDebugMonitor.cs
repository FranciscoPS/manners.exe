using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SandboxDebugMonitor : MonoBehaviour, IUpdateable
{
    [Header("UI")]
    [SerializeField] private GameObject panelRoot;

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

    private class UpgradeRow
    {
        public UpgradeType type;
        public TextMeshProUGUI label;
        public TextMeshProUGUI value;
    }

    private class SynergyRow
    {
        public SynergyData synergy;
        public TextMeshProUGUI label;
        public TextMeshProUGUI value;
    }

    private TextMeshProUGUI headerText;
    private TextMeshProUGUI footerText;
    private TextMeshProUGUI synergiesSectionTitle;
    private readonly List<UpgradeRow> upgradeRows = new List<UpgradeRow>();
    private readonly List<SynergyRow> synergyRows = new List<SynergyRow>();

    private PlayerHealth cachedPlayerHealth;
    private PlayerExperience cachedPlayerExperience;

    private float panelTimer;
    private float consoleTimer;
    private float smoothedFps;

    public bool IsActive => isActiveAndEnabled;

    private void Awake()
    {
        consoleTimer = consoleReportInterval;

        if (panelRoot != null)
            BuildLayout(panelRoot.transform);
    }

    private void OnEnable()
    {
        UpdateManager.Instance?.Register(this);

        if (logLevelUps) GameEvents.OnLevelUp += HandleLevelUp;
        if (logChests) GameEvents.OnChestSpawned += HandleChestSpawned;
        if (logWaveEvents) GameEvents.OnMatchTimeExpired += HandleMatchTimeExpired;

        if (logUpgrades && PlayerStatsManager.Instance != null)
            PlayerStatsManager.Instance.OnUpgradeApplied += HandleUpgradeApplied;

        if (SynergyManager.Instance != null)
        {
            SynergyManager.Instance.OnSynergyActivated += HandleSynergyActivated;
            SynergyManager.Instance.OnSynergyDeactivated += HandleSynergyDeactivated;
        }
    }

    private void OnDisable()
    {
        UpdateManager.Instance?.Unregister(this);

        GameEvents.OnLevelUp -= HandleLevelUp;
        GameEvents.OnChestSpawned -= HandleChestSpawned;
        GameEvents.OnMatchTimeExpired -= HandleMatchTimeExpired;

        if (PlayerStatsManager.Instance != null)
            PlayerStatsManager.Instance.OnUpgradeApplied -= HandleUpgradeApplied;

        if (SynergyManager.Instance != null)
        {
            SynergyManager.Instance.OnSynergyActivated -= HandleSynergyActivated;
            SynergyManager.Instance.OnSynergyDeactivated -= HandleSynergyDeactivated;
        }
    }

    public void OnUpdate(float deltaTime)
    {
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

    private void BuildLayout(Transform parent)
    {
        VerticalLayoutGroup rootLayout = GetOrAdd<VerticalLayoutGroup>(parent.gameObject);
        rootLayout.padding = new RectOffset(14, 14, 12, 12);
        rootLayout.spacing = 4f;
        rootLayout.childForceExpandHeight = false;
        rootLayout.childForceExpandWidth = true;
        rootLayout.childControlHeight = true;
        rootLayout.childControlWidth = true;

        ContentSizeFitter fitter = GetOrAdd<ContentSizeFitter>(parent.gameObject);
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        headerText = CreateText(parent, "Header", 16f, FontStyles.Normal, Color.white);

        CreateSectionTitle(parent, "MEJORAS");
        UpgradeType[] types = (UpgradeType[])System.Enum.GetValues(typeof(UpgradeType));
        for (int i = 0; i < types.Length; i++)
        {
            (TextMeshProUGUI label, TextMeshProUGUI value) = CreateRow(parent, FormatUpgradeName(types[i]));
            upgradeRows.Add(new UpgradeRow { type = types[i], label = label, value = value });
        }

        synergiesSectionTitle = CreateSectionTitle(parent, "SINERGIAS");
        List<SynergyData> synergies = SynergyDatabase.Instance != null ? SynergyDatabase.Instance.allSynergies : null;
        if (synergies != null)
        {
            for (int i = 0; i < synergies.Count; i++)
            {
                if (synergies[i] == null) continue;

                (TextMeshProUGUI label, TextMeshProUGUI value) = CreateRow(parent, synergies[i].synergyName);
                synergyRows.Add(new SynergyRow { synergy = synergies[i], label = label, value = value });
            }
        }

        CreateSectionTitle(parent, "PARTIDA");
        footerText = CreateText(parent, "Footer", 14f, FontStyles.Normal, new Color(0.85f, 0.85f, 0.85f));
    }

    private void RefreshPanel()
    {
        if (headerText != null) headerText.text = SandboxReportBuilder.BuildHeader(smoothedFps, cachedPlayerHealth, cachedPlayerExperience);
        if (footerText != null) footerText.text = SandboxReportBuilder.BuildFooter();

        SynergyManager synergyManager = SynergyManager.Instance;

        if (synergiesSectionTitle != null)
        {
            bool enabled = synergyManager == null || synergyManager.SynergiesEnabled;
            synergiesSectionTitle.text = enabled ? "SINERGIAS" : "SINERGIAS (desactivadas)";
            synergiesSectionTitle.color = enabled ? ColorSectionTitle : ColorInactive;
        }

        PlayerStatsManager stats = PlayerStatsManager.Instance;
        for (int i = 0; i < upgradeRows.Count; i++)
        {
            UpgradeRow row = upgradeRows[i];
            int level = stats != null ? stats.GetUpgradeLevel(row.type) : 0;

            row.value.text = level > 0 ? $"Nv {level}" : "—";
            row.value.color = level > 0 ? ColorActive : ColorInactive;
            row.label.color = level > 0 ? Color.white : ColorLabelDim;
        }

        for (int i = 0; i < synergyRows.Count; i++)
        {
            SynergyRow row = synergyRows[i];
            SynergyData synergy = row.synergy;
            bool active = synergyManager != null && synergyManager.IsSynergyActive(synergy);

            if (active)
            {
                row.value.text = "✔ ACTIVA";
                row.value.color = ColorActive;
                row.label.color = Color.white;
            }
            else
            {
                int levelA = stats != null ? stats.GetUpgradeLevel(synergy.requiredUpgradeA) : 0;
                int levelB = stats != null ? stats.GetUpgradeLevel(synergy.requiredUpgradeB) : 0;
                bool anyProgress = levelA > 0 || levelB > 0;

                row.value.text = $"{FormatUpgradeName(synergy.requiredUpgradeA)} {levelA}/{synergy.requiredLevelA}   {FormatUpgradeName(synergy.requiredUpgradeB)} {levelB}/{synergy.requiredLevelB}";
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
                   $"{SandboxLog.Prefix} Sinergias: {SandboxReportBuilder.BuildSynergiesLine()}\n" +
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

    private void HandleSynergyActivated(SynergyData synergy)
    {
        SandboxLog.Info($"SINERGIA desbloqueada: {synergy.synergyName}");
    }

    private void HandleSynergyDeactivated(SynergyData synergy)
    {
        SandboxLog.Info($"SINERGIA desactivada: {synergy.synergyName}");
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

    private TextMeshProUGUI CreateText(Transform parent, string name, float size, FontStyles style, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        TextMeshProUGUI text = obj.AddComponent<TextMeshProUGUI>();
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.alignment = TextAlignmentOptions.TopLeft;

        LayoutElement element = obj.AddComponent<LayoutElement>();
        element.flexibleWidth = 1f;

        return text;
    }

    private TextMeshProUGUI CreateSectionTitle(Transform parent, string text)
    {
        TextMeshProUGUI title = CreateText(parent, "Section_" + text, 14f, FontStyles.Bold, ColorSectionTitle);
        title.text = text;
        title.margin = new Vector4(0f, 8f, 0f, 2f);
        return title;
    }

    private (TextMeshProUGUI, TextMeshProUGUI) CreateRow(Transform parent, string labelText)
    {
        GameObject row = new GameObject("Row_" + labelText);
        row.transform.SetParent(parent, false);

        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.childForceExpandWidth = false;
        layout.childControlWidth = true;
        layout.spacing = 8f;

        LayoutElement rowElement = row.AddComponent<LayoutElement>();
        rowElement.preferredHeight = 20f;

        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(row.transform, false);
        TextMeshProUGUI label = labelObj.AddComponent<TextMeshProUGUI>();
        label.text = labelText;
        label.fontSize = 14f;
        label.color = ColorLabelDim;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        LayoutElement labelElement = labelObj.AddComponent<LayoutElement>();
        labelElement.preferredWidth = 220f;

        GameObject valueObj = new GameObject("Value");
        valueObj.transform.SetParent(row.transform, false);
        TextMeshProUGUI value = valueObj.AddComponent<TextMeshProUGUI>();
        value.fontSize = 14f;
        value.fontStyle = FontStyles.Bold;
        value.color = Color.white;
        value.alignment = TextAlignmentOptions.MidlineLeft;
        value.textWrappingMode = TextWrappingModes.NoWrap;
        LayoutElement valueElement = valueObj.AddComponent<LayoutElement>();
        valueElement.flexibleWidth = 1f;

        return (label, value);
    }
}
