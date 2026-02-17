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
                if (instance == null)
                {
                    Debug.LogError("UpgradeDatabase not found in Resources folder!");
                }
            }
            return instance;
        }
    }
    
    public List<UpgradeData> GetRandomUpgrades(Dictionary<UpgradeType, int> currentUpgradeLevels, int playerLevel)
    {
        List<UpgradeData> selectedUpgrades = new List<UpgradeData>();
        
        HashSet<UpgradeType> premiumUpgrades = new HashSet<UpgradeType>
        {
            UpgradeType.MultiShot,
            UpgradeType.ExplosiveShot,
            UpgradeType.Knockback
        };
        
        bool isMilestoneLevel = (playerLevel % 5 == 0);
        
        List<UpgradeData> availableUpgrades = allUpgrades.Where(upgrade =>
        {
            int currentLevel = currentUpgradeLevels.ContainsKey(upgrade.upgradeType) 
                ? currentUpgradeLevels[upgrade.upgradeType] 
                : 0;
            
            if (currentLevel >= upgrade.maxLevel)
                return false;
            
            if (isMilestoneLevel)
            {
                bool isPremium = premiumUpgrades.Contains(upgrade.upgradeType);
                if (!isPremium)
                {
                    Debug.Log($"[Level {playerLevel}] Skipping non-premium: {upgrade.upgradeName}");
                }
                return isPremium;
            }
            
            bool isNotPremium = !premiumUpgrades.Contains(upgrade.upgradeType);
            return isNotPremium;
            
        }).ToList();
        
        if (availableUpgrades.Count == 0)
        {
            Debug.LogWarning("No upgrades available! All upgrades are at max level.");
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
