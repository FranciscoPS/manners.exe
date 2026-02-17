using UnityEngine;
using System.Collections;

public class DamageFlash : MonoBehaviour
{
    [SerializeField] private float flashDuration = 0.1f;
    
    private Renderer enemyRenderer;
    private Material materialInstance;
    private Color originalColor;
    private bool isFlashing = false;
    
    private void Awake()
    {
        enemyRenderer = GetComponent<Renderer>();
        if (enemyRenderer != null)
        {
            materialInstance = enemyRenderer.material;
            originalColor = materialInstance.color;
        }
    }
    
    public void Flash()
    {
        if (!isFlashing && materialInstance != null)
        {
            StartCoroutine(FlashRoutine());
        }
    }
    
    private IEnumerator FlashRoutine()
    {
        isFlashing = true;
        
        // Cambiar a blanco
        materialInstance.color = Color.white;
        if (materialInstance.HasProperty("_BaseColor"))
            materialInstance.SetColor("_BaseColor", Color.white);
        if (materialInstance.HasProperty("_EmissionColor"))
            materialInstance.SetColor("_EmissionColor", Color.white);
        
        yield return new WaitForSeconds(flashDuration);
        
        // Restaurar color original
        materialInstance.color = originalColor;
        if (materialInstance.HasProperty("_BaseColor"))
            materialInstance.SetColor("_BaseColor", originalColor);
        if (materialInstance.HasProperty("_EmissionColor"))
            materialInstance.SetColor("_EmissionColor", originalColor * 0.5f);
        
        isFlashing = false;
    }
    
    private void OnDestroy()
    {
        if (materialInstance != null)
        {
            Destroy(materialInstance);
        }
    }
}
