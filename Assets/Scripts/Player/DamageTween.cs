using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class DamageTween : MonoBehaviour
{
    [Header("Visual Settings")]
    [SerializeField] private Color damageColor = Color.red;
    [SerializeField] private float tweenTime = 0.3f;
    [SerializeField] private int tweenLoops = 3;

    private GameObject targetObject;
    private SpriteRenderer spriteRenderer;
    private Graphic uiGraphic;
    private Renderer meshRenderer;

    private Material materialInstance;
    private bool createdMaterialInstance = false;

    private Color originalColor = Color.white;
    private Tween damageTween;
    private bool isInitialized = false;

    private void Awake()
    {
        InitializeMaterial();
    }

    private void OnEnable()
    {
        if (!isInitialized) InitializeMaterial();
        CaptureOriginalColor();
    }

    private void OnDisable()
    {
        SetTargetColor(originalColor);

        damageTween?.Kill();
    }

    public void InitializeMaterial()
    {
        if (targetObject == null)
        {
            targetObject = gameObject;
        }

        if (targetObject == null) return;
        if (isInitialized) return;

        spriteRenderer = targetObject.GetComponent<SpriteRenderer>();
        uiGraphic = targetObject.GetComponent<Graphic>();
        meshRenderer = (spriteRenderer == null && uiGraphic == null) ? targetObject.GetComponent<Renderer>() : null;

        if (meshRenderer != null)
        {
            materialInstance = meshRenderer.material;
            createdMaterialInstance = true;
        }

        isInitialized = true;
        CaptureOriginalColor();
    }

    private void CaptureOriginalColor()
    {
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
            return;
        }

        if (uiGraphic != null)
        {
            originalColor = uiGraphic.color;
            return;
        }

        if (materialInstance != null)
        {
            if (materialInstance.HasProperty("_BaseColor"))
                originalColor = materialInstance.GetColor("_BaseColor");
            else if (materialInstance.HasProperty("_Color"))
                originalColor = materialInstance.GetColor("_Color");
            else
                originalColor = materialInstance.color;
            return;
        }

        originalColor = Color.white;
    }

    public void TweenFx()
    {
        if (!isInitialized) InitializeMaterial();

        if (spriteRenderer == null && uiGraphic == null && materialInstance == null)
            return;

        damageTween?.Kill(true);

        float adjustedTweenTime = tweenTime / Mathf.Max(1, tweenLoops);

        damageTween = DOTween.To(
            () => GetCurrentColor(),
            color => SetTargetColor(color),
            damageColor,
            adjustedTweenTime
        )
        .SetLoops(tweenLoops, LoopType.Yoyo)
        .OnComplete(() =>
        {
            if (this != null)
            {
                SetTargetColor(originalColor);
            }
        });
    }

    private Color GetCurrentColor()
    {
        if (spriteRenderer != null) return spriteRenderer.color;
        if (uiGraphic != null) return uiGraphic.color;
        if (materialInstance != null)
        {
            if (materialInstance.HasProperty("_BaseColor"))
                return materialInstance.GetColor("_BaseColor");
            if (materialInstance.HasProperty("_Color"))
                return materialInstance.GetColor("_Color");
            return materialInstance.color;
        }
        return originalColor;
    }

    private void SetTargetColor(Color color)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
            return;
        }

        if (uiGraphic != null)
        {
            uiGraphic.color = color;
            return;
        }

        if (materialInstance != null)
        {
            if (materialInstance.HasProperty("_BaseColor"))
                materialInstance.SetColor("_BaseColor", color);
            if (materialInstance.HasProperty("_Color"))
                materialInstance.SetColor("_Color", color);
            materialInstance.color = color;
        }
    }

    private void OnDestroy()
    {
        damageTween?.Kill();

        if (createdMaterialInstance && materialInstance != null && Application.isPlaying)
        {
            Destroy(materialInstance);
        }
    }
}
