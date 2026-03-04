using UnityEngine;

public class ExplosionFlipbook : MonoBehaviour
{
    [Header("Flipbook Settings")]
    [SerializeField] private Texture2D explosionTexture;
    [SerializeField] private Material particleMaterial;
    [SerializeField] private int columns = 5;
    [SerializeField] private int rows = 5;
    [SerializeField] private float duration = 0.8f;
    [SerializeField] private float size = 2f;
    [SerializeField] private Color tintColor = Color.white;

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

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

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

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 1) });

        var shape = ps.shape;
        shape.enabled = false;

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

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = GetOrCreateMaterial();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        ps.Play();
    }

    private Material GetOrCreateMaterial()
    {

        if (particleMaterial != null)
        {

            if (_cachedMaterial == null || _cachedTexture != explosionTexture)
            {
                _cachedMaterial = new Material(particleMaterial);
                _cachedTexture  = explosionTexture;
                if (explosionTexture != null)
                    _cachedMaterial.mainTexture = explosionTexture;
            }
            return _cachedMaterial;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Particles/Standard Unlit");
        }

        if (shader == null)
        {
            return null;
        }

        Material mat = new Material(shader);
        mat.mainTexture = explosionTexture;

        mat.SetFloat("_Surface", 1);
        mat.SetFloat("_BlendOp", 0);
        mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        mat.SetFloat("_ZWrite", 0);
        mat.renderQueue = 3000;

        return mat;
    }
}
