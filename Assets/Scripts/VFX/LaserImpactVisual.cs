using UnityEngine;

public class LaserImpactVisual : MonoBehaviour
{
    private static readonly int FadeId = Shader.PropertyToID("_Fade");

    [Tooltip("Quad plano con el material 'Custom/LaserImpact' apoyado en el piso: brasa central, ondas de choque, chispas y quemadura alrededor del punto de impacto.")]
    [SerializeField] private Renderer groundRing;
    [Tooltip("Tamaño del quad relativo al radio de daño ('Impact Radius' del config). 2 = el quad mide exactamente el diámetro de daño; más grande deja espacio a las chispas y al halo.")]
    [SerializeField] private float sizePerImpactRadius = 3.2f;

    private MaterialPropertyBlock properties;

    public void Configure(float impactRadius)
    {
        if (groundRing == null) return;

        float size = Mathf.Max(0.01f, impactRadius * sizePerImpactRadius);
        groundRing.transform.localScale = new Vector3(size, size, 1f);
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    public void Follow(Vector3 groundPoint)
    {
        transform.position = groundPoint;
    }

    public void SetIntensity(float intensity)
    {
        if (groundRing == null) return;

        if (properties == null)
            properties = new MaterialPropertyBlock();

        groundRing.GetPropertyBlock(properties);
        properties.SetFloat(FadeId, Mathf.Clamp01(intensity));
        groundRing.SetPropertyBlock(properties);
    }
}
