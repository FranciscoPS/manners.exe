using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class HealthBarUI : MonoBehaviour
{
    [Header("Health Bar")]
    [SerializeField] private Image healthBarFill;
    [SerializeField] private Image healthBarBackground;

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

    private Color originalColor;
    private PlayerHealth playerHealth;
    private Tween blinkTween;
    private float targetFillAmount = 1f;
    private float currentFillAmount = 1f;

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

    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= UpdateHealthBar;
            playerHealth.OnDamageTaken -= PlayBlinkEffect;
        }

        blinkTween?.Kill();
    }

    private void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        if (healthBarFill != null)
        {
            targetFillAmount = maxHealth > 0 ? currentHealth / maxHealth : 0;
            currentFillAmount = targetFillAmount;
            healthBarFill.rectTransform.anchorMax = new Vector2(currentFillAmount, 1f);

            if (useStyleColors && UIStyle.Instance != null)
            {
                originalColor = UIStyle.Instance.HealthColor(currentFillAmount);
                if (blinkTween == null || !blinkTween.IsActive()) healthBarFill.color = originalColor;
            }
        }
    }

    private void PlayBlinkEffect()
    {
        if (healthBarFill == null) return;

        if (damageShake > 0f && transform is RectTransform barRect)
            barRect.Shake(damageShake, damageShakeDuration);

        blinkTween?.Kill();

        healthBarFill.color = originalColor;
        blinkTween = healthBarFill.DOColor(blinkColor, blinkDuration)
            .SetLoops(blinkCount * 2, LoopType.Yoyo)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                if (this != null && healthBarFill != null)
                {
                    healthBarFill.color = originalColor;
                }
            });
    }
}
