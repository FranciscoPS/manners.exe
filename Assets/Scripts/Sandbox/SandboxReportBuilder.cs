using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class SandboxReportBuilder
{
    public static string BuildHeader(float fps, PlayerHealth health, PlayerExperience experience)
    {
        StringBuilder builder = new StringBuilder(512);

        builder.AppendLine($"FPS: {fps:F0}   Time scale: x{Time.timeScale:F2}");

        AppendMatch(builder);
        AppendPlayer(builder, health, experience);
        AppendStats(builder);

        return builder.ToString();
    }

    public static string BuildFooter()
    {
        StringBuilder builder = new StringBuilder(256);

        AppendSpawning(builder);
        AppendPools(builder);

        return builder.ToString();
    }

    public static string BuildUpgradesLine()
    {
        PlayerStatsManager stats = PlayerStatsManager.Instance;
        if (stats == null) return "sin PlayerStatsManager";

        Dictionary<UpgradeType, int> levels = stats.GetAllUpgradeLevels();
        StringBuilder line = new StringBuilder(128);

        foreach (KeyValuePair<UpgradeType, int> entry in levels)
        {
            if (entry.Value <= 0) continue;

            if (line.Length > 0) line.Append("  ");
            line.Append($"{entry.Key} Lv{entry.Value}");
        }

        return line.Length > 0 ? line.ToString() : "ninguna";
    }

    public static string BuildSynergiesLine()
    {
        SynergyManager manager = SynergyManager.Instance;
        SynergyDatabase database = SynergyDatabase.Instance;

        if (manager == null || database == null || database.allSynergies == null)
            return "sin datos";

        StringBuilder line = new StringBuilder(128);

        for (int i = 0; i < database.allSynergies.Count; i++)
        {
            SynergyData synergy = database.allSynergies[i];
            if (synergy == null) continue;

            if (line.Length > 0) line.Append("  ");
            line.Append(manager.IsSynergyActive(synergy) ? $"✔{synergy.synergyName}" : $"✗{synergy.synergyName}");
        }

        return line.Length > 0 ? line.ToString() : "ninguna configurada";
    }

    private static void AppendMatch(StringBuilder builder)
    {
        GameTimeManager time = GameTimeManager.Instance;
        GameSessionStats stats = GameSessionStats.Instance;

        string clock = time != null ? $"{time.GetFormattedCountdown()} restante (transcurrido {time.GetFormattedTime()})" : "sin GameTimeManager";
        string session = stats != null
            ? $"bajas={stats.EnemiesKilled} edificios={stats.BuildingsDestroyed} monedas={stats.CoinsCollected} diamantes={stats.DiamondsCollected}"
            : "sin GameSessionStats";

        builder.AppendLine($"Partida:  {clock}");
        builder.AppendLine($"Sesión:   {session}");
    }

    private static void AppendPlayer(StringBuilder builder, PlayerHealth health, PlayerExperience experience)
    {
        if (health == null)
        {
            builder.AppendLine("Jugador:  no encontrado");
            return;
        }

        string level = experience != null
            ? $"nivel {experience.GetCurrentLevel()} ({experience.GetCurrentExperience()}/{experience.GetExperienceRequiredForNextLevel()} XP)"
            : "sin PlayerExperience";

        builder.AppendLine($"Jugador:  {health.CurrentHealth:F0}/{health.MaxHealth:F0} HP | {level}{(health.IsInvulnerable ? " | INVULNERABLE" : "")}");

        if (CurrencyManager.Instance != null)
            builder.AppendLine($"Monedero: {CurrencyManager.Instance.CurrentCoins} monedas, {CurrencyManager.Instance.CurrentDiamonds} diamantes");
    }

    private static void AppendStats(StringBuilder builder)
    {
        PlayerStatsManager stats = PlayerStatsManager.Instance;
        if (stats == null) return;

        float cooldown = stats.GetModifiedAttackCooldown();

        builder.AppendLine($"Combate:  daño={stats.GetModifiedDamage():F1} | cadencia={cooldown:F3}s ({(cooldown > 0f ? 1f / cooldown : 0f):F2}/s) | alcance={stats.GetModifiedAttackRange():F1} | imán={stats.GetModifiedMagnetRange():F1}");
        builder.AppendLine($"Especial: multishot={stats.GetMultiShotProbability():F0}% (+{stats.GetMultiShotExtraBullets()}) | explosivo={stats.GetExplosiveShotProbability():F0}% (r{stats.GetExplosionRadius():F1}) | knockback={stats.GetKnockbackProbability():F0}% (f{stats.GetKnockbackForce():F1}, cadena {stats.GetKnockbackChainJumps()})");
    }

    private static void AppendSpawning(StringBuilder builder)
    {
        EnemySpawnManager manager = EnemySpawnManager.Instance;

        string wave = manager != null ? $"wave {manager.CurrentWaveNumber}" : "sin EnemySpawnManager";
        string blocked = manager != null && manager.IsSpawnBlocked ? " | SPAWNS BLOQUEADOS" : "";

        builder.AppendLine($"Spawns:   {wave} | enemigos activos {EnemyHealth.ActiveEnemyCount}{blocked}");
    }

    private static void AppendPools(StringBuilder builder)
    {
        PoolManager pool = PoolManager.Instance;
        if (pool == null) return;

        PoolManager.PoolType[] types =
        {
            PoolManager.PoolType.BasicEnemy, PoolManager.PoolType.FastEnemy,
            PoolManager.PoolType.Projectile, PoolManager.PoolType.ExperienceOrb,
            PoolManager.PoolType.Coin, PoolManager.PoolType.Diamond
        };

        StringBuilder line = new StringBuilder(160);

        for (int i = 0; i < types.Length; i++)
        {
            if (!pool.TryGetPoolStats(types[i], out int total, out int available)) continue;

            if (line.Length > 0) line.Append("  ");
            line.Append($"{types[i]}={total - available}/{total}");
        }

        builder.AppendLine($"Pools:    {(line.Length > 0 ? line.ToString() : "sin datos")}  (en uso/total)");
    }
}
