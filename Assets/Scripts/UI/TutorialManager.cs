using System;
using System.Collections;
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

    /// <summary>
    /// Nombre del objetivo HUD al que debe apuntar la flecha.
    /// Valores válidos: "currency" | "expbar" | "health" | "hud" | "timer"
    /// Si está vacío, la flecha usa showArrow + arrowAngle normalmente.
    /// </summary>
    public string highlightTarget = "";
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

    [Header("Skip All")]
    [Tooltip("Botón visible en todo momento durante el tutorial. Lo completa y marca como hecho.")]
    [SerializeField] private Button skipAllButton;

    [Header("Typewriter Effect")]
    [SerializeField] private float     charsPerSecond = 38f;
    [SerializeField] private AudioClip typingSound;
    [SerializeField] [Range(0f, 1f)]   private float typingVolume = 0.4f;
    [SerializeField] [Range(0.75f, 1.25f)] private float typingPitch = 1.0f;

    [Header("HUD Highlight Targets")]
    [Tooltip("Arrastra aquí: CurrencyPanel")]
    [SerializeField] private RectTransform targetCurrencyPanel;
    [Tooltip("Arrastra aquí: ExpBarPanel")]
    [SerializeField] private RectTransform targetExpBar;
    [Tooltip("Arrastra aquí: HealthBarContainer")]
    [SerializeField] private RectTransform targetHealthBar;
    [Tooltip("Arrastra aquí: HUD")]
    [SerializeField] private RectTransform targetHUD;
    [Tooltip("Arrastra aquí: Timer")]
    [SerializeField] private RectTransform targetTimer;
    [Tooltip("Arrastra aquí: Minimap")]
    [SerializeField] private RectTransform targetMinimap;

    [Header("Highlight Pulse")]
    [SerializeField] private float highlightPulseScale    = 1.12f;  // tamaño máximo del pulso
    [SerializeField] private float highlightPulseDuration = 0.55f;  // segundos por ciclo completo

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

    // ── Estado de sesión estático ─────────────────────────────────────────
    // Persiste entre recargas de escena dentro de la misma sesión.
    // Permite que reiniciar desde Pausa o desde muerte preserve el progreso.
    private static bool s_isSessionRestart     = false;
    private static bool s_seqDone              = false; // intro secuencial completada
    private static bool s_shownCoins           = false;
    private static bool s_shownFirstShot       = false;
    private static bool s_shownFirstOrb        = false;
    private static bool s_shownFirstDamage     = false;
    private static bool s_shownFirstLevelUp    = false;
    private static bool s_shownFirstShopOpened = false;
    private static bool s_shownFirstShopClosed = false;

    // Typewriter state
    private Coroutine   typewriterCoroutine;
    private bool        isTyping = false;
    private AudioSource typingSource;

    // Highlight state
    private Coroutine     pulseCoroutine;
    private RectTransform currentHighlightTarget;
    private Vector3       highlightOriginalScale;
    private Canvas        tempHighlightCanvas;
    private bool          addedTempCanvas;

    // -----------------------------------------------------------------------
    // Unity lifecycle
    // -----------------------------------------------------------------------
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // AudioSource dedicado para el typewriter — pitch independiente por sonido
        typingSource = gameObject.AddComponent<AudioSource>();
        typingSource.playOnAwake  = false;
        typingSource.loop         = false;
        typingSource.spatialBlend = 0f;
        typingSource.priority     = 64;
    }

    private void Start()
    {
        HidePanel();

        if (TutorialConfig.Instance == null || !TutorialConfig.Instance.TutorialEnabled)
        { state = TutorialState.Complete; return; }

        // ── REINICIO DESDE PAUSA O MUERTE ────────────────────────────────────
        // Solo muestra tutoriales pendientes; si el tutorial ya estaba completo, no muestra nada.
        if (s_isSessionRestart)
        {
            s_isSessionRestart = false;

            // Solo saltar si el tutorial está hecho Y no estamos forzando mostrarlo siempre
            if (PlayerPrefs.GetInt(TUTORIAL_DONE_KEY, 0) == 1 && !TutorialConfig.Instance.ForceShowEveryRun)
            { state = TutorialState.Complete; return; }

            if (!LoadDataAndValidate()) return;

            // Restaurar el progreso de esta sesión
            shownCoins           = s_shownCoins;
            shownFirstShot       = s_shownFirstShot;
            shownFirstOrb        = s_shownFirstOrb;
            shownFirstDamage     = s_shownFirstDamage;
            shownFirstLevelUp    = s_shownFirstLevelUp;
            shownFirstShopOpened = s_shownFirstShopOpened;
            shownFirstShopClosed = s_shownFirstShopClosed;

            // Si la intro secuencial ya terminó, ir directo a los eventos pendientes
            if (s_seqDone)
            {
                // Si las monedas aún no se recogieron, bloquear enemigos igual que haría EnterWaitingForCoins
                if (!shownCoins && EnemySpawnManager.Instance != null)
                    EnemySpawnManager.Instance.SetSpawnBlocked(true);
                EnterWaitingForAnyEvent();
            }
            else
            {
                int first = FindNextSequentialStepAfter(-1);
                if (first >= 0) ShowStep(first);
                else            EnterWaitingForAnyEvent();
            }
            return;
        }

        // ── INICIO DESDE MENÚ PRINCIPAL ───────────────────────────────────────
        bool alreadyDone = PlayerPrefs.GetInt(TUTORIAL_DONE_KEY, 0) == 1;
        if (alreadyDone && !TutorialConfig.Instance.ForceShowEveryRun)
        { state = TutorialState.Complete; return; }

        if (!LoadDataAndValidate()) return;

        int firstStep = FindNextSequentialStepAfter(-1);
        if (firstStep >= 0) ShowStep(firstStep);
        else                EnterWaitingForAnyEvent();
    }

    /// <summary>Carga TutorialData.json, valida referencias UI y registra listeners de botones.
    /// Devuelve false si algo falla (y pone state = Complete).</summary>
    private bool LoadDataAndValidate()
    {
        TextAsset jsonAsset = Resources.Load<TextAsset>("TutorialData");
        if (jsonAsset == null)
        {
            Debug.LogError("[TutorialManager] Resources/TutorialData.json no encontrado.");
            state = TutorialState.Complete; return false;
        }

        data = JsonUtility.FromJson<TutorialStepList>(jsonAsset.text);
        if (data == null || data.steps == null || data.steps.Length == 0)
        {
            Debug.LogError("[TutorialManager] TutorialData.json vacío o mal formado.");
            state = TutorialState.Complete; return false;
        }

        bool ok = true;
        if (tutorialPanel  == null) { Debug.LogError("[TutorialManager] tutorialPanel sin asignar!");  ok = false; }
        if (messageText    == null) { Debug.LogError("[TutorialManager] messageText sin asignar!");    ok = false; }
        if (nextButton     == null) { Debug.LogError("[TutorialManager] nextButton sin asignar!");     ok = false; }
        if (nextButtonText == null) { Debug.LogError("[TutorialManager] nextButtonText sin asignar!"); ok = false; }

        bool hasChoice = Array.Exists(data.steps, s => s.stepType == "choice");
        if (hasChoice && choiceNoButton == null)
        { Debug.LogError("[TutorialManager] Hay pasos 'choice' pero choiceNoButton sin asignar!"); ok = false; }

        if (!ok) { state = TutorialState.Complete; return false; }

        nextButton.onClick.AddListener(OnNextClicked);
        if (choiceNoButton != null)
        {
            choiceNoButton.onClick.AddListener(OnNoClicked);
            choiceNoButton.gameObject.SetActive(false);
        }
        if (skipAllButton != null)
        {
            skipAllButton.onClick.AddListener(OnSkipAllClicked);
            skipAllButton.gameObject.SetActive(true);
        }
        return true;
    }

    private void OnDestroy()
    {
        if (nextButton     != null) nextButton.onClick.RemoveListener(OnNextClicked);
        if (choiceNoButton != null) choiceNoButton.onClick.RemoveListener(OnNoClicked);
        if (skipAllButton  != null) skipAllButton.onClick.RemoveListener(OnSkipAllClicked);
        UnsubscribeAll();
    }

    // -----------------------------------------------------------------------
    // Button handlers
    // -----------------------------------------------------------------------
    private void OnNextClicked()
    {
        if (currentStep == null || state != TutorialState.ShowingStep) return;

        // If still typing, skip to full text and wait for next click
        if (isTyping) { SkipTypewriter(); return; }

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
        if (isTyping) { SkipTypewriter(); return; }
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

        if (currentStep.freezeGame) FreezeGame();

        // Activar panel PRIMERO antes de cualquier cálculo de canvas
        if (tutorialPanel != null) tutorialPanel.SetActive(true);

        // Atenuar música de fondo mientras el panel está visible
        if (MusicManager.Instance != null) MusicManager.Instance.ReduceVolume();

        // Flecha normal (solo si no hay highlightTarget)
        if (arrowObject != null)
        {
            bool useHighlight = !string.IsNullOrEmpty(currentStep.highlightTarget);
            arrowObject.SetActive(!useHighlight && currentStep.showArrow);
            if (!useHighlight && currentStep.showArrow && arrowTransform != null)
                arrowTransform.localEulerAngles = new Vector3(0f, 0f, currentStep.arrowAngle);
        }

        // Highlight: poner elemento HUD en primer plano con pulso
        if (!string.IsNullOrEmpty(currentStep.highlightTarget))
            ApplyHighlight(currentStep.highlightTarget);
        else
            StopHighlight();

        // Start typewriter (stops any previous one)
        if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);
        if (messageText != null)
            typewriterCoroutine = StartCoroutine(TypewriterRoutine(currentStep.text));

        GameEvents.TriggerTutorialStepShown(currentStep.id);
    }

    // -----------------------------------------------------------------------
    // Highlight (pulse + canvas override)
    // -----------------------------------------------------------------------
    private void ApplyHighlight(string targetName)
    {
        RectTransform target = targetName.ToLower() switch
        {
            "currency" => targetCurrencyPanel,
            "expbar"   => targetExpBar,
            "health"   => targetHealthBar,
            "hud"      => targetHUD,
            "timer"    => targetTimer,
            "minimap"  => targetMinimap,
            _          => null
        };

        if (target == null)
        {
            Debug.LogWarning($"[TutorialManager] highlightTarget '{targetName}' sin RectTransform asignado.");
            return;
        }

        StopHighlight(); // limpiar highlight previo si lo hay

        currentHighlightTarget  = target;
        highlightOriginalScale  = target.localScale;

        // Añadir Canvas override para ponerlo por encima de todo (sortingOrder muy alto)
        tempHighlightCanvas = target.GetComponent<Canvas>();
        if (tempHighlightCanvas == null)
        {
            tempHighlightCanvas = target.gameObject.AddComponent<Canvas>();
            addedTempCanvas = true;
        }
        else
        {
            addedTempCanvas = false;
        }
        tempHighlightCanvas.overrideSorting = true;
        tempHighlightCanvas.sortingOrder    = 9999;

        pulseCoroutine = StartCoroutine(PulseRoutine(target));
    }

    private void StopHighlight()
    {
        if (pulseCoroutine != null) { StopCoroutine(pulseCoroutine); pulseCoroutine = null; }

        if (currentHighlightTarget != null)
        {
            currentHighlightTarget.localScale = highlightOriginalScale;

            if (tempHighlightCanvas != null)
            {
                if (addedTempCanvas)
                    Destroy(tempHighlightCanvas);
                else
                    tempHighlightCanvas.overrideSorting = false;
                tempHighlightCanvas = null;
            }

            currentHighlightTarget = null;
        }
    }

    private IEnumerator PulseRoutine(RectTransform target)
    {
        Vector3 baseScale = highlightOriginalScale;
        Vector3 bigScale  = baseScale * highlightPulseScale;
        float   half      = highlightPulseDuration * 0.5f;

        while (true)
        {
            // Crecer
            float t = 0f;
            while (t < half)
            {
                t += Time.unscaledDeltaTime;
                target.localScale = Vector3.Lerp(baseScale, bigScale, Mathf.SmoothStep(0f, 1f, t / half));
                yield return null;
            }
            // Encoger
            t = 0f;
            while (t < half)
            {
                t += Time.unscaledDeltaTime;
                target.localScale = Vector3.Lerp(bigScale, baseScale, Mathf.SmoothStep(0f, 1f, t / half));
                yield return null;
            }
        }
    }

    // -----------------------------------------------------------------------
    // Typewriter
    // -----------------------------------------------------------------------
    private IEnumerator TypewriterRoutine(string fullText)
    {
        isTyping = true;
        messageText.text = fullText;
        messageText.ForceMeshUpdate();
        int totalChars = messageText.textInfo.characterCount;
        messageText.maxVisibleCharacters = 0;

        float charDelay = charsPerSecond > 0f ? 1f / charsPerSecond : 0f;

        // Iniciar sonido en loop mientras escribe
        if (typingSound != null && typingSource != null)
        {
            typingSource.clip   = typingSound;
            typingSource.loop   = true;
            typingSource.volume = typingVolume * (MusicManager.Instance != null ? MusicManager.Instance.GetSFXVolume() : 1f);
            typingSource.pitch  = typingPitch;
            typingSource.Play();
        }

        for (int i = 0; i < totalChars; i++)
        {
            messageText.maxVisibleCharacters = i + 1;
            yield return new WaitForSecondsRealtime(charDelay);
        }

        // Detener sonido al terminar
        if (typingSource != null) typingSource.Stop();

        messageText.maxVisibleCharacters = int.MaxValue;
        isTyping = false;
        typewriterCoroutine = null;
    }

    /// <summary>Muestra el texto completo al instante y detiene la corutina del typewriter.</summary>
    private void SkipTypewriter()
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }
        if (typingSource != null) typingSource.Stop();
        if (messageText != null) messageText.maxVisibleCharacters = int.MaxValue;
        isTyping = false;
    }

    // -----------------------------------------------------------------------
    // Waiting states
    // -----------------------------------------------------------------------

    /// <summary>Llamado tras unfreezeOnNext: descongela y entra a esperar cualquier evento (monedas incluidas).</summary>
    private void EnterWaitingForCoins()
    {
        UnfreezeGame();
        HidePanel();
        s_seqDone = true; // La intro secuencial terminó; los reinicios de sesión saltarán directo a eventos
        // Bloquear enemigos hasta que el jugador recoja la primera moneda
        if (EnemySpawnManager.Instance != null)
            EnemySpawnManager.Instance.SetSpawnBlocked(true);
        EnterWaitingForAnyEvent();
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
        if (state != TutorialState.WaitingForAnyEvent) return;
        if (coins <= 0) return;

        shownCoins = true;  s_shownCoins = true;
        // Desbloquear enemigos ahora que el jugador ya recogió monedas
        if (EnemySpawnManager.Instance != null)
            EnemySpawnManager.Instance.SetSpawnBlocked(false);
        UnsubscribeAll();
        int idx = FindStepByEvent("coins_collected");
        if (idx >= 0) ShowStep(idx);
        else EnterWaitingForAnyEvent();
    }

    private void OnFirstEnemyDamaged(float damage)
    {
        if (state != TutorialState.WaitingForAnyEvent) return;
        shownFirstShot = true;  s_shownFirstShot = true;
        UnsubscribeAll();
        int idx = FindStepByEvent("first_enemy_damaged");
        if (idx >= 0) ShowStep(idx);
        else EnterWaitingForAnyEvent();
    }

    private void OnFirstOrbCollected(int amount)
    {
        if (state != TutorialState.WaitingForAnyEvent) return;
        shownFirstOrb = true;  s_shownFirstOrb = true;
        UnsubscribeAll();
        int idx = FindStepByEvent("first_orb_collected");
        if (idx >= 0) ShowStep(idx);
        else EnterWaitingForAnyEvent();
    }

    private void OnFirstPlayerDamaged(float damage)
    {
        if (state != TutorialState.WaitingForAnyEvent) return;
        shownFirstDamage = true;  s_shownFirstDamage = true;
        UnsubscribeAll();
        int idx = FindStepByEvent("first_player_damaged");
        if (idx >= 0) ShowStep(idx);
        else EnterWaitingForAnyEvent();
    }

    private void OnFirstLevelUp(int level)
    {
        if (state != TutorialState.WaitingForAnyEvent) return;
        shownFirstLevelUp = true;  s_shownFirstLevelUp = true;
        UnsubscribeAll();
        int idx = FindStepByEvent("first_level_up");
        if (idx >= 0) ShowStep(idx);
        else EnterWaitingForAnyEvent();
    }

    private void OnFirstShopOpened()
    {
        if (state != TutorialState.WaitingForAnyEvent) return;
        shownFirstShopOpened = true;  s_shownFirstShopOpened = true;
        UnsubscribeAll();
        int idx = FindStepByEvent("first_shop_opened");
        if (idx >= 0) ShowStep(idx);
        else EnterWaitingForAnyEvent();
    }

    private void OnFirstShopAutoClosed()
    {
        if (state != TutorialState.WaitingForAnyEvent) return;
        shownFirstShopClosed = true;  s_shownFirstShopClosed = true;
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
        SkipTypewriter();
        StopHighlight();
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
        // Restaurar volumen de música al cerrar el panel
        if (MusicManager.Instance != null) MusicManager.Instance.RestoreVolume();
    }

    // -----------------------------------------------------------------------
    // Completion
    // -----------------------------------------------------------------------
    private void OnSkipAllClicked()
    {
        CompleteTutorial();
    }

    private void CompleteTutorial()
    {
        state = TutorialState.Complete;
        UnfreezeGame();
        HidePanel();
        if (skipAllButton != null) skipAllButton.gameObject.SetActive(false);
        UnsubscribeAll();
        // Seguridad: desbloquear spawn si por algún motivo quedó bloqueado
        if (EnemySpawnManager.Instance != null)
            EnemySpawnManager.Instance.SetSpawnBlocked(false);
        PlayerPrefs.SetInt(TUTORIAL_DONE_KEY, 1);
        PlayerPrefs.Save();
        ClearSession(); // limpiar estado estático ya que el tutorial terminó definitivamente
        GameEvents.TriggerTutorialCompleted();
        Debug.Log("[TutorialManager] Tutorial completed.");
    }

    // ── Métodos estáticos para gestión de sesión ──────────────────────────

    /// <summary>Llamado desde Pausa o Game Over ANTES de recargar la escena.
    /// Preserva el progreso del tutorial para el reinicio.</summary>
    public static void MarkSessionRestart()
    {
        s_isSessionRestart = true;
    }

    /// <summary>Llamado cuando se regresa al Menú Principal.
    /// Limpia el estado de sesión para que el próximo inicio sea desde cero.</summary>
    public static void ClearSession()
    {
        s_isSessionRestart     = false;
        s_seqDone              = false;
        s_shownCoins           = false;
        s_shownFirstShot       = false;
        s_shownFirstOrb        = false;
        s_shownFirstDamage     = false;
        s_shownFirstLevelUp    = false;
        s_shownFirstShopOpened = false;
        s_shownFirstShopClosed = false;
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
