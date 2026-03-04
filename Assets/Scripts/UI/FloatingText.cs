using UnityEngine;
using TMPro;
using DG.Tweening;

public class FloatingText : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float lifetime = 1f;
    [SerializeField] private float fadeStartTime = 0.5f;

    private TextMeshProUGUI textMesh;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Tween moveTween;
    private Tween fadeTween;

    private void Awake()
    {
        textMesh = GetComponent<TextMeshProUGUI>();
        if (textMesh == null)
        {
        }

        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
        }

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
