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
    
    [Header("Effects")]
    public bool hasLight = true;
    public float lightIntensity = 5f;
    public float lightRange = 10f;
    
    public void ApplyToOrb(ExperienceOrb orb)
    {
        orb.SetExperienceValue(experienceValue);
        orb.SetVisuals(mesh, material, orbColor, orbScale);
        orb.SetAttractionRange(attractionRange);
        orb.SetMoveSpeed(moveSpeed);
        orb.SetLight(hasLight, lightIntensity, lightRange, orbColor);
    }
}
