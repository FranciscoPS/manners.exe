using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class HoldToSelectButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("References")]
    [SerializeField] private Image fillOverlayImage;
    
    [Header("Hold Settings")]
    [SerializeField] private float holdDuration = 0.5f;
    [SerializeField] private int holdSFXRepeatCount = 3;
    [SerializeField] private float holdSFXPitchStart = 1.0f;
    [SerializeField] private float holdSFXPitchEnd = 1.3f;
    
    private bool isHolding = false;
    private float holdTimer = 0f;
    private Button button;
    private int currentSFXPlayCount = 0;
    
    public System.Action OnHoldComplete;
    
    private void Awake()
    {
        button = GetComponent<Button>();
        
        if (fillOverlayImage != null)
        {
            RectTransform rt = fillOverlayImage.rectTransform;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            
            fillOverlayImage.gameObject.SetActive(false);
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
                RectTransform rt = fillOverlayImage.rectTransform;
                rt.anchorMax = new Vector2(fillProgress, 1f);
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
            RectTransform rt = fillOverlayImage.rectTransform;
            rt.anchorMax = new Vector2(0f, 1f);
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
            RectTransform rt = fillOverlayImage.rectTransform;
            rt.anchorMax = new Vector2(0f, 1f);
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
            RectTransform rt = fillOverlayImage.rectTransform;
            rt.anchorMax = new Vector2(0f, 1f);
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
