using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class HoldToSelectButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("References")]
    [SerializeField] private Image fillOverlayImage;
    
    [Header("Hold Settings")]
    [SerializeField] private float holdDuration = 0.5f;
    
    private bool isHolding = false;
    private float holdTimer = 0f;
    private Button button;
    
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
            
            if (holdTimer >= holdDuration)
            {
                CompleteHold();
            }
        }
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        if (button == null || !button.interactable)
            return;
        
        isHolding = true;
        holdTimer = 0f;
        
        if (fillOverlayImage != null)
        {
            fillOverlayImage.gameObject.SetActive(true);
            RectTransform rt = fillOverlayImage.rectTransform;
            rt.anchorMax = new Vector2(0f, 1f);
        }
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
