using UnityEngine;

[CreateAssetMenu(fileName = "TutorialConfig", menuName = "Game/Tutorial Configuration")]
public class TutorialConfig : ScriptableObject
{
    private static TutorialConfig instance;

    public static TutorialConfig Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<TutorialConfig>("TutorialConfig");
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

    public void SetTutorialEnabled(bool value) => tutorialEnabled = value;
    public void SetForceShowEveryRun(bool value) => forceShowEveryRun = value;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    public static void OverrideInstance(TutorialConfig config)
    {
        if (config != null)
            instance = config;
    }
}
