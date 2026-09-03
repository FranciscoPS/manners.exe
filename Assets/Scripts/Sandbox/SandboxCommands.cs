using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SandboxCommands
{
    private static int timeScaleIndex;
    private static GameObject activeSynergyHintsInstance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        timeScaleIndex = 0;
        activeSynergyHintsInstance = null;
    }

    public static void SpawnBurst(int count, float radius, EnemyConfiguration enemy)
    {
        if (enemy == null)
        {
            SandboxLog.Warn("Ráfaga manual: no hay ningún EnemyConfiguration asignado (revisa 'Burst Enemy' en SandboxHotkeys).");
            return;
        }

        if (SpawnFactory.Instance == null)
        {
            SandboxLog.Warn("Ráfaga manual: SpawnFactory no disponible.");
            return;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        Transform origin = playerObject != null ? playerObject.transform : null;

        if (origin == null)
        {
            SandboxLog.Warn("Ráfaga manual: no se encontró al jugador.");
            return;
        }

        int spawned = 0;

        for (int i = 0; i < count; i++)
        {
            float angle = (i / (float)count) * Mathf.PI * 2f;
            Vector3 position = origin.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;

            if (SpawnFactory.Instance.CreateEnemy(position, enemy) != null)
                spawned++;
        }

        SandboxLog.Command($"Ráfaga manual: {spawned}/{count} '{enemy.name}' a {radius}m del jugador. Activos ahora: {EnemyHealth.ActiveEnemyCount}");
    }

    public static void KillAllEnemies()
    {
        List<EnemyHealth> enemies = new List<EnemyHealth>(EnemyHealth.ActiveEnemies);
        int killed = 0;

        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i] == null) continue;
            enemies[i].TakeDamage(float.MaxValue);
            killed++;
        }

        SandboxLog.Command($"Aniquilación: {killed} enemigos eliminados.");
    }

    public static void GrantLevels(int levels)
    {
        PlayerExperience experience = Object.FindFirstObjectByType<PlayerExperience>();

        if (experience == null)
        {
            SandboxLog.Warn("Subir de nivel: no se encontró PlayerExperience.");
            return;
        }

        int startLevel = experience.GetCurrentLevel();

        for (int i = 0; i < levels; i++)
        {
            int missing = experience.GetExperienceRequiredForNextLevel() - experience.GetCurrentExperience();
            experience.AddExperience(Mathf.Max(1, missing));
        }

        SandboxLog.Command($"Nivel: {startLevel} → {experience.GetCurrentLevel()}");
    }

    public static UpgradeData GrantRandomUpgrade(bool premium)
    {
        UpgradeDatabase database = UpgradeDatabase.Instance;

        if (database == null || database.allUpgrades == null || database.allUpgrades.Count == 0)
        {
            SandboxLog.Warn("Mejora aleatoria: la UpgradeDatabase está vacía o no se pudo cargar.");
            return null;
        }

        if (PlayerStatsManager.Instance == null)
        {
            SandboxLog.Warn("Mejora aleatoria: PlayerStatsManager no disponible.");
            return null;
        }

        List<UpgradeData> pool = new List<UpgradeData>();
        int totalWeight = 0;

        for (int i = 0; i < database.allUpgrades.Count; i++)
        {
            UpgradeData upgrade = database.allUpgrades[i];
            if (upgrade == null || upgrade.isPremium != premium) continue;
            if (PlayerStatsManager.Instance.GetUpgradeLevel(upgrade.upgradeType) >= upgrade.maxLevel) continue;

            pool.Add(upgrade);
            totalWeight += Mathf.Max(1, upgrade.spawnWeight);
        }

        if (pool.Count == 0)
        {
            SandboxLog.Warn($"Mejora aleatoria: no quedan mejoras {(premium ? "premium" : "normales")} disponibles.");
            return null;
        }

        int roll = Random.Range(0, totalWeight);
        UpgradeData selected = pool[pool.Count - 1];

        for (int i = 0; i < pool.Count; i++)
        {
            roll -= Mathf.Max(1, pool[i].spawnWeight);
            if (roll < 0)
            {
                selected = pool[i];
                break;
            }
        }

        GrantUpgrade(selected, 1);
        return selected;
    }

    public static void GrantUpgrade(UpgradeData upgrade, int levels)
    {
        if (upgrade == null || PlayerStatsManager.Instance == null) return;

        for (int i = 0; i < levels; i++)
            PlayerStatsManager.Instance.ApplyUpgrade(upgrade);
    }

    public static void AddCurrency(int coins, int diamonds)
    {
        if (CurrencyManager.Instance == null)
        {
            SandboxLog.Warn("Monedas: CurrencyManager no disponible.");
            return;
        }

        if (coins != 0) CurrencyManager.Instance.AddCoins(coins);
        if (diamonds != 0) CurrencyManager.Instance.AddDiamonds(diamonds);

        SandboxLog.Command($"Monedero: +{coins} monedas, +{diamonds} diamantes → {CurrencyManager.Instance.CurrentCoins} / {CurrencyManager.Instance.CurrentDiamonds}");
    }

    public static void ToggleInvulnerable()
    {
        PlayerHealth health = Object.FindFirstObjectByType<PlayerHealth>();

        if (health == null)
        {
            SandboxLog.Warn("Invulnerabilidad: no se encontró PlayerHealth.");
            return;
        }

        bool value = !health.IsInvulnerable;
        health.SetInvulnerable(value);

        SandboxLog.Command($"Invulnerabilidad: {(value ? "ACTIVADA" : "desactivada")}");
    }

    public static void ToggleSpawning()
    {
        EnemySpawnManager manager = EnemySpawnManager.Instance;

        if (manager == null)
        {
            SandboxLog.Warn("Spawning: EnemySpawnManager no disponible.");
            return;
        }

        bool blocked = !manager.IsSpawnBlocked;
        manager.SetSpawnBlocked(blocked);

        SandboxLog.Command($"Spawning: {(blocked ? "BLOQUEADO" : "reanudado")}");
    }

    public static void ForceFinalRush()
    {
        GameEvents.TriggerMatchTimeExpired();
        SandboxLog.Command("Oleada final forzada (evento OnMatchTimeExpired disparado).");
    }

    public static void SpawnChestNow()
    {
        ChestSpawner spawner = ChestSpawner.Instance;

        if (spawner == null)
        {
            SandboxLog.Warn("Cofre: ChestSpawner no disponible.");
            return;
        }

        spawner.RequestSpawnNow();
        SandboxLog.Command("Cofre: spawn solicitado para el siguiente frame.");
    }

    public static void CycleTimeScale(float[] steps)
    {
        if (steps == null || steps.Length == 0) steps = new[] { 1f, 0.25f, 2f, 4f };

        timeScaleIndex = (timeScaleIndex + 1) % steps.Length;
        Time.timeScale = Mathf.Max(0f, steps[timeScaleIndex]);

        SandboxLog.Command($"Time scale: x{Time.timeScale}");
    }

    public static void ToggleSynergyHints(GameObject synergyHintsPanelPrefab)
    {
        if (activeSynergyHintsInstance != null)
        {
            Object.Destroy(activeSynergyHintsInstance);
            activeSynergyHintsInstance = null;

            SandboxLog.Command("Panel de sinergias: oculto.");
            return;
        }

        if (synergyHintsPanelPrefab == null)
        {
            SandboxLog.Warn("Panel de sinergias: no hay prefab asignado (revisa 'Synergy Hints Panel Prefab' en SandboxHotkeys).");
            return;
        }

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            SandboxLog.Warn("Panel de sinergias: no se encontró ningún Canvas en la escena.");
            return;
        }

        activeSynergyHintsInstance = Object.Instantiate(synergyHintsPanelPrefab, canvas.transform);
        activeSynergyHintsInstance.SetActive(true);

        SandboxLog.Command("Panel de sinergias: visible con el progreso actual (vuelve a pulsar para ocultarlo).");
    }

    public static void ClearSynergyDiscoveries()
    {
        SynergyDiscovery.Clear();

        if (activeSynergyHintsInstance != null)
        {
            foreach (SynergyHintRowUI row in activeSynergyHintsInstance.GetComponentsInChildren<SynergyHintRowUI>(true))
                row.Refresh();
        }

        SandboxLog.Command("Progreso guardado de sinergias borrado (mejoras y sinergias descubiertas en 0).");
    }

    public static void ReloadSandbox()
    {
        Scene active = SceneManager.GetActiveScene();
        Time.timeScale = 1f;

        SandboxLog.Command($"Reiniciando sandbox ('{active.name}')...");
        SceneManager.LoadScene(active.name);
    }
}
