using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ---------------------------------------------------------------------------
// Data classes â€” deserializadas desde TutorialData.json
// ---------------------------------------------------------------------------

[Serializable]
public class TutorialStep
{
    /// <summary>ID Ãºnico del paso. Usado por nextStepId / yesGoesToStep / noGoesToStep.</summary>
    public string id = "";

    /// <summary>Texto que se mostrarÃ¡ en el panel.</summary>
    public string text = "";

    /// <summary>Mostrar la flecha indicadora.</summary>
    public bool showArrow = false;

    /// <summary>Ãngulo Z de la flecha en grados (0 = arriba).</summary>
    public float arrowAngle = 0f;

    /// <summary>Congela Time.timeScale mientras este paso estÃ¡ visible.</summary>
    public bool freezeGame = true;

    // --- Botones ---
    /// <summary>"normal" = botÃ³n Next. "choice" = botones Yes + No.</summary>
    public string stepType = "normal";
    public string nextButtonLabel  = "Next";
    public string yesButtonLabel   = "Yes";
    public string noButtonLabel    = "No";

    // --- NavegaciÃ³n explÃ­cita ---
    /// <summary>Si estÃ¡ relleno, al pulsar Next se va directamente a este paso por ID.</summary>
    public string nextStepId     = "";
    /// <summary>Al pulsar YES (choice), ir a este paso por ID.</summary>
    public string yesGoesToStep  = "";
    /// <summary>Al pulsar NO (choice), ir a este paso por ID.</summary>
    public string noGoesToStep   = "";

    // --- Disparadores de evento ---
    /// <summary>
    /// El paso solo se activa cuando se dispara este evento del juego:
    ///   "coins_collected"       â†’ primera moneda recogida
    ///   "first_enemy_damaged"   â†’ primer disparo que impacta a un enemigo
    ///   "first_orb_collected"   â†’ primer orbe de experiencia recogido
    ///   "first_player_damaged"  â†’ primer daÃ±o recibido por el jugador
    ///   "first_level_up"        â†’ primer subida de nivel
    /// </summary>
    public string waitForEvent = "";

    // --- Control de flujo especial ---
    /// <summary>Al pulsar Next: descongela el juego, cierra el panel, y espera al evento "coins_collected".</summary>
    public bool unfreezeOnNext = false;

    /// <summary>Al pulsar Next: cierra el panel (SIN descongelar) y vuelve a esperar mÃºltiples eventos pendientes.</summary>
    public bool returnToWaitingOnNext = false;

    /// <summary>Al pulsar Next: el tutorial termina completamente.</summary>
    public bool endsAfterNext = false;
}

[Serializable]
public class TutorialStepList
{
    public TutorialStep[] steps;
}

// ---------------------------------------------------------------------------
// TutorialManager
// ---------------------------------------------------------------------------

