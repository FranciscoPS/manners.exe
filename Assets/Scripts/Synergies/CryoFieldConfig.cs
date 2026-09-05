using UnityEngine;

[CreateAssetMenu(fileName = "CryoFieldConfig", menuName = "Game/Synergies/Cryo Field Config")]
public class CryoFieldConfig : SynergyEffectConfig
{
    [Header("Área")]
    [Tooltip("Radio del área alrededor del jugador.")]
    public float radius = 4f;
    [Tooltip("Multiplicador de velocidad aplicado a los enemigos dentro del área (0.5 = -50% velocidad).")]
    [Range(0f, 1f)] public float slowMultiplier = 0.5f;
    [Tooltip("Daño aplicado a cada enemigo dentro del área en cada tick.")]
    public float damagePerTick = 2f;
    [Tooltip("Cada cuántos segundos se aplica daño/ralentización.")]
    public float tickInterval = 1f;

    [Header("Visual")]
    [Tooltip("Si se asigna, se instancia este prefab (VFX o modelo) como hijo del jugador en vez de la esfera generada por código, centrado en el mismo punto (el origen del jugador). Si el prefab trae CryoFieldVisual (Assets/Prefabs/VFX/CriogenicArea.prefab), la escarcha, la cúpula y la nevada se escalan solas a 'Radius' y la escarcha se abre con una animación al activarse; sin ese componente, tú controlas su escala y no se reescala.")]
    public GameObject visualPrefabOverride;
    [Tooltip("Color de la esfera de prueba generada por código. Está centrada exactamente en el origen del jugador (sin offset vertical). Solo se usa si 'Visual Prefab Override' está vacío.")]
    public Color visualColor = new Color(0.4f, 0.85f, 1f, 0.35f);

    [Header("Audio")]
    [Tooltip("Sonido que se reproduce una vez, al activarse la sinergia. Opcional.")]
    public AudioClip activationSFX;
    [Range(0f, 1f)] public float sfxVolume = 0.7f;

    public override void ApplyTo(GameObject effectInstance)
    {
        CryoFieldEffect effect = effectInstance.GetComponent<CryoFieldEffect>();
        effect?.Configure(this);
    }
}
