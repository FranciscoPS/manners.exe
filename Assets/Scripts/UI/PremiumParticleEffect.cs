using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(ParticleSystem))]
public class PremiumParticleEffect : MonoBehaviour
{
    [Header("Particle Settings")]
    [SerializeField] private int maxParticles = 30;
    [SerializeField] private float emissionRate = 15f;
    [SerializeField] private float particleSize = 8f;
    [SerializeField] private float particleLifetime = 1.5f;
    [SerializeField] private float particleSpeed = 30f;
    [SerializeField] private bool rainbowGradient = true;

    private ParticleSystem ps;
    private ParticleSystem.MainModule mainModule;
    private ParticleSystem.EmissionModule emissionModule;
    private ParticleSystem.ColorOverLifetimeModule colorModule;
    private ParticleSystem.ShapeModule shapeModule;
    private ParticleSystem.VelocityOverLifetimeModule velocityModule;

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
        mainModule.gravityModifier = 0f;

        emissionModule = ps.emission;
        emissionModule.rateOverTime = emissionRate;

        shapeModule = ps.shape;
        shapeModule.shapeType = ParticleSystemShapeType.Rectangle;
        RectTransform parentRect = transform.parent.GetComponent<RectTransform>();
        if (parentRect != null)
        {
            shapeModule.scale = new Vector3(parentRect.rect.width, parentRect.rect.height, 1);
        }
        else
        {
            shapeModule.scale = new Vector3(200, 80, 1);
        }
        shapeModule.randomDirectionAmount = 0.5f;

        velocityModule = ps.velocityOverLifetime;
        velocityModule.enabled = true;
        velocityModule.space = ParticleSystemSimulationSpace.Local;
        velocityModule.orbitalX = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocityModule.orbitalY = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocityModule.orbitalZ = new ParticleSystem.MinMaxCurve(50f, 100f);
        velocityModule.radial = new ParticleSystem.MinMaxCurve(-20f, 20f);

        if (rainbowGradient)
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(1f, 0f, 0f), 0f),
                    new GradientColorKey(new Color(1f, 0.5f, 0f), 0.15f),
                    new GradientColorKey(new Color(1f, 1f, 0f), 0.3f),
                    new GradientColorKey(new Color(0f, 1f, 0f), 0.45f),
                    new GradientColorKey(new Color(0f, 1f, 1f), 0.6f),
                    new GradientColorKey(new Color(0f, 0f, 1f), 0.75f),
                    new GradientColorKey(new Color(1f, 0f, 1f), 0.9f),
                    new GradientColorKey(new Color(1f, 0f, 0f), 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0.8f, 0f),
                    new GradientAlphaKey(1f, 0.3f),
                    new GradientAlphaKey(1f, 0.7f),
                    new GradientAlphaKey(0f, 1f)
                }
            );

            colorModule = ps.colorOverLifetime;
            colorModule.enabled = true;
            colorModule.color = new ParticleSystem.MinMaxGradient(gradient);
        }

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortMode = ParticleSystemSortMode.Distance;
            renderer.material = new Material(Shader.Find("UI/Default"));
        }
    }

    public void Play()
    {
        if (ps != null)
        {
            ps.Play();
        }
    }

    public void Stop()
    {
        if (ps != null)
        {
            ps.Stop();
        }
    }
}
