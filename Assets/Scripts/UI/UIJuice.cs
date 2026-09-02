using UnityEngine;
using DG.Tweening;

public static class UIJuice
{
    public static Tween PopIn(this RectTransform rectTransform, float duration = 0.3f, float overshoot = 0.9f, float delay = 0f)
    {
        rectTransform.DOKill();
        rectTransform.localScale = Vector3.zero;

        return rectTransform.DOScale(1f, duration)
            .SetDelay(delay)
            .SetEase(Ease.OutBack, overshoot)
            .SetUpdate(true);
    }

    public static Tween PunchScale(this RectTransform rectTransform, float punchScale = 1.2f, float duration = 0.4f)
    {
        rectTransform.DOKill();

        Sequence sequence = DOTween.Sequence();
        sequence.SetUpdate(true);
        sequence.Append(rectTransform.DOScale(punchScale, duration * 0.4f).SetEase(Ease.OutBack));
        sequence.Append(rectTransform.DOScale(1f, duration * 0.6f).SetEase(Ease.OutQuad));

        return sequence;
    }
}
