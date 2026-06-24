using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

[RequireComponent(typeof(RectTransform))]
public class MenuButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float scaleDuration = 0.15f;

    [Header("Texto al hover")]
    [SerializeField] private bool changeTextOnHover = false;
    [SerializeField] private string hoverText = "Próximamente";
    [SerializeField] private TextMeshProUGUI tmpText;
    [SerializeField] private Text uiText;

    private RectTransform rectTransform;
    private Vector3 originalScale;
    private string originalText;
    private bool hasText;
    private bool textChanged;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalScale = rectTransform.localScale;

        if (changeTextOnHover)
        {
            if (tmpText == null && uiText == null)
            {
                tmpText = GetComponentInChildren<TextMeshProUGUI>();
                uiText = GetComponentInChildren<Text>();
            }

            if (tmpText != null)
            {
                originalText = tmpText.text;
                hasText = true;
            }
            else if (uiText != null)
            {
                originalText = uiText.text;
                hasText = true;
            }
        }
    }

    private void OnDisable()
    {
        rectTransform.DOKill();
        rectTransform.localScale = originalScale;

        if (textChanged)
        {
            RestoreOriginalText();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        rectTransform.DOKill();
        rectTransform.DOScale(originalScale * hoverScale, scaleDuration).SetUpdate(true).SetEase(Ease.OutBack);
        MusicManager.Instance?.PlayUISound(MusicManager.Instance.hoverSFX);

        if (changeTextOnHover && hasText)
        {
            if (tmpText != null)
                tmpText.text = hoverText;
            else if (uiText != null)
                uiText.text = hoverText;

            textChanged = true;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        rectTransform.DOKill();
        rectTransform.DOScale(originalScale, scaleDuration).SetUpdate(true).SetEase(Ease.OutQuad);

        if (textChanged)
        {
            RestoreOriginalText();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        MusicManager.Instance?.PlayUISound(MusicManager.Instance.clickSFX);
    }

    private void RestoreOriginalText()
    {
        if (tmpText != null)
            tmpText.text = originalText;
        else if (uiText != null)
            uiText.text = originalText;

        textChanged = false;
    }
}
