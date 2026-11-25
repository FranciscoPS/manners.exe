using UnityEngine;

[CreateAssetMenu(fileName = "ScreenTransitionConfig", menuName = "Game/UI/Screen Transition Config")]
public class ScreenTransitionConfig : ScriptableObject
{
    [Header("=== APAGADO (al salir de la escena) ===")]
    [Tooltip("Segundos en que la imagen se aplasta hacia el centro hasta quedar en una línea horizontal brillante.")]
    [Min(0.01f)] public float collapseDuration = 0.2f;
    [Tooltip("Avance del aplastamiento en el tiempo: 0 = pantalla completa, 1 = línea.")]
    public AnimationCurve collapseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Tooltip("Segundos en que la línea se cierra desde los lados hasta quedar en un punto.")]
    [Min(0.01f)] public float shrinkDuration = 0.16f;
    [Tooltip("Avance del cierre horizontal en el tiempo: 0 = línea completa, 1 = punto.")]
    public AnimationCurve shrinkCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Tooltip("Segundos que el punto se queda encendido en el centro antes de apagarse.")]
    [Min(0f)] public float dotHoldDuration = 0.08f;
    [Tooltip("Segundos en que el punto se desvanece hasta dejar la pantalla apagada.")]
    [Min(0.01f)] public float dotFadeDuration = 0.25f;
    [Tooltip("Segundos de pantalla apagada antes de cargar la escena siguiente o cerrar el juego.")]
    [Min(0f)] public float offHoldDuration = 0.1f;

    [Header("=== ENCENDIDO (al entrar a la escena nueva) ===")]
    [Tooltip("Segundos de pantalla apagada después de cargar, para que la escena nueva arranque antes de verse.")]
    [Min(0f)] public float powerOnDelay = 0.15f;
    [Tooltip("Segundos en que aparece el punto en el centro.")]
    [Min(0.01f)] public float dotInDuration = 0.08f;
    [Tooltip("Segundos en que el punto se estira hacia los lados hasta formar la línea.")]
    [Min(0.01f)] public float expandDuration = 0.14f;
    [Tooltip("Avance del estiramiento horizontal en el tiempo: 0 = punto, 1 = línea completa.")]
    public AnimationCurve expandCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Tooltip("Segundos en que la línea se abre hacia arriba y hacia abajo hasta mostrar la escena completa.")]
    [Min(0.01f)] public float openDuration = 0.3f;
    [Tooltip("Avance de la apertura vertical en el tiempo: 0 = línea, 1 = pantalla completa.")]
    public AnimationCurve openCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("=== ASPECTO ===")]
    [Tooltip("Color de la pantalla apagada. Por defecto, la tinta de la paleta (el mismo azul noche del fundido anterior).")]
    public Color offColor = new Color32(0x06, 0x0A, 0x2B, 0xFF);
    [Tooltip("Color de la línea y del punto. Por defecto, el papel de la paleta.")]
    public Color lineColor = new Color32(0xF8, 0xFA, 0xFD, 0xFF);
    [Tooltip("Color del halo que rodea la imagen al aplastarse, la línea y el punto. Por defecto, el cian de la paleta.")]
    public Color glowColor = new Color32(0x19, 0xA8, 0xE6, 0xFF);
    [Tooltip("Multiplicador global del brillo de la línea, el punto y el halo. El canvas no pasa por el bloom: por encima de 1 solo satura hacia blanco.")]
    [Range(0f, 3f)] public float intensity = 1f;
    [Tooltip("Cuánto se blanquea la imagen al convertirse en línea: 0 = conserva sus colores, 1 = línea blanca pura.")]
    [Range(0f, 1f)] public float flashStrength = 1f;
    [Tooltip("Grosor de la línea en fracción del alto de la pantalla (0.006 ≈ 6 px a 1080p).")]
    [Range(0.001f, 0.05f)] public float lineThickness = 0.006f;
    [Tooltip("Diámetro del punto final en fracción del alto de la pantalla.")]
    [Range(0.001f, 0.08f)] public float dotSize = 0.016f;
    [Tooltip("Alcance del halo en fracción del alto de la pantalla. Más alto = resplandor más amplio y suave.")]
    [Range(0.001f, 0.3f)] public float glowSize = 0.04f;
    [Tooltip("Líneas de barrido a lo alto de la pantalla mientras la imagen se aplasta o se abre.")]
    [Min(0f)] public float scanlineCount = 270f;
    [Tooltip("Cuánto oscurecen las líneas de barrido. Crecen a medida que la imagen se aplasta. 0 = sin líneas.")]
    [Range(0f, 1f)] public float scanlineStrength = 0.3f;
    [Tooltip("Suavizado del borde de la imagen, la línea y el punto, en píxeles.")]
    [Range(0.5f, 8f)] public float edgeSoftness = 1.5f;

    [Header("=== SONIDO (opcional) ===")]
    [Tooltip("Sonido al apagarse la pantalla. Suena con el volumen de efectos del jugador.")]
    public AudioClip powerOffSFX;
    [Tooltip("Sonido al encenderse la pantalla en la escena nueva.")]
    public AudioClip powerOnSFX;
    [Range(0f, 1f)] public float sfxVolume = 0.8f;

    public float PowerOffDuration => collapseDuration + shrinkDuration + dotHoldDuration + dotFadeDuration + offHoldDuration;

    private static ScreenTransitionConfig instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    public static void OverrideInstance(ScreenTransitionConfig config)
    {
        if (config == null) return;
        instance = config;
    }

    public static ScreenTransitionConfig Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<ScreenTransitionConfig>("ScreenTransitionConfig_Production");

                if (instance == null)
                {
                    instance = CreateInstance<ScreenTransitionConfig>();
                    Debug.LogWarning("[ScreenTransitionConfig] No se encontró 'Resources/ScreenTransitionConfig_Production'. Usando valores por defecto embebidos.");
                }
            }

            return instance;
        }
    }
}
