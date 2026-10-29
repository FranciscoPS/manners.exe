using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class PremiumUpgradeVisuals : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float pulseScale = 1.06f;
    [SerializeField] private float pulseDuration = 1.0f;
    [Tooltip("Si está apagado, solo se muestra el foil/aura sin el pulso de escala.")]
    [SerializeField] private bool usePulse = true;

    public void SetPulseEnabled(bool value)
    {
        usePulse = value;

        if (!usePulse)
            StopAnimations();
    }

    private RectTransform rectTransform;
    private Tween pulseTween;
    [SerializeField] private RadiantAuraVFX aura;
    [SerializeField] private PokemonHoloEffect holo;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (aura != null) { aura.TrackTarget = rectTransform; aura.Initialize(rectTransform); }
    }

    public void SetPremium(bool premium, UpgradeMode mode = UpgradeMode.LevelUp)
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (premium)
        {
            EnablePremiumEffects(mode != UpgradeMode.Chest);
        }
        else
        {
            DisablePremiumEffects();
        }
    }

    private void EnablePremiumEffects(bool useHolo)
    {
        if (useHolo)
        {
            holo?.Play();
            aura?.Stop();
        }
        else
        {
            aura?.Play();
            holo?.Stop();
        }

        if (usePulse)
            StartPulseAnimation();
    }

    private void DisablePremiumEffects()
    {
        aura?.Stop();
        holo?.Stop();

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

    private void StopAnimations()
    {
        pulseTween?.Kill();

        if (rectTransform != null)
        {
            rectTransform.localScale = Vector3.one;
        }
    }

    private void OnDisable()
    {
        aura?.Stop();
        holo?.Stop();
        StopAnimations();
    }

    private void OnDestroy()
    {
        StopAnimations();

        if (aura != null)
        {
            Destroy(aura.gameObject);
        }

        if (holo != null)
        {
            Destroy(holo.gameObject);
        }
    }
}
