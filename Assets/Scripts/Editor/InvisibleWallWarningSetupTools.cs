using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class InvisibleWallWarningSetupTools
{
    internal const string ConfigPath = "Assets/Configurations/Production/Resources/InvisibleWallWarningConfig_Production.asset";
    internal const string PrefabPath = "Assets/Prefabs/VFX/InvisibleWallWarning.prefab";
    private const string MaterialFolder = "Assets/VFX/InvisibleWall";
    private const string HexShaderPath = "Assets/Shaders/WallWarningHex.shader";
    private const string StripeShaderPath = "Assets/Shaders/WarningStripe.shader";
    private const string HexMaterialPath = MaterialFolder + "/WallWarningHex_Mat.mat";
    private const string StripeMaterialPath = MaterialFolder + "/WarningStripe_Mat.mat";
    private const string TextMaterialPath = MaterialFolder + "/CyberpunkCraftpixPixel SDF - Wall Warning.mat";
    private const string FontPath = "Assets/Fonts/CyberpunkCraftpixPixel SDF.asset";
    private const string WallsRootName = "InvisibleWalls";
    private const string ContainerName = "WallWarnings";

    private const float PreviewZoneWidth = 5.5f;
    private const float PreviewZoneHeight = 4f;
    private const float PreviewHeight = 2f;
    private const float PreviewGroundDepth = 1.6f;
    private const float PreviewGroundLift = 0.03f;
    private const float CanvasScale = 0.01f;
    private const float CanvasDepthOffset = 0.02f;
    private const float StripeHeight = 80f;
    private const float StripeInset = 14f;
    private const float StripeTextPadding = 10f;
    private const int StripeMaskSoftness = 40;
    private const float TextSize = 56f;
    private const float TextSpacing = 6f;
    private const float TextGlow = 1.3f;
    private const float ScrollSpeed = 90f;
    private static readonly Color TextColor = new Color(1f, 0.86f, 0.82f, 1f);

    [MenuItem("Tools/Manners/Muros invisibles/1. Crear assets del aviso", false, 35)]
    public static void CreateWarningAssets()
    {
        EditorAssetUtility.EnsureFolder(MaterialFolder);
        EditorAssetUtility.EnsureFolder(Path.GetDirectoryName(PrefabPath).Replace('\\', '/'));

        Material hexMaterial = EnsureMaterial(HexMaterialPath, HexShaderPath);
        Material stripeMaterial = EnsureMaterial(StripeMaterialPath, StripeShaderPath);
        Material textMaterial = EnsureTextMaterial();
        if (hexMaterial == null || stripeMaterial == null || textMaterial == null) return;

        EnsureConfig();
        AssetDatabase.SaveAssets();
        SandboxSetupTools.CopyInvisibleWallWarningConfig();

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
            prefab = SavePrefab(hexMaterial, stripeMaterial, textMaterial);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);

        Debug.Log($"[InvisibleWallWarningSetup] Assets listos: prefab {PrefabPath}, materiales en {MaterialFolder}, config en {ConfigPath} (y su copia del sandbox). Los que ya existían no se tocaron. Siguiente: abre Sandbox.unity y ejecuta 'Tools > Manners > Sandbox > 3', o en otra escena 'Tools > Manners > Muros invisibles > 2'.");
    }

    [MenuItem("Tools/Manners/Muros invisibles/2. Colocar avisos en la escena abierta", false, 36)]
    public static void PlaceWarningsInOpenScene()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("[InvisibleWallWarningSetup] Sal de Play Mode antes de colocar los avisos.");
            return;
        }

        Scene scene = EditorSceneManager.GetActiveScene();
        if (!PlaceWarnings(scene)) return;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    [MenuItem("Tools/Manners/Muros invisibles/Regenerar prefab del aviso (descarta sus ajustes)", false, 37)]
    public static void RebuildPrefab()
    {
        bool confirmed = EditorUtility.DisplayDialog(
            "Regenerar prefab del aviso",
            $"Se reconstruirá {PrefabPath} con los valores por defecto y se perderán los ajustes hechos al prefab (tamaños, textos, colores del TMP). Los materiales y el config no se tocan.\n\nDespués vuelve a ejecutar el paso 2 en cada escena que tenga avisos.",
            "Regenerar", "Cancelar");
        if (!confirmed) return;

        Material hexMaterial = EnsureMaterial(HexMaterialPath, HexShaderPath);
        Material stripeMaterial = EnsureMaterial(StripeMaterialPath, StripeShaderPath);
        Material textMaterial = EnsureTextMaterial();
        if (hexMaterial == null || stripeMaterial == null || textMaterial == null) return;

        EditorAssetUtility.EnsureFolder(Path.GetDirectoryName(PrefabPath).Replace('\\', '/'));
        SavePrefab(hexMaterial, stripeMaterial, textMaterial);
        AssetDatabase.SaveAssets();

        Debug.Log($"[InvisibleWallWarningSetup] Prefab {PrefabPath} regenerado sobre el mismo asset (mismo GUID). Ejecuta el paso 2 en cada escena para reconectar sus instancias.");
    }

    internal static bool PlaceWarnings(Scene scene)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null || prefab.GetComponent<InvisibleWallWarning>() == null)
        {
            Debug.LogError($"[InvisibleWallWarningSetup] No existe {PrefabPath}. Ejecuta primero 'Tools > Manners > Muros invisibles > 1. Crear assets del aviso'.");
            return false;
        }

        Transform wallsRoot = FindWallsRoot(scene);
        if (wallsRoot == null)
        {
            Debug.LogError($"[InvisibleWallWarningSetup] La escena '{scene.name}' no tiene un objeto '{WallsRootName}' con muros (BoxCollider) como hijos directos. Debe estar en MAP > {WallsRootName}.");
            return false;
        }

        List<BoxCollider> walls = new List<BoxCollider>();
        for (int i = 0; i < wallsRoot.childCount; i++)
        {
            BoxCollider box = wallsRoot.GetChild(i).GetComponent<BoxCollider>();
            if (box != null) walls.Add(box);
        }

        Vector3 interiorReference = Vector3.zero;
        for (int i = 0; i < walls.Count; i++)
            interiorReference += walls[i].transform.TransformPoint(walls[i].center);
        if (walls.Count > 0)
            interiorReference /= walls.Count;

        Transform container = wallsRoot.Find(ContainerName);
        if (container == null)
        {
            container = new GameObject(ContainerName).transform;
            container.SetParent(wallsRoot, false);
        }

        Dictionary<BoxCollider, InvisibleWallWarning> byWall = new Dictionary<BoxCollider, InvisibleWallWarning>();
        int removed = 0;
        foreach (InvisibleWallWarning existing in container.GetComponentsInChildren<InvisibleWallWarning>(true))
        {
            BoxCollider wall = existing.Wall;
            if (wall == null || !walls.Contains(wall) || byWall.ContainsKey(wall))
            {
                Object.DestroyImmediate(existing.gameObject);
                removed++;
                continue;
            }

            byWall[wall] = existing;
        }

        int created = 0;

        for (int i = 0; i < walls.Count; i++)
        {
            BoxCollider wall = walls[i];
            if (!byWall.TryGetValue(wall, out InvisibleWallWarning warning))
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, container);
                warning = instance.GetComponent<InvisibleWallWarning>();

                SerializedObject warningSerialized = new SerializedObject(warning);
                warningSerialized.FindProperty("wall").objectReferenceValue = wall;
                warningSerialized.ApplyModifiedPropertiesWithoutUndo();
                created++;
            }

            GameObject warningObject = warning.gameObject;
            warningObject.name = $"Warning_{wall.name}";
            warningObject.SetActive(true);
            warning.transform.SetSiblingIndex(i);
            warning.AlignToWall(PreviewHeight, interiorReference);

            if (PrefabUtility.IsPartOfPrefabInstance(warningObject))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(warningObject);
                PrefabUtility.RecordPrefabInstancePropertyModifications(warning.transform);
            }
        }

        if (wallsRoot.GetComponent<InvisibleWallWarningSystem>() == null)
            wallsRoot.gameObject.AddComponent<InvisibleWallWarningSystem>();
        EditorUtility.SetDirty(wallsRoot.gameObject);

        Debug.Log($"[InvisibleWallWarningSetup] '{scene.name}': {walls.Count} muros con aviso ({created} nuevos, {removed} sobrantes eliminados) en {WallsRootName}/{ContainerName}. El sistema está en '{WallsRootName}' y controla todos los avisos hijos. Los avisos se ven en la escena para ajustarlos y se ocultan al dar Play hasta que el jugador se acerca.");
        return true;
    }

    private static Transform FindWallsRoot(Scene scene)
    {
        Transform fallback = null;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
            {
                if (candidate.name != WallsRootName || !HasWallChild(candidate)) continue;
                if (candidate.parent != null && candidate.parent.name == "MAP") return candidate;
                if (fallback == null) fallback = candidate;
            }
        }

        return fallback;
    }

    private static bool HasWallChild(Transform candidate)
    {
        for (int i = 0; i < candidate.childCount; i++)
        {
            if (candidate.GetChild(i).GetComponent<BoxCollider>() != null) return true;
        }

        return false;
    }

    private static Material EnsureMaterial(string path, string shaderPath)
    {
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
        if (shader == null)
        {
            Debug.LogError($"[InvisibleWallWarningSetup] No se encontró el shader {shaderPath}.");
            return null;
        }

        Material material = new Material(shader) { name = Path.GetFileNameWithoutExtension(path) };
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static Material EnsureTextMaterial()
    {
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(TextMaterialPath);
        if (existing != null) return existing;

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null || font.material == null)
        {
            Debug.LogError($"[InvisibleWallWarningSetup] No se encontró la fuente pixel {FontPath}.");
            return null;
        }

        Material preset = new Material(font.material) { name = Path.GetFileNameWithoutExtension(TextMaterialPath) };
        preset.SetColor("_FaceColor", new Color(TextGlow, TextGlow, TextGlow, 1f));
        preset.SetColor("_OutlineColor", new Color(0.12f, 0f, 0f, 1f));
        preset.SetFloat("_OutlineWidth", 0.1f);
        AssetDatabase.CreateAsset(preset, TextMaterialPath);
        return preset;
    }

    private static InvisibleWallWarningConfig EnsureConfig()
    {
        InvisibleWallWarningConfig config = AssetDatabase.LoadAssetAtPath<InvisibleWallWarningConfig>(ConfigPath);
        if (config != null) return config;

        config = ScriptableObject.CreateInstance<InvisibleWallWarningConfig>();
        AssetDatabase.CreateAsset(config, ConfigPath);
        return config;
    }

    private static GameObject SavePrefab(Material hexMaterial, Material stripeMaterial, Material textMaterial)
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        GameObject root = BuildWarning(hexMaterial, stripeMaterial, textMaterial, font);
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static GameObject BuildWarning(Material hexMaterial, Material stripeMaterial, Material textMaterial, TMP_FontAsset font)
    {
        GameObject root = new GameObject(Path.GetFileNameWithoutExtension(PrefabPath));
        InvisibleWallWarning warning = root.AddComponent<InvisibleWallWarning>();

        GameObject hexObject = new GameObject("HexPanel", typeof(MeshFilter), typeof(MeshRenderer));
        hexObject.transform.SetParent(root.transform, false);
        hexObject.transform.localScale = new Vector3(PreviewZoneWidth, PreviewZoneHeight, 1f);
        hexObject.GetComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");

        MeshRenderer hexRenderer = hexObject.GetComponent<MeshRenderer>();
        hexRenderer.sharedMaterial = hexMaterial;
        hexRenderer.shadowCastingMode = ShadowCastingMode.Off;
        hexRenderer.receiveShadows = false;
        hexRenderer.lightProbeUsage = LightProbeUsage.Off;
        hexRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        hexRenderer.sortingOrder = 1;

        GameObject groundObject = new GameObject("GroundBand", typeof(MeshFilter), typeof(MeshRenderer));
        groundObject.transform.SetParent(root.transform, false);
        groundObject.transform.localPosition = new Vector3(0f, -PreviewHeight + PreviewGroundLift, -PreviewGroundDepth * 0.5f);
        groundObject.transform.localRotation = Quaternion.LookRotation(Vector3.down, Vector3.back);
        groundObject.transform.localScale = new Vector3(PreviewZoneWidth, PreviewGroundDepth, 1f);
        groundObject.GetComponent<MeshFilter>().sharedMesh = hexObject.GetComponent<MeshFilter>().sharedMesh;

        MeshRenderer groundRenderer = groundObject.GetComponent<MeshRenderer>();
        groundRenderer.sharedMaterial = hexMaterial;
        groundRenderer.shadowCastingMode = ShadowCastingMode.Off;
        groundRenderer.receiveShadows = false;
        groundRenderer.lightProbeUsage = LightProbeUsage.Off;
        groundRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        groundRenderer.sortingOrder = 1;

        GameObject canvasObject = new GameObject("Stripes", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
        canvasObject.transform.SetParent(root.transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 2;

        RectTransform canvasRect = (RectTransform)canvasObject.transform;
        canvasRect.pivot = new Vector2(0.5f, 0.5f);
        canvasRect.localPosition = new Vector3(0f, 0f, -CanvasDepthOffset);
        canvasRect.localRotation = Quaternion.identity;
        canvasRect.localScale = Vector3.one * CanvasScale;
        canvasRect.sizeDelta = new Vector2(PreviewZoneWidth / CanvasScale, PreviewZoneHeight / CanvasScale);

        CanvasGroup group = canvasObject.GetComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;

        WarningMarquee top = BuildStripe(canvasRect, "TopStripe", true, ScrollSpeed, stripeMaterial, textMaterial, font);
        WarningMarquee bottom = BuildStripe(canvasRect, "BottomStripe", false, -ScrollSpeed, stripeMaterial, textMaterial, font);

        SerializedObject warningSerialized = new SerializedObject(warning);
        warningSerialized.FindProperty("hexPanel").objectReferenceValue = hexRenderer;
        warningSerialized.FindProperty("groundBand").objectReferenceValue = groundRenderer;
        warningSerialized.FindProperty("stripesCanvas").objectReferenceValue = canvasRect;
        warningSerialized.FindProperty("stripesGroup").objectReferenceValue = group;
        SerializedProperty marquees = warningSerialized.FindProperty("marquees");
        marquees.arraySize = 2;
        marquees.GetArrayElementAtIndex(0).objectReferenceValue = top;
        marquees.GetArrayElementAtIndex(1).objectReferenceValue = bottom;
        warningSerialized.ApplyModifiedPropertiesWithoutUndo();

        return root;
    }

    private static WarningMarquee BuildStripe(RectTransform canvas, string name, bool top, float scrollSpeed,
        Material stripeMaterial, Material textMaterial, TMP_FontAsset font)
    {
        GameObject stripe = new GameObject(name, typeof(RectTransform));
        RectTransform stripeRect = (RectTransform)stripe.transform;
        stripeRect.SetParent(canvas, false);
        float edge = top ? 1f : 0f;
        stripeRect.anchorMin = new Vector2(0f, edge);
        stripeRect.anchorMax = new Vector2(1f, edge);
        stripeRect.pivot = new Vector2(0.5f, edge);
        stripeRect.sizeDelta = new Vector2(0f, StripeHeight);
        stripeRect.anchoredPosition = new Vector2(0f, top ? -StripeInset : StripeInset);

        GameObject band = new GameObject("Band", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        Stretch((RectTransform)band.transform, stripeRect, 0f);
        Image bandImage = band.GetComponent<Image>();
        bandImage.material = stripeMaterial;
        bandImage.color = Color.white;
        bandImage.raycastTarget = false;

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        Stretch((RectTransform)viewport.transform, stripeRect, StripeTextPadding);
        viewport.GetComponent<RectMask2D>().softness = new Vector2Int(StripeMaskSoftness, 0);

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform textRect = (RectTransform)textObject.transform;
        textRect.SetParent(viewport.transform, false);
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(0f, 1f);
        textRect.pivot = new Vector2(0f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = new Vector2(PreviewZoneWidth / CanvasScale * 4f, 0f);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.fontSharedMaterial = textMaterial;
        text.fontSize = TextSize;
        text.color = TextColor;
        text.characterSpacing = TextSpacing;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;

        WarningMarquee marquee = stripe.AddComponent<WarningMarquee>();
        SerializedObject marqueeSerialized = new SerializedObject(marquee);
        marqueeSerialized.FindProperty("label").objectReferenceValue = text;
        marqueeSerialized.FindProperty("scrollSpeed").floatValue = scrollSpeed;
        marqueeSerialized.ApplyModifiedPropertiesWithoutUndo();
        marquee.ApplyMessage();

        return marquee;
    }

    private static void Stretch(RectTransform rect, RectTransform parent, float horizontalPadding)
    {
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(horizontalPadding, 0f);
        rect.offsetMax = new Vector2(-horizontalPadding, 0f);
    }
}
