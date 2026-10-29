using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class HealthBarUI : MonoBehaviour
{
    [Header("Health Bar")]
    [SerializeField] private Image healthBarFill;
    [SerializeField] private Image healthBarBackground;

    [Header("Lectura numérica")]
    [Tooltip("Texto con la vida actual y la máxima dentro de la barra. Vacío = la barra no muestra números.")]
    [SerializeField] private TMP_Text valueText;
    [Tooltip("Formato de la lectura: {0} vida actual, {1} vida máxima.")]
    [SerializeField] private LocalizedString valueFormat = new LocalizedString("HP {0}/{1}", "VIDA {0}/{1}");

    [Header("Blink Settings")]
    [SerializeField] private float blinkDuration = 0.1f;
    [SerializeField] private int blinkCount = 3;
    [SerializeField] private Color blinkColor = Color.red;
    [Tooltip("El color de la barra sale del estilo y cambia con la vida: verde, naranja y rojo.")]
    [SerializeField] private bool useStyleColors = false;

    [Header("Golpe")]
    [Tooltip("Sacudida de toda la barra (marco y relleno) al recibir daño, en unidades de canvas. 0 = sin sacudida.")]
    [SerializeField] private float damageShake = 9f;
    [SerializeField] private float damageShakeDuration = 0.28f;

    [Header("Vida baja")]
    [Tooltip("Por debajo de esta proporción de vida el relleno late entre su color y el rojo de alerta del estilo. 0 = sin latido.")]
    [Range(0f, 1f)] [SerializeField] private float lowHealthRatio = 0.3f;
    [Tooltip("Duración de cada latido (ida) del relleno con vida baja.")]
    [SerializeField] private float lowHealthPulseDuration = 0.5f;

    private Color originalColor;
    private PlayerHealth playerHealth;
    private Tween blinkTween;
    private Tween pulseTween;
    private float currentFillAmount = 1f;
    private float lastCurrentHealth;
    private float lastMaxHealth;

    private bool IsBlinking => blinkTween != null && blinkTween.IsActive() && blinkTween.IsPlaying();

    private void Start()
    {
        if (healthBarFill != null)
        {
            originalColor = healthBarFill.color;
        }

        playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += UpdateHealthBar;
            playerHealth.OnDamageTaken += PlayBlinkEffect;
            UpdateHealthBar(playerHealth.CurrentHealth, playerHealth.MaxHealth);
        }
    }

    private void OnEnable()
    {
        GameLocalization.LanguageChanged += RefreshValue;
    }

    private void OnDisable()
    {
        GameLocalization.LanguageChanged -= RefreshValue;
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
        pulseTween?.Kill();
    }

    private void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        lastCurrentHealth = currentHealth;
        lastMaxHealth = maxHealth;

        if (healthBarFill != null)
        {
            currentFillAmount = maxHealth > 0 ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;
            healthBarFill.rectTransform.anchorMax = new Vector2(currentFillAmount, 1f);

            if (useStyleColors && UIStyle.Instance != null)
            {
                originalColor = UIStyle.Instance.HealthColor(currentFillAmount);
            }

            if (!IsBlinking) healthBarFill.color = originalColor;
            UpdateLowHealthPulse();
        }

        RefreshValue();
    }

    private void RefreshValue()
    {
        if (valueText == null) return;

        int current = Mathf.CeilToInt(Mathf.Max(0f, lastCurrentHealth));
        int max = Mathf.CeilToInt(Mathf.Max(0f, lastMaxHealth));
        valueText.text = valueFormat.Format(current, max);
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

        healthBarFill.color = originalColor;
        blinkTween = healthBarFill.DOColor(blinkColor, blinkDuration)
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
    }
}
