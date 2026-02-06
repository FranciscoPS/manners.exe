using UnityEngine;

[CreateAssetMenu(fileName = "OrbConfig", menuName = "Game/Orb Configuration")]
public class OrbConfiguration : ScriptableObject
{
    [Header("Experience Settings")]
    public int experienceValue = 10;
    
    [Header("Visual Settings")]
    public Mesh mesh;
    public Material material;
    public Color orbColor = Color.cyan;
    public float orbScale = 1f;
    
    [Header("Movement Settings")]
    public float attractionRange = 5f;
    public float moveSpeed = 8f;
    
    [Header("Emission Settings")]
    [Range(0f, 10f)] public float emissionIntensity = 3f;
    [Range(0.1f, 5f)] public float fresnelPower = 2f;
    
    public void ApplyToOrb(ExperienceOrb orb)
    {
        orb.SetExperienceValue(experienceValue);
        orb.SetVisuals(mesh, material, orbColor, orbScale);
        orb.SetAttractionRange(attractionRange);
        orb.SetMoveSpeed(moveSpeed);
        orb.SetEmission(emissionIntensity, fresnelPower);
    }
}
