using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class PremiumUpgradeVisuals : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Outline outline;
    [SerializeField] private Shadow shadow;
    
    [Header("Premium Colors")]
    [SerializeField] private Color premiumBackgroundTint = new Color(1f, 0.95f, 0.7f, 1f);
    [SerializeField] private Color premiumOutlineColor = new Color(1f, 0.84f, 0f);
    [SerializeField] private Color premiumShadowColor = new Color(1f, 1f, 0f, 0.5f);
    
    [Header("Animation Settings")]
    [SerializeField] private float pulseScale = 1.05f;
    [SerializeField] private float pulseDuration = 1.5f;
    [SerializeField] private float glowPulseMin = 0.3f;
    [SerializeField] private float glowPulseMax = 0.7f;
    
    private RectTransform rectTransform;
    private Tween pulseTween;
    private Tween glowTween;
    private bool isPremium = false;
    
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
        
        outline.effectColor = premiumOutlineColor;
        outline.effectDistance = new Vector2(3, -3);
        outline.enabled = true;
        
        if (shadow == null)
        {
            shadow = gameObject.AddComponent<Shadow>();
        }
        
        shadow.effectColor = premiumShadowColor;
        shadow.effectDistance = new Vector2(0, 0);
        shadow.enabled = true;
        
        if (backgroundImage != null)
        {
            backgroundImage.color = premiumBackgroundTint;
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
        
        StopAnimations();
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
        
        Color startColor = new Color(premiumShadowColor.r, premiumShadowColor.g, premiumShadowColor.b, glowPulseMin);
        Color endColor = new Color(premiumShadowColor.r, premiumShadowColor.g, premiumShadowColor.b, glowPulseMax);
        
        shadow.effectColor = startColor;
        
        glowTween = DOTween.To(() => shadow.effectColor, x => shadow.effectColor = x, endColor, pulseDuration / 2f)
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