/// <summary>
/// Controla el flujo completo del tutorial en partida.
///
/// FLUJO DE ESTADOS:
///   Inactive â†’ ShowingStep â†” WaitingForCoins â†’ WaitingForAnyEvent â†’ Complete
///
/// WaitingForCoins:     espera la primera moneda (tras "unfreezeOnNext")
/// WaitingForAnyEvent:  espera cualquiera de: first_shot / first_orb / first_damage / first_levelup
///                      los que ya se mostraron se ignoran; cuando todos se han visto â†’ Complete
///
/// CRÃTICO â€” Level Up:
///   LevelUpManager ya congela Time.timeScale=0 ANTES de que llegue el evento OnLevelUp.
///   FreezeGame() comprueba si el tiempo ya estÃ¡ a 0 antes de actuar â†’ weFrozeTime=false.
///   Al cerrar el tutorial tras los pasos de level-up, UnfreezeGame() no hace nada porque
///   weFrozeTime=false â†’ el panel de level-up sigue congelado y activo. Comportamiento correcto.
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
    // Inspector
    // -----------------------------------------------------------------------
    [Header("UI References")]
    [SerializeField] private GameObject     tutorialPanel;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button          nextButton;
    [SerializeField] private TextMeshProUGUI nextButtonText;
    [SerializeField] private GameObject     arrowObject;
    [SerializeField] private RectTransform  arrowTransform;

    [Header("Choice Buttons")]
    [SerializeField] private Button          choiceNoButton;
    [SerializeField] private TextMeshProUGUI choiceNoButtonText;

    // -----------------------------------------------------------------------
    // State
    // -----------------------------------------------------------------------
    private TutorialStepList data;
    private int          currentStepIndex = -1;
    private TutorialStep currentStep;

    private enum TutorialState
    {
        Inactive,
        ShowingStep,
        WaitingForCoins,       // despuÃ©s de "unfreezeOnNext" â€” solo espera monedas
        WaitingForAnyEvent,    // espera cualquiera de los 4 eventos restantes
        Complete
    }
    private TutorialState state = TutorialState.Inactive;

    // Â¿Fuimos NOSOTROS quienes congelamos el tiempo?
    private bool weFrozeTime = false;

    // Grupos de eventos ya mostrados
    private bool shownCoins            = false;
    private bool shownFirstShot        = false;
    private bool shownFirstOrb         = false;
    private bool shownFirstDamage      = false;
    private bool shownFirstLevelUp     = false;
    private bool shownFirstShopOpened  = false;
    private bool shownFirstShopClosed  = false;

    private const string TUTORIAL_DONE_KEY = "TutorialCompleted_v1";

    // -----------------------------------------------------------------------
    // Unity lifecycle
    // -----------------------------------------------------------------------
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        HidePanel();

        if (TutorialConfig.Instance == null || !TutorialConfig.Instance.TutorialEnabled)
        { state = TutorialState.Complete; return; }

        bool alreadyDone = PlayerPrefs.GetInt(TUTORIAL_DONE_KEY, 0) == 1;
        if (alreadyDone && !TutorialConfig.Instance.ForceShowEveryRun)
        { state = TutorialState.Complete; return; }

        TextAsset jsonAsset = Resources.Load<TextAsset>("TutorialData");
        if (jsonAsset == null)
        {
            Debug.LogError("[TutorialManager] Resources/TutorialData.json no encontrado.");
            state = TutorialState.Complete; return;
        }

        data = JsonUtility.FromJson<TutorialStepList>(jsonAsset.text);
        if (data == null || data.steps == null || data.steps.Length == 0)
        {
            Debug.LogError("[TutorialManager] TutorialData.json vacÃ­o o mal formado.");
            state = TutorialState.Complete; return;
        }

        // Validar referencias
        bool ok = true;
        if (tutorialPanel  == null) { Debug.LogError("[TutorialManager] tutorialPanel sin asignar!");  ok = false; }
        if (messageText    == null) { Debug.LogError("[TutorialManager] messageText sin asignar!");    ok = false; }
        if (nextButton     == null) { Debug.LogError("[TutorialManager] nextButton sin asignar!");     ok = false; }
        if (nextButtonText == null) { Debug.LogError("[TutorialManager] nextButtonText sin asignar!"); ok = false; }

        bool hasChoice = Array.Exists(data.steps, s => s.stepType == "choice");
        if (hasChoice && choiceNoButton == null)
        { Debug.LogError("[TutorialManager] Hay pasos 'choice' pero choiceNoButton sin asignar!"); ok = false; }

        if (!ok) { state = TutorialState.Complete; return; }

        nextButton.onClick.AddListener(OnNextClicked);
        if (choiceNoButton != null)
        {
            choiceNoButton.onClick.AddListener(OnNoClicked);
            choiceNoButton.gameObject.SetActive(false);
        }

        int first = FindNextSequentialStepAfter(-1);
        if (first >= 0) ShowStep(first);
        else            EnterWaitingForAnyEvent();
    }

    private void OnDestroy()
    {
        if (nextButton     != null) nextButton.onClick.RemoveListener(OnNextClicked);
        if (choiceNoButton != null) choiceNoButton.onClick.RemoveListener(OnNoClicked);
        UnsubscribeAll();
    }

    // -----------------------------------------------------------------------
    // Button handlers
    // -----------------------------------------------------------------------
    private void OnNextClicked()
    {
        if (currentStep == null || state != TutorialState.ShowingStep) return;

        if (currentStep.stepType == "choice")
        {
            NavigateToId(currentStep.yesGoesToStep);
            return;
        }

        if (currentStep.endsAfterNext)       { CompleteTutorial(); return; }
        if (currentStep.returnToWaitingOnNext){ ReturnToWaiting();  return; }
        if (currentStep.unfreezeOnNext)       { EnterWaitingForCoins(); return; }

        // nextStepId explÃ­cito tiene prioridad sobre bÃºsqueda secuencial
        if (!string.IsNullOrEmpty(currentStep.nextStepId))
        {
            NavigateToId(currentStep.nextStepId);
            return;
        }

        int next = FindNextSequentialStepAfter(currentStepIndex);
        if (next >= 0) ShowStep(next);
        else           EnterWaitingForAnyEvent();
    }

    private void OnNoClicked()
    {
        if (currentStep == null || state != TutorialState.ShowingStep) return;
        if (currentStep.stepType != "choice") return;
        NavigateToId(currentStep.noGoesToStep);
    }

    private void NavigateToId(string id)
    {
        if (string.IsNullOrEmpty(id)) { CompleteTutorial(); return; }
        int idx = FindStepIndexById(id);
        if (idx >= 0) ShowStep(idx);
        else          CompleteTutorial();
    }

    // -----------------------------------------------------------------------
    // Step display
    // -----------------------------------------------------------------------
    private void ShowStep(int index)
    {
        currentStepIndex = index;
        currentStep      = data.steps[index];
        state            = TutorialState.ShowingStep;

        if (messageText != null)
            messageText.text = currentStep.text;

        if (currentStep.stepType == "choice")
        {
            if (choiceNoButton     != null) choiceNoButton.gameObject.SetActive(true);
            if (nextButtonText     != null) nextButtonText.text     = string.IsNullOrEmpty(currentStep.yesButtonLabel) ? "Yes" : currentStep.yesButtonLabel;
            if (choiceNoButtonText != null) choiceNoButtonText.text = string.IsNullOrEmpty(currentStep.noButtonLabel)  ? "No"  : currentStep.noButtonLabel;
        }
        else
        {
            if (choiceNoButton != null) choiceNoButton.gameObject.SetActive(false);
            if (nextButtonText != null) nextButtonText.text = string.IsNullOrEmpty(currentStep.nextButtonLabel) ? "Next" : currentStep.nextButtonLabel;
        }

        if (arrowObject != null)
        {
            arrowObject.SetActive(currentStep.showArrow);
            if (currentStep.showArrow && arrowTransform != null)
                arrowTransform.localEulerAngles = new Vector3(0f, 0f, currentStep.arrowAngle);
        }

        if (currentStep.freezeGame) FreezeGame();

        if (tutorialPanel != null) tutorialPanel.SetActive(true);

        GameEvents.TriggerTutorialStepShown(currentStep.id);
    }

    // -----------------------------------------------------------------------
    // Waiting states
    // -----------------------------------------------------------------------

    /// <summary>Llamado tras unfreezeOnNext: descongela y espera solo la primera moneda.</summary>
    private void EnterWaitingForCoins()
    {
        UnfreezeGame();
        HidePanel();
        state = TutorialState.WaitingForCoins;

        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnCoinsChanged += OnCoinsChanged_Tutorial;
    }

    /// <summary>
    /// Llamado tras returnToWaitingOnNext (fin de un grupo de evento).
    /// NO descongela si el tiempo ya fue congelado por otro sistema (ej: level-up panel).
    /// </summary>
    private void ReturnToWaiting()
    {
        HidePanel();
        UnfreezeGame(); // solo actÃºa si weFrozeTime == true
        EnterWaitingForAnyEvent();
    }

    private bool AllEventGroupsShown() =>
        shownCoins && shownFirstShot && shownFirstOrb &&
        shownFirstDamage && shownFirstLevelUp &&
        shownFirstShopOpened && shownFirstShopClosed;

    private void EnterWaitingForAnyEvent()
    {
        // Si todos los grupos ya se mostraron, mostrar los pasos finales de compleción
        if (AllEventGroupsShown())
        {
            int idx = FindStepByEvent("all_events_complete");
            if (idx >= 0) ShowStep(idx);
            else          CompleteTutorial();
            return;
        }

        state = TutorialState.WaitingForAnyEvent;
        UnsubscribeAll(); // evitar dobles suscripciones

        if (!shownCoins && CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnCoinsChanged += OnCoinsChanged_Tutorial;

        if (!shownFirstShot)        GameEvents.OnEnemyDamaged     += OnFirstEnemyDamaged;
        if (!shownFirstOrb)         GameEvents.OnExperienceGained  += OnFirstOrbCollected;
        if (!shownFirstDamage)      GameEvents.OnPlayerDamaged     += OnFirstPlayerDamaged;
        if (!shownFirstLevelUp)     GameEvents.OnLevelUp           += OnFirstLevelUp;
        if (!shownFirstShopOpened)  GameEvents.OnShopOpened        += OnFirstShopOpened;
        if (!shownFirstShopClosed)  GameEvents.OnShopAutoClosed    += OnFirstShopAutoClosed;
    }

    // -----------------------------------------------------------------------
    // Event handlers
    // -----------------------------------------------------------------------
    private void OnCoinsChanged_Tutorial(int coins)
    {
        if (state != TutorialState.WaitingForCoins && state != TutorialState.WaitingForAnyEvent) return;
        if (coins <= 0) return;

        shownCoins = true;
        UnsubscribeAll();
        int idx = FindStepByEvent("coins_collected");
        if (idx >= 0) ShowStep(idx);
        else EnterWaitingForAnyEvent();
    }

    private void OnFirstEnemyDamaged(float damage)
    {
        if (state != TutorialState.WaitingForAnyEvent) return;
        shownFirstShot = true;
        UnsubscribeAll();
        int idx = FindStepByEvent("first_enemy_damaged");
        if (idx >= 0) ShowStep(idx);
        else EnterWaitingForAnyEvent();
    }

    private void OnFirstOrbCollected(int amount)
    {
        if (state != TutorialState.WaitingForAnyEvent) return;
        shownFirstOrb = true;
        UnsubscribeAll();
        int idx = FindStepByEvent("first_orb_collected");
        if (idx >= 0) ShowStep(idx);
        else EnterWaitingForAnyEvent();
    }

    private void OnFirstPlayerDamaged(float damage)
    {
        if (state != TutorialState.WaitingForAnyEvent) return;
        shownFirstDamage = true;
        UnsubscribeAll();
        int idx = FindStepByEvent("first_player_damaged");
        if (idx >= 0) ShowStep(idx);
        else EnterWaitingForAnyEvent();
    }

    private void OnFirstLevelUp(int level)
    {
        if (state != TutorialState.WaitingForAnyEvent) return;
        shownFirstLevelUp = true;
        UnsubscribeAll();
        int idx = FindStepByEvent("first_level_up");
        if (idx >= 0) ShowStep(idx);
        else EnterWaitingForAnyEvent();
    }

    private void OnFirstShopOpened()
    {
        if (state != TutorialState.WaitingForAnyEvent) return;
        shownFirstShopOpened = true;
        UnsubscribeAll();
        int idx = FindStepByEvent("first_shop_opened");
        if (idx >= 0) ShowStep(idx);
        else EnterWaitingForAnyEvent();
    }

    private void OnFirstShopAutoClosed()
    {
        if (state != TutorialState.WaitingForAnyEvent) return;
        shownFirstShopClosed = true;
        UnsubscribeAll();
        int idx = FindStepByEvent("first_shop_auto_closed");
        if (idx >= 0) ShowStep(idx);
        else EnterWaitingForAnyEvent();
    }

    // -----------------------------------------------------------------------
    // Event subscription management
    // -----------------------------------------------------------------------
    private void UnsubscribeAll()
    {
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnCoinsChanged -= OnCoinsChanged_Tutorial;

        GameEvents.OnEnemyDamaged     -= OnFirstEnemyDamaged;
        GameEvents.OnExperienceGained -= OnFirstOrbCollected;
        GameEvents.OnPlayerDamaged    -= OnFirstPlayerDamaged;
        GameEvents.OnLevelUp          -= OnFirstLevelUp;
        GameEvents.OnShopOpened       -= OnFirstShopOpened;
        GameEvents.OnShopAutoClosed   -= OnFirstShopAutoClosed;
    }

    // -----------------------------------------------------------------------
    // Time control (non-invasive)
    // -----------------------------------------------------------------------
    private void FreezeGame()
    {
        if (Time.timeScale != 0f)
        {
            Time.timeScale = 0f;
            weFrozeTime    = true;
        }
        // Si timeScale ya era 0 (ej: LevelUpManager lo congelÃ³), weFrozeTime queda false â†’ no lo restauramos al cerrar
    }

    private void UnfreezeGame()
    {
        if (!weFrozeTime) return;
        Time.timeScale = 1f;
        weFrozeTime    = false;
    }

    private void HidePanel()
    {
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
    }

    // -----------------------------------------------------------------------
    // Completion
    // -----------------------------------------------------------------------
    private void CompleteTutorial()
    {
        state = TutorialState.Complete;
        UnfreezeGame();
        HidePanel();
        UnsubscribeAll();
        PlayerPrefs.SetInt(TUTORIAL_DONE_KEY, 1);
        PlayerPrefs.Save();
        GameEvents.TriggerTutorialCompleted();
        Debug.Log("[TutorialManager] Tutorial completed.");
    }

    // -----------------------------------------------------------------------
    // Step search helpers
    // -----------------------------------------------------------------------
    private int FindNextSequentialStepAfter(int afterIndex)
    {
        if (data?.steps == null) return -1;
        for (int i = afterIndex + 1; i < data.steps.Length; i++)
            if (string.IsNullOrEmpty(data.steps[i].waitForEvent)) return i;
        return -1;
    }

    private int FindStepByEvent(string eventName)
    {
        if (data?.steps == null) return -1;
        for (int i = 0; i < data.steps.Length; i++)
            if (string.Equals(data.steps[i].waitForEvent, eventName, StringComparison.OrdinalIgnoreCase))
                return i;
        return -1;
    }

    private int FindStepIndexById(string id)
    {
        if (data?.steps == null || string.IsNullOrEmpty(id)) return -1;
        for (int i = 0; i < data.steps.Length; i++)
            if (string.Equals(data.steps[i].id, id, StringComparison.OrdinalIgnoreCase))
                return i;
        Debug.LogError($"[TutorialManager] Step id '{id}' not found in TutorialData.json");
        return -1;
    }

    // -----------------------------------------------------------------------
    // Public API
    // -----------------------------------------------------------------------
    /// <summary>True mientras el panel del tutorial estÃ¡ visible. Usado por PauseMenu para bloquear ESC.</summary>
    public bool IsTutorialPanelActive => state == TutorialState.ShowingStep;

    /// <summary>True en cualquier estado activo (mostrando O esperando evento).</summary>
    public bool IsTutorialActive => state != TutorialState.Inactive && state != TutorialState.Complete;

    public static void ResetTutorial()
    {
        PlayerPrefs.DeleteKey(TUTORIAL_DONE_KEY);
        PlayerPrefs.Save();
    }
}
