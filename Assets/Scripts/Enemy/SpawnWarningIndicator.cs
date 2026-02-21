using UnityEngine;
using DG.Tweening;

public class SpawnWarningIndicator : MonoBehaviour
{
    [Header("Visual Settings")]
    [SerializeField] private float warningDuration = 1f;
    [SerializeField] private float blinkInterval = 0.25f;
    [SerializeField] private Color warningColor = new Color(1f, 0f, 0f, 0.8f);
    [SerializeField] private float indicatorRadius = 2f;
    
    private GameObject circleObject;
    private Renderer circleRenderer;
    private Sequence blinkSequence;

    private void Awake()
    {
        CreateCircleIndicator();
        gameObject.SetActive(false);
    }

    private void CreateCircleIndicator()
    {
        circleObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        circleObject.transform.SetParent(transform);
        circleObject.transform.localPosition = Vector3.up * 0.05f;
        circleObject.transform.localRotation = Quaternion.identity;
        circleObject.transform.localScale = new Vector3(indicatorRadius * 2f, 0.01f, indicatorRadius * 2f);
        
        Destroy(circleObject.GetComponent<Collider>());
        
        circleRenderer = circleObject.GetComponent<Renderer>();
        
        // Cargar material desde Resources (garantiza inclusión en build)
        Material templateMaterial = Resources.Load<Material>("SpawnWarningMaterial");
        if (templateMaterial != null)
        {
            Material mat = new Material(templateMaterial);
            mat.color = warningColor;
            circleRenderer.material = mat;
            Debug.Log("[SpawnWarningIndicator] Material loaded from Resources successfully");
        }
        else
        {
            // Fallback: intentar con shader URP Unlit
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlitShader == null)
            {
                unlitShader = Shader.Find("Unlit/Color");
            }
            if (unlitShader == null)
            {
                unlitShader = Shader.Find("Mobile/Unlit (Supports Lightmap)");
            }
            
            if (unlitShader != null)
            {
                Material mat = new Material(unlitShader);
                mat.color = warningColor;
                circleRenderer.material = mat;
            }
            else
            {
                Debug.LogWarning("[SpawnWarningIndicator] No suitable shader found, using default material");
                circleRenderer.material.color = warningColor;
            }
        }
        
        circleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        circleRenderer.receiveShadows = false;
    }

    public void ShowWarning(Vector3 position, float duration = -1f, float radius = -1f)
    {
        if (duration > 0)
        {
            warningDuration = duration;
        }
        
        if (radius > 0)
        {
            indicatorRadius = radius;
            if (circleObject != null)
            {
                circleObject.transform.localScale = new Vector3(indicatorRadius * 2f, 0.01f, indicatorRadius * 2f);
            }
        }
        
        transform.position = position;
        gameObject.SetActive(true);
        
        if (circleObject != null && circleRenderer != null)
        {
            circleObject.SetActive(true);
            StartBlinking();
            DOVirtual.DelayedCall(warningDuration, () => HideWarning());
        }
    }

    private void StartBlinking()
    {
        blinkSequence?.Kill();
        
        blinkSequence = DOTween.Sequence();
        
        blinkSequence.AppendCallback(() => circleObject.SetActive(true));
        blinkSequence.AppendInterval(blinkInterval);
        blinkSequence.AppendCallback(() => circleObject.SetActive(false));
        blinkSequence.AppendInterval(blinkInterval);
        
        blinkSequence.SetLoops(-1);
    }

    public void HideWarning()
    {
        blinkSequence?.Kill();
        
        if (circleObject != null)
        {
            circleObject.SetActive(false);
        }
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        blinkSequence?.Kill();
        
        if (circleObject != null)
        {
            Destroy(circleObject);
        }
    }
}
