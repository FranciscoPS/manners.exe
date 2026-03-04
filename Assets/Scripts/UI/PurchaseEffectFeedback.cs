using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class PurchaseEffectFeedback : MonoBehaviour
{
    [Header("Flash Settings")]
    [SerializeField] private Color flashColor = new Color(1f, 1f, 0.5f, 1f);
    [SerializeField] private float flashDuration = 0.3f;

    [Header("Scale Settings")]
    [SerializeField] private float punchScale = 1.2f;
    [SerializeField] private float scaleDuration = 0.4f;

    [Header("References")]
    [SerializeField] private Image flashImage;

    private RectTransform rectTransform;
    private Image backgroundImage;
    private Color originalColor;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        backgroundImage = GetComponent<Image>();

        if (flashImage == null)
        {
            CreateFlashImage();
        }

        if (backgroundImage != null)
        {
            originalColor = backgroundImage.color;
        }
    }

    private void CreateFlashImage()
    {
        GameObject flashObj = new GameObject("FlashImage");
        flashObj.transform.SetParent(transform, false);

        RectTransform flashRect = flashObj.AddComponent<RectTransform>();
        flashRect.anchorMin = Vector2.zero;
        flashRect.anchorMax = Vector2.one;
        flashRect.sizeDelta = Vector2.zero;
        flashRect.anchoredPosition = Vector2.zero;

        flashImage = flashObj.AddComponent<Image>();
        flashImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);
        flashImage.raycastTarget = false;

        flashRect.SetAsFirstSibling();
    }

    public void PlayPurchaseEffect()
    {
        PlayFlashEffect();
        PlayScaleEffect();
    }

    private void PlayFlashEffect()
    {
        if (flashImage == null) return;

        Sequence flashSequence = DOTween.Sequence();
        flashSequence.SetUpdate(true);

        flashSequence.Append(flashImage.DOColor(flashColor, flashDuration / 2f));
        flashSequence.Append(flashImage.DOColor(new Color(flashColor.r, flashColor.g, flashColor.b, 0f), flashDuration / 2f));
    }

    private void PlayScaleEffect()
    {
        if (rectTransform == null) return;

        rectTransform.DOKill();

        Sequence scaleSequence = DOTween.Sequence();
        scaleSequence.SetUpdate(true);

        scaleSequence.Append(rectTransform.DOScale(punchScale, scaleDuration / 2f).SetEase(Ease.OutBack));
        scaleSequence.Append(rectTransform.DOScale(1f, scaleDuration / 2f).SetEase(Ease.InOutSine));
    }
}
