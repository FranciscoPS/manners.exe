using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "UpgradeDatabase", menuName = "Game/Upgrade Database")]
public class UpgradeDatabase : ScriptableObject
{
    [Header("Available Upgrades")]
    [Tooltip("Lista de todos los upgrades disponibles en el juego")]
    public List<UpgradeData> allUpgrades = new List<UpgradeData>();

    [Header("Selection Settings")]
    [Tooltip("Número de opciones a mostrar por level up")]
    [Range(1, 5)]
    public int optionsPerLevelUp = 3;

    [Tooltip("Si es true, no pueden aparecer upgrades duplicados en la misma selección")]
    public bool preventDuplicates = true;

    [Header("Level Requirements")]
    [Tooltip("Nivel mínimo del jugador para comenzar a ver upgrades")]
    public int minPlayerLevel = 1;

    private static UpgradeDatabase instance;
    public static UpgradeDatabase Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<UpgradeDatabase>("UpgradeDatabase");
            }
            return instance;
        }
    }

    public List<UpgradeData> GetRandomUpgrades(Dictionary<UpgradeType, int> currentUpgradeLevels, int playerLevel)
    {
        List<UpgradeData> selectedUpgrades = new List<UpgradeData>();

        bool isMilestoneLevel = (playerLevel % 5 == 0);

        List<UpgradeData> availableUpgrades = new List<UpgradeData>(allUpgrades.Count);

        for (int i = 0; i < allUpgrades.Count; i++)
        {
            UpgradeData upgrade = allUpgrades[i];

            int currentLevel = currentUpgradeLevels.ContainsKey(upgrade.upgradeType)
                ? currentUpgradeLevels[upgrade.upgradeType]
                : 0;

            if (currentLevel >= upgrade.maxLevel)
                continue;

            if (isMilestoneLevel)
            {
                if (!upgrade.isPremium)
                {
                    continue;
                }
            }

            else if (upgrade.isPremium)
            {
                continue;
            }

            availableUpgrades.Add(upgrade);
        }

        if (availableUpgrades.Count == 0)
        {
            return selectedUpgrades;
        }

        List<UpgradeData> weightedList = new List<UpgradeData>();
        foreach (var upgrade in availableUpgrades)
        {
            for (int i = 0; i < upgrade.spawnWeight; i++)
            {
                weightedList.Add(upgrade);
            }
        }

        int selectCount = Mathf.Min(optionsPerLevelUp, availableUpgrades.Count);
        HashSet<UpgradeType> selectedTypes = new HashSet<UpgradeType>();

        for (int i = 0; i < selectCount; i++)
        {
            if (weightedList.Count == 0) break;

            UpgradeData selected = null;
            int attempts = 0;
            int maxAttempts = 100;

            do
            {
                int randomIndex = Random.Range(0, weightedList.Count);
                selected = weightedList[randomIndex];
                attempts++;

                if (!preventDuplicates || !selectedTypes.Contains(selected.upgradeType))
                {
                    break;
                }

            } while (attempts < maxAttempts);

            if (selected != null && (!preventDuplicates || !selectedTypes.Contains(selected.upgradeType)))
            {
                selectedUpgrades.Add(selected);
                selectedTypes.Add(selected.upgradeType);

                if (preventDuplicates)
                {
                    weightedList.RemoveAll(u => u.upgradeType == selected.upgradeType);
                }
            }
        }

        return selectedUpgrades;
    }
}
