using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ---------------------------------------------------------------------------
// Data classes - usadas internamente por TutorialManager para deserializar JSON
// ---------------------------------------------------------------------------

[Serializable]
public class TutorialStep
{
    /// <summary>Identificador único del paso (ej: "intro_1").</summary>
    public string id = "";

    /// <summary>Texto que se mostrará en el panel.</summary>
    public string text = "";

    /// <summary>Mostrar u ocultar la flecha indicadora.</summary>
    public bool showArrow = false;

    /// <summary>Ángulo de rotación Z de la flecha en grados (0 = arriba, 90 = izquierda, etc.).</summary>
    public float arrowAngle = 0f;

    /// <summary>Si es true, congela Time.timeScale mientras este paso está visible.</summary>
    public bool freezeGame = true;

    /// <summary>Texto del botón Next (pasos normales).</summary>
    public string nextButtonLabel = "Next";

    /// <summary>
    /// "normal" = paso estándar con un botón Next.
    /// "choice" = muestra dos botones Yes/No en lugar de Next.
    /// </summary>
    public string stepType = "normal";

    /// <summary>Label del botón YES cuando stepType=="choice".</summary>
    public string yesButtonLabel = "Yes";

    /// <summary>Label del botón NO cuando stepType=="choice".</summary>
    public string noButtonLabel  = "No";

    /// <summary>ID del paso al que se salta cuando el jugador pulsa YES.</summary>
    public string yesGoesToStep  = "";

    /// <summary>ID del paso al que se salta cuando el jugador pulsa NO.</summary>
    public string noGoesToStep   = "";

    /// <summary>
    /// Vacío = paso secuencial (se muestra tras el anterior).
    /// "coins_collected" = se activa automáticamente cuando el jugador recoge su primera moneda.
    /// </summary>
    public string waitForEvent = "";

    /// <summary>
    /// Cuando el jugador pulsa Next en este paso, se descongela el juego
    /// y se cierra el panel en lugar de avanzar al siguiente paso.
    /// El manager entra en modo de espera hasta que un evento de juego dispare el siguiente grupo.
    /// </summary>
    public bool unfreezeOnNext = false;

    /// <summary>
    /// El tutorial termina completamente después de pulsar Next en este paso.
    /// </summary>
    public bool endsAfterNext = false;
}

[Serializable]
public class TutorialStepList
{
    public TutorialStep[] steps;
}

// ---------------------------------------------------------------------------
// TutorialManager - Controla el flujo completo del tutorial en partida
// ---------------------------------------------------------------------------

/// <summary>
/// Gestor del tutorial en juego.
///
/// SETUP EN UNITY:
/// 1. Crea un GameObject "TutorialManager" en la escena del juego.
/// 2. Añade este componente.
/// 3. Crea un Canvas hijo llamado "TutorialCanvas" (Sort Order alto, ej. 50).
/// 4. Dentro, construye el panel con:
///    - Panel raíz (TutorialPanel): Image con fondo semitransparente
///    - MessageText: TextMeshProUGUI para el mensaje principal
///    - NextButton: Button con un TextMeshProUGUI hijo (NextButtonText)
///    - ArrowObject: GameObject con Image de flecha (hijo del panel, opcional)
/// 5. Asigna todos los GameObjects/Components a los campos del Inspector.
/// 6. Crea TutorialConfig.asset en Assets/Resources/ (Assets > Create > Game > Tutorial Configuration).
/// 7. TutorialData.json ya está en Assets/Resources/ — edita los textos ahí directamente.
///
/// COMPORTAMIENTO:
/// - Pasos sin waitForEvent se muestran en secuencia al pulsar "Siguiente".
/// - El paso con unfreezeOnNext=true libera el movimiento al pulsar "Siguiente" y espera un evento.
/// - El paso con waitForEvent="coins_collected" se activa al recoger la primera moneda.
/// - Solo restaura timeScale si fue el propio tutorial quien lo congeló (no invasivo).
/// - Bloquea el menú de pausa mientras el panel está visible (check en PauseMenu).
/// </summary>
public class TutorialManager : MonoBehaviour
{
    // -----------------------------------------------------------------------
    // Singleton
    // -----------------------------------------------------------------------
    public static TutorialManager Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Instance = null;

    // -----------------------------------------------------------------------
    // Inspector fields
    // -----------------------------------------------------------------------
    [Header("UI References")]
    [Tooltip("GameObject raíz del panel del tutorial. Se activa/desactiva según el estado.")]
    [SerializeField] private GameObject tutorialPanel;

