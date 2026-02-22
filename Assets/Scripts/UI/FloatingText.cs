using UnityEngine;
using TMPro;
using DG.Tweening;

public class FloatingText : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float lifetime = 1f;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float fadeStartTime = 0.5f;
    
    private TextMeshProUGUI textMesh;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private float timer;
    private Tween moveTween;
    private Tween fadeTween;
    
    private void Awake()
    {
        textMesh = GetComponent<TextMeshProUGUI>();
        if (textMesh == null)
        {
            Debug.LogError("FloatingText: TextMeshProUGUI component not found!");
        }
        
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogError("FloatingText: RectTransform component not found!");
        }
        
        // Añadir CanvasGroup si no existe
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }
    
    public void Initialize(string text, Color color, Vector3 screenPosition)
    {
        if (textMesh == null || rectTransform == null || canvasGroup == null)
        {
            Debug.LogError("FloatingText: Missing required components! Reinitializing...");
            Awake();
            
            if (textMesh == null || rectTransform == null || canvasGroup == null)
            {
                Debug.LogError("FloatingText: Failed to initialize components!");
                return;
            }
        }
        
        textMesh.text = text;
        textMesh.color = color;
        canvasGroup.alpha = 1f;
        
        // Usar la posición de pantalla directamente (el canvas es ScreenSpace-Overlay)
        rectTransform.position = screenPosition;
        
        // Añadir variación aleatoria horizontal y offset vertical
        float randomX = Random.Range(-30f, 30f);
        Vector3 targetPosition = screenPosition + new Vector3(randomX, 150f, 0f);
        
        // Cancelar tweens anteriores
        moveTween?.Kill();
        fadeTween?.Kill();
        
        // Animar movimiento hacia arriba
        moveTween = rectTransform.DOMove(targetPosition, lifetime)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true); // Usar unscaled time para que funcione con pausa
        
        // Animar fade out
        fadeTween = canvasGroup.DOFade(0f, lifetime - fadeStartTime)
            .SetDelay(fadeStartTime)
            .SetUpdate(true) // Usar unscaled time para que funcione con pausa
            .OnComplete(() => 
            {
                if (FloatingTextManager.Instance != null)
                {
                    FloatingTextManager.Instance.ReturnToPool(this);
                }
            });
        
        timer = 0f;
    }
    
    private void OnDestroy()
    {
        moveTween?.Kill();
        fadeTween?.Kill();
    }
}
