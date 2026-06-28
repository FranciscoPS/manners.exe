using UnityEngine;

[DisallowMultipleComponent]
public class BuildingFader : MonoBehaviour
{
    [Header("Fade")]
    [Tooltip("Incluir renderers de los hijos (normalmente sí, el visual cuelga del root).")]
    [SerializeField] private bool affectsChildRenderers = true;

    private float minVisibleAlpha = 0.6f;

    private static readonly int FadeID = Shader.PropertyToID("_Fade");
    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorID = Shader.PropertyToID("_Color");

    private Renderer[] renderers;
    private Material[][] sharedMatsPerRenderer;
    private Material[][] fadeMatsPerRenderer;
    private bool fadeMatsBuilt = false;
    private bool usingFadeMats = false;

    private bool useFadeProperty = false;
    private MaterialPropertyBlock mpb;

    private float currentFade = 0f;
    private float targetFade = 0f;
    private bool suspended = false;

    public Bounds WorldBounds { get; private set; }
    public bool NeedsTick => !Mathf.Approximately(currentFade, targetFade);

    private void Awake()
    {
        renderers = affectsChildRenderers
            ? GetComponentsInChildren<Renderer>(true)
            : GetComponents<Renderer>();

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
        if (fadeMatsPerRenderer == null) return;
        for (int i = 0; i < fadeMatsPerRenderer.Length; i++)
        {
            var arr = fadeMatsPerRenderer[i];
            if (arr == null) continue;
            for (int j = 0; j < arr.Length; j++)
                if (arr[j] != null) Destroy(arr[j]);
        }
    }

    public void SetOccluded(bool occluded)
    {
        if (suspended) return;
        targetFade = occluded ? 1f : 0f;
    }

    public void Tick(float deltaTime, float speed, float minAlpha)
    {
        if (suspended) return;
        minVisibleAlpha = minAlpha;
        currentFade = Mathf.MoveTowards(currentFade, targetFade, speed * deltaTime);
        Apply();
    }

    public void ForceApply(float minAlpha)
    {
        if (suspended) return;
        minVisibleAlpha = minAlpha;
        Apply();
    }

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

        float alpha = Mathf.Lerp(1f, minVisibleAlpha, currentFade);
        for (int i = 0; i < fadeMatsPerRenderer.Length; i++)
        {
            var mats = fadeMatsPerRenderer[i];
            if (mats == null) continue;
            for (int j = 0; j < mats.Length; j++)
                SetMaterialAlpha(mats[j], alpha);
        }
    }

    private void SwitchToFadeMats()
    {
        if (!fadeMatsBuilt) BuildFadeMats();
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && fadeMatsPerRenderer[i] != null)
                renderers[i].materials = fadeMatsPerRenderer[i];
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
                inst[j] = shared[j] != null ? new Material(shared[j]) : null;
                if (inst[j] != null) SetupTransparentMaterial(inst[j]);
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
