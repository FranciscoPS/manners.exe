using DG.Tweening;
using UnityEngine;

public class DamageTween : MonoBehaviour
{
    [SerializeField] private int playerLayer;
    [SerializeField] private int enemyLayer;
    [SerializeField] private float tweenTime;
    [SerializeField] private int tweenLoops;
    [SerializeField] private MeshRenderer targetRenderer;
    
    private Tween damageTween;

    public void TweenFx()
    {
        if (targetRenderer == null)
        {
            return;
        }

        ToggleImmunity(true);

        damageTween?.Kill(true);
        Material material = targetRenderer.material;

        float adjustedTweenTime = tweenTime / tweenLoops;

        damageTween = material.DOColor(Color.red, adjustedTweenTime)
            .SetLoops(tweenLoops, LoopType.Yoyo)
            .OnComplete(() => ToggleImmunity(false));
    }

    private void ToggleImmunity(bool immune)
    {
        Physics.IgnoreLayerCollision(playerLayer, enemyLayer, immune);
    }
}
