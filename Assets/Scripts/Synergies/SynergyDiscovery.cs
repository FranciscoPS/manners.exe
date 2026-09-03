using UnityEngine;

public static class SynergyDiscovery
{
    private const string UpgradeKeyPrefix = "SynergyDiscovery_Upgrade_";
    private const string SynergyKeyPrefix = "SynergyDiscovery_Synergy_";

    public static int GetMaxUpgradeLevel(UpgradeType type)
    {
        return PlayerPrefs.GetInt(UpgradeKeyPrefix + type, 0);
    }

    public static bool IsSynergyUnlocked(SynergyData synergy)
    {
        return synergy != null && PlayerPrefs.GetInt(SynergyKeyPrefix + synergy.name, 0) == 1;
    }

    public static void RecordUpgradeLevel(UpgradeType type, int level)
    {
        if (level <= GetMaxUpgradeLevel(type)) return;

        PlayerPrefs.SetInt(UpgradeKeyPrefix + type, level);
        PlayerPrefs.Save();
    }

    public static void RecordSynergyUnlocked(SynergyData synergy)
    {
        if (synergy == null || IsSynergyUnlocked(synergy)) return;

        PlayerPrefs.SetInt(SynergyKeyPrefix + synergy.name, 1);
        PlayerPrefs.Save();
    }

    public static void Forget(SynergyData synergy)
    {
        if (synergy == null) return;

        PlayerPrefs.DeleteKey(SynergyKeyPrefix + synergy.name);
        PlayerPrefs.Save();
    }

    public static void Clear()
    {
        foreach (UpgradeType type in System.Enum.GetValues(typeof(UpgradeType)))
            PlayerPrefs.DeleteKey(UpgradeKeyPrefix + type);

        SynergyDatabase database = SynergyDatabase.Instance;
        if (database != null && database.allSynergies != null)
        {
            for (int i = 0; i < database.allSynergies.Count; i++)
            {
                if (database.allSynergies[i] != null)
                    PlayerPrefs.DeleteKey(SynergyKeyPrefix + database.allSynergies[i].name);
            }
        }

        PlayerPrefs.Save();
    }
}
