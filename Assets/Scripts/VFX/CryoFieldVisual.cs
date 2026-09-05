using UnityEngine;

public class CryoFieldVisual : MonoBehaviour, IUpdateable
{
    private static readonly int RevealId = Shader.PropertyToID("_Reveal");
    private static readonly int ExtentId = Shader.PropertyToID("_Extent");

    [Header("Referencias")]
    [Tooltip("Quad plano con el material 'Custom/CryoField' (escarcha del piso). Su posición local define la altura del piso respecto al origen del jugador.")]
    [SerializeField] private Renderer floor;
    [Tooltip("Esfera con el material 'Custom/CryoDome' (cúpula de niebla helada). Se escala al diámetro del área; su centro debe estar a la altura del piso.")]
    [SerializeField] private Transform dome;
    [Tooltip("Nevada del área. Se escala en proporción al radio para que los copos caigan dentro del área.")]
    [SerializeField] private ParticleSystem snowfall;

    [Header("Escala")]
    [Tooltip("Cuánto sobresale el quad del piso respecto al radio del área, para que el halo del borde no se recorte.")]
    [SerializeField] private float glowMargin = 1.25f;
    [Tooltip("Radio de la cúpula relativo al radio del área (1 = la cúpula termina justo en el borde de la escarcha).")]
    [SerializeField] private float domeRadiusScale = 1f;
    [Tooltip("Radio del área para el que está ajustada la nevada (su Shape). Con otro radio en el config se escala proporcionalmente.")]
    [SerializeField] private float snowfallAuthoredRadius = 10f;

    [Header("Aparición")]
    [Tooltip("Segundos que tarda la escarcha en extenderse desde el jugador hasta el radio completo al activarse la sinergia.")]
    [SerializeField] private float revealDuration = 0.7f;
    [Tooltip("Cuánto se pasa del radio la escarcha antes de asentarse (0.12 = 12%). 0 = sin rebote.")]
    [SerializeField, Range(0f, 0.5f)] private float revealOvershoot = 0.12f;

    private float radius;
    private float elapsed;
    private bool revealing;
    private MaterialPropertyBlock properties;

    public bool IsActive => isActiveAndEnabled;

    public void Play(float areaRadius)
    {
        radius = Mathf.Max(0.01f, areaRadius);
        elapsed = 0f;
        revealing = true;

        if (floor != null)
        {
            float size = radius * 2f * glowMargin;
            floor.transform.localScale = new Vector3(size, size, 1f);
        }

        if (snowfall != null)
            snowfall.transform.localScale = Vector3.one * (radius / Mathf.Max(0.01f, snowfallAuthoredRadius));

        ApplyReveal(0f);
    }

    private void OnEnable()
    {
        UpdateManager.Instance?.Register(this);
    }

    private void OnDisable()
    {
        UpdateManager.Instance?.Unregister(this);
    }

    public void OnUpdate(float deltaTime)
    {
        if (!revealing) return;

        elapsed += deltaTime;
        float t = revealDuration > 0f ? Mathf.Clamp01(elapsed / revealDuration) : 1f;
        ApplyReveal(EaseOutBack(t));

        if (t >= 1f)
        {
            revealing = false;
            ApplyReveal(1f);
        }
    }

    private void ApplyReveal(float reveal)
    {
        if (floor != null)
        {
            if (properties == null)
                properties = new MaterialPropertyBlock();

            floor.GetPropertyBlock(properties);
            properties.SetFloat(ExtentId, glowMargin);
            properties.SetFloat(RevealId, reveal);
            floor.SetPropertyBlock(properties);
        }

        if (dome != null)
            dome.localScale = Vector3.one * (radius * 2f * domeRadiusScale * reveal);
    }

    private float EaseOutBack(float t)
    {
        float s = 1.70158f * (revealOvershoot / 0.1f);
        float u = t - 1f;
        return 1f + u * u * ((s + 1f) * u + s);
    }
}
