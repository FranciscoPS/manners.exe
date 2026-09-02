using System.Collections.Generic;
using DG.Tweening;
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

    private GameObject targetObject;
    private SpriteRenderer spriteRenderer;
    private Graphic uiGraphic;

    private Material[] materialInstances;
    private bool createdMaterialInstances = false;

    private Color[] originalColors;
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
            Renderer[] renderers = includeChildRenderers
                ? targetObject.GetComponentsInChildren<Renderer>(true)
                : new[] { targetObject.GetComponent<Renderer>() };

            List<Material> combined = new List<Material>();
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                combined.AddRange(renderers[i].materials);
            }

            if (combined.Count > 0)
            {
                materialInstances = combined.ToArray();
                createdMaterialInstances = true;
            }
        }

        isInitialized = true;
        CaptureOriginalColor();
    }

    private void CaptureOriginalColor()
    {
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
            originalColors = null;
            return;
        }

        if (uiGraphic != null)
        {
            originalColor = uiGraphic.color;
            originalColors = null;
            return;
        }

        if (materialInstances != null && materialInstances.Length > 0)
        {
            originalColors = new Color[materialInstances.Length];
            for (int i = 0; i < materialInstances.Length; i++)
            {
                Material m = materialInstances[i];
                if (m == null) { originalColors[i] = Color.white; continue; }

                if (m.HasProperty("_BaseColor"))
                    originalColors[i] = m.GetColor("_BaseColor");
                else if (m.HasProperty("_Color"))
                    originalColors[i] = m.GetColor("_Color");
                else
                    originalColors[i] = m.color;
            }

            originalColor = originalColors.Length > 0 ? originalColors[0] : Color.white;
            return;
        }

        originalColors = null;
        originalColor = Color.white;
    }

    public void TweenFx()
    {
        if (!isInitialized) InitializeMaterial();

        if (spriteRenderer == null && uiGraphic == null && (materialInstances == null || materialInstances.Length == 0))
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
                RestoreOriginalColors();
            }
        });
    }

    private Color GetCurrentColor()
    {
        if (spriteRenderer != null) return spriteRenderer.color;
        if (uiGraphic != null) return uiGraphic.color;
        if (materialInstances != null && materialInstances.Length > 0)
        {
            Material m = materialInstances[0];
            if (m != null)
            {
                if (m.HasProperty("_BaseColor"))
                    return m.GetColor("_BaseColor");
                if (m.HasProperty("_Color"))
                    return m.GetColor("_Color");
                return m.color;
            }
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

        if (materialInstances != null && materialInstances.Length > 0)
        {
            for (int i = 0; i < materialInstances.Length; i++)
            {
                Material m = materialInstances[i];
                if (m == null) continue;

                if (m.HasProperty("_BaseColor"))
                    m.SetColor("_BaseColor", color);
                if (m.HasProperty("_Color"))
                    m.SetColor("_Color", color);
                m.color = color;
            }
        }
    }

    private void RestoreOriginalColors()
    {
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

        if (materialInstances != null && originalColors != null)
        {
            int count = Mathf.Min(materialInstances.Length, originalColors.Length);
            for (int i = 0; i < count; i++)
            {
                Material m = materialInstances[i];
                if (m == null) continue;

                Color c = originalColors[i];

                if (m.HasProperty("_BaseColor"))
                    m.SetColor("_BaseColor", c);
                if (m.HasProperty("_Color"))
                    m.SetColor("_Color", c);
                m.color = c;
            }
        }
    }

    private void OnDestroy()
    {
        damageTween?.Kill();

        if (createdMaterialInstances && materialInstances != null && Application.isPlaying)
        {
            for (int i = 0; i < materialInstances.Length; i++)
            {
                if (materialInstances[i] != null)
                {
                    Destroy(materialInstances[i]);
                }
            }
        }
    }
}
