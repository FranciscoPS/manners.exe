using System.Text;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class NewsBroadcastOverlay : MonoBehaviour, IUpdateable
{
    private const float LowHealthRearmMargin = 0.1f;
    private const float MaxUnscaledStep = 0.1f;

    [Tooltip("Canal, titulares, cintillo y tiempos del noticiero (sandbox: NewsBroadcastConfig_Sandbox).")]
    [SerializeField] private NewsBroadcastConfig config;
    [Tooltip("Vida del jugador, para el titular de vida baja. Si queda vacío se busca en la escena al empezar.")]
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Referencias del prefab")]
    [Tooltip("Grupo que se apaga al desactivar el modo noticiero (tecla del sandbox).")]
    [SerializeField] private CanvasGroup broadcastGroup;
    [SerializeField] private TMP_Text channelText;
    [SerializeField] private TMP_Text clockText;
    [SerializeField] private TMP_Text liveText;
    [Tooltip("Placa de EN VIVO que late.")]
    [SerializeField] private Graphic liveBadge;
    [Tooltip("Máscara que revela el titular y la bajada de izquierda a derecha.")]
    [SerializeField] private RectMask2D headlineMask;
    [SerializeField] private TMP_Text headlineText;
    [SerializeField] private TMP_Text sublineText;
    [Tooltip("Ventana del cintillo; el texto corre dentro de ella.")]
    [SerializeField] private RectTransform tickerViewport;
    [SerializeField] private TMP_Text tickerText;

    private readonly NewsBulletinSchedule schedule = new NewsBulletinSchedule();
    private readonly StringBuilder tickerBuilder = new StringBuilder(512);
    private NewsHeadline current;
    private bool hasCurrent;
    private float reveal;
    private Tween revealTween;
    private float startDelay = -1f;
    private float liveClock;
    private int shownClockMinutes = -1;
    private float tickerX;
    private float tickerWidth;
    private bool lowHealthArmed = true;
    private bool overrideSubscribed;

    public bool IsActive => isActiveAndEnabled;

    public bool IsShowingHeadline => schedule.IsShowing;

    private void OnEnable()
    {
        UpdateManager.Instance?.Register(this);
        GameEvents.OnGameStarted += HandleGameStarted;
        GameEvents.OnLevelUp += HandleLevelUp;
        GameEvents.OnChestSpawned += HandleChestSpawned;
        GameEvents.OnShopClosed += HandleShopClosed;
        GameEvents.OnMatchTimeExpired += HandleMatchTimeExpired;
        GameLocalization.LanguageChanged += ApplyLanguage;
        BroadcastMode.Changed += ApplyMode;
        SubscribeOverrides();
        if (playerHealth == null) playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null) playerHealth.OnHealthChanged += HandleHealthChanged;

        SetReveal(0f);
        ApplyLanguage();
        ApplyMode();
        if (GameTimeManager.Instance != null && GameTimeManager.Instance.IsGameActive) HandleGameStarted();
    }

    private void Start()
    {
        SubscribeOverrides();
    }

    private void OnDisable()
    {
        UpdateManager.Instance?.Unregister(this);
        GameEvents.OnGameStarted -= HandleGameStarted;
        GameEvents.OnLevelUp -= HandleLevelUp;
        GameEvents.OnChestSpawned -= HandleChestSpawned;
        GameEvents.OnShopClosed -= HandleShopClosed;
        GameEvents.OnMatchTimeExpired -= HandleMatchTimeExpired;
        GameLocalization.LanguageChanged -= ApplyLanguage;
        BroadcastMode.Changed -= ApplyMode;
        if (overrideSubscribed && OverrideManager.Instance != null) OverrideManager.Instance.OnOverrideActivated -= HandleOverrideActivated;
        overrideSubscribed = false;
        if (playerHealth != null) playerHealth.OnHealthChanged -= HandleHealthChanged;
        revealTween?.Kill();
    }

    public void OnUpdate(float deltaTime)
    {
        if (config == null) return;

        if (schedule.Advance(deltaTime)) PlayReveal(0f, null);

        if (startDelay >= 0f)
        {
            startDelay -= deltaTime;
            if (startDelay < 0f) Report(NewsStory.MatchStart);
        }

        float realStep = Mathf.Min(Time.unscaledDeltaTime, MaxUnscaledStep);
        UpdateClock();
        UpdateLiveBadge(realStep);
        UpdateTicker(realStep);
    }

    public void Report(NewsStory story)
    {
        if (config == null) return;

        NewsStoryPool pool = config.Pool(story);
        int count = pool.headlines != null ? pool.headlines.Length : 0;
        if (count == 0) return;

        bool wasShowing = schedule.IsShowing;
        if (!schedule.TryStart(pool.priority, config.headlineDuration, config.headlineCooldown)) return;

        current = pool.headlines[schedule.NextHeadlineIndex(story, count)];
        hasCurrent = true;

        if (wasShowing)
        {
            PlayReveal(0f, () =>
            {
                ApplyHeadline();
                PlayReveal(1f, null);
            });
        }
        else
        {
            ApplyHeadline();
            PlayReveal(1f, null);
        }
    }

    private void HandleGameStarted()
    {
        if (config == null) return;

        schedule.Reset();
        revealTween?.Kill();
        SetReveal(0f);
        lowHealthArmed = true;
        shownClockMinutes = -1;
        startDelay = config.firstHeadlineDelay;
    }

    private void HandleLevelUp(int level)
    {
        if (config != null && level % Mathf.Max(1, config.levelUpEvery) == 0) Report(NewsStory.LevelUp);
    }

    private void HandleChestSpawned() => Report(NewsStory.ChestSpawned);

    private void HandleShopClosed() => Report(NewsStory.ShopVisited);

    private void HandleMatchTimeExpired() => Report(NewsStory.Overtime);

    private void HandleOverrideActivated(OverrideData overrideData) => Report(NewsStory.OverrideActivated);

    private void HandleHealthChanged(float current, float max)
    {
        if (config == null || max <= 0f) return;

        float ratio = current / max;
        if (lowHealthArmed && current > 0f && ratio < config.lowHealthThreshold)
        {
            lowHealthArmed = false;
            Report(NewsStory.LowHealth);
        }
        else if (!lowHealthArmed && ratio > config.lowHealthThreshold + LowHealthRearmMargin)
        {
            lowHealthArmed = true;
        }
    }

    private void SubscribeOverrides()
    {
        if (overrideSubscribed) return;

        OverrideManager.EnsureExists();
        if (OverrideManager.Instance == null) return;

        OverrideManager.Instance.OnOverrideActivated += HandleOverrideActivated;
        overrideSubscribed = true;
    }

    private void ApplyLanguage()
    {
        if (config == null) return;

        if (channelText != null) channelText.text = config.channelName;
        if (liveText != null) liveText.text = config.liveLabel.Value;
        if (hasCurrent) ApplyHeadline();
        RebuildTicker();
    }

    private void ApplyMode()
    {
        if (broadcastGroup != null) broadcastGroup.alpha = BroadcastMode.Enabled ? 1f : 0f;
    }

    private void ApplyHeadline()
    {
        if (headlineText != null) headlineText.text = current.headline.Value;
        if (sublineText != null) sublineText.text = current.subline.Value;
    }

    private void PlayReveal(float target, TweenCallback onComplete)
    {
        revealTween?.Kill();
        float duration = config != null ? config.wipeDuration : 0.35f;
        revealTween = DOTween.To(GetReveal, SetReveal, target, duration)
            .SetEase(target > reveal ? Ease.OutCubic : Ease.InCubic)
            .OnComplete(onComplete);
    }

    private float GetReveal() => reveal;

    private void SetReveal(float value)
    {
        reveal = Mathf.Clamp01(value);
        if (headlineMask == null) return;

        float width = headlineMask.rectTransform.rect.width;
        headlineMask.padding = new Vector4(0f, 0f, (1f - reveal) * width, 0f);
    }

    private void UpdateClock()
    {
        if (clockText == null) return;

        float seconds = GameTimeManager.Instance != null ? GameTimeManager.Instance.GetGameTime() : 0f;
        int minutes = NewsBroadcastConfig.ClockMinutes(config.clockStartMinutes, seconds);
        if (minutes == shownClockMinutes) return;

        shownClockMinutes = minutes;
        clockText.text = config.FormatClock(seconds);
    }

    private void UpdateLiveBadge(float deltaTime)
    {
        if (liveBadge == null) return;

        liveClock += deltaTime;
        float wave = 0.5f + 0.5f * Mathf.Cos(liveClock * config.livePulseSpeed * Mathf.PI * 2f);
        liveBadge.canvasRenderer.SetAlpha(Mathf.Lerp(config.livePulseMinAlpha, 1f, wave));
    }

    private void RebuildTicker()
    {
        if (tickerText == null) return;

        tickerBuilder.Clear();
        LocalizedString[] items = config.tickerItems;
        for (int i = 0; items != null && i < items.Length; i++)
        {
            string value = items[i].Value;
            if (string.IsNullOrEmpty(value)) continue;
            if (tickerBuilder.Length > 0) tickerBuilder.Append(config.tickerSeparator);
            tickerBuilder.Append(value);
        }
        if (tickerBuilder.Length > 0) tickerBuilder.Append(config.tickerSeparator);

        tickerText.text = tickerBuilder.ToString();
        tickerText.ForceMeshUpdate();
        tickerWidth = tickerText.preferredWidth;
        RectTransform rect = tickerText.rectTransform;
        rect.sizeDelta = new Vector2(tickerWidth, rect.sizeDelta.y);
        tickerX = 0f;
        rect.anchoredPosition = new Vector2(tickerX, rect.anchoredPosition.y);
    }

    private void UpdateTicker(float deltaTime)
    {
        if (tickerText == null || tickerViewport == null || tickerWidth <= 0f) return;

        tickerX -= config.tickerSpeed * deltaTime;
        if (tickerX < -tickerWidth) tickerX = tickerViewport.rect.width;

        RectTransform rect = tickerText.rectTransform;
        rect.anchoredPosition = new Vector2(tickerX, rect.anchoredPosition.y);
    }
}
