using UnityEngine;

public enum UpgradeType
{
    Damage,           // Aumenta daño base del jugador
    AttackSpeed,      // Reduce cooldown de ataque
    MagnetRange,      // Aumenta rango de atracción de orbes
    MoveSpeed,        // Aumenta velocidad de movimiento
    MultiShot,        // Dispara múltiples proyectiles
    ExplosiveShot,    // Proyectiles explotan al impactar
    Knockback         // Empuja enemigos al golpearlos
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
    
    [Tooltip("Si es true, el upgrade REDUCE el valor (ej: -8% cooldown). Si es false, AUMENTA (+15% daño)")]
    public bool isReduction = false;
    
    [Tooltip("Sufijo para mostrar en UI (ej: 'HP', 'Range')")]
    public string valueSuffix = "";
    
    [Header("Rarity & Weight")]
    [Tooltip("Peso de aparición (mayor = más probable de aparecer)")]
    [Range(1, 100)]
    public int spawnWeight = 50;
    
    /// <summary>
    /// Calcula el valor del upgrade en un nivel específico
    /// </summary>
    public float CalculateValueAtLevel(int level)
    {
        if (level <= 0) return 0f;
        if (level > maxLevel) level = maxLevel;
        
        // Fórmula: baseValue * (multiplier ^ (level - 1))
        return baseValue * Mathf.Pow(multiplierPerLevel, level - 1);
    }
    
    /// <summary>
    /// Obtiene el texto formateado para mostrar el valor en UI
    /// </summary>
    public string GetFormattedValue(int level)
    {
        float value = CalculateValueAtLevel(level);
        
        if (isPercentage)
        {
            // Para AttackSpeed, invertir el signo para mostrar como aumento de velocidad
            bool invertSign = (upgradeType == UpgradeType.AttackSpeed);
            string sign;
            
            if (invertSign)
            {
                // AttackSpeed: mostrar como positivo aunque internamente sea reducción
                sign = "+";
            }
            else
            {
                sign = isReduction ? "-" : "+";
            }
            
            return $"{sign}{value:F1}%";
        }
        else
        {
            string sign = isReduction ? "-" : "+";
            return $"{sign}{value:F0}{valueSuffix}";
        }
    }
    
    /// <summary>
    /// Obtiene texto con comparación entre niveles
    /// </summary>
    public string GetUpgradeComparison(int currentLevel, int nextLevel)
    {
        string current = GetFormattedValue(currentLevel);
        string next = GetFormattedValue(nextLevel);
        return $"{current} → {next}";
    }
}
