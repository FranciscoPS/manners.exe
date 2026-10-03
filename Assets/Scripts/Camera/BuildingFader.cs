using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class BuildingFader : MonoBehaviour
{
    [Header("Fade")]
    [Tooltip("Incluir renderers de los hijos (normalmente sí, el visual cuelga del root).")]
    [SerializeField] private bool affectsChildRenderers = true;

    [Tooltip("Renderers hijos que no se desvanecen con el edificio ni cuentan para detectar si tapa algo. Por ejemplo, la esfera de rango de la tienda, cuyo material controla ShopVisualFeedback.")]
    [SerializeField] private Renderer[] excludedRenderers = new Renderer[0];

    [Header("Opacidad propia")]
    [Tooltip("Activo: este edificio usa su propia opacidad al tapar al jugador o a enemigos, en vez de la global del BuildingTransparencyManager. Para edificios importantes que no deben perderse de vista, como la tienda.")]
    [SerializeField] private bool useOwnMinVisibleAlpha = false;

    [Tooltip("Opacidad de este edificio cuando tapa algo (solo con 'Use Own Min Visible Alpha' activo). 0 = invisible, 1 = opaco. Se puede ajustar en Play Mode y se ve al instante.")]
    [Range(0f, 1f)]
    [SerializeField] private float ownMinVisibleAlpha = 0.7f;

    private float globalMinVisibleAlpha = 0.6f;

    private static readonly int FadeID = Shader.PropertyToID("_Fade");
    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorID = Shader.PropertyToID("_Color");

    private Renderer[] renderers;
    private Material[][] sharedMatsPerRenderer;
    private Material[][] fadeMatsPerRenderer;
    private readonly Dictionary<Material, Material> fadeMaterials = new Dictionary<Material, Material>();
    private bool fadeMatsBuilt = false;
    private bool usingFadeMats = false;

    private bool useFadeProperty = false;
    private MaterialPropertyBlock mpb;

    private float currentFade = 0f;
    private float targetFade = 0f;
    private bool suspended = false;

    public Bounds WorldBounds { get; private set; }
    public bool NeedsTick => !Mathf.Approximately(currentFade, targetFade);

    private float MinVisibleAlpha => useOwnMinVisibleAlpha ? ownMinVisibleAlpha : globalMinVisibleAlpha;

    private void Awake()
    {
        Renderer[] found = affectsChildRenderers
            ? GetComponentsInChildren<Renderer>(true)
            : GetComponents<Renderer>();
        renderers = System.Array.FindAll(found, r => !ToonEnvironmentStyle.IsGroundShadow(r)
            && System.Array.IndexOf(excludedRenderers, r) < 0);

        sharedMatsPerRenderer = new Material[renderers.Length][];
        for (int i = 0; i < renderers.Length; i++)
        {
            sharedMatsPerRenderer[i] = renderers[i] != null ? renderers[i].sharedMaterials : System.Array.Empty<Material>();
        }

        for (int i = 0; i < renderers.Length && !useFadeProperty; i++)
        {
            var mats = sharedMatsPerRenderer[i];
            for (int j = 0; j < mats.Length; j++)
            {
                if (mats[j] != null && mats[j].HasProperty(FadeID))
                {
                    useFadeProperty = true;
                    break;
                }
            }
        }

        mpb = new MaterialPropertyBlock();
        RecalculateBounds();
    }

    public void RecalculateBounds()
    {
        bool has = false;
        Bounds b = default;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            if (!has) { b = renderers[i].bounds; has = true; }
            else b.Encapsulate(renderers[i].bounds);
        }
        if (!has) b = new Bounds(transform.position, Vector3.one);
        WorldBounds = b;
    }

    private void OnEnable()
    {
        BuildingTransparencyManager.Register(this);
    }

    private void OnDisable()
    {
        BuildingTransparencyManager.Unregister(this);
        RestoreSharedImmediate();
    }

    private void OnDestroy()
    {
        foreach (Material material in fadeMaterials.Values)
        {
            if (material == null) continue;
            if (Application.isPlaying) Destroy(material);
            else DestroyImmediate(material);
        }
        fadeMaterials.Clear();
    }

    public void SetOccluded(bool occluded)
    {
        if (suspended) return;
        targetFade = occluded ? 1f : 0f;
    }

    public void Tick(float deltaTime, float speed, float minAlpha)
    {
        if (suspended) return;
        globalMinVisibleAlpha = minAlpha;
        currentFade = Mathf.MoveTowards(currentFade, targetFade, speed * deltaTime);
        Apply();
    }

    public void ForceApply(float minAlpha)
    {
        if (suspended) return;
        globalMinVisibleAlpha = minAlpha;
        Apply();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying || renderers == null || suspended) return;
        Apply();
    }
#endif

    private void Apply()
    {
        if (useFadeProperty)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r == null) continue;
                r.GetPropertyBlock(mpb);
                mpb.SetFloat(FadeID, currentFade);
                r.SetPropertyBlock(mpb);
            }
            return;
        }

        if (currentFade <= 0.0001f)
        {
            if (usingFadeMats) RestoreShared();
            return;
        }

        if (!usingFadeMats) SwitchToFadeMats();

        float alpha = Mathf.Lerp(1f, MinVisibleAlpha, currentFade);
        foreach (Material material in fadeMaterials.Values) SetMaterialAlpha(material, alpha);
    }

    private void SwitchToFadeMats()
    {
        if (!fadeMatsBuilt) BuildFadeMats();
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && fadeMatsPerRenderer[i] != null)
                renderers[i].sharedMaterials = fadeMatsPerRenderer[i];
        }
        usingFadeMats = true;
    }

    private void BuildFadeMats()
    {
        fadeMatsPerRenderer = new Material[renderers.Length][];
        for (int i = 0; i < renderers.Length; i++)
        {
            var shared = sharedMatsPerRenderer[i];
            var inst = new Material[shared.Length];
            for (int j = 0; j < shared.Length; j++)
            {
                if (shared[j] == null) continue;
                if (!fadeMaterials.TryGetValue(shared[j], out Material material))
                {
                    material = new Material(shared[j]);
                    SetupTransparentMaterial(material);
                    fadeMaterials.Add(shared[j], material);
                }
                inst[j] = material;
            }
            fadeMatsPerRenderer[i] = inst;
        }
        fadeMatsBuilt = true;
    }

    private void RestoreShared()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && sharedMatsPerRenderer[i] != null)
                renderers[i].sharedMaterials = sharedMatsPerRenderer[i];
        }
        usingFadeMats = false;
    }

    private void RestoreSharedImmediate()
    {
        if (usingFadeMats) RestoreShared();
        currentFade = 0f;
        targetFade = 0f;
    }

    public void SuspendForDestruction()
    {
        suspended = true;
        RestoreSharedImmediate();
    }

    private void SetupTransparentMaterial(Material mat)
    {
        if (mat.HasProperty("_Surface"))
        {
            mat.SetFloat("_Surface", 1);
            mat.SetFloat("_Blend", 0);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3000;
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        }
        else if (mat.HasProperty("_Mode"))
        {
            mat.SetFloat("_Mode", 3);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
        }
    }

    private void SetMaterialAlpha(Material mat, float alpha)
    {
        if (mat == null) return;
        if (mat.HasProperty(BaseColorID))
        {
            Color c = mat.GetColor(BaseColorID);
            c.a = alpha;
            mat.SetColor(BaseColorID, c);
        }
        else if (mat.HasProperty(ColorID))
        {
            Color c = mat.GetColor(ColorID);
            c.a = alpha;
            mat.SetColor(ColorID, c);
        }
    }
}
