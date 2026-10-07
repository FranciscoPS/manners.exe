using UnityEngine.Serialization;
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
    [FormerlySerializedAs("itemName")] [SerializeField] private string itemNameSpanish = "\u00cdtem";
    [SerializeField] [TextArea(1, 6)] private string itemNameEnglish = "";
    public string itemName { get => GameLocalization.Language == GameLanguage.Spanish || string.IsNullOrEmpty(itemNameEnglish) ? itemNameSpanish : itemNameEnglish; set => itemNameSpanish = value; }
    [TextArea] [FormerlySerializedAs("description")] [SerializeField] private string descriptionSpanish = "";
    [SerializeField] [TextArea(1, 6)] private string descriptionEnglish = "";
    public string description { get => GameLocalization.Language == GameLanguage.Spanish || string.IsNullOrEmpty(descriptionEnglish) ? descriptionSpanish : descriptionEnglish; set => descriptionSpanish = value; }
    public Sprite icon;
    public Color accentColor = new Color(1f, 0.84f, 0f);

    [Header("Effect")]
    public ChestItemEffect effect = ChestItemEffect.GiantMagnet;
}
