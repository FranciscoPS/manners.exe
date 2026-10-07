using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class PokemonHoloEffect : MonoBehaviour, IUpdateable
{
    [Header("Holo Foil")]
    [SerializeField] private float scrollSpeed = 0.35f;
    private Image image;
    private Material material;
    private float offset;
    private bool playing;

    private void Awake()
    {
        image = GetComponent<Image>();
        if (image.material != null && image.material != Graphic.defaultGraphicMaterial)
        {
            material = new Material(image.material);
            image.material = material;
        }
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

    public bool IsActive => isActiveAndEnabled;
    private void OnEnable() => UpdateManager.Instance?.Register(this);
    private void OnDisable() => UpdateManager.Instance?.Unregister(this);
    public void OnUpdate(float deltaTime)
    {
        if (!playing || material == null) return;

        offset += Time.unscaledDeltaTime * scrollSpeed;
        material.SetFloat("_Offset", offset);
    }

    private void OnDestroy()
    {
        if (!Application.isPlaying) return;
        if (material != null) Destroy(material);
    }
}
