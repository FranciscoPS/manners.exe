using UnityEngine;

public enum UpgradeType
{
    Damage,
    AttackSpeed,
    MagnetRange,
    MoveSpeed,
    MultiShot,
    ExplosiveShot,
    Knockback
}

[CreateAssetMenu(fileName = "Upgrade", menuName = "Game/Upgrade Data")]
public class UpgradeData : ScriptableObject
{
    [Header("Identity")]
    public string upgradeName = "Upgrade Name";
    [TextArea(2, 4)]
    public string description = "Upgrade description";
    public UpgradeType upgradeType;
    public Sprite icon;
    
    [Header("Progression")]
    [Tooltip("Valor base del primer nivel (ej: 15 para +15% daño)")]
    public float baseValue = 15f;
    
    [Tooltip("Multiplicador aplicado por nivel (ej: 1.2 significa +20% por nivel)")]
    public float multiplierPerLevel = 1.2f;
    
    [Tooltip("Máximo nivel que puede alcanzar este upgrade (999 = infinito)")]
    [Range(1, 999)]
    public int maxLevel = 999;
    
    [Header("Display Settings")]
    [Tooltip("Si es true, muestra como porcentaje (15 → 15%). Si es false, muestra como valor absoluto (20 → 20 HP)")]
    public bool isPercentage = true;
    
    [Tooltip("Sufijo para mostrar en UI (ej: 'HP', 'Range')")]
    public string valueSuffix = "";
    
    [Header("Rarity & Weight")]
    [Tooltip("Si es true, este upgrade solo aparece en niveles milestone (5, 10, 15...)")]
    public bool isPremium = false;
    
    [Tooltip("Peso de aparición (mayor = más probable de aparecer)")]
    [Range(1, 100)]
    public int spawnWeight = 50;
    
    public float CalculateValueAtLevel(int level)
    {
        if (level <= 0) return 0f;
        if (level > maxLevel) level = maxLevel;
        
        if (upgradeType == UpgradeType.AttackSpeed)
        {
            return baseValue + (multiplierPerLevel * (level - 1));
        }
        
        return baseValue * Mathf.Pow(multiplierPerLevel, level - 1);
    }
    
    public string GetFormattedValue(int level)
    {
        float value = CalculateValueAtLevel(level);
        
        if (isPercentage)
        {
            return $"+{value:F1}%";
        }
        else
        {
            return $"+{value:F0}{valueSuffix}";
        }
    }
    
    public string GetUpgradeComparison(int currentLevel, int nextLevel)
    {
        string current = GetFormattedValue(currentLevel);
        string next = GetFormattedValue(nextLevel);
        return $"{current} → {next}";
    }
}
