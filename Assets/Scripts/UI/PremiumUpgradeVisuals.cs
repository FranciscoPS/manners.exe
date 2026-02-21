using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class PremiumUpgradeVisuals : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Outline outline;
    [SerializeField] private Shadow shadow;
    [SerializeField] private ParticleSystem particleEffect;
    
    [Header("Premium Colors")]
    [SerializeField] private Color premiumBackgroundTint = new Color(1f, 0.95f, 0.7f, 1f);
    [SerializeField] private bool useRainbowBorder = true;
    [SerializeField] private float rainbowSpeed = 2f;
    
    [Header("Animation Settings")]
    [SerializeField] private float pulseScale = 1.08f;
    [SerializeField] private float pulseDuration = 1.2f;
    [SerializeField] private float glowPulseMin = 0.5f;
    [SerializeField] private float glowPulseMax = 1f;
    [SerializeField] private Vector2 outlineDistance = new Vector2(4, -4);
    
    private RectTransform rectTransform;
    private Tween pulseTween;
    private Tween glowTween;
    private bool isPremium = false;
    private float rainbowHue = 0f;
    
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        
        if (backgroundImage == null)
        {
            backgroundImage = GetComponent<Image>();
        }
        
        if (outline == null)
        {
            outline = gameObject.GetComponent<Outline>();
        }
        
        if (shadow == null)
        {
            shadow = gameObject.GetComponent<Shadow>();
        }
    }
    
    public void SetPremium(bool premium)
    {
        isPremium = premium;
        
        if (premium)
        {
            EnablePremiumEffects();
        }
        else
        {
            DisablePremiumEffects();
        }
    }
    
    private void EnablePremiumEffects()
    {
        if (outline == null)
        {
            outline = gameObject.AddComponent<Outline>();
        }
        
        outline.effectDistance = outlineDistance;
        outline.enabled = true;
        
        if (shadow == null)
        {
            shadow = gameObject.AddComponent<Shadow>();
        }
        
        shadow.effectDistance = new Vector2(0, 0);
        shadow.enabled = true;
        
        if (backgroundImage != null)
        {
            backgroundImage.color = premiumBackgroundTint;
        }

        if (particleEffect != null)
        {
            particleEffect.Play();
        }
        
        StartPulseAnimation();
        StartGlowPulse();
    }
    
    private void DisablePremiumEffects()
    {
        if (outline != null)
        {
            outline.enabled = false;
        }
        
        if (shadow != null)
        {
            shadow.enabled = false;
        }
        
        if (backgroundImage != null)
        {
            backgroundImage.color = Color.white;
        }

        if (particleEffect != null)
        {
            particleEffect.Stop();
        }
        
        StopAnimations();
    }

    private void Update()
    {
        if (isPremium && useRainbowBorder && outline != null)
        {
            rainbowHue += Time.unscaledDeltaTime * rainbowSpeed * 0.1f;
            if (rainbowHue > 1f) rainbowHue -= 1f;
            
            outline.effectColor = Color.HSVToRGB(rainbowHue, 1f, 1f);
        }

        if (isPremium && shadow != null && useRainbowBorder)
        {
            float shadowHue = rainbowHue + 0.5f;
            if (shadowHue > 1f) shadowHue -= 1f;
            Color shadowBase = Color.HSVToRGB(shadowHue, 0.8f, 1f);
            shadow.effectColor = new Color(shadowBase.r, shadowBase.g, shadowBase.b, shadow.effectColor.a);
        }
    }
    
    private void StartPulseAnimation()
    {
        pulseTween?.Kill();
        
        pulseTween = rectTransform.DOScale(pulseScale, pulseDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true);
    }
    
    private void StartGlowPulse()
    {
        if (shadow == null) return;
        
        glowTween?.Kill();
        
        float currentAlpha = glowPulseMin;
        
        glowTween = DOTween.To(() => currentAlpha, x => {
            currentAlpha = x;
            if (shadow != null)
            {
                Color c = shadow.effectColor;
                shadow.effectColor = new Color(c.r, c.g, c.b, currentAlpha);
            }
        }, glowPulseMax, pulseDuration / 2f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true);
    }
    
    private void StopAnimations()
    {
        pulseTween?.Kill();
        glowTween?.Kill();
        
        if (rectTransform != null)
        {
            rectTransform.localScale = Vector3.one;
        }
    }
    
    private void OnDestroy()
    {
        StopAnimations();
    }
}
