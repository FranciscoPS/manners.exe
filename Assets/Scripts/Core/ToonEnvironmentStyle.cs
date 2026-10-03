using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(fileName = "ToonEnvironmentStyle", menuName = "Game/Estilo toon del entorno")]
public sealed class ToonEnvironmentStyle : ScriptableObject
{
    public const string ResourceName = "ToonEnvironmentStyle";
    public const string ShaderName = "Manners/Toon Environment";
    public const string CharacterShaderName = "Manners/Toon Character";
    public const string TerrainShaderName = "Manners/Toon Terrain";
    public const string GroundShadowShaderName = "Manners/Toon Ground Shadow";
    public const string OutlinePassLightMode = "ToonOutline";
    public const string GroundShadowPassLightMode = "ToonGroundShadow";
    public const string RimKeyword = "_TOON_RIM";
    public const string TerrainDetailKeyword = "_TOON_TERRAIN_DETAIL";
    public const int MaxTerrainLayers = 8;

    public static readonly int OutlineDisabledId = Shader.PropertyToID("_ToonOutlineDisabled");
    public static readonly int GroundHeightId = Shader.PropertyToID("_ToonGroundHeight");
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
    public static readonly int RimColorId = Shader.PropertyToID("_RimColor");
    public static readonly int RimThresholdId = Shader.PropertyToID("_RimThreshold");
    public static readonly int RimSoftnessId = Shader.PropertyToID("_RimSoftness");
    public static readonly int GroundShadowColorId = Shader.PropertyToID("_GroundShadowColor");
    public static readonly int StencilRefId = Shader.PropertyToID("_StencilRef");
    public static readonly int TerrainControl1Id = Shader.PropertyToID("_Control1");
    public static readonly int InkColorId = Shader.PropertyToID("_InkColor");
    public static readonly int InkWidthId = Shader.PropertyToID("_InkWidth");
    public static readonly int InkFadeStartId = Shader.PropertyToID("_InkFadeStart");
    public static readonly int InkFadeEndId = Shader.PropertyToID("_InkFadeEnd");

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
    [Tooltip("Paleta HDR que se reparte entre los materiales con máscara de ventanas al convertirlos (una por material, en orden). Por debajo del umbral de Bloom las ventanas quedan nítidas; por encima aparece el glow.")]
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

    [Header("Alcance")]
    [Tooltip("Escenas dentro del alcance del estilo toon. Solo se convierten los prefabs, personajes y modelos que estas escenas usan; lo que pertenece a otras escenas (como el Mapa 2 en pausa) no se toca. Añadir aquí una escena la incluye en la conversión, el ajuste de sombras y la validación.")]
    public string[] scenesToScan =
    {
        "Assets/Scenes/MainMenu.unity",
        "Assets/Scenes/Final Levels/LEVEL 1/LEVEL 1.unity",
        "Assets/Scenes/Sandbox.unity",
    };

    [Tooltip("Carpetas de modelos cuyos materiales embebidos en el FBX se extraen y convierten cuando aparecen en las escenas del alcance.")]
    public string[] sceneModelFolders = { "Assets/3DModels/Map" };

    [Tooltip("Materiales del entorno que la conversión gestiona aunque no cuelguen de un prefab de edificios: los extraídos de modelos colocados directamente en las escenas del alcance. Se pueden añadir más a mano.")]
    public List<Material> additionalEnvironmentMaterials = new List<Material>();

    [Header("Terreno (piso y montañas)")]
    [Tooltip("Material del terreno con el shader toon. Guarda el color plano de cada capa, la línea de tinta y la trama. Lo crea y asigna la herramienta 'Configurar terreno toon'.")]
    public Material terrainMaterial;

    [Tooltip("Umbral de sombra propio del terreno. Más alto que el de los edificios para que las laderas suaves también tengan cara en sombra. El suelo plano debe quedar siempre iluminado.")]
    [Range(0f, 1f)] public float terrainShadeThreshold = 0.8f;

    [Tooltip("Grosor en píxeles a 1080p de la línea de tinta que separa las capas del terreno (arena, césped, calle...). 0 = sin línea.")]
    [Range(0f, 8f)] public float terrainInkWidth = 2f;

