using UnityEngine;

public class EmpPulseVisual : MonoBehaviour, IUpdateable
{
    private static readonly int ProgressId = Shader.PropertyToID("_Progress");
    private static readonly int FadeId = Shader.PropertyToID("_Fade");
    private static readonly int ExtentId = Shader.PropertyToID("_Extent");

    [Header("Referencias")]
    [Tooltip("Quad plano con el material 'Custom/EmpPulse' que dibuja la onda expansiva. Su posición local define la altura respecto al origen del jugador.")]
    [SerializeField] private Renderer ring;

    [Header("Onda")]
    [Tooltip("Cuánto sobresale el quad respecto al radio del pulso, para que el halo exterior no se recorte. La onda en sí siempre coincide con el radio de la sinergia.")]
    [SerializeField] private float glowMargin = 1.35f;
    [Tooltip("Tiempo que la onda se mantiene en el radio máximo antes de desvanecerse.")]
    [SerializeField] private float holdDuration = 0.12f;
    [Tooltip("Duración del desvanecimiento final; al terminar, el objeto se destruye solo.")]
    [SerializeField] private float fadeDuration = 0.45f;

    private Transform origin;
    private float expandDuration;
    private float elapsed;
    private bool playing;
    private MaterialPropertyBlock properties;

    public bool IsActive => isActiveAndEnabled;

    public void Play(Transform followTarget, float radius, float duration)
    {
        origin = followTarget;
        expandDuration = Mathf.Max(0.01f, duration);
        elapsed = 0f;
        playing = true;

        float size = radius * 2f * glowMargin;
        ring.transform.localScale = new Vector3(size, size, 1f);

        Follow();
        Apply(0f, 1f);
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
        if (!playing) return;

        elapsed += deltaTime;
        Follow();

        float progress = Mathf.Clamp01(elapsed / expandDuration);
        float fadeStart = expandDuration + holdDuration;
        float fade = elapsed <= fadeStart ? 1f : 1f - Mathf.Clamp01((elapsed - fadeStart) / fadeDuration);
        Apply(progress, fade);

        if (elapsed >= fadeStart + fadeDuration)
        {
            playing = false;
            Destroy(gameObject);
        }
    }

    private void Follow()
    {
        if (origin != null)
            transform.position = origin.position;
    }

    private void Apply(float progress, float fade)
    {
        if (ring == null) return;

        if (properties == null)
            properties = new MaterialPropertyBlock();

        ring.GetPropertyBlock(properties);
        properties.SetFloat(ExtentId, glowMargin);
        properties.SetFloat(ProgressId, progress);
        properties.SetFloat(FadeId, fade);
        ring.SetPropertyBlock(properties);
    }
}
