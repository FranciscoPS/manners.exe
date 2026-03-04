using UnityEngine;

/// <summary>
/// Sistema optimizado de explosión usando Particle System con Texture Sheet Animation
/// Usado en juegos AAA - GPU accelerated, sin overhead de CPU
/// </summary>
public class ExplosionFlipbook : MonoBehaviour
{
    [Header("Flipbook Settings")]
    [SerializeField] private Texture2D explosionTexture;
    [SerializeField] private Material particleMaterial; // Material pre-configurado (REQUERIDO para builds)
    [SerializeField] private int columns = 5;
    [SerializeField] private int rows = 5;
    [SerializeField] private float duration = 0.8f;
    [SerializeField] private float size = 2f;
    [SerializeField] private Color tintColor = Color.white;
    
    // Material cacheado estáticamente: se crea UNA sola vez para todas las instancias.
    // Antes se creaba `new Material(particleMaterial)` en CADA explosión → alloc de GPU por muerte de enemigo.
    private static Material _cachedMaterial;
    private static Texture2D _cachedTexture;
    
    private void Start()
    {
        CreateParticleSystem();
        Destroy(gameObject, duration + 0.1f);
    }
    
    private void CreateParticleSystem()
    {
        ParticleSystem ps = gameObject.AddComponent<ParticleSystem>();
        
        // Detener el sistema antes de configurarlo
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        
        // Main module
        var main = ps.main;
        main.duration = duration;
        main.loop = false;
        main.startLifetime = duration;
        main.startSpeed = 0;
        main.startSize = size;
        main.startColor = tintColor;
        main.startRotation = 0;
        main.gravityModifier = 0;
        main.playOnAwake = false;
        main.maxParticles = 1;
        
        // Emission (una sola partícula)
        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 1) });
        
        // Shape (deshabilitado)
        var shape = ps.shape;
        shape.enabled = false;
        
        // Texture Sheet Animation (flipbook) - ESTO ES LO CLAVE
        var textureSheet = ps.textureSheetAnimation;
        textureSheet.enabled = true;
        textureSheet.mode = ParticleSystemAnimationMode.Grid;
        textureSheet.numTilesX = columns;
        textureSheet.numTilesY = rows;
        textureSheet.animation = ParticleSystemAnimationType.WholeSheet;
        textureSheet.timeMode = ParticleSystemAnimationTimeMode.Lifetime;
        textureSheet.cycleCount = 1;
        textureSheet.rowMode = ParticleSystemAnimationRowMode.Custom;
        textureSheet.startFrame = 0;
        
        // Renderer
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = GetOrCreateMaterial();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        
        // Iniciar partícula
        ps.Play();
    }
    
    private Material GetOrCreateMaterial()
    {
        // Usar material serializado si existe (requerido para builds)
        if (particleMaterial != null)
        {
            // Reutilizar caché estática si el material base y la textura no cambiaron.
            if (_cachedMaterial == null || _cachedTexture != explosionTexture)
            {
                _cachedMaterial = new Material(particleMaterial);
                _cachedTexture  = explosionTexture;
                if (explosionTexture != null)
                    _cachedMaterial.mainTexture = explosionTexture;
            }
            return _cachedMaterial;
        }
        
        // Fallback: crear material en runtime (solo funciona en Editor)
        Debug.LogWarning("ExplosionFlipbook: No hay material asignado. Esto NO funcionará en builds.");
        
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Particles/Standard Unlit");
        }
        
        if (shader == null)
        {
            Debug.LogError("ExplosionFlipbook: No se encontró shader de partículas");
            return null;
        }
        
        Material mat = new Material(shader);
        mat.mainTexture = explosionTexture;
        
        // Configurar como Additive para efecto de explosión brillante
        mat.SetFloat("_Surface", 1); // Transparent
        mat.SetFloat("_BlendOp", 0); // Add
        mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One); // Additive
        mat.SetFloat("_ZWrite", 0);
        mat.renderQueue = 3000;
        
        return mat;
    }
}
