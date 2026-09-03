using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class RadiantAuraVFX : MonoBehaviour
{
    [Header("Rayos")]
    [SerializeField] private int raySegments = 14;
    [SerializeField] private float raySharpness = 3f;
    [SerializeField] private float coreGlowIntensity = 1.1f;
    [SerializeField] private float sizeMultiplier = 2.4f;
    [SerializeField] private float spinSpeedDegPerSec = 20f;
    [SerializeField] private float secondarySpinSpeedDegPerSec = -11f;
    [SerializeField] private float secondarySizeMultiplier = 0.72f;
    [SerializeField] private float secondaryAlphaMultiplier = 0.55f;
    [SerializeField] private float holeRadius = 0f;
    [SerializeField] private float holeSoftness = 0.001f;

    [Header("Pulso")]
    [SerializeField] private float pulseSpeed = 1.5f;
    [SerializeField] private float pulseAmount = 0.14f;

    [Header("Color")]
    [SerializeField] private float colorCycleSpeed = 0.22f;
    [SerializeField] private float colorAlpha = 0.85f;
    [SerializeField] private float colorSaturation = 0.8f;

    public int RaySegments { get => raySegments; set => raySegments = value; }
    public float SizeMultiplier { get => sizeMultiplier; set => sizeMultiplier = value; }
    public float SpinSpeedDegPerSec { get => spinSpeedDegPerSec; set => spinSpeedDegPerSec = value; }
    public float ColorAlpha { get => colorAlpha; set => colorAlpha = value; }
    public float HoleRadius { get => holeRadius; set => holeRadius = value; }
    public float HoleSoftness { get => holeSoftness; set => holeSoftness = value; }

    [System.NonSerialized] public float SpinMultiplier = 1f;
    [System.NonSerialized] public RectTransform TrackTarget;

    private static Texture2D softDotTexture;
    private static Shader sunburstShader;

    private RectTransform rectTransform;
    private RectTransform primaryRay;
    private RectTransform secondaryRay;
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

        Vector2 baseSize = host != null ? host.rect.size : new Vector2(320f, 440f);
        if (baseSize.x < 1f) baseSize.x = 320f;
        if (baseSize.y < 1f) baseSize.y = 440f;

        BuildRayLayer(out secondaryRay, out secondaryMat, "AuraSecondary", baseSize * sizeMultiplier * secondarySizeMultiplier);
        BuildRayLayer(out primaryRay, out primaryMat, "AuraPrimary", baseSize * sizeMultiplier);

        gameObject.SetActive(false);
    }

    private static Shader GetSunburstShader()
    {
        if (sunburstShader == null)
            sunburstShader = Shader.Find("UI/SunburstAura");
        return sunburstShader;
    }

    private void BuildRayLayer(out RectTransform rt, out Material mat, string layerName, Vector2 size)
    {
        GameObject go = new GameObject(layerName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(transform, false);

        rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;

        Image img = go.GetComponent<Image>();
        img.raycastTarget = false;

        Shader shader = GetSunburstShader();
        mat = shader != null ? new Material(shader) : null;

        if (mat != null)
        {
            mat.SetFloat("_RaySegments", raySegments);
            mat.SetFloat("_RaySharpness", raySharpness);
            mat.SetFloat("_CoreIntensity", coreGlowIntensity);
            mat.SetFloat("_HoleRadius", holeRadius);
            mat.SetFloat("_HoleSoftness", Mathf.Max(0.001f, holeSoftness));

            float minDim = Mathf.Max(1f, Mathf.Min(size.x, size.y));
            mat.SetVector("_RectSize", new Vector4(size.x / minDim, size.y / minDim, 0f, 0f));

            img.material = mat;
        }

        img.color = Color.white;
    }

    public void Play()
    {
        playing = true;
        gameObject.SetActive(true);
    }

    public void Stop()
    {
        playing = false;
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!playing) return;

        float dt = Time.unscaledDeltaTime;

        if (TrackTarget != null && rectTransform != null)
        {
            rectTransform.anchorMin = TrackTarget.anchorMin;
            rectTransform.anchorMax = TrackTarget.anchorMax;
            rectTransform.pivot = TrackTarget.pivot;
            rectTransform.anchoredPosition = TrackTarget.anchoredPosition;
            rectTransform.sizeDelta = TrackTarget.sizeDelta;
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
        if (primaryMat != null) Destroy(primaryMat);
        if (secondaryMat != null) Destroy(secondaryMat);
    }

    internal static Texture2D GetOrCreateSoftDotTexture()
    {
        if (softDotTexture != null) return softDotTexture;

        const int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float maxDist = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) / maxDist;
                float alpha = Mathf.Pow(Mathf.Clamp01(1f - dist), 1.8f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        softDotTexture = tex;
        return softDotTexture;
    }
}
