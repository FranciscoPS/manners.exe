using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class MannersFaceUI : MonoBehaviour
{
    public enum Mood
    {
        Calm,
        Happy,
        Hurt,
        Angry,
        Dead,
        Glitch
    }

    [Tooltip("Imagen de los ojos: cambia de sprite y de color según el estado.")]
    [SerializeField] private Image eyes;
    [Tooltip("Reacciona a la partida: daño, vida baja, subida de nivel, sobrecargas y tiempo agotado.")]
    [SerializeField] private bool reactToGame = true;
    [Tooltip("De vez en cuando la cara amable se convierte un instante en la cara de peligro (menú principal).")]
    [SerializeField] private bool maskSlips;

    [Header("Ritmo")]
    [SerializeField] private float slipMin = 5f;
    [SerializeField] private float slipMax = 11f;
    [SerializeField] private float slipDuration = 0.16f;
    [Tooltip("Por debajo de esta proporción de vida la cara pasa a peligro de forma permanente.")]
    [Range(0f, 1f)] [SerializeField] private float lowHealth = 0.3f;

    [Header("Golpe")]
    [Tooltip("Escala máxima de los ojos al recibir daño. 1 = sin exageración.")]
    [SerializeField] private float hurtScale = 1.55f;
    [Tooltip("Giro de los ojos al recibir daño, en grados.")]
    [SerializeField] private float hurtRotation = -14f;
    [Tooltip("Duración del golpe de escala y giro de los ojos.")]
    [SerializeField] private float hurtPunchDuration = 0.42f;
    [Tooltip("Tiempo que la cara mantiene el gesto de dolor con los ojos en rojo.")]
    [SerializeField] private float hurtHold = 0.7f;
    [Tooltip("Sacudida de toda la cara al recibir daño, en unidades de canvas. 0 = sin sacudida.")]
    [SerializeField] private float hurtShake = 12f;
    [SerializeField] private float hurtShakeDuration = 0.35f;
    [Tooltip("La pantalla de la cara parpadea en rojo de alerta al recibir daño.")]
    [SerializeField] private bool hurtScreenFlash = true;
    [SerializeField] private float hurtFlashDuration = 0.08f;
    [SerializeField] private int hurtFlashCount = 2;

    private PlayerHealth playerHealth;
    private Image screen;
    private Color screenColor;
    private Tween screenTween;
    private Mood baseMood = Mood.Calm;
    private Mood shownMood = Mood.Calm;
    private bool overtime;
    private bool lowOnHealth;
    private Coroutine idleRoutine;
    private Coroutine reactionRoutine;

    private void Awake()
    {
        screen = GetComponent<Image>();
        if (screen != null) screenColor = screen.color;
    }

    private void OnEnable()
    {
        Show(baseMood);
        idleRoutine = StartCoroutine(Idle());

        if (!reactToGame) return;

        GameEvents.OnMatchTimeExpired += HandleOvertime;
    }

    private void Start()
    {
        if (!reactToGame) return;

        playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += HandleHealthChanged;
            playerHealth.OnDamageTaken += HandleDamage;
        }

        if (ExperienceManager.Instance != null)
            ExperienceManager.Instance.OnLevelUp += HandleLevelUp;

        if (OverrideManager.Instance != null)
            OverrideManager.Instance.OnOverrideActivated += HandleOverride;
    }

    private void OnDisable()
    {
        GameEvents.OnMatchTimeExpired -= HandleOvertime;

        if (idleRoutine != null) StopCoroutine(idleRoutine);
        if (reactionRoutine != null) StopCoroutine(reactionRoutine);
        idleRoutine = null;
        reactionRoutine = null;

        if (eyes != null)
        {
            eyes.rectTransform.DOKill();
            eyes.rectTransform.localScale = Vector3.one;
            eyes.rectTransform.anchoredPosition = Vector2.zero;
        }

        ResetHurtVisuals();
    }

    private void OnDestroy()
    {
        screenTween?.Kill();

        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= HandleHealthChanged;
            playerHealth.OnDamageTaken -= HandleDamage;
        }

        if (ExperienceManager.Instance != null)
            ExperienceManager.Instance.OnLevelUp -= HandleLevelUp;

        if (OverrideManager.Instance != null)
            OverrideManager.Instance.OnOverrideActivated -= HandleOverride;
    }

    private void HandleHealthChanged(float current, float max)
    {
        if (current <= 0f)
        {
            SetBase(Mood.Dead);
            return;
        }

        lowOnHealth = max > 0f && current / max <= lowHealth;
        SetBase(lowOnHealth || overtime ? Mood.Angry : Mood.Calm);
    }

    private void HandleDamage()
    {
        if (baseMood != Mood.Dead) React(Mood.Hurt, hurtHold);
    }

    private void HandleLevelUp(int level)
    {
        if (baseMood != Mood.Dead) React(Mood.Happy, 1.4f);
    }

    private void HandleOverride(OverrideData overrideData)
    {
        if (baseMood != Mood.Dead) React(Mood.Glitch, 1.6f);
    }

    private void HandleOvertime()
    {
        overtime = true;
        if (baseMood != Mood.Dead) SetBase(Mood.Angry);
    }

    private void SetBase(Mood mood)
    {
        baseMood = mood;
        if (reactionRoutine == null) Show(mood);
    }

    private void React(Mood mood, float duration)
    {
        if (!isActiveAndEnabled) return;
        if (reactionRoutine != null) StopCoroutine(reactionRoutine);
        reactionRoutine = StartCoroutine(Reaction(mood, duration));
    }

    private IEnumerator Reaction(Mood mood, float duration)
    {
        Show(mood);
        if (mood == Mood.Hurt) PlayHurt();
        else if (eyes != null) eyes.rectTransform.Punch();
        yield return new WaitForSecondsRealtime(duration);
        reactionRoutine = null;
        Show(baseMood);
    }

    private void PlayHurt()
    {
        if (eyes != null) eyes.rectTransform.Punch(hurtScale, hurtRotation, hurtPunchDuration);
        if (hurtShake > 0f && transform is RectTransform face) face.Shake(hurtShake, hurtShakeDuration);
        if (!hurtScreenFlash || screen == null) return;

        UIStyle style = UIStyle.Instance;
        Color flash = style != null ? style.alert : Color.red;
        screenTween?.Kill();
        screen.color = screenColor;
        screenTween = screen.DOColor(flash, hurtFlashDuration)
            .SetLoops(hurtFlashCount * 2, LoopType.Yoyo)
            .SetEase(Ease.Linear)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                if (screen != null) screen.color = screenColor;
                screenTween = null;
            });
    }

    private void ResetHurtVisuals()
    {
        screenTween?.Kill();
        screenTween = null;
        if (screen != null) screen.color = screenColor;
        if (transform is RectTransform face) face.DOKill();
    }

    private IEnumerator Idle()
    {
        float nextSlip = Random.Range(slipMin, slipMax);

        while (true)
        {
            UIStyle style = UIStyle.Instance;
            float wait = style != null ? Random.Range(style.faceBlinkMin, style.faceBlinkMax) : 1f;
            yield return new WaitForSecondsRealtime(wait);
            nextSlip -= wait;

            if (eyes == null || reactionRoutine != null || shownMood == Mood.Dead) continue;

            if (maskSlips && nextSlip <= 0f)
            {
                nextSlip = Random.Range(slipMin, slipMax);
                Show(Mood.Angry);
                yield return new WaitForSecondsRealtime(slipDuration);
                Show(baseMood);
                continue;
            }

            RectTransform rect = eyes.rectTransform;
            rect.DOKill();
            rect.localScale = Vector3.one;
            rect.anchoredPosition = Vector2.zero;

            if (style != null && Random.value < style.faceGlanceChance)
            {
                float reach = rect.rect.width * (Random.value < 0.5f ? -0.09f : 0.09f);
                DOTween.Sequence().SetUpdate(true).SetTarget(rect)
                    .Append(rect.DOAnchorPosX(reach, 0.09f).SetEase(Ease.OutCubic))
                    .AppendInterval(0.3f)
                    .Append(rect.DOAnchorPosX(0f, 0.11f).SetEase(Ease.OutCubic));
                continue;
            }

            int blinks = Random.value < 0.3f ? 2 : 1;
            Sequence blink = DOTween.Sequence().SetUpdate(true).SetTarget(rect);
            for (int i = 0; i < blinks; i++)
            {
                blink.Append(rect.DOScaleY(0.1f, 0.06f));
                blink.AppendInterval(0.05f);
                blink.Append(rect.DOScaleY(1f, 0.08f));
            }
        }
    }

    private void Show(Mood mood)
    {
        shownMood = mood;
        UIStyle style = UIStyle.Instance;
        if (eyes == null || style == null) return;

        switch (mood)
        {
            case Mood.Happy:
                eyes.sprite = style.faceHappy;
                eyes.color = style.yellow;
                break;
            case Mood.Hurt:
                eyes.sprite = style.faceHurt;
                eyes.color = style.alert;
                break;
            case Mood.Angry:
                eyes.sprite = style.faceAngry;
                eyes.color = style.alert;
                break;
            case Mood.Dead:
                eyes.sprite = style.faceDead;
                eyes.color = style.textDim;
                break;
            case Mood.Glitch:
                eyes.sprite = style.faceAngry;
                eyes.color = Color.Lerp(style.anomaly, style.paper, 0.45f);
                break;
            default:
                eyes.sprite = style.faceCalm;
                eyes.color = style.cyan;
                break;
        }
    }
}
