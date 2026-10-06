using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class UIHideUnderOverlay : MonoBehaviour
{
    [Tooltip("Duración del fundido al ocultarse o reaparecer.")]
    [SerializeField] private float fade = 0.12f;

    private CanvasGroup group;

    private void Awake()
    {
        group = GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        UIOverlay.Changed += Refresh;
        group.alpha = UIOverlay.ActiveCount > 0 ? 0f : 1f;
    }

    private void OnDisable()
    {
        UIOverlay.Changed -= Refresh;
        group.DOKill();
    }

    private void Refresh()
    {
        group.DOKill();
        group.DOFade(UIOverlay.ActiveCount > 0 ? 0f : 1f, fade).SetUpdate(true);
    }
}
