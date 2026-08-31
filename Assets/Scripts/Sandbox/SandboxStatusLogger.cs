using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class SandboxStatusLogger : MonoBehaviour, IUpdateable
{
    private static readonly List<KeyValuePair<string, Func<string>>> sections = new List<KeyValuePair<string, Func<string>>>();
    private static SandboxStatusLogger active;

    private SandboxConfig config;
    private float timer;

    public bool IsActive => isActiveAndEnabled;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        sections.Clear();
        active = null;
    }

    public static void RegisterSection(string title, Func<string> provider)
    {
        if (string.IsNullOrEmpty(title) || provider == null) return;

        UnregisterSection(title);
        sections.Add(new KeyValuePair<string, Func<string>>(title, provider));
    }

    public static void UnregisterSection(string title)
    {
        for (int i = sections.Count - 1; i >= 0; i--)
        {
            if (sections[i].Key == title)
                sections.RemoveAt(i);
        }
    }

    public static void ReportNow()
    {
        if (active != null)
            active.PrintReport();
    }

    private void Awake()
    {
        active = this;
        config = SandboxBootstrapper.Instance != null ? SandboxBootstrapper.Instance.Config : null;
        timer = config != null ? config.StatusReportInterval : 15f;
    }

    private void OnEnable()
    {
        UpdateManager.Instance?.Register(this);

        if (config == null) return;

        if (config.LogLevelUps) GameEvents.OnLevelUp += HandleLevelUp;
        if (config.LogChests) GameEvents.OnChestSpawned += HandleChestSpawned;
        if (config.LogWaveEvents) GameEvents.OnMatchTimeExpired += HandleMatchTimeExpired;

        if (config.LogUpgrades && PlayerStatsManager.Instance != null)
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

        if (active == this)
            active = null;
    }

    public void OnUpdate(float deltaTime)
    {
        if (config == null || config.StatusReportInterval <= 0f) return;

        timer -= deltaTime;
        if (timer > 0f) return;

        timer = config.StatusReportInterval;
        PrintReport();
    }

    private void HandleLevelUp(int level)
    {
        SandboxLog.Info($"NIVEL {level} alcanzado. {BuildStatsLine()}");
    }

    private void HandleUpgradeApplied(UpgradeType type, int level)
    {
        UpgradeData data = FindUpgrade(type);
        string value = data != null ? data.GetFormattedValue(level) : level.ToString();

        SandboxLog.Info($"MEJORA aplicada: {type} → nivel {level} ({value}). {BuildStatsLine()}");
    }

    private void HandleChestSpawned()
    {
        SandboxLog.Info("COFRE generado en el mapa.");
    }

    private void HandleMatchTimeExpired()
    {
        SandboxLog.Info("TIEMPO AGOTADO: comienza la oleada final.");
    }

    private void PrintReport()
    {
        StringBuilder builder = new StringBuilder(768);

        builder.AppendLine($"{SandboxLog.Prefix} ══ INFORME ══════════════════════════════════");
        builder.AppendLine($"{SandboxLog.Prefix} Config: {(config != null ? config.name : "?")} | time scale x{Time.timeScale:F2}");

        AppendMatch(builder);
        AppendPlayer(builder);
        AppendStats(builder);
        AppendUpgrades(builder);
        AppendSpawning(builder);
        AppendPools(builder);
        AppendCustomSections(builder);

        builder.Append($"{SandboxLog.Prefix} ═════════════════════════════════════════════");

        Debug.Log(builder.ToString());
    }

    private void AppendMatch(StringBuilder builder)
    {
        GameTimeManager time = GameTimeManager.Instance;
        GameSessionStats stats = GameSessionStats.Instance;

        string clock = time != null ? $"{time.GetFormattedCountdown()} restante (transcurrido {time.GetFormattedTime()})" : "sin GameTimeManager";
        string session = stats != null
            ? $"bajas={stats.EnemiesKilled} edificios={stats.BuildingsDestroyed} monedas={stats.CoinsCollected} diamantes={stats.DiamondsCollected}"
            : "sin GameSessionStats";

        builder.AppendLine($"{SandboxLog.Prefix} Partida:  {clock}");
        builder.AppendLine($"{SandboxLog.Prefix} Sesión:   {session}");
    }

    private void AppendPlayer(StringBuilder builder)
    {
        SandboxBootstrapper sandbox = SandboxBootstrapper.Instance;
        PlayerHealth health = sandbox != null ? sandbox.PlayerHealth : null;
        PlayerExperience experience = sandbox != null ? sandbox.PlayerExperience : null;

        if (health == null)
        {
            builder.AppendLine($"{SandboxLog.Prefix} Jugador:  no encontrado");
            return;
        }

        string level = experience != null
            ? $"nivel {experience.GetCurrentLevel()} ({experience.GetCurrentExperience()}/{experience.GetExperienceRequiredForNextLevel()} XP)"
            : "sin PlayerExperience";

        builder.AppendLine($"{SandboxLog.Prefix} Jugador:  {health.CurrentHealth:F0}/{health.MaxHealth:F0} HP | {level}{(health.IsInvulnerable ? " | INVULNERABLE" : "")}");

        if (CurrencyManager.Instance != null)
            builder.AppendLine($"{SandboxLog.Prefix} Monedero: {CurrencyManager.Instance.CurrentCoins} monedas, {CurrencyManager.Instance.CurrentDiamonds} diamantes");
    }

    private void AppendStats(StringBuilder builder)
    {
        PlayerStatsManager stats = PlayerStatsManager.Instance;
        if (stats == null) return;

        float cooldown = stats.GetModifiedAttackCooldown();

        builder.AppendLine($"{SandboxLog.Prefix} Combate:  daño={stats.GetModifiedDamage():F1} | cadencia={cooldown:F3}s ({(cooldown > 0f ? 1f / cooldown : 0f):F2}/s) | alcance={stats.GetModifiedAttackRange():F1} | imán={stats.GetModifiedMagnetRange():F1}");
        builder.AppendLine($"{SandboxLog.Prefix} Especial: multishot={stats.GetMultiShotProbability():F0}% (+{stats.GetMultiShotExtraBullets()} balas) | explosivo={stats.GetExplosiveShotProbability():F0}% (radio {stats.GetExplosionRadius():F1}) | knockback={stats.GetKnockbackProbability():F0}% (fuerza {stats.GetKnockbackForce():F1}, cadena {stats.GetKnockbackChainJumps()})");
    }

    private void AppendUpgrades(StringBuilder builder)
    {
        PlayerStatsManager stats = PlayerStatsManager.Instance;
        if (stats == null) return;

        Dictionary<UpgradeType, int> levels = stats.GetAllUpgradeLevels();
        StringBuilder line = new StringBuilder(128);

        foreach (KeyValuePair<UpgradeType, int> entry in levels)
        {
            if (entry.Value <= 0) continue;

            if (line.Length > 0) line.Append("  ");
            line.Append($"{entry.Key} Lv{entry.Value}");
        }

        builder.AppendLine($"{SandboxLog.Prefix} Mejoras:  {(line.Length > 0 ? line.ToString() : "ninguna")}");
    }

    private void AppendSpawning(StringBuilder builder)
    {
        EnemySpawnManager manager = EnemySpawnManager.Instance;
        SandboxBootstrapper sandbox = SandboxBootstrapper.Instance;

        int cap = config != null ? config.MaxConcurrentEnemies : 0;
        string wave = manager != null ? $"wave {manager.CurrentWaveNumber}" : "sin EnemySpawnManager";
        string blocked = manager != null && manager.IsSpawnBlocked ? " | SPAWNS BLOQUEADOS" : "";
        int points = sandbox != null ? sandbox.SpawnPoints.Count : 0;

        builder.AppendLine($"{SandboxLog.Prefix} Spawns:   {wave} | enemigos {EnemyHealth.ActiveEnemyCount}/{cap} | {points} puntos de spawn{blocked}");
    }

    private void AppendPools(StringBuilder builder)
    {
        PoolManager pool = PoolManager.Instance;
        if (pool == null || config == null || config.Pools == null) return;

        StringBuilder line = new StringBuilder(160);

        for (int i = 0; i < config.Pools.Count; i++)
        {
            PoolManager.PoolConfig entry = config.Pools[i];
            if (entry == null) continue;

            if (!pool.TryGetPoolStats(entry.poolType, out int total, out int available)) continue;

            if (line.Length > 0) line.Append("  ");
            line.Append($"{entry.poolType}={total - available}/{total}");
        }

        builder.AppendLine($"{SandboxLog.Prefix} Pools:    {(line.Length > 0 ? line.ToString() : "sin datos")}  (en uso/total)");
    }

    private void AppendCustomSections(StringBuilder builder)
    {
        for (int i = 0; i < sections.Count; i++)
        {
            string content;

            try
            {
                content = sections[i].Value?.Invoke();
            }
            catch (Exception exception)
            {
                content = $"error: {exception.Message}";
            }

            if (string.IsNullOrEmpty(content)) continue;

            builder.AppendLine($"{SandboxLog.Prefix} {sections[i].Key}: {content}");
        }
    }

    private string BuildStatsLine()
    {
        PlayerStatsManager stats = PlayerStatsManager.Instance;
        if (stats == null) return string.Empty;

        return $"[daño={stats.GetModifiedDamage():F1} cadencia={stats.GetModifiedAttackCooldown():F3}s alcance={stats.GetModifiedAttackRange():F1} imán={stats.GetModifiedMagnetRange():F1}]";
    }

    private static UpgradeData FindUpgrade(UpgradeType type)
    {
        UpgradeDatabase database = UpgradeDatabase.Instance;
        if (database == null || database.allUpgrades == null) return null;

        return database.allUpgrades.Find(u => u != null && u.upgradeType == type);
    }
}
