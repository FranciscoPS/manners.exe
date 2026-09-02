using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(fileName = "ChestOpeningConfig", menuName = "Game/Chest Opening Config")]
public class ChestOpeningConfig : ScriptableObject
{
    [Header("=== TIEMPOS (segundos) ===")]
    [Tooltip("Anticipación: el cofre tiembla, el fondo se oscurece y los rayos aparecen lentamente.")]
    public float anticipationDuration = 1.6f;
    [Tooltip("Duración del destello blanco y la sacudida fuerte de cámara en el momento del estallido.")]
    public float burstDuration = 0.35f;
    [Tooltip("Duración de reserva para la tapa abriéndose si el Animator del cofre no reporta una duración válida.")]
    public float lidOpenFallbackDuration = 1.2f;
    [Tooltip("Cuánto se mantienen los rayos a máxima velocidad después de que la tapa termina de abrirse, antes de mostrar la carta del objeto.")]
    public float revealHoldDuration = 1.85f;

    [Header("=== CÁMARA ===")]
    [Tooltip("Fuerza de las sacudidas leves y repetidas durante la anticipación.")]
    public float anticipationShakeForce = 0.15f;
    [Tooltip("Cada cuántos segundos se repite la sacudida leve durante la anticipación.")]
    public float anticipationShakeInterval = 0.35f;
    [Tooltip("Fuerza de la sacudida fuerte en el momento del estallido.")]
    public float burstShakeForce = 2.2f;

    [Header("=== FONDO Y DESTELLO ===")]
    public Color dimColor = new Color(0f, 0f, 0f, 0.82f);
    public Color flashColor = new Color(1f, 0.98f, 0.85f, 1f);

    [Header("=== TEXTO ===")]
    public string promptMessage = "¡COFRE!";

    [Header("=== SFX (opcional) ===")]
    [Tooltip("Sonido que arranca junto con la anticipación (temblor + oscurecido).")]
    public AudioClip buildupSFX;
    [Tooltip("Sonido del estallido de luz.")]
    public AudioClip burstSFX;
    [Range(0f, 1f)] public float sfxVolume = 0.85f;

    [Header("=== CONTROLES ===")]
    [Tooltip("Si está activo, el jugador puede saltarse la cinemática presionando la tecla de salto.")]
    public bool allowSkip = true;
    public Key skipKey = Key.Space;

    public float TotalDuration => anticipationDuration + burstDuration + lidOpenFallbackDuration + revealHoldDuration;

    private static ChestOpeningConfig instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    public static void OverrideInstance(ChestOpeningConfig config)
    {
        if (config == null) return;
        instance = config;
    }

    public static ChestOpeningConfig Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<ChestOpeningConfig>("ChestOpeningConfig");

                if (instance == null)
                {
                    instance = CreateInstance<ChestOpeningConfig>();
                    Debug.LogWarning("[ChestOpeningConfig] No se encontró 'Resources/ChestOpeningConfig'. Usando valores por defecto embebidos. Ejecuta 'Tools > Manners > VFX > Crear configuración de apertura de cofre' para poder editarlo desde el Inspector.");
                }
            }

            return instance;
        }
    }
}
