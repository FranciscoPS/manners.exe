using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

[RequireComponent(typeof(RectTransform))]
public class MenuButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float scaleDuration = 0.15f;

    private RectTransform rectTransform;
    private Vector3 originalScale;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalScale = rectTransform.localScale;
    }

    private void OnDisable()
    {
        // OnPointerExit no se dispara cuando el panel se desactiva mientras el cursor
        // está encima del botón. Sin esto el botón queda con el scale de hover.
        rectTransform.DOKill();
        rectTransform.localScale = originalScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        rectTransform.DOKill();
        rectTransform.DOScale(originalScale * hoverScale, scaleDuration).SetUpdate(true).SetEase(Ease.OutBack);
        MainMenuUIManager.Instance?.PlaySFX(MainMenuUIManager.Instance.hoverSFX);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        rectTransform.DOKill();
        rectTransform.DOScale(originalScale, scaleDuration).SetUpdate(true).SetEase(Ease.OutQuad);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        MainMenuUIManager.Instance?.PlaySFX(MainMenuUIManager.Instance.clickSFX);
    }
}
