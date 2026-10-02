using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ToonEnvironmentStyle", menuName = "Game/Estilo toon del entorno")]
public sealed class ToonEnvironmentStyle : ScriptableObject
{
    public const string ResourceName = "ToonEnvironmentStyle";
    public const string ShaderName = "Manners/Toon Environment";
    public const string OutlinePassLightMode = "ToonOutline";

    public static readonly int OutlineDisabledId = Shader.PropertyToID("_ToonOutlineDisabled");
    public static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    public static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    public static readonly int ShadeColorId = Shader.PropertyToID("_ShadeColor");
    public static readonly int ShadeThresholdId = Shader.PropertyToID("_ShadeThreshold");
    public static readonly int ShadeSoftnessId = Shader.PropertyToID("_ShadeSoftness");
    public static readonly int LightColorInfluenceId = Shader.PropertyToID("_LightColorInfluence");
    public static readonly int EmissionMapId = Shader.PropertyToID("_EmissionMap");
    public static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    public static readonly int EmissionBaseTintId = Shader.PropertyToID("_EmissionBaseTint");
    public static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
    public static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");
    public static readonly int OutlineFadeStartId = Shader.PropertyToID("_OutlineFadeStart");
    public static readonly int OutlineFadeEndId = Shader.PropertyToID("_OutlineFadeEnd");

    private static ToonEnvironmentStyle instance;

    [Header("Sombra toon (valores base para todos los materiales gestionados)")]
    [Tooltip("Color que multiplica la textura en las caras que no miran a la luz direccional. Azulado y medio = look Denshattack. Se aplica a los materiales con 'Aplicar estilo'.")]
    public Color shadeColor = new Color(0.55f, 0.6f, 0.78f, 1f);

    [Tooltip("Dónde corta la sombra respecto a la luz. 0.5 = justo en el terminador (caras de espaldas a la luz en sombra). Más bajo = menos sombra.")]
    [Range(0f, 1f)] public float shadeThreshold = 0.5f;

    [Tooltip("Suavidad del borde entre luz y sombra. 0 = corte duro de cel shading.")]
    [Range(0f, 0.5f)] public float shadeSoftness = 0.02f;

    [Tooltip("0 = los colores de la textura se muestran tal cual (como en Substance). 1 = el color e intensidad de la luz direccional tiñen la cara iluminada.")]
    [Range(0f, 1f)] public float lightColorInfluence = 0f;

    [Header("Contorno (inverted hull, grosor constante en pantalla)")]
    [Tooltip("Color de la línea de contorno.")]
    public Color outlineColor = new Color(0.05f, 0.05f, 0.08f, 1f);

    [Tooltip("Grosor en píxeles a 1080p (se escala con la resolución). 0 oculta la línea pero sigue costando una pasada; para quitarla del todo usa la opción Contornos de los ajustes gráficos.")]
    [Range(0f, 8f)] public float outlineWidth = 2f;

    [Tooltip("Distancia a la cámara (metros) a partir de la cual el contorno empieza a adelgazar, para que el fondo de la ciudad no se llene de líneas.")]
    [Min(0f)] public float outlineFadeStart = 50f;

    [Tooltip("Distancia a la cámara (metros) a la que el contorno desaparece por completo.")]
    [Min(0f)] public float outlineFadeEnd = 110f;

    [Header("Ventanas y luces")]
    [Tooltip("Paleta HDR que se reparte entre los materiales con máscara de ventanas al convertirlos (una por material, en orden). Mantener por debajo del umbral de Bloom (2.2) para ventanas nítidas; por encima se produce el glow de neón.")]
    [ColorUsage(false, true)] public Color[] windowEmissionPalette =
    {
        new Color(1.9f, 1.7f, 1.25f, 1f),
        new Color(1.1f, 1.8f, 2.0f, 1f),
        new Color(2.0f, 1.2f, 1.75f, 1f),
        new Color(1.95f, 1.85f, 1.1f, 1f),
    };

    [Tooltip("Intensidad HDR para los materiales de ventana cuya textura completa emite (los 'Bn-W' de la ciudad vieja). Blanco neutro para respetar los colores pintados en la textura.")]
    [ColorUsage(false, true)] public Color windowTextureEmission = new Color(1.8f, 1.8f, 1.8f, 1f);

