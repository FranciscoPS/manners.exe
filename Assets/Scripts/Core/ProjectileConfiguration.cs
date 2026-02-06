using UnityEngine;

[CreateAssetMenu(fileName = "ProjectileConfig", menuName = "Game/Projectile Configuration")]
public class ProjectileConfiguration : ScriptableObject
{
    [Header("Stats")]
    public float speed = 15f;
    public float damage = 10f;
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
        projectile.SetStats(speed, damage, lifetime);
        projectile.SetVisuals(mesh, material, color, scale);
        projectile.SetEffects(trailEffect, hitEffect, hasLight, lightColor, lightIntensity);
    }
}
