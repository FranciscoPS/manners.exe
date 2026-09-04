using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(fileName = "ChestOpeningConfig", menuName = "Game/Chest Opening Config")]
public class ChestOpeningConfig : ScriptableObject
{
    [Header("=== TIEMPOS (segundos) ===")]
    [Tooltip("Con cofre 3D la cinemática sigue el clip de animación del cofre (ChestANIM) a velocidad normal. Este valor indica cuántos segundos antes de que termine ese clip se corta: el cofre, el oscurecido y los rayos desaparecen con el fundido corto de abajo y justo después aparece la carta del objeto.")]
    public float revealLeadTime = 1f;
    [Tooltip("Duración del fundido final (cofre, oscurecido, rayos y texto) justo antes de que aparezca la carta. 0 = desaparece de golpe.")]
    public float fadeOutDuration = 0.4f;
    [Tooltip("Duración del destello blanco y la sacudida fuerte de cámara en el momento del estallido.")]
    public float burstDuration = 0.35f;
    [Tooltip("Solo sin cofre 3D (prefab ausente o showcase desactivado): anticipación en la que el fondo se oscurece y los rayos aparecen lentamente.")]
    public float anticipationDuration = 1.6f;
    [Tooltip("Solo sin cofre 3D: duración de reserva de la apertura.")]
    public float lidOpenFallbackDuration = 1.2f;
    [Tooltip("Solo sin cofre 3D: cuánto se mantienen los rayos a máxima velocidad antes de mostrar la carta del objeto.")]
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
    [Tooltip("Texto pequeño bajo el mensaje principal que indica cómo saltar la cinemática. {0} se sustituye por el nombre de la tecla.")]
    public string skipHintMessage = "Pulsa {0} para saltar";

    [Header("=== COFRE 3D (showcase) ===")]
    [Tooltip("Muestra el modelo 3D del cofre en el centro de la cinemática. Se renderiza con una cámara propia a una textura temporal solo mientras dura la apertura.")]
    public bool showcaseEnabled = true;
    [Tooltip("Prefab del cofre 3D. Si se deja vacío se carga 'ChestForAnimation' desde Resources.")]
    public GameObject showcasePrefab;
    [Tooltip("Animator Controller con la animación de apertura (ChestANIM). Solo se usa si el prefab no trae uno asignado.")]
    public RuntimeAnimatorController showcaseController;
    [Tooltip("Segundo del clip (a velocidad real) en que el cofre se aplasta y salta abriéndose: ahí se disparan el destello, la sacudida fuerte y las partículas. Hasta ese instante corre la anticipación (oscurecido, rayos y temblores) y después el clip sigue hasta su final.")]
    public float showcaseBurstClipTime = 2.45f;
    [Tooltip("Ángulo de visión de la cámara del cofre. Más bajo = menos perspectiva.")]
    public float showcaseFieldOfView = 35f;
    [Tooltip("Inclinación de la cámara en grados (positivo = mira hacia abajo).")]
    public float showcaseCameraPitch = 12f;
    [Tooltip("Giro de la cámara alrededor del cofre en grados. 180 mira al frente del cofre (bisagra atrás); usa 0 si lo ves de espaldas.")]
    public float showcaseCameraYaw = 180f;
    [Tooltip("Margen del encuadre: 1 = el cofre en reposo llena la vista; más alto = más aire (hace falta porque el cofre salta y abre la tapa).")]
    public float showcaseFramePadding = 1.4f;
    [Tooltip("Hacia dónde apunta la cámara verticalmente, como fracción de la altura del cofre en reposo. 0 = centro del cofre; 0.6 = más arriba, para dejar sitio al salto.")]
    public float showcaseFocusHeight = 0.6f;
    [Tooltip("Tamaño (en unidades del canvas de referencia 1920x1080) del recuadro donde se dibuja el cofre 3D.")]
    public Vector2 showcaseViewSize = new Vector2(900f, 900f);
    [Tooltip("Desplazamiento del recuadro respecto al centro de la pantalla.")]
    public Vector2 showcaseViewOffset = new Vector2(0f, 40f);
    [Range(0.25f, 2f)]
    [Tooltip("Resolución de la textura del cofre respecto a su tamaño en pantalla. 1 = misma resolución; menos = más barato; más = bordes más suaves.")]
    public float showcaseRenderScale = 1f;

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
