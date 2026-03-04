using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[Serializable]
public class TutorialStep
{

    public string id = "";

    public string text = "";

    public bool showArrow = false;

    public float arrowAngle = 0f;

    public bool freezeGame = true;

    public string stepType = "normal";
    public string nextButtonLabel  = "Next";
    public string yesButtonLabel   = "Yes";
    public string noButtonLabel    = "No";

    public string nextStepId     = "";

    public string yesGoesToStep  = "";

    public string noGoesToStep   = "";

    public string waitForEvent = "";

    public bool unfreezeOnNext = false;

    public bool returnToWaitingOnNext = false;

    public bool endsAfterNext = false;

    public string highlightTarget = "";
}

[Serializable]
public class TutorialStepList
{
    public TutorialStep[] steps;
}

public class TutorialManager : MonoBehaviour
{

    public static TutorialManager Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Instance = null;

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
    [SerializeField] private float highlightPulseScale    = 1.12f;
    [SerializeField] private float highlightPulseDuration = 0.55f;

    private TutorialStepList data;
    private int          currentStepIndex = -1;
    private TutorialStep currentStep;

    private enum TutorialState
    {
        Inactive,
        ShowingStep,
        WaitingForCoins,
        WaitingForAnyEvent,
        Complete
    }
    private TutorialState state = TutorialState.Inactive;

    private bool weFrozeTime = false;

    private bool shownCoins            = false;
    private bool shownFirstShot        = false;
    private bool shownFirstOrb         = false;
    private bool shownFirstDamage      = false;
    private bool shownFirstLevelUp     = false;
    private bool shownFirstShopOpened  = false;
    private bool shownFirstShopClosed  = false;

    private const string TUTORIAL_DONE_KEY = "TutorialCompleted_v1";

    private static bool s_isSessionRestart     = false;
    private static bool s_seqDone              = false;
    private static bool s_shownCoins           = false;
    private static bool s_shownFirstShot       = false;
    private static bool s_shownFirstOrb        = false;
    private static bool s_shownFirstDamage     = false;
    private static bool s_shownFirstLevelUp    = false;
    private static bool s_shownFirstShopOpened = false;
    private static bool s_shownFirstShopClosed = false;

    private Coroutine   typewriterCoroutine;
    private bool        isTyping = false;
    private AudioSource typingSource;

    private Coroutine     pulseCoroutine;
    private RectTransform currentHighlightTarget;
    private Vector3       highlightOriginalScale;
    private Canvas        tempHighlightCanvas;
    private bool          addedTempCanvas;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

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

        if (s_isSessionRestart)
        {
            s_isSessionRestart = false;

            if (PlayerPrefs.GetInt(TUTORIAL_DONE_KEY, 0) == 1 && !TutorialConfig.Instance.ForceShowEveryRun)
            { state = TutorialState.Complete; return; }

            if (!LoadDataAndValidate()) return;

            shownCoins           = s_shownCoins;
            shownFirstShot       = s_shownFirstShot;
            shownFirstOrb        = s_shownFirstOrb;
            shownFirstDamage     = s_shownFirstDamage;
            shownFirstLevelUp    = s_shownFirstLevelUp;
            shownFirstShopOpened = s_shownFirstShopOpened;
            shownFirstShopClosed = s_shownFirstShopClosed;

            if (s_seqDone)
            {

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

        bool alreadyDone = PlayerPrefs.GetInt(TUTORIAL_DONE_KEY, 0) == 1;
        if (alreadyDone && !TutorialConfig.Instance.ForceShowEveryRun)
        { state = TutorialState.Complete; return; }

        if (!LoadDataAndValidate()) return;

        int firstStep = FindNextSequentialStepAfter(-1);
        if (firstStep >= 0) ShowStep(firstStep);
        else                EnterWaitingForAnyEvent();
    }

    private bool LoadDataAndValidate()
    {
        TextAsset jsonAsset = Resources.Load<TextAsset>("TutorialData");
        if (jsonAsset == null)
        {
            state = TutorialState.Complete; return false;
        }

        data = JsonUtility.FromJson<TutorialStepList>(jsonAsset.text);
        if (data == null || data.steps == null || data.steps.Length == 0)
        {
            state = TutorialState.Complete; return false;
        }

        bool ok = true;

        bool hasChoice = Array.Exists(data.steps, s => s.stepType == "choice");
        if (hasChoice && choiceNoButton == null)

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

    private void OnNextClicked()
    {
        if (currentStep == null || state != TutorialState.ShowingStep) return;

        if (isTyping) { SkipTypewriter(); return; }

        if (currentStep.stepType == "choice")
        {
            NavigateToId(currentStep.yesGoesToStep);
            return;
        }

        if (currentStep.endsAfterNext)       { CompleteTutorial(); return; }
        if (currentStep.returnToWaitingOnNext){ ReturnToWaiting();  return; }
        if (currentStep.unfreezeOnNext)       { EnterWaitingForCoins(); return; }

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

        if (tutorialPanel != null) tutorialPanel.SetActive(true);

        if (MusicManager.Instance != null) MusicManager.Instance.ReduceVolume();

        if (arrowObject != null)
        {
            bool useHighlight = !string.IsNullOrEmpty(currentStep.highlightTarget);
            arrowObject.SetActive(!useHighlight && currentStep.showArrow);
            if (!useHighlight && currentStep.showArrow && arrowTransform != null)
                arrowTransform.localEulerAngles = new Vector3(0f, 0f, currentStep.arrowAngle);
        }

        if (!string.IsNullOrEmpty(currentStep.highlightTarget))
            ApplyHighlight(currentStep.highlightTarget);
        else
            StopHighlight();

        if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);
        if (messageText != null)
            typewriterCoroutine = StartCoroutine(TypewriterRoutine(currentStep.text));

        GameEvents.TriggerTutorialStepShown(currentStep.id);
    }

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
            return;
        }

