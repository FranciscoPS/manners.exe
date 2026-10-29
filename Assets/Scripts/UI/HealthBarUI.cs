using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class HealthBarUI : MonoBehaviour
{
    [Header("Health Bar")]
    [SerializeField] private Image healthBarFill;
    [SerializeField] private Image healthBarBackground;
    [Tooltip("Marco de la barra. Al recibir daño parpadea en rojo junto con el relleno.")]
    [SerializeField] private Image frameImage;

    [Header("Blink Settings")]
    [SerializeField] private float blinkDuration = 0.1f;
    [SerializeField] private int blinkCount = 3;
    [Tooltip("Color del parpadeo al recibir daño. Con 'Use Style Colors' se usa el rojo de alerta del estilo.")]
    [SerializeField] private Color blinkColor = Color.red;
    [Tooltip("El color de la barra sale del estilo y cambia con la vida: verde, naranja y rojo.")]
    [SerializeField] private bool useStyleColors = false;

    [Header("Golpe")]
    [Tooltip("Sacudida de toda la barra (marco, relleno y cara) al recibir daño, en unidades de canvas. 0 = sin sacudida.")]
    [SerializeField] private float damageShake = 16f;
    [SerializeField] private float damageShakeDuration = 0.35f;

    [Header("Vida baja")]
    [Tooltip("Por debajo de esta proporción de vida el relleno late entre su color y el rojo de alerta del estilo. 0 = sin latido.")]
    [Range(0f, 1f)] [SerializeField] private float lowHealthRatio = 0.3f;
    [Tooltip("Duración de cada latido (ida) del relleno con vida baja.")]
    [SerializeField] private float lowHealthPulseDuration = 0.5f;

    private Color originalColor;
    private Color frameColor;
    private PlayerHealth playerHealth;
    private Tween blinkTween;
    private Tween frameTween;
    private Tween pulseTween;
    private float currentFillAmount = 1f;

    private bool IsBlinking => blinkTween != null && blinkTween.IsActive() && blinkTween.IsPlaying();
    private Color FlashColor => useStyleColors && UIStyle.Instance != null ? UIStyle.Instance.alert : blinkColor;

    private void Start()
    {
        if (healthBarFill != null)
        {
            originalColor = healthBarFill.color;
        }

        if (frameImage != null)
        {
            frameColor = frameImage.color;
        }

        playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += UpdateHealthBar;
            playerHealth.OnDamageTaken += PlayBlinkEffect;
            UpdateHealthBar(playerHealth.CurrentHealth, playerHealth.MaxHealth);
        }
    }

    private void OnDisable()
    {
        StopPulse();
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= UpdateHealthBar;
            playerHealth.OnDamageTaken -= PlayBlinkEffect;
        }

        blinkTween?.Kill();
        frameTween?.Kill();
        pulseTween?.Kill();
    }

    private void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        if (healthBarFill == null) return;

        currentFillAmount = maxHealth > 0 ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;
        healthBarFill.rectTransform.anchorMax = new Vector2(currentFillAmount, 1f);

        if (useStyleColors && UIStyle.Instance != null)
        {
            originalColor = UIStyle.Instance.HealthColor(currentFillAmount);
        }

        if (!IsBlinking) healthBarFill.color = originalColor;
        UpdateLowHealthPulse();
    }

    private void UpdateLowHealthPulse()
    {
        bool low = lowHealthRatio > 0f && currentFillAmount > 0f && currentFillAmount <= lowHealthRatio;
        if (!low)
        {
            StopPulse();
            return;
        }

        if (IsBlinking) return;
        StartPulse();
    }

    private void StartPulse()
    {
        if (healthBarFill == null) return;

        pulseTween?.Kill();
        healthBarFill.color = originalColor;
        Color alert = UIStyle.Instance != null ? UIStyle.Instance.alert : blinkColor;
        pulseTween = healthBarFill.DOColor(alert, lowHealthPulseDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine)
            .SetUpdate(true);
    }

    private void StopPulse()
    {
        if (pulseTween == null) return;

        pulseTween.Kill();
        pulseTween = null;

        if (healthBarFill != null && !IsBlinking)
            healthBarFill.color = originalColor;
    }

    private void PlayBlinkEffect()
    {
        if (healthBarFill == null) return;

        if (damageShake > 0f && transform is RectTransform barRect)
            barRect.Shake(damageShake, damageShakeDuration);

        StopPulse();
        blinkTween?.Kill();
        frameTween?.Kill();

        Color flash = FlashColor;
        healthBarFill.color = originalColor;
        blinkTween = healthBarFill.DOColor(flash, blinkDuration)
            .SetLoops(blinkCount * 2, LoopType.Yoyo)
            .SetEase(Ease.Linear)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                if (this == null || healthBarFill == null) return;

                healthBarFill.color = originalColor;
                blinkTween = null;
                UpdateLowHealthPulse();
            });

        if (frameImage == null) return;

        frameImage.color = frameColor;
        frameTween = frameImage.DOColor(flash, blinkDuration)
            .SetLoops(blinkCount * 2, LoopType.Yoyo)
            .SetEase(Ease.Linear)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                if (frameImage != null) frameImage.color = frameColor;
                frameTween = null;
            });
    }
}
