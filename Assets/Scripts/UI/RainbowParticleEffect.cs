using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class RainbowParticleEffect : MonoBehaviour
{
    [Header("Particle Settings")]
    [SerializeField] private int maxParticles = 50;
    [SerializeField] private float emissionRate = 10f;
    [SerializeField] private float particleSize = 5f;
    [SerializeField] private float particleLifetime = 1f;
    [SerializeField] private float particleSpeed = 20f;

    private ParticleSystem ps;
    private ParticleSystem.MainModule mainModule;
    private ParticleSystem.EmissionModule emissionModule;
    private ParticleSystem.ColorOverLifetimeModule colorModule;
    private ParticleSystem.ShapeModule shapeModule;

    private void Awake()
    {
        SetupParticleSystem();
    }

    private void SetupParticleSystem()
    {
        ps = GetComponent<ParticleSystem>();

        mainModule = ps.main;
        mainModule.loop = true;
        mainModule.startLifetime = particleLifetime;
        mainModule.startSpeed = particleSpeed;
        mainModule.startSize = particleSize;
        mainModule.maxParticles = maxParticles;
        mainModule.simulationSpace = ParticleSystemSimulationSpace.Local;

        emissionModule = ps.emission;
        emissionModule.rateOverTime = emissionRate;

        shapeModule = ps.shape;
        shapeModule.shapeType = ParticleSystemShapeType.Rectangle;
        shapeModule.scale = new Vector3(100, 100, 1);

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.red, 0f),
                new GradientColorKey(Color.yellow, 0.15f),
                new GradientColorKey(Color.green, 0.35f),
                new GradientColorKey(Color.cyan, 0.5f),
                new GradientColorKey(Color.blue, 0.65f),
                new GradientColorKey(Color.magenta, 0.85f),
                new GradientColorKey(Color.red, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            }
        );

        colorModule = ps.colorOverLifetime;
        colorModule.enabled = true;
        colorModule.color = new ParticleSystem.MinMaxGradient(gradient);

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortMode = ParticleSystemSortMode.Distance;
        }
    }
}
