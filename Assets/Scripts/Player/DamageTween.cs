using DG.Tweening;
using UnityEngine;

public class DamageTween : MonoBehaviour
{
    [Header("Visual Settings")]
    [SerializeField] private MeshRenderer targetRenderer;
    [SerializeField] private Color damageColor = Color.red;
    [SerializeField] private float tweenTime = 0.3f;
    [SerializeField] private int tweenLoops = 3;
    
    private Material materialInstance;
    private Color originalColor;
    private Tween damageTween;
    private bool isInitialized = false;

    private void Awake()
    {
        InitializeMaterial();
    }
    
    private void OnEnable()
    {
        // Re-capturar el color cuando se activa (para objetos del pool)
        if (targetRenderer != null && materialInstance != null)
        {
            CaptureOriginalColor();
        }
    }
    
    private void OnDisable()
    {
        // Restaurar color original al desactivarse (pooling)
        if (materialInstance != null)
        {
            SetMaterialColor(originalColor);
        }
        
        // Matar el tween si está activo
        damageTween?.Kill();
    }
    
    public void InitializeMaterial()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<MeshRenderer>();
        }
        
        if (targetRenderer != null && !isInitialized)
        {
            // Crear material instance para evitar modificar el material compartido
            materialInstance = targetRenderer.material;
            isInitialized = true;
        }
        
        CaptureOriginalColor();
    }
    
    private void CaptureOriginalColor()
    {
        if (materialInstance == null) return;
        
        // Guardar color original del material
        if (materialInstance.HasProperty("_BaseColor"))
            originalColor = materialInstance.GetColor("_BaseColor");
        else if (materialInstance.HasProperty("_Color"))
            originalColor = materialInstance.GetColor("_Color");
        else
            originalColor = materialInstance.color;
    }

    public void TweenFx()
    {
        if (materialInstance == null || targetRenderer == null)
        {
            return;
        }

        damageTween?.Kill(true);

        float adjustedTweenTime = tweenTime / tweenLoops;

        damageTween = DOTween.To(
            () => originalColor,
            color => SetMaterialColor(color),
            damageColor,
            adjustedTweenTime
        )
        .SetLoops(tweenLoops, LoopType.Yoyo)
        .OnComplete(() => 
        {
            SetMaterialColor(originalColor);
        });
    }
    
    private void SetMaterialColor(Color color)
    {
        if (materialInstance == null) return;
        
        if (materialInstance.HasProperty("_BaseColor"))
            materialInstance.SetColor("_BaseColor", color);
        if (materialInstance.HasProperty("_Color"))
            materialInstance.SetColor("_Color", color);
        
        materialInstance.color = color;
    }
    
    private void OnDestroy()
    {
        damageTween?.Kill();
        
        // Destruir material instance para evitar memory leaks
        if (materialInstance != null && Application.isPlaying)
        {
            Destroy(materialInstance);
        }
    }
}
