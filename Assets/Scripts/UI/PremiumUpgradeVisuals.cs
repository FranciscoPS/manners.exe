using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class PremiumUpgradeVisuals : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float pulseScale = 1.06f;
    [SerializeField] private float pulseDuration = 1.0f;

    private RectTransform rectTransform;
    private Tween pulseTween;
    private RadiantAuraVFX aura;
    private PokemonHoloEffect holo;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
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
            if (holo == null)
            {
                holo = CreateHolo();
            }

            holo.Play();
            aura?.Stop();
        }
        else
        {
            if (aura == null)
            {
                aura = CreateAura();
            }

            aura.Play();
            holo?.Stop();
        }

        StartPulseAnimation();
    }

    private PokemonHoloEffect CreateHolo()
    {
        GameObject holoObj = new GameObject("HoloFoil", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        holoObj.transform.SetParent(rectTransform, false);
        holoObj.transform.SetAsLastSibling();

        RectTransform holoRect = holoObj.GetComponent<RectTransform>();
        holoRect.anchorMin = Vector2.zero;
        holoRect.anchorMax = Vector2.one;
        holoRect.offsetMin = Vector2.zero;
        holoRect.offsetMax = Vector2.zero;

        return holoObj.AddComponent<PokemonHoloEffect>();
    }

    private RadiantAuraVFX CreateAura()
    {
        Transform parent = rectTransform.parent != null ? rectTransform.parent : rectTransform;

        GameObject auraObj = new GameObject("CardAura", typeof(RectTransform));
        auraObj.transform.SetParent(parent, false);
        auraObj.transform.SetSiblingIndex(Mathf.Max(0, rectTransform.GetSiblingIndex()));

        RectTransform auraRect = auraObj.GetComponent<RectTransform>();
        auraRect.anchorMin = rectTransform.anchorMin;
        auraRect.anchorMax = rectTransform.anchorMax;
        auraRect.pivot = rectTransform.pivot;
        auraRect.anchoredPosition = rectTransform.anchoredPosition;
        auraRect.sizeDelta = rectTransform.sizeDelta;

        LayoutElement layoutElement = auraObj.AddComponent<LayoutElement>();
        layoutElement.ignoreLayout = true;

        RadiantAuraVFX newAura = auraObj.AddComponent<RadiantAuraVFX>();
        newAura.TrackTarget = rectTransform;
        newAura.Initialize(auraRect);

        return newAura;
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
