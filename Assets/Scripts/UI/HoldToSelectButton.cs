using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class HoldToSelectButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("References")]
    [SerializeField] private Image fillOverlayImage;
    [Tooltip("Margen del relleno respecto al borde de la tarjeta, para que no tape el contorno.")]
    [SerializeField] private Vector2 fillInset = Vector2.zero;

    [Header("Hold Settings")]
    [SerializeField] private float holdDuration = 0.5f;
    [SerializeField] private int holdSFXRepeatCount = 3;
    [SerializeField] private float holdSFXPitchStart = 1.0f;
    [SerializeField] private float holdSFXPitchEnd = 1.3f;

    [Header("Premium Style")]
    [Tooltip("Color del relleno para mejoras especiales/premium: más brillante para no perderse contra el fondo arcoiris.")]
    [SerializeField] private Color premiumFillColor = new Color(1f, 0.95f, 0.6f, 0.9f);
    [Tooltip("Velocidad del brillo pulsante del relleno en mejoras especiales.")]
    [SerializeField] private float premiumShimmerSpeed = 6f;

    private bool isHolding = false;
    private float holdTimer = 0f;
    private Button button;
    private int currentSFXPlayCount = 0;
    private Color normalFillColor;
    private bool isPremiumStyle;

    public System.Action OnHoldComplete;

    private void Awake()
    {
        button = GetComponent<Button>();

        if (fillOverlayImage != null)
        {
            normalFillColor = fillOverlayImage.color;
            fillOverlayImage.raycastTarget = false;
            SetFill(0f);

            fillOverlayImage.gameObject.SetActive(false);
        }
    }

    private void SetFill(float progress)
    {
        RectTransform rt = fillOverlayImage.rectTransform;
        RectTransform parent = rt.parent as RectTransform;
        float width = parent != null ? Mathf.Max(0f, parent.rect.width - fillInset.x * 2f) : 0f;

        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.offsetMin = new Vector2(fillInset.x, fillInset.y);
        rt.offsetMax = new Vector2(fillInset.x + width * progress, -fillInset.y);
    }

    public void SetPremiumStyle(bool premium)
    {
        isPremiumStyle = premium;

        if (fillOverlayImage != null)
        {
            fillOverlayImage.color = premium ? premiumFillColor : normalFillColor;
        }
    }

    private void Update()
    {
        if (isHolding && button != null && button.interactable)
        {
            holdTimer += Time.unscaledDeltaTime;

            if (fillOverlayImage != null)
            {
                float fillProgress = Mathf.Clamp01(holdTimer / holdDuration);
                SetFill(fillProgress);

                if (isPremiumStyle)
                {
                    float shimmerT = (Mathf.Sin(holdTimer * premiumShimmerSpeed) + 1f) * 0.5f;
                    Color shimmerColor = Color.Lerp(premiumFillColor, Color.white, shimmerT * 0.5f);
                    shimmerColor.a = premiumFillColor.a;
                    fillOverlayImage.color = shimmerColor;
                }
            }

            int targetPlayCount = Mathf.FloorToInt((holdTimer / holdDuration) * holdSFXRepeatCount);
            if (targetPlayCount > currentSFXPlayCount && currentSFXPlayCount < holdSFXRepeatCount)
            {
                currentSFXPlayCount = targetPlayCount;
                PlayHoldSFX();
            }

            if (holdTimer >= holdDuration)
            {
                CompleteHold();
            }
        }
    }

    private void PlayHoldSFX()
    {
        if (MusicManager.Instance != null && SFXDatabase.Instance != null && SFXDatabase.Instance.holdUpgradeSFX != null)
        {
            float progress = (float)currentSFXPlayCount / holdSFXRepeatCount;
            float pitch = Mathf.Lerp(holdSFXPitchStart, holdSFXPitchEnd, progress);
            MusicManager.Instance.PlaySFXOneShot(SFXDatabase.Instance.holdUpgradeSFX, SFXDatabase.Instance.upgradeVolume, pitch);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (button == null || !button.interactable)
            return;

        isHolding = true;
        holdTimer = 0f;
        currentSFXPlayCount = 0;

        if (fillOverlayImage != null)
        {
            fillOverlayImage.gameObject.SetActive(true);
            fillOverlayImage.transform.SetAsLastSibling();
            SetFill(0f);
            fillOverlayImage.color = isPremiumStyle ? premiumFillColor : normalFillColor;
        }

        PlayHoldSFX();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        ResetHold();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ResetHold();
    }

    private void CompleteHold()
    {
        isHolding = false;

        if (MusicManager.Instance != null && SFXDatabase.Instance != null && SFXDatabase.Instance.completeUpgradeSFX != null)
        {
            MusicManager.Instance.PlaySFXOneShot(SFXDatabase.Instance.completeUpgradeSFX, SFXDatabase.Instance.upgradeVolume);
        }

        OnHoldComplete?.Invoke();

        if (fillOverlayImage != null)
        {
            fillOverlayImage.gameObject.SetActive(false);
            SetFill(0f);
        }
    }

    private void ResetHold()
    {
        if (!isHolding)
            return;

        isHolding = false;
        holdTimer = 0f;
        currentSFXPlayCount = 0;

        if (fillOverlayImage != null)
        {
            fillOverlayImage.gameObject.SetActive(false);
            SetFill(0f);
        }
    }

    public void SetInteractable(bool interactable)
    {
        if (!interactable)
        {
            ResetHold();
        }
    }
}
