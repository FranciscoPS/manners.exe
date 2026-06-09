using UnityEngine;

/// <summary>
/// Tipos de efecto \u00fanico que puede otorgar un \u00edtem de cofre.
/// Para a\u00f1adir un nuevo \u00edtem: agrega un valor aqu\u00ed y su caso en ChestItemProvider.ApplyEffect.
/// </summary>
public enum ChestItemEffect
{
    GiantMagnet,
    FullHeal,
    KillAllEnemies
}

/// <summary>
/// \u00cdtem de efecto \u00fanico que aparece dentro de un Cofre. No es una mejora de stats:
/// ejecuta un efecto instant\u00e1neo al elegirse.
/// Crea assets desde: Assets > Create > Game > Chest Item.
/// </summary>
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
