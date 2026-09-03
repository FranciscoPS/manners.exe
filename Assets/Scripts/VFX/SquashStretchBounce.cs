using UnityEngine;
using DG.Tweening;

[System.Serializable]
public class SquashStretchBounceSettings
{
    [Tooltip("Altura del salto, en unidades locales del padre del objeto que salta.")]
    public float jumpHeight = 0.6f;

    [Tooltip("Duración total del salto (subida + bajada), en segundos.")]
    public float jumpDuration = 0.45f;

    [Tooltip("Cuánto se aplasta al agacharse antes de saltar y al aterrizar: ancho crece y alto se reduce en este porcentaje (0.25 = 25%).")]
    public float squashAmount = 0.25f;

    [Tooltip("Cuánto se estira en el aire: alto crece y ancho se reduce (0.2 = 20%).")]
    public float stretchAmount = 0.2f;

    [Tooltip("Tiempo que pasa agachado antes de despegar, en segundos.")]
    public float anticipationDuration = 0.12f;

    [Tooltip("Tiempo que tarda en recuperar su forma tras aterrizar, con rebote elástico tipo toon.")]
    public float recoverDuration = 0.35f;

    [Tooltip("Pausa quieto entre un salto y el siguiente. 0 = salta sin parar.")]
    public float restBetweenJumps = 0f;
}

public static class SquashStretchBounce
{
    public static Sequence PlayLoop(Transform target, SquashStretchBounceSettings settings, Vector3 baseScale, float baseLocalY)
    {
        target.DOKill();

        Vector3 squashed = new Vector3(
            baseScale.x * (1f + settings.squashAmount),
            baseScale.y * (1f - settings.squashAmount),
            baseScale.z * (1f + settings.squashAmount));

        Vector3 stretched = new Vector3(
            baseScale.x * (1f - settings.stretchAmount * 0.5f),
            baseScale.y * (1f + settings.stretchAmount),
            baseScale.z * (1f - settings.stretchAmount * 0.5f));

        float half = Mathf.Max(0.05f, settings.jumpDuration * 0.5f);
        float apexTime = settings.anticipationDuration + half * 0.5f;

        Sequence sequence = DOTween.Sequence().SetTarget(target);
        sequence.Append(target.DOScale(squashed, settings.anticipationDuration).SetEase(Ease.OutQuad));
        sequence.Append(target.DOLocalMoveY(baseLocalY + settings.jumpHeight, half).SetEase(Ease.OutQuad));
        sequence.Join(target.DOScale(stretched, half * 0.5f).SetEase(Ease.OutQuad));
        sequence.Insert(apexTime, target.DOScale(baseScale, half * 0.5f).SetEase(Ease.InOutSine));
        sequence.Append(target.DOLocalMoveY(baseLocalY, half).SetEase(Ease.InQuad));
        sequence.Join(target.DOScale(stretched, half).SetEase(Ease.InQuad));
        sequence.Append(target.DOScale(squashed, settings.recoverDuration * 0.3f).SetEase(Ease.OutQuad));
        sequence.Append(target.DOScale(baseScale, settings.recoverDuration * 0.7f).SetEase(Ease.OutElastic, 1.2f, 0.4f));

        if (settings.restBetweenJumps > 0f)
            sequence.AppendInterval(settings.restBetweenJumps);

        sequence.SetLoops(-1, LoopType.Restart);
        return sequence;
    }

    public static Tween Settle(Transform target, Vector3 baseScale, float baseLocalY, float duration = 0.15f)
    {
        target.DOKill();

        Sequence sequence = DOTween.Sequence().SetTarget(target).SetUpdate(true);
        sequence.Append(target.DOLocalMoveY(baseLocalY, duration).SetEase(Ease.OutQuad));
        sequence.Join(target.DOScale(baseScale, duration).SetEase(Ease.OutQuad));
        return sequence;
    }

    public static void ResetPose(Transform target, Vector3 baseScale, float baseLocalY)
    {
        target.DOKill();

        Vector3 localPosition = target.localPosition;
        localPosition.y = baseLocalY;
        target.localPosition = localPosition;
        target.localScale = baseScale;
    }
}
