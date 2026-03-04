using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "ShopUpgradeDatabase", menuName = "Game/Shop Upgrade Database")]
public class ShopUpgradeDatabase : ScriptableObject
{
    private static ShopUpgradeDatabase instance;
    public static ShopUpgradeDatabase Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<ShopUpgradeDatabase>("ShopUpgradeDatabase");

                if (instance == null)
                {
                }
            }
            return instance;
        }
    }

    [Header("Available Shop Upgrades")]
    [SerializeField] private List<UpgradeData> shopUpgrades = new List<UpgradeData>();

    [Header("Shop Settings")]
    [SerializeField] private int upgradesPerRefresh = 3;
    [SerializeField] private float shopGlobalCooldown = 120f;

    public List<UpgradeData> GetAvailableUpgrades(Dictionary<UpgradeType, int> currentLevels)
    {
        List<UpgradeData> availableUpgrades = new List<UpgradeData>();

        foreach (var upgrade in shopUpgrades)
        {
            if (upgrade == null)
                continue;

            if (!upgrade.isAvailableInShop)
                continue;

            UpgradeType type = upgrade.upgradeType;
            int currentLevel = currentLevels.ContainsKey(type) ? currentLevels[type] : 0;

            if (currentLevel >= upgrade.maxLevel)
                continue;

            availableUpgrades.Add(upgrade);
        }

        return availableUpgrades;
    }

    public List<UpgradeData> GetRandomShopUpgrades(Dictionary<UpgradeType, int> currentLevels)
    {
        List<UpgradeData> availableUpgrades = GetAvailableUpgrades(currentLevels);

        if (availableUpgrades.Count == 0)
        {
            return new List<UpgradeData>();
        }

        List<UpgradeData> selectedUpgrades = new List<UpgradeData>();
        List<UpgradeData> tempList = new List<UpgradeData>(availableUpgrades);

        int count = Mathf.Min(upgradesPerRefresh, tempList.Count);

        for (int i = 0; i < count; i++)
        {
            if (tempList.Count == 0) break;

            float totalWeight = 0f;
            foreach (var upgrade in tempList)
            {
                totalWeight += upgrade.shopSpawnWeight;
            }

            float randomValue = Random.Range(0f, totalWeight);
            float currentSum = 0f;

            UpgradeData selected = null;
            foreach (var upgrade in tempList)
            {
                currentSum += upgrade.shopSpawnWeight;
                if (randomValue <= currentSum)
                {
                    selected = upgrade;
                    break;
                }
            }

            if (selected != null)
            {
                selectedUpgrades.Add(selected);
                tempList.Remove(selected);
            }
        }

        return selectedUpgrades;
    }

    public UpgradeData GetShopUpgradeByType(UpgradeType type)
    {
        return shopUpgrades.FirstOrDefault(u => u != null && u.upgradeType == type);
    }

    public float ShopGlobalCooldown => shopGlobalCooldown;
}
