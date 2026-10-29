using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Override", menuName = "Game/Override")]
public class OverrideData : ScriptableObject
{
    [SerializeField, HideInInspector] private string persistentId;
    public string PersistentId => string.IsNullOrEmpty(persistentId) ? GameAssetPaths.BaseName(name) : persistentId;
    [Header("Identidad")]
    [FormerlySerializedAs("synergyName")] [FormerlySerializedAs("overrideName")] [SerializeField] private string overrideNameSpanish = "Override Name";
    [SerializeField] [TextArea(1, 6)] private string overrideNameEnglish = "";
    public string overrideName { get => GameLocalization.Language == GameLanguage.Spanish || string.IsNullOrEmpty(overrideNameEnglish) ? overrideNameSpanish : overrideNameEnglish; set => overrideNameSpanish = value; }
    [TextArea(2, 4)] [FormerlySerializedAs("description")] [SerializeField] private string descriptionSpanish = "";
    [SerializeField] [TextArea(1, 6)] private string descriptionEnglish = "";
    public string description { get => GameLocalization.Language == GameLanguage.Spanish || string.IsNullOrEmpty(descriptionEnglish) ? descriptionSpanish : descriptionEnglish; set => descriptionSpanish = value; }
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
