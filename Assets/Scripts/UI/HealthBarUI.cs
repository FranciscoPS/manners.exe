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

    private void Update()
    {
        if (healthBarFill != null)
        {
            RectTransform rt = healthBarFill.rectTransform;
            rt.anchorMax = new Vector2(currentFillAmount, 1f);
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
        }
    }

    private void PlayBlinkEffect()
    {
        if (healthBarFill == null) return;

        blinkTween?.Kill();

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
