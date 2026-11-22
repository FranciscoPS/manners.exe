using UnityEngine;

[CreateAssetMenu(fileName = "PlayerSettingsDefaults", menuName = "Game/Opciones por defecto del jugador")]
public sealed class PlayerSettingsDefaults : ScriptableObject
{
    public const string ResourceName = "PlayerSettingsDefaults_Production";

    private static PlayerSettingsDefaults instance;

    [Header("Idioma")]
    [Tooltip("Idioma con el que arranca quien nunca lo cambió. Después manda lo que elija en Opciones > Idioma.")]
    public GameLanguage language = GameLocalization.FallbackLanguage;

    [Header("Opciones > Gráficos")]
    [Tooltip("Resolución con la que arranca quien nunca la cambió y a la que vuelve Restablecer. 0 = Nativa (la de la pantalla); también 720, 900, 1080, 1440 o 2160.")]
    public int resolutionHeight = GameGraphicsSettings.NativeResolution;

    [Header("Opciones > Accesibilidad")]
    [Tooltip("Valores con los que empieza un jugador nuevo y a los que vuelve el botón Restablecer.")]
    public GameAccessibilitySettings.SettingsData accessibility = new GameAccessibilitySettings.SettingsData();

    public static PlayerSettingsDefaults Instance
    {
        get
        {
            if (instance == null) instance = Resources.Load<PlayerSettingsDefaults>(ResourceName);
            return instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }
}
