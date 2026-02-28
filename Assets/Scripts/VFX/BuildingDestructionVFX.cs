using UnityEngine;

/// <summary>
/// Sistema optimizado de destrucción de edificios usando Particle System con Texture Sheet Animation
/// UNA SOLA partícula con flipbook - igual que ExplosionFlipbook.cs
/// </summary>
public class BuildingDestructionVFX : MonoBehaviour
{
    [Header("Flipbook Settings")]
    [SerializeField] private Texture2D dustTexture;
    [SerializeField] private int columns = 8;
    [SerializeField] private int rows = 8;
    [SerializeField] private float duration = 2f;
    [SerializeField] private float size = 4f;
    [SerializeField] private Color tintColor = new Color(0.8f, 0.7f, 0.6f, 1f);
    
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
        renderer.material = CreateMaterial();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        
        // Iniciar partícula
        ps.Play();
    }
    
    private Material CreateMaterial()
    {
        // Buscar shader URP para partículas
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Particles/Standard Unlit");
        }
        
        if (shader == null)
        {
            Debug.LogError("BuildingDestructionVFX: No se encontró shader de partículas");
            return null;
        }
        
        Material mat = new Material(shader);
        mat.mainTexture = dustTexture;
        
        // Configurar como Alpha Blend para efecto de polvo suave (no additive)
        mat.SetFloat("_Surface", 1); // Transparent
        mat.SetFloat("_BlendOp", 0); // Add
        mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha); // Alpha Blend
        mat.SetFloat("_ZWrite", 0);
        mat.renderQueue = 3000;
        
        return mat;
    }
    
    /// <summary>
    /// Configurar VFX desde Factory (opcional)
    /// </summary>
    public void Initialize(Vector3 position, float scale = 1f)
    {
        transform.position = position;
        transform.localScale = Vector3.one * scale;
    }
}
