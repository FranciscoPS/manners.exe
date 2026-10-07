using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class RadiantAuraVFX : MonoBehaviour, IUpdateable
{
    [Header("Rayos")]
    [SerializeField] private float spinSpeedDegPerSec = 20f;
    [SerializeField] private float secondarySpinSpeedDegPerSec = -11f;
    [SerializeField] private float secondaryAlphaMultiplier = 0.55f;

    [Header("Pulso")]
    [SerializeField] private float pulseSpeed = 1.5f;
    [SerializeField] private float pulseAmount = 0.14f;

    [Header("Color")]
    [SerializeField] private float colorCycleSpeed = 0.22f;
    [SerializeField] private float colorAlpha = 0.85f;
    [SerializeField] private float colorSaturation = 0.8f;

    public float SpinSpeedDegPerSec { get => spinSpeedDegPerSec; set => spinSpeedDegPerSec = value; }
    public float ColorAlpha { get => colorAlpha; set => colorAlpha = value; }

    [System.NonSerialized] public float SpinMultiplier = 1f;
    [System.NonSerialized] public RectTransform TrackTarget;

    private RectTransform rectTransform;
    [SerializeField] private RectTransform primaryRay;
    [SerializeField] private RectTransform secondaryRay;
    private Material primaryMat;
    private Material secondaryMat;
    private float hue;
    private bool playing;
    private bool initialized;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        hue = Random.value;
    }

    public void Initialize(RectTransform host)
    {
        if (initialized) return;
        initialized = true;
        rectTransform = GetComponent<RectTransform>();
        if (primaryRay != null) { Image image = primaryRay.GetComponent<Image>(); primaryMat = new Material(image.material); image.material = primaryMat; }
        if (secondaryRay != null) { Image image = secondaryRay.GetComponent<Image>(); secondaryMat = new Material(image.material); image.material = secondaryMat; }
    }

    public void Play()
    {
        Initialize(TrackTarget);
        playing = true;
        gameObject.SetActive(true);
    }

    public void Stop()
    {
        playing = false;
        gameObject.SetActive(false);
    }

    public bool IsActive => isActiveAndEnabled;
    private void OnEnable() => UpdateManager.Instance?.Register(this);
    private void OnDisable() => UpdateManager.Instance?.Unregister(this);
    public void OnUpdate(float deltaTime)
    {
        if (!playing) return;

        float dt = Time.unscaledDeltaTime;

        if (TrackTarget != null && rectTransform != null)
        {
            if (transform.parent == TrackTarget)
            {
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = rectTransform.offsetMax = Vector2.zero;
            }
            else
            {
                rectTransform.anchorMin = TrackTarget.anchorMin;
                rectTransform.anchorMax = TrackTarget.anchorMax;
                rectTransform.pivot = TrackTarget.pivot;
                rectTransform.anchoredPosition = TrackTarget.anchoredPosition;
                rectTransform.sizeDelta = TrackTarget.sizeDelta;
            }
        }

        if (primaryRay != null)
            primaryRay.Rotate(Vector3.forward, spinSpeedDegPerSec * SpinMultiplier * dt);

        if (secondaryRay != null)
            secondaryRay.Rotate(Vector3.forward, secondarySpinSpeedDegPerSec * SpinMultiplier * dt);

        hue += colorCycleSpeed * dt;
        if (hue > 1f) hue -= 1f;

        Color cycled = Color.HSVToRGB(hue, colorSaturation, 1f);

        if (primaryMat != null)
        {
            Color primaryColor = cycled;
            primaryColor.a = colorAlpha;
            primaryMat.SetColor("_RayColor", primaryColor);
        }

        if (secondaryMat != null)
        {
            Color secondaryColor = cycled;
            secondaryColor.a = colorAlpha * secondaryAlphaMultiplier;
            secondaryMat.SetColor("_RayColor", secondaryColor);
        }

        float pulse = 1f + Mathf.Sin(Time.unscaledTime * pulseSpeed * Mathf.PI * 2f) * pulseAmount;
        transform.localScale = Vector3.one * pulse;
    }

    private void OnDestroy()
    {
        if (!Application.isPlaying) return;
        if (primaryMat != null) Destroy(primaryMat);
        if (secondaryMat != null) Destroy(secondaryMat);
    }

}
