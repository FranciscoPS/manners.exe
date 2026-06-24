using UnityEngine;

public enum ChestItemEffect
{
    GiantMagnet,
    FullHeal,
    KillAllEnemies
}

[CreateAssetMenu(fileName = "ChestItem", menuName = "Game/Chest Item")]
public class ChestItemData : ScriptableObject
{
    [Header("Display")]
    public string itemName = "\u00cdtem";
    [TextArea] public string description = "";
    public Sprite icon;
    public Color accentColor = new Color(1f, 0.84f, 0f);

    [Header("Effect")]
    public ChestItemEffect effect = ChestItemEffect.GiantMagnet;
}
