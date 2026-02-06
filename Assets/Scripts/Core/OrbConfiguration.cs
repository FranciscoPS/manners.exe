using UnityEngine;

[CreateAssetMenu(fileName = "OrbConfig", menuName = "Game/Orb Configuration")]
public class OrbConfiguration : ScriptableObject
{
    [Header("Experience Settings")]
    public int experienceValue = 10;
    
    [Header("Visual Settings")]
    public Color orbColor = Color.cyan;
    public float orbScale = 1f;
    
    [Header("Movement Settings")]
    public float attractionRange = 5f;
    public float moveSpeed = 8f;
    
    public void ApplyToOrb(ExperienceOrb orb)
    {
        orb.SetExperienceValue(experienceValue);
        orb.SetOrbColor(orbColor);
        orb.SetAttractionRange(attractionRange);
        orb.SetMoveSpeed(moveSpeed);
        orb.transform.localScale = Vector3.one * orbScale;
    }
}
