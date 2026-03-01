using UnityEngine;

/// <summary>
/// ScriptableObject de configuración del tutorial.
/// Crea una instancia en Assets/Configurations con:
///   Assets > Create > Game > Tutorial Configuration
/// Asigna la instancia al campo "Tutorial Config" del TutorialManager en la escena.
/// </summary>
[CreateAssetMenu(fileName = "TutorialConfig", menuName = "Game/Tutorial Configuration")]
public class TutorialConfig : ScriptableObject
{
    private static TutorialConfig instance;

    /// <summary>
    /// Instancia activa. Se carga automáticamente desde Resources/TutorialConfig.asset.
    /// </summary>
    public static TutorialConfig Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<TutorialConfig>("TutorialConfig");
                if (instance == null)
                    Debug.LogError("[TutorialConfig] No encontrado en la carpeta Resources. Crea el asset en Assets/Resources/TutorialConfig.asset");
            }
            return instance;
        }
    }

    [Header("Tutorial Settings")]
    [Tooltip("Habilita o deshabilita el tutorial al inicio de la partida.")]
    [SerializeField] private bool tutorialEnabled = true;

    [Tooltip("Si está activo, el tutorial se muestra aunque ya se haya completado anteriormente (ignora PlayerPrefs). Útil en desarrollo.")]
    [SerializeField] private bool forceShowEveryRun = false;

    public bool TutorialEnabled    => tutorialEnabled;
    public bool ForceShowEveryRun  => forceShowEveryRun;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }
}
