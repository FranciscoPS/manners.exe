using UnityEngine;

[CreateAssetMenu(fileName = "Synergy", menuName = "Game/Synergy")]
public class SynergyData : ScriptableObject
{
    [Header("Identidad")]
    public string synergyName = "Synergy Name";
    [TextArea(2, 4)] public string description = "";
    public Sprite icon;

    [Header("Requisitos (dos mejoras al nivel indicado)")]
    public UpgradeType requiredUpgradeA = UpgradeType.MoveSpeed;
    [Range(1, 20)] public int requiredLevelA = 5;

    public UpgradeType requiredUpgradeB = UpgradeType.MagnetRange;
    [Range(1, 20)] public int requiredLevelB = 5;

    [Header("Efecto")]
    [Tooltip("Prefab con el componente que implementa ISynergyEffect. Sus propios campos (radio, daño, intervalo...) controlan el efecto.")]
    public GameObject effectPrefab;
}
