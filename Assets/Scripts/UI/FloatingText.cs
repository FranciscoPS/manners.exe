using UnityEngine;
using TMPro;
using DG.Tweening;

public class FloatingText : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float lifetime = 1f;
    [SerializeField] private float fadeStartTime = 0.5f;
    [Tooltip("Escala con la que aparece el número antes de asentarse en su tamaño normal.")]
    [SerializeField] private float popScale = 1.2f;
    [SerializeField] private float popDuration = 0.16f;

    [SerializeField] private TextMeshProUGUI textMesh;
    private RectTransform rectTransform;
    [SerializeField] private CanvasGroup canvasGroup;
    private Tween moveTween;
    private Tween fadeTween;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (canvasGroup == null)
        {
            Debug.LogError("FloatingText requiere CanvasGroup en su prefab.", this);
        }
    }

    public void Initialize(string text, Color color, Vector3 screenPosition)
    {
        if (textMesh == null || rectTransform == null || canvasGroup == null)
        {
            Awake();

            if (textMesh == null || rectTransform == null || canvasGroup == null)
            {
                return;
            }
        }

        textMesh.text = text;
        textMesh.color = color;
        canvasGroup.alpha = 1f;

        rectTransform.position = screenPosition;
        rectTransform.DOKill();
        rectTransform.localScale = Vector3.one * popScale;
        rectTransform.DOScale(1f, popDuration).SetEase(Ease.OutBack).SetUpdate(true);

        float randomX = Random.Range(-30f, 30f);
        Vector3 targetPosition = screenPosition + new Vector3(randomX, 150f, 0f);

        moveTween?.Kill();
        fadeTween?.Kill();

        moveTween = rectTransform.DOMove(targetPosition, lifetime)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);

        fadeTween = canvasGroup.DOFade(0f, lifetime - fadeStartTime)
            .SetDelay(fadeStartTime)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                if (this != null && FloatingTextManager.Instance != null)
                {
                    FloatingTextManager.Instance.ReturnToPool(this);
                }
            });
    }

    private void OnDestroy()
    {
        moveTween?.Kill();
        fadeTween?.Kill();
    }
}
