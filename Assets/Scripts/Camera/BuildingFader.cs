using UnityEngine;

/// <summary>
/// Componente por edificio que permite desvanecerlo cuando algo (enemigo o jugador)
/// queda oculto detrás de él respecto a la cámara.
///
/// Dos modos de funcionamiento:
///  - Modo dither (preferido): si alguno de los materiales del edificio expone la
///    propiedad "_Fade" (p. ej. un shader URP con screen-door dithering), el fade se
///    aplica vía MaterialPropertyBlock. No instancia materiales -> cero GC y conserva
///    el batching.
///  - Modo fallback (por defecto, sin shader especial): instancia los materiales una
///    sola vez (cacheados, sin asignaciones por frame), los pasa a transparente y anima
///    el alpha. Al revelarse por completo restaura los materiales compartidos para
///    reactivar el batching.
/// </summary>
[DisallowMultipleComponent]
public class BuildingFader : MonoBehaviour
{
    [Header("Fade")]
    [Tooltip("Alpha mínimo visible cuando el edificio está totalmente 'oculto'. 0 = invisible, 1 = opaco.")]
    [Range(0f, 1f)]
    [SerializeField] private float minVisibleAlpha = 0.3f;

    [Tooltip("Incluir renderers de los hijos (normalmente sí, el visual cuelga del root).")]
    [SerializeField] private bool affectsChildRenderers = true;

    private static readonly int FadeID = Shader.PropertyToID("_Fade");
    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorID = Shader.PropertyToID("_Color");

    private Renderer[] renderers;
    private Material[][] sharedMatsPerRenderer; // materiales originales compartidos
    private Material[][] fadeMatsPerRenderer;   // instancias transparentes (lazy)
    private bool fadeMatsBuilt = false;
    private bool usingFadeMats = false;

    private bool useFadeProperty = false; // Modo dither
    private MaterialPropertyBlock mpb;

    private float currentFade = 0f; // 0 = visible, 1 = oculto
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

        // Detectar si existe un shader con soporte de "_Fade" (modo dither).
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

    /// <summary>Recalcula el bounding box combinado en espacio de mundo (edificios estáticos: solo una vez).</summary>
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

    /// <summary>Llamado por el manager cada tick de detección.</summary>
    public void SetOccluded(bool occluded)
    {
        if (suspended) return;
        targetFade = occluded ? 1f : 0f;
    }

    /// <summary>Interpolación suave del fade. Lo invoca el manager cada frame mientras NeedsTick.</summary>
    public void Tick(float deltaTime, float speed)
    {
        if (suspended) return;
        currentFade = Mathf.MoveTowards(currentFade, targetFade, speed * deltaTime);
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

        // Modo fallback: instanciar/transparentar materiales.
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

    /// <summary>
    /// Llamado por BuildingsScript al iniciar la secuencia de destrucción: deja de
    /// gestionar el fade y devuelve los materiales compartidos para no interferir.
    /// </summary>
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
