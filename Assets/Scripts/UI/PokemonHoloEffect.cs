using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class PokemonHoloEffect : MonoBehaviour
{
    [Header("Holo Foil")]
    [SerializeField] private float scrollSpeed = 0.35f;
    [SerializeField] private float bandFrequency = 3f;
    [SerializeField] private float diagonalAngleDegrees = 45f;
    [SerializeField] private float saturation = 0.9f;
    [SerializeField] private float sheenIntensity = 0.55f;

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

        if (holoShader == null)
            holoShader = Shader.Find("UI/PokemonHolo");

        if (holoShader != null)
        {
            material = new Material(holoShader);
            material.SetFloat("_Angle", diagonalAngleDegrees * Mathf.Deg2Rad);
            material.SetFloat("_Frequency", bandFrequency);
            material.SetFloat("_Saturation", saturation);
            material.SetFloat("_Intensity", sheenIntensity);
            image.material = material;
        }

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
