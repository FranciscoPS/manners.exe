using DG.Tweening;
using DG.Tweening.Core;
using UnityEngine;
using UnityEngine.UI;

public class DamageTween : MonoBehaviour
{
    [Header("Visual Settings")]
    [SerializeField] private Color damageColor = Color.red;
    [SerializeField] private float tweenTime = 0.3f;
    [SerializeField] private int tweenLoops = 3;
    [Tooltip("Si está activo, también tiñe los Renderer de los hijos (útil para modelos con varias partes, como la torreta de un tanque). Si no, solo tiñe el Renderer del propio GameObject.")]
    [SerializeField] private bool includeChildRenderers = false;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private GameObject targetObject;
    private SpriteRenderer spriteRenderer;
    private Graphic uiGraphic;

    private Renderer[] renderers;
    private Color[] rendererOriginalColors;
    private bool[] rendererHasBaseColor;
    private bool[] rendererHasColor;
    private MaterialPropertyBlock propertyBlock;

    private Color originalColor = Color.white;
    private Color currentColor = Color.white;

    private Tween damageTween;
    private bool isInitialized = false;
    private DOGetter<Color> getColor;
    private DOSetter<Color> setColor;
    private TweenCallback restoreColors;

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
        RestoreOriginalColors();

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

        if (spriteRenderer == null && uiGraphic == null)
        {
            renderers = includeChildRenderers
                ? targetObject.GetComponentsInChildren<Renderer>(true)
                : new[] { targetObject.GetComponent<Renderer>() };

            rendererHasBaseColor = new bool[renderers.Length];
            rendererHasColor = new bool[renderers.Length];
            rendererOriginalColors = new Color[renderers.Length];

            for (int i = 0; i < renderers.Length; i++)
            {
                Material shared = renderers[i] != null ? renderers[i].sharedMaterial : null;
                if (shared == null) continue;

                rendererHasBaseColor[i] = shared.HasProperty(BaseColorId);
                rendererHasColor[i] = shared.HasProperty(ColorId);

                rendererOriginalColors[i] = rendererHasBaseColor[i] ? shared.GetColor(BaseColorId)
                    : rendererHasColor[i] ? shared.GetColor(ColorId)
                    : shared.color;
            }

            propertyBlock = new MaterialPropertyBlock();
        }

        isInitialized = true;
        CaptureOriginalColor();
    }

    private void CaptureOriginalColor()
    {
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
            currentColor = originalColor;
            return;
        }

        if (uiGraphic != null)
        {
            originalColor = uiGraphic.color;
            currentColor = originalColor;
            return;
        }

        originalColor = rendererOriginalColors != null && rendererOriginalColors.Length > 0
            ? rendererOriginalColors[0]
            : Color.white;

        currentColor = originalColor;
    }

    public void TweenFx()
    {
        if (!isInitialized) InitializeMaterial();

        if (spriteRenderer == null && uiGraphic == null && (renderers == null || renderers.Length == 0))
            return;

        if (damageTween != null && damageTween.IsActive() && damageTween.IsPlaying())
            return;

        damageTween?.Kill(true);

        if (getColor == null)
        {
            getColor = GetCurrentColor;
            setColor = SetTargetColor;
            restoreColors = RestoreIfAlive;
        }

        float adjustedTweenTime = tweenTime / Mathf.Max(1, tweenLoops);

        damageTween = DOTween.To(getColor, setColor, damageColor, adjustedTweenTime)
            .SetLoops(tweenLoops, LoopType.Yoyo)
            .OnComplete(restoreColors);
    }

    private Color GetCurrentColor()
    {
        return currentColor;
    }

    private void RestoreIfAlive()
    {
        if (this != null)
        {
            RestoreOriginalColors();
        }
    }

    private void SetTargetColor(Color color)
    {
        currentColor = color;

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

        ApplyColorToRenderers(color);
    }

    private void ApplyColorToRenderers(Color color)
    {
        if (renderers == null) return;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null) continue;

            r.GetPropertyBlock(propertyBlock);

            if (rendererHasBaseColor[i]) propertyBlock.SetColor(BaseColorId, color);
            if (rendererHasColor[i]) propertyBlock.SetColor(ColorId, color);

            r.SetPropertyBlock(propertyBlock);
        }
    }

    private void RestoreOriginalColors()
    {
        currentColor = originalColor;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
            return;
        }

        if (uiGraphic != null)
        {
            uiGraphic.color = originalColor;
            return;
        }

        if (renderers == null) return;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null) continue;

            r.GetPropertyBlock(propertyBlock);

            if (rendererHasBaseColor[i]) propertyBlock.SetColor(BaseColorId, rendererOriginalColors[i]);
            if (rendererHasColor[i]) propertyBlock.SetColor(ColorId, rendererOriginalColors[i]);

            r.SetPropertyBlock(propertyBlock);
        }
    }
}
