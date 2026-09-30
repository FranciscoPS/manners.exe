using UnityEngine;

[CreateAssetMenu(fileName = "InvisibleWallWarningConfig", menuName = "Game/Invisible Wall Warning Config")]
public class InvisibleWallWarningConfig : ScriptableObject
{
    [Header("=== ACTIVACIÓN ===")]
    [Tooltip("Distancia en metros entre el borde del jugador y la pared a la que el aviso empieza a aparecer (quieto o moviéndose en paralelo a la pared).")]
    [Min(0.1f)] public float activationDistance = 6f;
    [Tooltip("Distancia en metros a la que el aviso llega a su intensidad máxima. 0 = solo al tocar la pared.")]
    [Min(0f)] public float fullIntensityDistance = 1f;
    [Tooltip("Anticipación en segundos: la distancia de activación crece con la velocidad a la que el jugador se acerca a la pared (velocidad × este valor). Así, al huir rápido hacia un callejón el aviso aparece antes. 0 = sin anticipación.")]
    [Min(0f)] public float lookAheadSeconds = 0.6f;
    [Tooltip("Máximo de metros que la anticipación puede sumar a la distancia de activación.")]
    [Min(0f)] public float maxLookAheadDistance = 8f;
    [Tooltip("Si el jugador se acerca en diagonal, la zona se adelanta a lo largo de la pared hacia el punto donde va a chocar (hasta estos segundos de movimiento). Al tocar la pared queda justo frente al jugador. 0 = siempre frente al jugador.")]
    [Min(0f)] public float impactLeadSeconds = 0.5f;

    [Header("=== TRANSICIONES ===")]
    [Tooltip("Velocidad de aparición (intensidad por segundo). Más alto = aparece de golpe.")]
    [Min(0.01f)] public float fadeInSpeed = 5f;
    [Tooltip("Velocidad de desaparición (intensidad por segundo) al alejarse de la pared.")]
    [Min(0.01f)] public float fadeOutSpeed = 2.5f;
    [Tooltip("Qué tan pegada sigue la zona al jugador a lo largo de la pared. Más alto = más pegada; 0 = sin suavizado.")]
    [Min(0f)] public float followSharpness = 14f;
    [Tooltip("Intensidad (0-1) a partir de la cual aparecen las franjas de WARNING. Por debajo solo se ven los hexágonos.")]
    [Range(0f, 0.95f)] public float stripesAppearAt = 0.35f;

    [Header("=== ZONA ALREDEDOR DEL JUGADOR ===")]
    [Tooltip("Metros que la zona sobresale a cada lado del ancho del jugador (medido con su collider).")]
    [Min(0f)] public float sidePadding = 1.2f;
    [Tooltip("Metros que la zona sobresale por encima de la cabeza del jugador.")]
    [Min(0f)] public float topPadding = 1f;
    [Tooltip("Metros que la zona baja por debajo de los pies del jugador, para que nazca del piso.")]
    [Min(0f)] public float bottomExtension = 0.1f;
    [Tooltip("Separación en metros entre la zona y la cara de la pared, para que no parpadee contra ella.")]
    [Min(0f)] public float surfaceOffset = 0.05f;

    [Header("=== VISIBILIDAD ===")]
    [Tooltip("Opacidad del aviso cuando la pared queda entre la cámara y el jugador (paredes del lado de la cámara), para no tapar al jugador. 1 = igual que en las demás paredes.")]
    [Range(0f, 1f)] public float opacityBetweenCameraAndPlayer = 0.55f;
    [Tooltip("Ocultar los avisos cuando el jugador muere.")]
    public bool hideWhenPlayerDies = true;

    private static InvisibleWallWarningConfig instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    public static void OverrideInstance(InvisibleWallWarningConfig config)
    {
        if (config == null) return;
        instance = config;
    }

    public static InvisibleWallWarningConfig Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<InvisibleWallWarningConfig>("InvisibleWallWarningConfig");

                if (instance == null)
                {
                    instance = CreateInstance<InvisibleWallWarningConfig>();
                    Debug.LogWarning("[InvisibleWallWarningConfig] No se encontró 'Resources/InvisibleWallWarningConfig'. Usando valores por defecto embebidos. Ejecuta 'Tools > Manners > Muros invisibles > 1. Crear assets del aviso' para poder editarlo desde el Inspector.");
                }
            }

            return instance;
        }
    }
}
