using UnityEngine;

[CreateAssetMenu(fileName = "CollectibleConfig", menuName = "Game/Collectible Configuration")]
public class CollectibleConfiguration : ScriptableObject
{
    [Header("Type")]
    public Collectible.CollectibleType type = Collectible.CollectibleType.Coin;
    
    [Header("Value")]
    public int value = 1;
    
    [Header("Visual Settings")]
    public Mesh mesh;
    public Material material;
    public Color color = Color.yellow;
    public float scale = 0.5f;
    
    [Header("Movement Settings")]
    public float attractionRange = 5f;
    public float moveSpeed = 8f;
    
    public void ApplyToCollectible(Collectible collectible)
    {
        collectible.SetType(type);
        collectible.SetValue(value);
        collectible.SetVisuals(mesh, material, color, scale);
        collectible.SetAttractionRange(attractionRange);
        collectible.SetMoveSpeed(moveSpeed);
    }
}
