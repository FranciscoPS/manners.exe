using UnityEngine;

[CreateAssetMenu(fileName = "ProjectileConfig", menuName = "Game/Projectile Configuration")]
public class ProjectileConfiguration : ScriptableObject
{
    [Header("Stats")]
    public float speed = 15f;
    [Tooltip("Multiplier for base damage. Leave at 1.0 for normal damage")]
    public float damageMultiplier = 1f;
    public float lifetime = 5f;
    
    [Header("Visual")]
    public Mesh mesh;
    public Material material;
    public Color color = Color.white;
    public Vector3 scale = Vector3.one;
    
    [Header("Effects")]
    public GameObject trailEffect;
    public GameObject hitEffect;
    public bool hasLight = false;
    public Color lightColor = Color.white;
    public float lightIntensity = 2f;
    
    public void ApplyToProjectile(Projectile projectile)
    {
        // Usar el daño modificado del PlayerStatsManager (incluye upgrades)
        float finalDamage = 10f; // Fall back
        
        if (PlayerStatsManager.Instance != null)
        {
            finalDamage = PlayerStatsManager.Instance.GetModifiedDamage() * damageMultiplier;
        }
        else if (GameBalanceConfig.Instance != null)
        {
            finalDamage = GameBalanceConfig.Instance.PlayerBaseDamage * damageMultiplier;
        }
        else
        {
            Debug.LogError("[ProjectileConfig] GameBalanceConfig.Instance is NULL! Using fallback damage: 10");
        }
        
        projectile.SetStats(speed, finalDamage, lifetime);
        projectile.SetVisuals(mesh, material, color, scale);
        projectile.SetEffects(trailEffect, hitEffect, hasLight, lightColor, lightIntensity);
    }
}
