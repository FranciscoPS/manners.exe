using UnityEngine;

public static class OverrideDiscovery
{
    private const string UpgradeKeyPrefix = "OverrideDiscovery_Upgrade_";
    private const string OverrideKeyPrefix = "OverrideDiscovery_Override_";
    private const string OverrideAssetPrefix = "Override_";
    private const string LegacyUpgradeKeyPrefix = "SynergyDiscovery_Upgrade_";
    private const string LegacyOverrideKeyPrefix = "SynergyDiscovery_Synergy_Synergy_";

    private static int newPiecesThisRun;
    private static int newOverridesThisRun;
    private static bool legacyProgressMigrated;

    public static int NewPiecesThisRun => newPiecesThisRun;
    public static int NewOverridesThisRun => newOverridesThisRun;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        legacyProgressMigrated = false;
        BeginRun();
    }

    public static void BeginRun()
    {
        newPiecesThisRun = 0;
        newOverridesThisRun = 0;
    }

    public static int GetMaxUpgradeLevel(UpgradeType type)
    {
        MigrateLegacyProgress();
        return PlayerPrefs.GetInt(UpgradeKeyPrefix + type, 0);
    }

    public static bool IsOverrideUnlocked(OverrideData overrideData)
    {
        MigrateLegacyProgress();
        return overrideData != null && PlayerPrefs.GetInt(OverrideKeyPrefix + overrideData.PersistentId, 0) == 1;
    }

    public static void RecordUpgradeLevel(UpgradeType type, int level)
    {
        int previous = GetMaxUpgradeLevel(type);
        if (level <= previous) return;

        if (previous == 0 && IsOverrideRequirement(type))
            newPiecesThisRun++;

        PlayerPrefs.SetInt(UpgradeKeyPrefix + type, level);
        PlayerPrefs.Save();
    }

    public static void RecordOverrideUnlocked(OverrideData overrideData)
    {
        if (overrideData == null || IsOverrideUnlocked(overrideData)) return;

        newOverridesThisRun++;

        PlayerPrefs.SetInt(OverrideKeyPrefix + overrideData.PersistentId, 1);
        PlayerPrefs.Save();
    }

    public static void Forget(OverrideData overrideData)
    {
        if (overrideData == null) return;

        MigrateLegacyProgress();
        PlayerPrefs.DeleteKey(OverrideKeyPrefix + overrideData.PersistentId);
        PlayerPrefs.Save();
    }

    public static void Clear()
    {
        MigrateLegacyProgress();
        foreach (UpgradeType type in System.Enum.GetValues(typeof(UpgradeType)))
            PlayerPrefs.DeleteKey(UpgradeKeyPrefix + type);

        OverrideDatabase database = OverrideDatabase.Instance;
        if (database != null && database.allOverrides != null)
        {
            for (int i = 0; i < database.allOverrides.Count; i++)
            {
                if (database.allOverrides[i] != null)
                    PlayerPrefs.DeleteKey(OverrideKeyPrefix + database.allOverrides[i].PersistentId);
            }
        }

        PlayerPrefs.Save();
        BeginRun();
    }

    private static void MigrateLegacyProgress()
    {
        if (legacyProgressMigrated) return;
        legacyProgressMigrated = true;
        bool migrated = false;

        foreach (UpgradeType type in System.Enum.GetValues(typeof(UpgradeType)))
        {
            string legacyKey = LegacyUpgradeKeyPrefix + type;
            if (!PlayerPrefs.HasKey(legacyKey)) continue;

            string key = UpgradeKeyPrefix + type;
            PlayerPrefs.SetInt(key, Mathf.Max(PlayerPrefs.GetInt(key, 0), PlayerPrefs.GetInt(legacyKey, 0)));
            PlayerPrefs.DeleteKey(legacyKey);
            migrated = true;
        }

        OverrideDatabase database = OverrideDatabase.Instance;
        if (database != null && database.allOverrides != null)
        {
            for (int i = 0; i < database.allOverrides.Count; i++)
            {
                OverrideData overrideData = database.allOverrides[i];
                if (overrideData == null || !overrideData.PersistentId.StartsWith(OverrideAssetPrefix)) continue;

                string legacyKey = LegacyOverrideKeyPrefix + overrideData.PersistentId.Substring(OverrideAssetPrefix.Length);
                if (!PlayerPrefs.HasKey(legacyKey)) continue;

                if (PlayerPrefs.GetInt(legacyKey, 0) == 1)
                    PlayerPrefs.SetInt(OverrideKeyPrefix + overrideData.PersistentId, 1);
                PlayerPrefs.DeleteKey(legacyKey);
                migrated = true;
            }
        }

        if (migrated)
            PlayerPrefs.Save();
    }

    private static bool IsOverrideRequirement(UpgradeType type)
    {
        OverrideDatabase database = OverrideDatabase.Instance;
        if (database == null || database.allOverrides == null) return false;

        for (int i = 0; i < database.allOverrides.Count; i++)
        {
            OverrideData overrideData = database.allOverrides[i];
            if (overrideData == null) continue;

            if (overrideData.requiredUpgradeA == type || overrideData.requiredUpgradeB == type)
                return true;
        }

        return false;
    }
}