    [Tooltip("Factor de brillo con el que se calcula el color plano inicial de cada capa a partir de su textura. Solo se usa al crear el material o al añadir capas; después los colores se ajustan en el material.")]
    [Range(0.5f, 2f)] public float terrainPaletteBrightness = 1.3f;

    [Header("Sombras planas en el suelo")]
    [Tooltip("Material único de las sombras proyectadas de los edificios. Lo crea la herramienta 'Generar sombras planas'.")]
    public Material groundShadowMaterial;

    [Tooltip("Color que multiplica el suelo dentro de la sombra. El alfa es la fuerza (1 = sombra completa).")]
    public Color groundShadowColor = new Color(0.55f, 0.6f, 0.78f, 1f);

    [Tooltip("Apagado: 'Ajustar escenas' quita las sombras en tiempo real de la luz direccional, así no se genera el shadow map y las únicas sombras son las planas del estilo. Activarlo solo si algún objeto necesita sombra real; cuesta una pasada extra y una lectura por píxel.")]
    public bool realtimeShadows;

    [Tooltip("Altura del suelo (Y en mundo) sobre la que se aplastan las sombras. El piso de la ciudad está en 0.")]
    public float groundHeight = 0f;

    [Tooltip("Los edificios y props más bajos que esto (metros en el prefab) no generan sombra plana: farolas, basureros y similares no la justifican.")]
    [Min(0f)] public float groundShadowMinHeight = 2f;

    [Tooltip("Carpetas de prefabs de edificios y props a los que se les genera la malla de sombra. Solo se procesan los que usan las escenas del alcance.")]
    public string[] groundShadowPrefabFolders = { "Assets/Prefabs/Buildings", "Assets/Prefabs/Props" };

    [Tooltip("Personajes que proyectan sombra plana (se dibujan una vez más). Por defecto solo el jugador; añadir enemigos aquí duplica sus draw calls.")]
    public List<GameObject> groundShadowCharacterPrefabs = new List<GameObject>();

    [Tooltip("Materiales de pisos planos (no terreno) que reciben las sombras planas, como el piso del sandbox. Se convierten al shader toon sin contorno.")]
    public List<Material> groundReceiverMaterials = new List<Material>();

    [Header("Personajes (jugador y enemigos)")]
    [Tooltip("Carpetas con los prefabs de personajes. Se convierten al shader toon de personajes los que usan las escenas del alcance (el jugador y los enemigos del Mapa 1).")]
    public string[] characterPrefabFolders = { "Assets/Prefabs/Characters" };

    [Tooltip("Color de sombra de los personajes. Algo más oscuro que el del entorno para que destaquen sobre la ciudad.")]
    public Color characterShadeColor = new Color(0.46f, 0.48f, 0.68f, 1f);

    [Tooltip("Grosor del contorno de los personajes en píxeles a 1080p.")]
    [Range(0f, 8f)] public float characterOutlineWidth = 2.5f;

    [Tooltip("Si está apagado, los personajes no dibujan contorno aunque la opción Contornos esté activa. Cada personaje con contorno se dibuja dos veces.")]
    public bool characterOutlines = true;

    [Tooltip("Personajes con luz de borde (un filo de color en las caras que miran de lado a la cámara). Por defecto solo el jugador.")]
    public List<GameObject> rimLightCharacterPrefabs = new List<GameObject>();

    [Tooltip("Color de la luz de borde. El alfa es la fuerza.")]
    public Color rimColor = new Color(0f, 0.66f, 0.95f, 0.65f);

    [Tooltip("Cuánto tiene que girar una cara respecto a la cámara para recibir la luz de borde. Más alto = filo más fino.")]
    [Range(0f, 1f)] public float rimThreshold = 0.8f;

    [Tooltip("Suavidad del filo de la luz de borde.")]
    [Range(0f, 0.5f)] public float rimSoftness = 0.03f;

    [Header("Post-procesado")]
    [Tooltip("Perfil de post-procesado del estilo toon: sin tonemapping (los colores planos se ven tal cual se pintaron) y sin aberración cromática (no deshace las líneas de contorno). 'Ajustar escenas' lo asigna al Global Volume de las escenas del alcance. El bloom, la viñeta y lo demás se ajustan en este asset.")]
    public VolumeProfile postProcessingProfile;

    [Tooltip("Activa FXAA en la cámara principal de las escenas del alcance al ajustarlas. Suaviza el dentado de los contornos; se apaga solo cuando el jugador desactiva el post-procesado.")]
    public bool antialiasing = true;

