using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Override", menuName = "Game/Override")]
public class OverrideData : ScriptableObject
{
    [Header("Identidad")]
    [FormerlySerializedAs("synergyName")] public string overrideName = "Override Name";
    [TextArea(2, 4)] public string description = "";
    public Sprite icon;

    [Header("Requisitos (dos mejoras al nivel indicado)")]
    public UpgradeType requiredUpgradeA = UpgradeType.MoveSpeed;
    [Range(1, 20)] public int requiredLevelA = 5;

    public UpgradeType requiredUpgradeB = UpgradeType.MagnetRange;
    [Range(1, 20)] public int requiredLevelB = 5;

    [Header("Efecto")]
    [Tooltip("Prefab con el componente que implementa IOverrideEffect (solo comportamiento, sin números).")]
    public GameObject effectPrefab;

    [Tooltip("Config con los números y el visual de este efecto (radio, daño, intervalo, prefab de VFX...). Debe ser del tipo que espera effectPrefab (ej. CryoFieldConfig para CryoFieldEffect).")]
    public OverrideEffectConfig effectConfig;
}
