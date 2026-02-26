using UnityEngine;

public class DustBurst : MonoBehaviour
{
    [SerializeField] private float lifetime = 2f;
    [SerializeField] private float finalScaleMultiplier = 1.5f;
    [SerializeField] private float rotationSpeed = 30f;
    private float randomOffset;

    private Material mat;
    private float timer;
    private Vector3 startScale;
    private Vector3 targetScale;

    private static readonly int OpacityID = Shader.PropertyToID("_Opacity");

    private void Awake()
    {
        mat = GetComponentInChildren<Renderer>().material;
        startScale = transform.localScale;
    }

    public void Play(float sizeMultiplier, float duration)
    {
        lifetime = duration;
        timer = 0f;
        randomOffset = Random.Range(-rotationSpeed, rotationSpeed);

        startScale = new Vector3(
            sizeMultiplier * 1.8f,
            sizeMultiplier * 0.6f,
            sizeMultiplier * 1.8f
        );

        targetScale = startScale * finalScaleMultiplier;

        transform.localScale = startScale;

        mat.SetFloat(OpacityID, 1f);
        gameObject.SetActive(true);
    }

    private void Update()
    {
        timer += Time.deltaTime;
        float t = timer / lifetime;

        transform.localScale = Vector3.Lerp(startScale, targetScale, t);

        transform.Rotate(0f, (rotationSpeed + randomOffset) * Time.deltaTime, 0f);

        float fadeStart = 0.7f;
        float opacity = (t < fadeStart) ? 1f : 1f - ((t - fadeStart) / (1f - fadeStart));
        mat.SetFloat(OpacityID, opacity);

        if (t >= 1f)
            gameObject.SetActive(false);
    }
}