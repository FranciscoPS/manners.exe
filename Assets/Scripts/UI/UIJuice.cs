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

    public static Tween Punch(this RectTransform rectTransform)
    {
        UIStyle style = UIStyle.Instance;
        float scale = style != null ? style.punchScale : 1.16f;
        float rotation = style != null ? style.punchRotation : 4f;
        float duration = style != null ? style.punchDuration : 0.32f;

        rectTransform.DOKill();
        rectTransform.localScale = Vector3.one;
        rectTransform.localRotation = Quaternion.identity;

        Sequence sequence = DOTween.Sequence();
        sequence.SetUpdate(true).SetTarget(rectTransform);
        sequence.Append(rectTransform.DOScale(scale, duration * 0.35f).SetEase(Ease.OutBack));
        sequence.Join(rectTransform.DOLocalRotate(new Vector3(0f, 0f, rotation), duration * 0.35f).SetEase(Ease.OutQuad));
        sequence.Append(rectTransform.DOScale(1f, duration * 0.65f).SetEase(Ease.OutQuad));
        sequence.Join(rectTransform.DOLocalRotate(Vector3.zero, duration * 0.65f).SetEase(Ease.OutBack));
        sequence.OnKill(() =>
        {
            if (rectTransform == null) return;
            rectTransform.localScale = Vector3.one;
            rectTransform.localRotation = Quaternion.identity;
        });

        return sequence;
    }

    public static Tween Shake(this RectTransform rectTransform, float strength = 10f, float duration = 0.3f)
    {
        rectTransform.DOKill(true);
        Vector2 rest = rectTransform.anchoredPosition;

        return rectTransform.DOShakeAnchorPos(duration, strength, 22, 90f, false, true)
            .SetUpdate(true)
            .OnKill(() =>
            {
                if (rectTransform != null) rectTransform.anchoredPosition = rest;
            });
    }

    public static Tween PopOut(this RectTransform rectTransform, float duration = 0.2f, float overshoot = 0.7f, float delay = 0f)
    {
        rectTransform.DOKill();

        return rectTransform.DOScale(0f, duration)
            .SetDelay(delay)
            .SetEase(Ease.InBack, overshoot)
            .SetUpdate(true);
    }
}