    [Tooltip("Fragmentos del nombre del material o de su textura que lo identifican como material de ventana (toda la textura emite).")]
    public string[] windowMaterialMarkers = { "-W", "Window" };

    [Tooltip("Carpetas donde la conversión intenta generar automáticamente una máscara de ventanas (suavidad del MaskMap o azul oscuro de la textura). Fuera de ellas la emisión se añade a mano con la herramienta de máscaras.")]
    public string[] autoEmissionFolders = { "Assets/Materials/Map/Buildings" };

    [Tooltip("Carpetas de materiales del mapa que la conversión recorre además de los prefabs, para que las variaciones aún no colocadas en escena usen el mismo shader.")]
    public string[] materialFolders = { "Assets/Materials/Map/Buildings", "Assets/Materials/Map/Props" };

    [Header("Materiales gestionados (los llena la herramienta Tools > Manners > Visual toon)")]
    [Tooltip("Materiales del entorno convertidos al shader toon. Los ajustes gráficos usan esta lista para apagar la pasada de contorno en Eco.")]
    public List<Material> environmentMaterials = new List<Material>();

    [Tooltip("Materiales que la herramienta nunca convierte (p. ej. la esfera de rango de la tienda o el suelo).")]
    public List<Material> excludedMaterials = new List<Material>();

    [Tooltip("Prefabs cuyos materiales están embebidos en el FBX: la conversión los extrae a una carpeta Materials junto al modelo antes de convertirlos.")]
    public List<GameObject> prefabsWithEmbeddedMaterials = new List<GameObject>();

    public static ToonEnvironmentStyle Instance
    {
        get
        {
            if (instance == null) instance = Resources.Load<ToonEnvironmentStyle>(ResourceName);
            return instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    public static void ApplyOutlines(bool enabled) => ApplyOutlines(enabled, Instance);

    public static void ApplyOutlines(bool enabled, ToonEnvironmentStyle style)
    {
        Shader.SetGlobalFloat(OutlineDisabledId, enabled ? 0f : 1f);
        if (style == null) return;
        for (int i = 0; i < style.environmentMaterials.Count; i++)
        {
            Material material = style.environmentMaterials[i];
            if (material == null) continue;
            if (material.GetShaderPassEnabled(OutlinePassLightMode) != enabled)
                material.SetShaderPassEnabled(OutlinePassLightMode, enabled);
        }
    }

    public Color WindowEmissionColor(int index)
    {
        if (windowEmissionPalette == null || windowEmissionPalette.Length == 0) return new Color(1.9f, 1.7f, 1.25f, 1f);
        int wrapped = ((index % windowEmissionPalette.Length) + windowEmissionPalette.Length) % windowEmissionPalette.Length;
        return windowEmissionPalette[wrapped];
    }

    public bool IsWindowMaterial(Material material)
    {
        if (material == null || windowMaterialMarkers == null) return false;
        Texture baseMap = material.HasProperty(BaseMapId) ? material.GetTexture(BaseMapId) : null;
        foreach (string marker in windowMaterialMarkers)
        {
            if (string.IsNullOrEmpty(marker)) continue;
            if (material.name.IndexOf(marker, System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (baseMap != null && baseMap.name.IndexOf(marker, System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
        }
        return false;
    }

    public void ApplyShadingTo(Material material)
    {
        if (material == null) return;
        material.SetColor(ShadeColorId, shadeColor);
        material.SetFloat(ShadeThresholdId, shadeThreshold);
        material.SetFloat(ShadeSoftnessId, shadeSoftness);
        material.SetFloat(LightColorInfluenceId, lightColorInfluence);
        material.SetColor(OutlineColorId, outlineColor);
        material.SetFloat(OutlineWidthId, outlineWidth);
        material.SetFloat(OutlineFadeStartId, outlineFadeStart);
        material.SetFloat(OutlineFadeEndId, Mathf.Max(outlineFadeEnd, outlineFadeStart + 0.01f));
    }
}
