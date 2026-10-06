using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class PokemonHoloEffect : MonoBehaviour
{
    [Header("Holo Foil")]
    [SerializeField] private float scrollSpeed = 0.35f;
    [SerializeField] private float rainbowRepeats = 1f;
    [Tooltip("Ancho, en unidades de canvas, que ocupa una repetición del arcoíris.")]
    [SerializeField] private float rainbowSpan = 420f;
    [SerializeField] private float diagonalAngleDegrees = 45f;
    [SerializeField] private float saturation = 0.9f;
    [SerializeField] private float sheenIntensity = 0.55f;
    [SerializeField] private float minBrightness = 0.55f;

    private static Shader holoShader;

    private Image image;
    private Material material;
    private float offset;
    private bool playing;

    private void Awake()
    {
        image = GetComponent<Image>();
        image.raycastTarget = false;
        image.color = Color.white;

        Image shape = transform.parent != null ? transform.parent.GetComponent<Image>() : null;
        if (shape != null && shape.sprite != null)
        {
            UIStyle style = UIStyle.Instance;
            image.sprite = style != null && shape.sprite == style.panelDark && style.screenFrame != null ? style.screenFrame : shape.sprite;
            image.type = shape.type;
            image.pixelsPerUnitMultiplier = shape.pixelsPerUnitMultiplier;

            UISkew skew = shape.GetComponent<UISkew>();
            if (skew != null) gameObject.AddComponent<UISkew>().Amount = skew.Amount;
        }

        if (holoShader == null)
            holoShader = Shader.Find("UI/PokemonHolo");

        if (holoShader != null)
        {
            material = new Material(holoShader);
            material.SetFloat("_Angle", diagonalAngleDegrees * Mathf.Deg2Rad);
            material.SetFloat("_Frequency", rainbowRepeats);
            material.SetFloat("_Span", Mathf.Max(1f, rainbowSpan));
            material.SetFloat("_Saturation", saturation);
            material.SetFloat("_Intensity", sheenIntensity);
            material.SetFloat("_MinBrightness", minBrightness);
            image.material = material;
        }

        if (!playing)
            gameObject.SetActive(false);
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
        if (!playing || material == null) return;

        offset += Time.unscaledDeltaTime * scrollSpeed;
        material.SetFloat("_Offset", offset);
    }

    private void OnDestroy()
    {
        if (material != null) Destroy(material);
    }
}
