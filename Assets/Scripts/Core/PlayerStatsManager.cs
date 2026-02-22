using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class PlayerStatsManager : MonoBehaviour
{
    private static PlayerStatsManager instance;
    public static PlayerStatsManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<PlayerStatsManager>();
                if (instance == null)
                {
                    GameObject obj = new GameObject("PlayerStatsManager");
                    instance = obj.AddComponent<PlayerStatsManager>();
                }
            }
            return instance;
        }
    }
    
    private Dictionary<UpgradeType, int> upgradeLevels = new Dictionary<UpgradeType, int>();
    
    public event System.Action<UpgradeType, int> OnUpgradeApplied;
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            InitializeUpgrades();
            
#if !UNITY_EDITOR
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
#else
            gameObject.hideFlags = HideFlags.DontSave;
#endif
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
#if !UNITY_EDITOR
            SceneManager.sceneLoaded -= OnSceneLoaded;
#endif
            instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InitializeUpgrades();
    }
    
    private void InitializeUpgrades()
    {
        foreach (UpgradeType type in System.Enum.GetValues(typeof(UpgradeType)))
        {
            upgradeLevels[type] = 0;
        }
    }
    
    public int GetUpgradeLevel(UpgradeType type)
    {
        return upgradeLevels.ContainsKey(type) ? upgradeLevels[type] : 0;
    }
    
    public void ApplyUpgrade(UpgradeData upgrade)
    {
        if (upgrade == null)
        {
            Debug.LogError("Trying to apply null upgrade!");
            return;
        }
        
        int currentLevel = GetUpgradeLevel(upgrade.upgradeType);
        
        if (currentLevel >= upgrade.maxLevel)
        {
            Debug.LogWarning($"Upgrade {upgrade.upgradeName} is already at max level ({upgrade.maxLevel})");
            return;
        }
        
        upgradeLevels[upgrade.upgradeType] = currentLevel + 1;
        int newLevel = upgradeLevels[upgrade.upgradeType];
        
        OnUpgradeApplied?.Invoke(upgrade.upgradeType, newLevel);
        
        ApplyUpgradeToPlayer(upgrade, newLevel);
    }
    
    private void ApplyUpgradeToPlayer(UpgradeData upgrade, int level)
    {
        float value = upgrade.CalculateValueAtLevel(level);
        
        switch (upgrade.upgradeType)
        {
            case UpgradeType.Damage:
                ApplyDamageUpgrade(value);
                break;
            case UpgradeType.AttackSpeed:
                ApplyAttackSpeedUpgrade(value);
                break;
            case UpgradeType.AttackRange:
                ApplyAttackRangeUpgrade(value);
                break;
            case UpgradeType.MoveSpeed:
                ApplyMoveSpeedUpgrade(value);
                break;
            case UpgradeType.MagnetRange:
                break;
            case UpgradeType.MultiShot:
                break;
            case UpgradeType.ExplosiveShot:
                break;
            case UpgradeType.Knockback:
                break;
        }
    }
    
    private void ApplyDamageUpgrade(float percentageIncrease)
    {
    }
    
    private void ApplyAttackSpeedUpgrade(float percentageDecrease)
    {
    }
    
    private void ApplyAttackRangeUpgrade(float percentageIncrease)
    {
    }
    
    private void ApplyMoveSpeedUpgrade(float percentageIncrease)
    {
        PlayerController playerController = FindFirstObjectByType<PlayerController>();
        if (playerController != null)
        {
            playerController.ApplySpeedModifier(percentageIncrease);
        }
    }
    
    public float GetModifiedDamage()
    {
        float baseDamage = GameBalanceConfig.Instance != null 
            ? GameBalanceConfig.Instance.PlayerBaseDamage 
            : 10f;
        
        int damageLevel = GetUpgradeLevel(UpgradeType.Damage);
        if (damageLevel > 0 && UpgradeDatabase.Instance != null)
        {
            UpgradeData damageUpgrade = UpgradeDatabase.Instance.allUpgrades.Find(u => u.upgradeType == UpgradeType.Damage);
            if (damageUpgrade != null)
            {
                float percentageBonus = damageUpgrade.CalculateValueAtLevel(damageLevel);
                baseDamage *= (1f + percentageBonus / 100f);
            }
        }
        
        return baseDamage;
    }
    
    public float GetModifiedAttackCooldown()
    {
        float baseCooldown = GameBalanceConfig.Instance != null 
            ? GameBalanceConfig.Instance.PlayerAttackCooldown 
            : 0.5f;
        
        int attackSpeedLevel = GetUpgradeLevel(UpgradeType.AttackSpeed);
        if (attackSpeedLevel > 0 && UpgradeDatabase.Instance != null)
        {
            UpgradeData attackSpeedUpgrade = UpgradeDatabase.Instance.allUpgrades.Find(u => u.upgradeType == UpgradeType.AttackSpeed);
            if (attackSpeedUpgrade != null)
            {
                float percentage = attackSpeedUpgrade.CalculateValueAtLevel(attackSpeedLevel);
                float baseFireRate = 1f / baseCooldown;
                float newFireRate = baseFireRate * (1f + percentage / 100f);
                baseCooldown = 1f / newFireRate;
            }
        }
        
        return baseCooldown;
    }
    
    public float GetModifiedAttackRange()
    {
        float baseRange = GameBalanceConfig.Instance != null 
            ? GameBalanceConfig.Instance.PlayerAttackRange 
            : 10f;
        
        int attackRangeLevel = GetUpgradeLevel(UpgradeType.AttackRange);
        if (attackRangeLevel > 0 && UpgradeDatabase.Instance != null)
        {
            UpgradeData attackRangeUpgrade = UpgradeDatabase.Instance.allUpgrades.Find(u => u.upgradeType == UpgradeType.AttackRange);
            if (attackRangeUpgrade != null)
            {
                float percentageBonus = attackRangeUpgrade.CalculateValueAtLevel(attackRangeLevel);
                baseRange *= (1f + percentageBonus / 100f);
            }
        }
        
        return baseRange;
    }
    
    public float GetModifiedMagnetRange()
    {
        float baseRange = GameBalanceConfig.Instance != null 
            ? GameBalanceConfig.Instance.OrbAttractionRange 
            : 5f;
        
        int magnetRangeLevel = GetUpgradeLevel(UpgradeType.MagnetRange);
        if (magnetRangeLevel > 0 && UpgradeDatabase.Instance != null)
        {
            UpgradeData magnetRangeUpgrade = UpgradeDatabase.Instance.allUpgrades.Find(u => u.upgradeType == UpgradeType.MagnetRange);
            if (magnetRangeUpgrade != null)
            {
                float percentageBonus = magnetRangeUpgrade.CalculateValueAtLevel(magnetRangeLevel);
                baseRange *= (1f + percentageBonus / 100f);
            }
        }
        
        return baseRange;
    }
    
    public int GetProjectileCount()
    {
        return 1;
    }
    
    public float GetMultiShotProbability()
    {
        int multiShotLevel = GetUpgradeLevel(UpgradeType.MultiShot);
        if (multiShotLevel <= 0) return 0f;
        
        if (UpgradeDatabase.Instance != null)
        {
            UpgradeData multiShotUpgrade = UpgradeDatabase.Instance.allUpgrades.Find(u => u.upgradeType == UpgradeType.MultiShot);
            if (multiShotUpgrade != null)
            {
                return multiShotUpgrade.CalculateValueAtLevel(multiShotLevel);
            }
        }
        
        return 0f;
    }
    
    public int GetMultiShotExtraBullets()
    {
        int multiShotLevel = GetUpgradeLevel(UpgradeType.MultiShot);
        if (multiShotLevel <= 0) return 0;
        
        return multiShotLevel * 3;
    }
    
    public float GetExplosiveShotProbability()
    {
        int explosiveLevel = GetUpgradeLevel(UpgradeType.ExplosiveShot);
        if (explosiveLevel <= 0) return 0f;
        
        if (UpgradeDatabase.Instance != null)
        {
            UpgradeData explosiveUpgrade = UpgradeDatabase.Instance.allUpgrades.Find(u => u.upgradeType == UpgradeType.ExplosiveShot);
            if (explosiveUpgrade != null)
            {
                return explosiveUpgrade.CalculateValueAtLevel(explosiveLevel);
            }
        }
        
        return 0f;
    }
    
    public float GetExplosionRadius()
    {
        int explosiveLevel = GetUpgradeLevel(UpgradeType.ExplosiveShot);
        if (explosiveLevel <= 0) return 0f;
        
        return 3f;
    }
    
    public float GetKnockbackProbability()
    {
        int knockbackLevel = GetUpgradeLevel(UpgradeType.Knockback);
        if (knockbackLevel <= 0) return 0f;
        
        if (UpgradeDatabase.Instance != null)
        {
            UpgradeData knockbackUpgrade = UpgradeDatabase.Instance.allUpgrades.Find(u => u.upgradeType == UpgradeType.Knockback);
            if (knockbackUpgrade != null)
            {
                return knockbackUpgrade.CalculateValueAtLevel(knockbackLevel);
            }
        }
        
        return 0f;
    }
    
    public float GetKnockbackForce()
    {
        int knockbackLevel = GetUpgradeLevel(UpgradeType.Knockback);
        if (knockbackLevel <= 0) return 0f;
        
        return 5f + (knockbackLevel - 1) * 0.5f;
    }
    
    public Dictionary<UpgradeType, int> GetAllUpgradeLevels()
    {
        return new Dictionary<UpgradeType, int>(upgradeLevels);
    }
    
    public float GetBaseGameValue(UpgradeType upgradeType)
    {
        GameBalanceConfig config = GameBalanceConfig.Instance;
        if (config == null) return 0f;
        
        switch (upgradeType)
        {
            case UpgradeType.Damage:
                return config.PlayerBaseDamage;
            
            case UpgradeType.AttackSpeed:
                return config.PlayerAttackCooldown;
            
            case UpgradeType.AttackRange:
                return config.PlayerAttackRange;
            
            case UpgradeType.MoveSpeed:
                return config.PlayerMoveSpeed;
            
            case UpgradeType.MagnetRange:
                return config.OrbAttractionRange;
            
            case UpgradeType.MultiShot:
                return 1f;
            
            case UpgradeType.ExplosiveShot:
                return 0f;
            
            case UpgradeType.Knockback:
                return 0f;
            
            default:
                return 0f;
        }
    }
    
    public void ResetUpgrades()
    {
        InitializeUpgrades();
    }
}
