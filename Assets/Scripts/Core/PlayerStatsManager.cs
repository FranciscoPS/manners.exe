using UnityEngine;
using System.Collections.Generic;

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
    
    // Diccionario que guarda el nivel actual de cada tipo de upgrade
    private Dictionary<UpgradeType, int> upgradeLevels = new Dictionary<UpgradeType, int>();
    
    // Evento que se dispara cuando se aplica un upgrade
    public event System.Action<UpgradeType, int> OnUpgradeApplied;
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            transform.SetParent(null); // Convertir en root antes de DDOL
            DontDestroyOnLoad(gameObject);
            InitializeUpgrades();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    private void InitializeUpgrades()
    {
        // Inicializar todos los tipos de upgrade en nivel 0
        foreach (UpgradeType type in System.Enum.GetValues(typeof(UpgradeType)))
        {
            upgradeLevels[type] = 0;
        }
    }
    
    /// <summary>
    /// Obtiene el nivel actual de un upgrade específico
    /// </summary>
    public int GetUpgradeLevel(UpgradeType type)
    {
        return upgradeLevels.ContainsKey(type) ? upgradeLevels[type] : 0;
    }
    
    /// <summary>
    /// Aplica un upgrade (incrementa su nivel)
    /// </summary>
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
        
        // Incrementar nivel
        upgradeLevels[upgrade.upgradeType] = currentLevel + 1;
        int newLevel = upgradeLevels[upgrade.upgradeType];
        
        // Notificar a otros sistemas
        OnUpgradeApplied?.Invoke(upgrade.upgradeType, newLevel);
        
        // Aplicar el upgrade inmediatamente
        ApplyUpgradeToPlayer(upgrade, newLevel);
    }
    
    /// <summary>
    /// Aplica los efectos del upgrade al jugador
    /// </summary>
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
            case UpgradeType.MagnetRange:
                ApplyMagnetRangeUpgrade(value);
                break;
            case UpgradeType.MoveSpeed:
                ApplyMoveSpeedUpgrade(value);
                break;
            case UpgradeType.MultiShot:
                // Multishot se calcula dinámicamente
                break;
            case UpgradeType.ExplosiveShot:
                // ExplosiveShot es booleano
                break;
            case UpgradeType.Knockback:
                // Knockback se calcula dinámicamente
                break;
        }
    }
    
    // ========== APLICACIÓN DE UPGRADES ==========
    
    private void ApplyDamageUpgrade(float percentageIncrease)
    {
        // El daño se calculará dinámicamente desde GetModifiedDamage()
    }
    
    private void ApplyAttackSpeedUpgrade(float percentageDecrease)
    {
        // El cooldown se calculará dinámicamente desde GetModifiedAttackCooldown()
    }
    
    private void ApplyMagnetRangeUpgrade(float percentageIncrease)
    {
        // El rango se calculará dinámicamente desde GetModifiedMagnetRange()
    }
    
    private void ApplyMoveSpeedUpgrade(float percentageIncrease)
    {
        PlayerController playerController = FindFirstObjectByType<PlayerController>();
        if (playerController != null)
        {
            playerController.ApplySpeedModifier(percentageIncrease);
        }
    }
    
    // ========== GETTERS DE STATS MODIFICADOS ==========
    
    /// <summary>
    /// Obtiene el daño base del jugador con todos los modificadores aplicados
    /// </summary>
    public float GetModifiedDamage()
    {
        float baseDamage = GameBalanceConfig.Instance != null 
            ? GameBalanceConfig.Instance.PlayerBaseDamage 
            : 10f;
        
        int damageLevel = GetUpgradeLevel(UpgradeType.Damage);
        if (damageLevel > 0 && UpgradeDatabase.Instance != null)
        {
            // Buscar el upgrade de daño en la database
            UpgradeData damageUpgrade = UpgradeDatabase.Instance.allUpgrades.Find(u => u.upgradeType == UpgradeType.Damage);
            if (damageUpgrade != null)
            {
                float percentageBonus = damageUpgrade.CalculateValueAtLevel(damageLevel);
                baseDamage *= (1f + percentageBonus / 100f);
            }
        }
        
        return baseDamage;
    }
    
    /// <summary>
    /// Obtiene el cooldown de ataque con todos los modificadores aplicados
    /// </summary>
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
                float percentageReduction = attackSpeedUpgrade.CalculateValueAtLevel(attackSpeedLevel);
                baseCooldown *= (1f - percentageReduction / 100f);
            }
        }
        
        return baseCooldown;
    }
    
    /// <summary>
    /// Obtiene el rango de atracción de orbes con todos los modificadores aplicados
    /// </summary>
    public float GetModifiedMagnetRange()
    {
        float baseRange = GameBalanceConfig.Instance != null 
            ? GameBalanceConfig.Instance.OrbAttractionRange 
            : 5f;
        
        int magnetLevel = GetUpgradeLevel(UpgradeType.MagnetRange);
        if (magnetLevel > 0 && UpgradeDatabase.Instance != null)
        {
            UpgradeData magnetUpgrade = UpgradeDatabase.Instance.allUpgrades.Find(u => u.upgradeType == UpgradeType.MagnetRange);
            if (magnetUpgrade != null)
            {
                float percentageBonus = magnetUpgrade.CalculateValueAtLevel(magnetLevel);
                baseRange *= (1f + percentageBonus / 100f);
            }
        }
        
        return baseRange;
    }
    
    /// <summary>
    /// Obtiene el número de proyectiles a disparar (multishot)
    /// </summary>
    public int GetProjectileCount()
    {
        int multiShotLevel = GetUpgradeLevel(UpgradeType.MultiShot);
        if (multiShotLevel <= 0) return 1;
        
        if (UpgradeDatabase.Instance != null)
        {
            UpgradeData multiShotUpgrade = UpgradeDatabase.Instance.allUpgrades.Find(u => u.upgradeType == UpgradeType.MultiShot);
            if (multiShotUpgrade != null)
            {
                // Cada nivel agrega 1 proyectil (nivel 1 = 2 proyectiles, nivel 2 = 3, etc.)
                return (int)multiShotUpgrade.CalculateValueAtLevel(multiShotLevel) + 1;
            }
        }
        
        return 1;
    }
    
    /// <summary>
    /// Verifica si los proyectiles son explosivos
    /// </summary>
    public bool IsExplosiveShot()
    {
        return GetUpgradeLevel(UpgradeType.ExplosiveShot) > 0;
    }
    
    /// <summary>
    /// Obtiene la fuerza de knockback
    /// </summary>
    public float GetKnockbackForce()
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
    
    /// <summary>
    /// Obtiene una copia del diccionario de niveles de upgrade
    /// </summary>
    public Dictionary<UpgradeType, int> GetAllUpgradeLevels()
    {
        return new Dictionary<UpgradeType, int>(upgradeLevels);
    }
    
    /// <summary>
    /// Obtiene el valor base del juego (sin upgrades) para un tipo específico
    /// </summary>
    public float GetBaseGameValue(UpgradeType upgradeType)
    {
        GameBalanceConfig config = GameBalanceConfig.Instance;
        if (config == null) return 0f;
        
        switch (upgradeType)
        {
            case UpgradeType.Damage:
                return config.PlayerBaseDamage;
            
            case UpgradeType.AttackSpeed:
                // Para attack speed mostramos el cooldown base
                return config.PlayerAttackCooldown;
            
            case UpgradeType.MagnetRange:
                // Valor base del rango de atracción (desde config)
                return config.OrbAttractionRange;
            
            case UpgradeType.MoveSpeed:
                return config.PlayerMoveSpeed;
            
            case UpgradeType.MultiShot:
                return 1f; // 1 proyectil base
            
            case UpgradeType.ExplosiveShot:
                return 0f; // No explosivo por defecto
            
            case UpgradeType.Knockback:
                return 0f; // Sin knockback por defecto
            
            default:
                return 0f;
        }
    }
    
    /// <summary>
    /// Resetea todos los upgrades (útil para reiniciar partida)
    /// </summary>
    public void ResetUpgrades()
    {
        InitializeUpgrades();
    }
}