        StopHighlight();

        currentHighlightTarget  = target;
        highlightOriginalScale  = target.localScale;

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

            float t = 0f;
            while (t < half)
            {
                t += Time.unscaledDeltaTime;
                target.localScale = Vector3.Lerp(baseScale, bigScale, Mathf.SmoothStep(0f, 1f, t / half));
                yield return null;
            }

            t = 0f;
            while (t < half)
            {
                t += Time.unscaledDeltaTime;
                target.localScale = Vector3.Lerp(bigScale, baseScale, Mathf.SmoothStep(0f, 1f, t / half));
                yield return null;
            }
        }
    }

    private IEnumerator TypewriterRoutine(string fullText)
    {
        isTyping = true;
        messageText.text = fullText;
        messageText.ForceMeshUpdate();
        int totalChars = messageText.textInfo.characterCount;
        messageText.maxVisibleCharacters = 0;

        float charDelay = charsPerSecond > 0f ? 1f / charsPerSecond : 0f;

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

        if (typingSource != null) typingSource.Stop();

        messageText.maxVisibleCharacters = int.MaxValue;
        isTyping = false;
        typewriterCoroutine = null;
    }

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

    private void EnterWaitingForCoins()
    {
        UnfreezeGame();
        HidePanel();
        s_seqDone = true;

        if (EnemySpawnManager.Instance != null)
            EnemySpawnManager.Instance.SetSpawnBlocked(true);
        EnterWaitingForAnyEvent();
    }

    private void ReturnToWaiting()
    {
        HidePanel();
        UnfreezeGame();
        EnterWaitingForAnyEvent();
    }

    private bool AllEventGroupsShown() =>
        shownCoins && shownFirstShot && shownFirstOrb &&
        shownFirstDamage && shownFirstLevelUp &&
        shownFirstShopOpened && shownFirstShopClosed;

    private void EnterWaitingForAnyEvent()
    {

        if (AllEventGroupsShown())
        {
            int idx = FindStepByEvent("all_events_complete");
            if (idx >= 0) ShowStep(idx);
            else          CompleteTutorial();
            return;
        }

        state = TutorialState.WaitingForAnyEvent;
        UnsubscribeAll();

        if (!shownCoins && CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnCoinsChanged += OnCoinsChanged_Tutorial;

        if (!shownFirstShot)        GameEvents.OnEnemyDamaged     += OnFirstEnemyDamaged;
        if (!shownFirstOrb)         GameEvents.OnExperienceGained  += OnFirstOrbCollected;
        if (!shownFirstDamage)      GameEvents.OnPlayerDamaged     += OnFirstPlayerDamaged;
        if (!shownFirstLevelUp)     GameEvents.OnLevelUp           += OnFirstLevelUp;
        if (!shownFirstShopOpened)  GameEvents.OnShopOpened        += OnFirstShopOpened;
        if (!shownFirstShopClosed)  GameEvents.OnShopAutoClosed    += OnFirstShopAutoClosed;
    }

    private void OnCoinsChanged_Tutorial(int coins)
    {
        if (state != TutorialState.WaitingForAnyEvent) return;
        if (coins <= 0) return;

        shownCoins = true;  s_shownCoins = true;

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

    private void FreezeGame()
    {
        if (Time.timeScale != 0f)
        {
            Time.timeScale = 0f;
            weFrozeTime    = true;
        }

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

        if (MusicManager.Instance != null) MusicManager.Instance.RestoreVolume();
    }

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

        if (EnemySpawnManager.Instance != null)
            EnemySpawnManager.Instance.SetSpawnBlocked(false);
        PlayerPrefs.SetInt(TUTORIAL_DONE_KEY, 1);
        PlayerPrefs.Save();
        ClearSession();
        GameEvents.TriggerTutorialCompleted();
    }

    public static void MarkSessionRestart()
    {
        s_isSessionRestart = true;
    }

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
        return -1;
    }

    public bool IsTutorialPanelActive => state == TutorialState.ShowingStep;

    public bool IsTutorialActive => state != TutorialState.Inactive && state != TutorialState.Complete;

    public static void ResetTutorial()
    {
        PlayerPrefs.DeleteKey(TUTORIAL_DONE_KEY);
        PlayerPrefs.Save();
    }
}
