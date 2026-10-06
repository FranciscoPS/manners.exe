using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

[RequireComponent(typeof(RectTransform))]
public class MenuButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float scaleDuration = 0.15f;

    [Header("Estilo unificado")]
    [Tooltip("Giro en grados mientras el cursor está encima. 0 = sin giro.")]
    [SerializeField] private float hoverRotation = 0f;
    [Tooltip("Escala mientras el botón está pulsado.")]
    [SerializeField] private float pressScale = 0.95f;
    [Tooltip("Cuánto crece la sombra dura bajo el cursor (multiplicador). 1 = no cambia.")]
    [SerializeField] private float hoverShadow = 1f;

    [Header("Texto al hover")]
    [SerializeField] private bool changeTextOnHover = false;
    [SerializeField] private LocalizedString hoverTextLocalized = new LocalizedString("Coming soon", "Próximamente");
    [SerializeField] private TextMeshProUGUI tmpText;
    [SerializeField] private Text uiText;
    [SerializeField] private TextMeshProUGUI replacementText;

    private RectTransform rectTransform;
    private Selectable selectable;
    private Shadow shadow;
    private Vector3 originalScale;
    private Quaternion originalRotation;
    private Vector2 originalShadow;
    private string originalText;
    private bool hasText;
    private bool textChanged;
    private bool hovering;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        selectable = GetComponent<Selectable>();
        shadow = GetComponent<Shadow>();
        originalScale = rectTransform.localScale;
        originalRotation = rectTransform.localRotation;
        if (shadow != null) originalShadow = shadow.effectDistance;

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

    public void Configure(float scale, float duration, float rotation, float press, float shadowGrowth)
    {
        hoverScale = scale;
        scaleDuration = duration;
        hoverRotation = rotation;
        pressScale = press;
        hoverShadow = shadowGrowth;
    }

    private void OnDisable()
    {
        hovering = false;
        rectTransform.DOKill();
        rectTransform.localScale = originalScale;
        rectTransform.localRotation = originalRotation;
        if (shadow != null) shadow.effectDistance = originalShadow;

        if (textChanged)
        {
            RestoreOriginalText();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovering = true;
        if (IsInteractable()) Animate(hoverScale, hoverRotation, hoverShadow, Ease.OutBack);
        MusicManager.Instance?.PlayUISound(MusicManager.Instance.hoverSFX);

        if (changeTextOnHover)
        {
            if (replacementText != null)
            {
                replacementText.gameObject.SetActive(true);
                if (tmpText != null) tmpText.gameObject.SetActive(false);
                if (uiText != null) uiText.gameObject.SetActive(false);
                textChanged = true;
            }
            else if (hasText)
            {
                if (tmpText != null)
                    tmpText.text = hoverTextLocalized.Value;
                else if (uiText != null)
                    uiText.text = hoverTextLocalized.Value;

                textChanged = true;
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovering = false;
        Animate(1f, 0f, 1f, Ease.OutQuad);

        if (textChanged)
        {
            RestoreOriginalText();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!IsInteractable()) return;
        Animate(pressScale, 0f, 0.35f, Ease.OutQuad);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!IsInteractable()) return;

        if (hovering) Animate(hoverScale, hoverRotation, hoverShadow, Ease.OutBack);
        else Animate(1f, 0f, 1f, Ease.OutQuad);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        MusicManager.Instance?.PlayUISound(MusicManager.Instance.clickSFX);
    }

    private bool IsInteractable()
    {
        return selectable == null || selectable.IsInteractable();
    }

    private void Animate(float scale, float rotation, float shadowMultiplier, Ease ease)
    {
        rectTransform.DOKill();
        rectTransform.DOScale(originalScale * scale, scaleDuration).SetUpdate(true).SetEase(ease);

        if (!Mathf.Approximately(hoverRotation, 0f))
        {
            Quaternion target = originalRotation * Quaternion.Euler(0f, 0f, rotation);
            rectTransform.DOLocalRotateQuaternion(target, scaleDuration).SetUpdate(true).SetEase(Ease.OutQuad);
        }

        if (shadow != null && !Mathf.Approximately(hoverShadow, 1f))
        {
            DOTween.To(() => shadow.effectDistance, value => shadow.effectDistance = value, originalShadow * shadowMultiplier, scaleDuration)
                .SetUpdate(true)
                .SetEase(Ease.OutQuad)
                .SetTarget(rectTransform);
        }
    }

    private void RestoreOriginalText()
    {
        if (replacementText != null)
        {
            replacementText.gameObject.SetActive(false);
            if (tmpText != null) tmpText.gameObject.SetActive(true);
            if (uiText != null) uiText.gameObject.SetActive(true);
        }
        else
        {
            if (tmpText != null)
                { LocalizedText localized = tmpText.GetComponent<LocalizedText>(); if (localized != null) localized.Apply(); else tmpText.text = originalText; }
            else if (uiText != null)
                uiText.text = originalText;
        }

        textChanged = false;
    }
}