    [Header("Materiales gestionados (los llena la herramienta Tools > Manners > Visual toon)")]
    [Tooltip("Materiales del entorno convertidos al shader toon. Los ajustes gráficos usan esta lista para apagar la pasada de contorno en Eco.")]
    public List<Material> environmentMaterials = new List<Material>();

    [Tooltip("Materiales de personajes convertidos al shader toon de personajes.")]
    public List<Material> characterMaterials = new List<Material>();

    [Tooltip("Materiales de personajes que proyectan sombra plana (los de 'Ground Shadow Character Prefabs').")]
    public List<Material> groundShadowCasterMaterials = new List<Material>();

    [Tooltip("Materiales que la herramienta nunca convierte (p. ej. la esfera de rango de la tienda).")]
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
        SetPassEnabled(style.environmentMaterials, OutlinePassLightMode, enabled);
        SetPassEnabled(style.characterMaterials, OutlinePassLightMode, enabled && style.characterOutlines);
    }

    public static void ApplyGroundShadows(bool enabled) => ApplyGroundShadows(enabled, Instance);

    public static void ApplyGroundShadows(bool enabled, ToonEnvironmentStyle style)
    {
        if (style == null) return;
        Shader.SetGlobalFloat(GroundHeightId, style.groundHeight);
        SetPassEnabled(style.groundShadowMaterial, GroundShadowPassLightMode, enabled);
        SetPassEnabled(style.groundShadowCasterMaterials, GroundShadowPassLightMode, enabled);
    }

    public static bool IsGroundShadow(Renderer renderer)
    {
        ToonEnvironmentStyle style = Instance;
        return style != null && style.groundShadowMaterial != null && renderer != null
            && renderer.sharedMaterial == style.groundShadowMaterial;
    }

    private static void SetPassEnabled(List<Material> materials, string lightMode, bool enabled)
    {
        for (int i = 0; i < materials.Count; i++) SetPassEnabled(materials[i], lightMode, enabled);
    }

    private static void SetPassEnabled(Material material, string lightMode, bool enabled)
    {
        if (material != null && material.GetShaderPassEnabled(lightMode) != enabled)
            material.SetShaderPassEnabled(lightMode, enabled);
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
        ApplyOutlineTo(material, outlineWidth);
    }

    public void ApplyCharacterShadingTo(Material material)
    {
        if (material == null) return;
        material.SetColor(ShadeColorId, characterShadeColor);
        material.SetFloat(ShadeThresholdId, shadeThreshold);
        material.SetFloat(ShadeSoftnessId, shadeSoftness);
        material.SetFloat(LightColorInfluenceId, lightColorInfluence);
        material.SetColor(RimColorId, rimColor);
        material.SetFloat(RimThresholdId, rimThreshold);
        material.SetFloat(RimSoftnessId, rimSoftness);
        material.SetColor(GroundShadowColorId, groundShadowColor);
        ApplyOutlineTo(material, characterOutlineWidth);
    }

    public void ApplyTerrainShadingTo(Material material)
    {
        if (material == null) return;
        material.SetColor(ShadeColorId, shadeColor);
        material.SetFloat(ShadeThresholdId, terrainShadeThreshold);
        material.SetFloat(ShadeSoftnessId, Mathf.Min(shadeSoftness, 0.01f));
        material.SetFloat(LightColorInfluenceId, lightColorInfluence);
        material.SetColor(InkColorId, outlineColor);
        material.SetFloat(InkWidthId, terrainInkWidth);
        material.SetFloat(InkFadeStartId, outlineFadeStart);
        material.SetFloat(InkFadeEndId, Mathf.Max(outlineFadeEnd, outlineFadeStart + 0.01f));
    }

    public void ApplyGroundShadowTo(Material material)
    {
        if (material != null) material.SetColor(GroundShadowColorId, groundShadowColor);
    }

    private void ApplyOutlineTo(Material material, float width)
    {
        material.SetColor(OutlineColorId, outlineColor);
        material.SetFloat(OutlineWidthId, width);
        material.SetFloat(OutlineFadeStartId, outlineFadeStart);
        material.SetFloat(OutlineFadeEndId, Mathf.Max(outlineFadeEnd, outlineFadeStart + 0.01f));
    }
}
