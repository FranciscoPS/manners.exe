using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class PremiumUpgradeVisuals : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image backgroundImage;
    
    [Header("Rainbow Overlay Settings")]
    [SerializeField] private bool useRainbowOverlay = true;
    [SerializeField] private float rainbowSpeed = 2f;
    [SerializeField] private float rainbowBrightness = 1.8f;
    [SerializeField] private float overlayAlpha = 0.5f;
    
    [Header("Particles Settings")]
    [SerializeField] private bool useParticles = true;
    
    [Header("Animation Settings")]
    [SerializeField] private float pulseScale = 1.12f;
    [SerializeField] private float pulseDuration = 1.0f;
    
    private RectTransform rectTransform;
    private Tween pulseTween;
    private bool isPremium = false;
    private Image rainbowOverlayImage;
    private Material rainbowMaterial;
    private float rainbowHue = 0f;
    private PremiumParticleEffect particleEffect;
    
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        
        if (backgroundImage == null)
        {
            backgroundImage = GetComponent<Image>();
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
        if (useRainbowOverlay && rainbowOverlayImage == null)
        {
            CreateRainbowOverlay();
        }

        if (rainbowOverlayImage != null)
        {
            rainbowOverlayImage.gameObject.SetActive(true);
        }

        if (useParticles && particleEffect == null)
        {
            CreateParticleEffect();
        }

        if (particleEffect != null)
        {
            particleEffect.Play();
        }
        
        StartPulseAnimation();
    }

    private void CreateParticleEffect()
    {
        GameObject particleObj = new GameObject("PremiumParticles");
        particleObj.transform.SetParent(transform, false);
        
        RectTransform particleRect = particleObj.AddComponent<RectTransform>();
        particleRect.anchorMin = Vector2.zero;
        particleRect.anchorMax = Vector2.one;
        particleRect.offsetMin = Vector2.zero;
        particleRect.offsetMax = Vector2.zero;
        particleRect.localPosition = Vector3.zero;
        particleRect.SetAsLastSibling();
        
        particleObj.AddComponent<ParticleSystem>();
        particleEffect = particleObj.AddComponent<PremiumParticleEffect>();
    }

    private void CreateRainbowOverlay()
    {
        GameObject overlayObj = new GameObject("RainbowOverlay");
        overlayObj.transform.SetParent(transform, false);
        
        rainbowOverlayImage = overlayObj.AddComponent<Image>();
        rainbowOverlayImage.raycastTarget = false;
        
        RectTransform overlayRect = overlayObj.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        overlayRect.SetAsLastSibling();
        
        Shader rainbowShader = Shader.Find("UI/RainbowOverlay");
        if (rainbowShader != null)
        {
            rainbowMaterial = new Material(rainbowShader);
            rainbowOverlayImage.material = rainbowMaterial;
        }
        
        rainbowOverlayImage.color = new Color(1f, 1f, 1f, overlayAlpha);
    }
    
    private void DisablePremiumEffects()
    {
        if (rainbowOverlayImage != null)
        {
            rainbowOverlayImage.gameObject.SetActive(false);
        }

        if (particleEffect != null)
        {
            particleEffect.Stop();
        }
        
        StopAnimations();
    }

    private void Update()
    {
        if (isPremium && useRainbowOverlay && rainbowMaterial != null)
        {
            rainbowHue += Time.unscaledDeltaTime * rainbowSpeed * 0.1f;
            if (rainbowHue > 1f) rainbowHue -= 1f;
            
            Color rainbowColor = Color.HSVToRGB(rainbowHue, 1f, rainbowBrightness);
            rainbowColor.a = overlayAlpha;
            rainbowMaterial.SetColor("_RainbowColor", rainbowColor);
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
    
    private void StopAnimations()
    {
        pulseTween?.Kill();
        
        if (rectTransform != null)
        {
            rectTransform.localScale = Vector3.one;
        }
    }
    
    private void OnDestroy()
    {
        StopAnimations();
        
        if (rainbowMaterial != null)
        {
            Destroy(rainbowMaterial);
        }
    }
}