    [Tooltip("TextMeshProUGUI que muestra el mensaje del paso actual.")]
    [SerializeField] private TextMeshProUGUI messageText;

    [Tooltip("Botón para avanzar al siguiente paso.")]
    [SerializeField] private Button nextButton;

    [Tooltip("TextMeshProUGUI hijo del nextButton para cambiar el label.")]
    [SerializeField] private TextMeshProUGUI nextButtonText;

    [Tooltip("GameObject de la flecha indicadora (puede ser null si no se usa).")]
    [SerializeField] private GameObject arrowObject;

    [Tooltip("RectTransform de la flecha para aplicar rotación (puede ser null).")]
    [SerializeField] private RectTransform arrowTransform;

    [Header("Choice Step Buttons")]
    [Tooltip("Botón NO — solo se usa en pasos de tipo 'choice'. Puede dejarse vacío si no hay pasos de elección.")]
    [SerializeField] private Button choiceNoButton;

    [Tooltip("TextMeshProUGUI hijo del choiceNoButton para cambiar el label.")]
    [SerializeField] private TextMeshProUGUI choiceNoButtonText;

    // -----------------------------------------------------------------------
    // Private state
    // -----------------------------------------------------------------------
    private TutorialStepList data;
    private int  currentStepIndex = -1;
    private TutorialStep currentStep;

    private enum TutorialState { Inactive, ShowingStep, WaitingForEvent, Complete }
    private TutorialState state = TutorialState.Inactive;

    // Control no invasivo: solo restauramos timeScale si NOSOTROS lo congelamos
    private bool weFrozeTime = false;

    private const string TUTORIAL_DONE_KEY = "TutorialCompleted_v1";

    // -----------------------------------------------------------------------
    // Unity lifecycle
    // -----------------------------------------------------------------------
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Ocultar panel al inicio — siempre, por si quedó activo en escena
        HidePanel();

        // ¿Tutorial habilitado?
        if (TutorialConfig.Instance == null || !TutorialConfig.Instance.TutorialEnabled)
        {
            state = TutorialState.Complete;
            return;
        }

        // ¿Ya se completó? (salvo que ForceShowEveryRun esté activo)
        bool alreadyDone = PlayerPrefs.GetInt(TUTORIAL_DONE_KEY, 0) == 1;
        if (alreadyDone && !TutorialConfig.Instance.ForceShowEveryRun)
        {
            state = TutorialState.Complete;
            return;
        }

        // Cargar JSON desde Resources/TutorialData.json
        TextAsset jsonAsset = Resources.Load<TextAsset>("TutorialData");
        if (jsonAsset == null)
        {
            Debug.LogError("[TutorialManager] No se encontró Resources/TutorialData.json. " +
                           "Asegúrate de que el archivo esté en Assets/Resources/TutorialData.json.");
            state = TutorialState.Complete;
            return;
        }

        data = JsonUtility.FromJson<TutorialStepList>(jsonAsset.text);
        if (data == null || data.steps == null || data.steps.Length == 0)
        {
            Debug.LogError("[TutorialManager] TutorialData.json está vacío o mal formado.");
            state = TutorialState.Complete;
            return;
        }

        // Validar referencias obligatorias antes de proceder
        bool refsOk = true;
        if (tutorialPanel == null)  { Debug.LogError("[TutorialManager] ¡tutorialPanel no asignado en el Inspector!"); refsOk = false; }
        if (messageText == null)    { Debug.LogError("[TutorialManager] ¡messageText no asignado en el Inspector!"); refsOk = false; }
        if (nextButton == null)     { Debug.LogError("[TutorialManager] ¡nextButton no asignado en el Inspector!"); refsOk = false; }
        if (nextButtonText == null) { Debug.LogError("[TutorialManager] ¡nextButtonText no asignado en el Inspector!"); refsOk = false; }

        // Comprobar si algún paso requiere los botones de elección
        bool hasChoiceStep = false;
        foreach (var s in data.steps)
            if (s.stepType == "choice") { hasChoiceStep = true; break; }

        if (hasChoiceStep && choiceNoButton == null)
        {
            Debug.LogError("[TutorialManager] Hay pasos de tipo 'choice' pero choiceNoButton no está asignado en el Inspector!");
            refsOk = false;
        }

