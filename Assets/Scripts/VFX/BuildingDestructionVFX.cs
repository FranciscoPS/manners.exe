using UnityEngine;

public class BuildingDestructionVFX : MonoBehaviour
{
    [Header("Flipbook Settings")]
    [SerializeField] private Texture2D dustTexture;
    [SerializeField] private Material particleMaterial;
    [SerializeField] private int columns = 8;
    [SerializeField] private int rows = 8;
    [SerializeField] private float duration = 2f;
    [SerializeField] private float size = 4f;
    [SerializeField] private Color tintColor = new Color(0.8f, 0.7f, 0.6f, 1f);

    private Material ownedMaterial;


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
        renderer.sharedMaterial = GetOrCreateMaterial();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        ps.Play();
    }

    private Material GetOrCreateMaterial()
    {
        if (ownedMaterial != null) return ownedMaterial;
        return FlipbookMaterialUtility.Resolve(particleMaterial, dustTexture, false, out ownedMaterial);
    }

    private void OnDestroy() => FlipbookMaterialUtility.Release(ref ownedMaterial);

    public void Initialize(Vector3 position, float scale = 1f)
    {
        transform.position = position;
        transform.localScale = Vector3.one * scale;
    }
}
