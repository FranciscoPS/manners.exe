using UnityEngine;

public class ShopVisualFeedback : MonoBehaviour
{
    [Header("Visual References")]
    [SerializeField] private GameObject visualSphere;
    [SerializeField] private Renderer sphereRenderer;
    
    [Header("Blink Settings")]
    [SerializeField] private Color availableColor = new Color(0f, 1f, 0f, 1f);
    [SerializeField] private Color cooldownColor = new Color(1f, 0f, 0f, 1f);
    [SerializeField][Range(0f, 1f)] private float sphereAlpha = 0.3f;
    [SerializeField] private float blinkSpeed = 2f;
    [SerializeField] private float minEmission = 0.5f;
    [SerializeField] private float maxEmission = 2f;
    
    [Header("Managers")]
    [SerializeField] private LevelUpManager levelUpManager;
    
    private Material sphereMaterial;
    private bool isOnCooldown = false;
    private static readonly int EmissionColorProperty = Shader.PropertyToID("_EmissionColor");
    private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");

    private void Start()
    {
        if (levelUpManager == null)
        {
            levelUpManager = FindFirstObjectByType<LevelUpManager>();
        }
        
        if (sphereRenderer == null && visualSphere != null)
        {
            sphereRenderer = visualSphere.GetComponent<Renderer>();
        }
        
        if (sphereRenderer != null)
        {
            sphereMaterial = sphereRenderer.material;
            sphereMaterial.EnableKeyword("_EMISSION");
            
            if (sphereMaterial.HasProperty("_Surface"))
            {
                sphereMaterial.SetFloat("_Surface", 1);
            }
            
            if (sphereMaterial.HasProperty("_Blend"))
            {
                sphereMaterial.SetFloat("_Blend", 0);
            }
            
            sphereMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            sphereMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            sphereMaterial.SetInt("_ZWrite", 0);
            sphereMaterial.DisableKeyword("_ALPHATEST_ON");
            sphereMaterial.EnableKeyword("_ALPHABLEND_ON");
            sphereMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            sphereMaterial.renderQueue = 3000;
        }
        
        UpdateVisualState();
    }

    private void Update()
    {
        if (levelUpManager == null || sphereMaterial == null)
            return;
        
        bool shopAvailable = levelUpManager.IsShopAvailable();
        
        if (!shopAvailable)
        {
            isOnCooldown = true;
            BlinkSphere();
        }
        else
        {
            if (isOnCooldown)
            {
                isOnCooldown = false;
                SetAvailableState();
            }
        }
    }

    private void BlinkSphere()
    {
        float emission = Mathf.Lerp(minEmission, maxEmission, (Mathf.Sin(Time.time * blinkSpeed) + 1f) / 2f);
        Color baseColor = new Color(cooldownColor.r, cooldownColor.g, cooldownColor.b, sphereAlpha);
        Color emissionColor = new Color(cooldownColor.r * emission, cooldownColor.g * emission, cooldownColor.b * emission, 1f);
        
        sphereMaterial.SetColor(EmissionColorProperty, emissionColor);
        sphereMaterial.SetColor(BaseColorProperty, baseColor);
    }

    private void SetAvailableState()
    {
        Color baseColor = new Color(availableColor.r, availableColor.g, availableColor.b, sphereAlpha);
        Color emissionColor = new Color(availableColor.r * maxEmission, availableColor.g * maxEmission, availableColor.b * maxEmission, 1f);
        
        sphereMaterial.SetColor(EmissionColorProperty, emissionColor);
        sphereMaterial.SetColor(BaseColorProperty, baseColor);
    }

    private void UpdateVisualState()
    {
        if (levelUpManager != null && sphereMaterial != null)
        {
            if (levelUpManager.IsShopAvailable())
            {
                SetAvailableState();
            }
            else
            {
                isOnCooldown = true;
            }
        }
    }

    private void OnDestroy()
    {
        if (sphereMaterial != null)
        {
            Destroy(sphereMaterial);
        }
    }
}