        if (!refsOk)
        {
            Debug.LogError("[TutorialManager] Faltan referencias — el tutorial no puede iniciarse. Asigna todos los campos en el Inspector.");
            state = TutorialState.Complete;
            return;
        }

        // Conectar botones
        nextButton.onClick.AddListener(OnNextButtonClicked);
        if (choiceNoButton != null)
        {
            choiceNoButton.onClick.AddListener(OnNoButtonClicked);
            choiceNoButton.gameObject.SetActive(false); // oculto por defecto
        }

        // Mostrar primer paso secuencial (sin waitForEvent)
        int firstStep = FindNextSequentialStepAfter(-1);
        if (firstStep >= 0)
            ShowStep(firstStep);
        else
            CompleteTutorial(); // No hay pasos secuenciales, nada que hacer
    }

    private void OnDestroy()
    {
        if (nextButton != null)
            nextButton.onClick.RemoveListener(OnNextButtonClicked);

        if (choiceNoButton != null)
            choiceNoButton.onClick.RemoveListener(OnNoButtonClicked);

        UnsubscribeFromGameEvents();
    }

    // -----------------------------------------------------------------------
    // Button handlers
    // -----------------------------------------------------------------------
    private void OnNextButtonClicked()
    {
        if (currentStep == null || state != TutorialState.ShowingStep) return;

        if (currentStep.stepType == "choice")
        {
            // En pasos de elección el botón Next actúa como YES
            int targetIndex = FindStepIndexById(currentStep.yesGoesToStep);
            if (targetIndex >= 0) ShowStep(targetIndex);
            else CompleteTutorial();
        }
        else if (currentStep.endsAfterNext)
        {
            // Este paso cierra el tutorial al pulsar Next
            CompleteTutorial();
        }
        else if (currentStep.unfreezeOnNext)
        {
            // Liberar movimiento y esperar a que un evento de juego dispare el siguiente paso
            UnfreezeGame();
            HidePanel();
            state = TutorialState.WaitingForEvent;
            SubscribeToGameEvents();
        }
        else
        {
            // Avanzar al siguiente paso secuencial
            int next = FindNextSequentialStepAfter(currentStepIndex);
            if (next >= 0) ShowStep(next);
            else CompleteTutorial();
        }
    }

    private void OnNoButtonClicked()
    {
        if (currentStep == null || state != TutorialState.ShowingStep) return;
        if (currentStep.stepType != "choice") return;

        int targetIndex = FindStepIndexById(currentStep.noGoesToStep);
        if (targetIndex >= 0) ShowStep(targetIndex);
        else CompleteTutorial();
    }

    // -----------------------------------------------------------------------
    // Step display
    // -----------------------------------------------------------------------
    private void ShowStep(int index)
    {
        currentStepIndex = index;
        currentStep      = data.steps[index];
        state            = TutorialState.ShowingStep;

        // Texto principal
        if (messageText != null)
            messageText.text = currentStep.text;

        // Configurar botones según el tipo de paso
        if (currentStep.stepType == "choice")
        {
            // Mostrar botón NO y usar el botón Next como YES
            if (choiceNoButton != null) choiceNoButton.gameObject.SetActive(true);
            if (nextButtonText    != null) nextButtonText.text    = string.IsNullOrEmpty(currentStep.yesButtonLabel) ? "Yes" : currentStep.yesButtonLabel;
            if (choiceNoButtonText != null) choiceNoButtonText.text = string.IsNullOrEmpty(currentStep.noButtonLabel)  ? "No"  : currentStep.noButtonLabel;
        }
        else
        {
            // Ocultar botón NO y usar label normal
            if (choiceNoButton != null) choiceNoButton.gameObject.SetActive(false);
            if (nextButtonText != null) nextButtonText.text = string.IsNullOrEmpty(currentStep.nextButtonLabel) ? "Next" : currentStep.nextButtonLabel;
        }

        // Flecha
        if (arrowObject != null)
        {
            arrowObject.SetActive(currentStep.showArrow);
            if (currentStep.showArrow && arrowTransform != null)
                arrowTransform.localEulerAngles = new Vector3(0f, 0f, currentStep.arrowAngle);
        }

        // Congelar tiempo si corresponde
        if (currentStep.freezeGame)
            FreezeGame();

        if (tutorialPanel != null)
            tutorialPanel.SetActive(true);

        // Notificar al resto del juego que se está mostrando este paso
        GameEvents.TriggerTutorialStepShown(currentStep.id);
    }

    // -----------------------------------------------------------------------
    // Step search helpers
    // -----------------------------------------------------------------------
    /// <summary>Devuelve el índice del siguiente paso secuencial (sin waitForEvent) tras afterIndex.</summary>
    private int FindNextSequentialStepAfter(int afterIndex)
    {
        if (data?.steps == null) return -1;
        for (int i = afterIndex + 1; i < data.steps.Length; i++)
        {
            if (string.IsNullOrEmpty(data.steps[i].waitForEvent))
                return i;
        }
        return -1;
    }

    /// <summary>Devuelve el índice del primer paso cuyo waitForEvent coincida con eventName.</summary>
    private int FindStepByEvent(string eventName)
    {
        if (data?.steps == null) return -1;
        for (int i = 0; i < data.steps.Length; i++)
        {
            if (string.Equals(data.steps[i].waitForEvent, eventName,
                              StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }

    /// <summary>Devuelve el índice del paso cuyo id coincida exactamente con el string dado.</summary>
    private int FindStepIndexById(string id)
    {
        if (data?.steps == null || string.IsNullOrEmpty(id)) return -1;
        for (int i = 0; i < data.steps.Length; i++)
        {
            if (string.Equals(data.steps[i].id, id, StringComparison.OrdinalIgnoreCase))
                return i;
        }
        Debug.LogError($"[TutorialManager] No se encontró el paso con id '{id}' en TutorialData.json");
        return -1;
    }

    // -----------------------------------------------------------------------
    // Non-invasive time control
    // -----------------------------------------------------------------------
    private void FreezeGame()
    {
        // Solo congelar si el tiempo corre (no pisamos una pausa existente)
        if (Time.timeScale != 0f)
        {
            Time.timeScale = 0f;
            weFrozeTime = true;
        }
    }

    private void UnfreezeGame()
    {
        // Solo restaurar si NOSOTROS lo congelamos
        if (weFrozeTime)
        {
            Time.timeScale = 1f;
            weFrozeTime    = false;
        }
    }

    private void HidePanel()
    {
        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);
    }

    // -----------------------------------------------------------------------
    // Tutorial completion
    // -----------------------------------------------------------------------
    private void CompleteTutorial()
    {
        state = TutorialState.Complete;
        UnfreezeGame();
        HidePanel();
        UnsubscribeFromGameEvents();
        PlayerPrefs.SetInt(TUTORIAL_DONE_KEY, 1);
        PlayerPrefs.Save();
        GameEvents.TriggerTutorialCompleted();
        Debug.Log("[TutorialManager] Tutorial completado.");
    }

    // -----------------------------------------------------------------------
    // Game event subscriptions (para pasos disparados por eventos del juego)
    // -----------------------------------------------------------------------
    private void SubscribeToGameEvents()
    {
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnCoinsChanged += OnCoinsChangedWhileWaiting;
    }

    private void UnsubscribeFromGameEvents()
    {
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnCoinsChanged -= OnCoinsChangedWhileWaiting;
    }

    private void OnCoinsChangedWhileWaiting(int currentCoins)
    {
        if (state != TutorialState.WaitingForEvent) return;
        if (currentCoins <= 0) return;

        // Primera moneda recogida → desuscribir y mostrar el paso correspondiente
        UnsubscribeFromGameEvents();

        int stepIndex = FindStepByEvent("coins_collected");
        if (stepIndex >= 0)
            ShowStep(stepIndex);
        else
            CompleteTutorial();
    }

    // -----------------------------------------------------------------------
    // Public API
    // -----------------------------------------------------------------------

    /// <summary>
    /// True cuando el panel del tutorial está visible y bloqueando la pantalla.
    /// Úsalo en PauseMenu para bloquear ESC durante el tutorial.
    /// </summary>
    public bool IsTutorialPanelActive => state == TutorialState.ShowingStep;

    /// <summary>
    /// True en cualquier estado activo del tutorial (mostrando panel O esperando evento).
    /// </summary>
    public bool IsTutorialActive => state != TutorialState.Inactive && state != TutorialState.Complete;

    /// <summary>
    /// Borra el marcador de tutorial completado. El tutorial se mostrará de nuevo la próxima partida.
    /// Útil para botones de "Repetir tutorial" en menús de ajustes.
    /// </summary>
    public static void ResetTutorial()
    {
        PlayerPrefs.DeleteKey(TUTORIAL_DONE_KEY);
        PlayerPrefs.Save();
        Debug.Log("[TutorialManager] Progreso del tutorial reseteado.");
    }
}
