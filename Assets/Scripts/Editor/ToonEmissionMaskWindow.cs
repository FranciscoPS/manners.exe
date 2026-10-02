using UnityEditor;
using UnityEngine;

public sealed class ToonEmissionMaskWindow : EditorWindow
{
    private Material material;
    private ToonEmissionMaskBuilder.Source source = ToonEmissionMaskBuilder.Source.Automatico;
    private Color keyColor = new Color(0.25f, 0.31f, 0.44f, 1f);
    private float tolerance = 0.16f;
    private float smoothnessThreshold = 0.75f;
    private int size = ToonEmissionMaskBuilder.DefaultMaskSize;
    private Texture2D preview;
    private float previewCoverage;
    private string previewMethod;
    private string message;
    private Vector2 scroll;

    public static void Open()
    {
        var window = GetWindow<ToonEmissionMaskWindow>(true, "Máscara de ventanas (emisión toon)");
        window.minSize = new Vector2(420, 560);
        window.Show();
    }

    private void OnDisable() => ClearPreview();

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.HelpBox("Genera una máscara en blanco y negro con las ventanas que deben verse encendidas y la asigna al material toon. Fuentes: automática (suavidad del MaskMap o azul oscuro de la textura), suavidad del MaskMap de Substance, o un color clave elegido con el cuentagotas sobre la textura base.", MessageType.Info);

        EditorGUI.BeginChangeCheck();
        material = (Material)EditorGUILayout.ObjectField("Material", material, typeof(Material), false);
        if (EditorGUI.EndChangeCheck()) ClearPreview();

        if (material == null)
        {
            EditorGUILayout.EndScrollView();
            return;
        }

        Texture baseMap = material.HasProperty(ToonEnvironmentStyle.BaseMapId) ? material.GetTexture(ToonEnvironmentStyle.BaseMapId) : null;
        Texture maskMap = material.HasProperty("_MetallicGlossMap") ? material.GetTexture("_MetallicGlossMap") : null;
        bool isToon = material.shader != null && material.shader.name == ToonEnvironmentStyle.ShaderName;
        if (!isToon) EditorGUILayout.HelpBox("El material no usa el shader toon. Conviértelo primero con Tools > Manners > Visual toon > 1.", MessageType.Warning);
        if (baseMap == null) EditorGUILayout.HelpBox("El material no tiene textura base.", MessageType.Error);
        else
        {
            EditorGUILayout.LabelField("Textura base", baseMap.name);
            Rect rect = GUILayoutUtility.GetRect(192, 192, GUILayout.ExpandWidth(false));
            EditorGUI.DrawPreviewTexture(rect, baseMap, null, ScaleMode.ScaleToFit);
        }

        source = (ToonEmissionMaskBuilder.Source)EditorGUILayout.EnumPopup("Fuente", source);
        if (source == ToonEmissionMaskBuilder.Source.ColorClaveDelBaseMap)
        {
            keyColor = EditorGUILayout.ColorField(new GUIContent("Color clave (usa el cuentagotas sobre la textura)"), keyColor, true, false, false);
            tolerance = EditorGUILayout.Slider("Tolerancia", tolerance, 0.02f, 0.6f);
        }
        else if (source == ToonEmissionMaskBuilder.Source.SuavidadDelMaskMap)
        {
            if (maskMap == null) EditorGUILayout.HelpBox("El material ya no referencia el MaskMap (se limpia al convertir). Arrastra el MaskMap original a este campo.", MessageType.Info);
            maskMap = (Texture)EditorGUILayout.ObjectField("MaskMap (alfa = suavidad)", maskMap, typeof(Texture), false);
            smoothnessThreshold = EditorGUILayout.Slider("Umbral de suavidad", smoothnessThreshold, 0.3f, 1f);
        }
        size = EditorGUILayout.IntPopup("Resolución de la máscara", size, new[] { "256", "512", "1024", "2048" }, new[] { 256, 512, 1024, 2048 });

        using (new EditorGUI.DisabledScope(baseMap == null))
        {
            if (GUILayout.Button("Previsualizar máscara")) BuildPreview(baseMap, maskMap);
        }

        if (preview != null)
        {
            EditorGUILayout.LabelField("Método", previewMethod);
            EditorGUILayout.LabelField("Cobertura", previewCoverage.ToString("P1"));
            Rect rect = GUILayoutUtility.GetRect(256, 256, GUILayout.ExpandWidth(false));
            EditorGUI.DrawPreviewTexture(rect, preview, null, ScaleMode.ScaleToFit);
            using (new EditorGUI.DisabledScope(!isToon))
            {
                if (GUILayout.Button("Guardar PNG y asignar al material")) SaveAndAssign(baseMap);
            }
        }

        using (new EditorGUI.DisabledScope(!isToon || baseMap == null))
        {
            if (GUILayout.Button("Usar la textura base entera como emisión (material de ventana)"))
            {
                Assign(baseMap);
                message = "La textura base se usa como emisión.";
            }
        }

        if (!string.IsNullOrEmpty(message)) EditorGUILayout.HelpBox(message, MessageType.None);
        EditorGUILayout.EndScrollView();
    }

    private void BuildPreview(Texture baseMap, Texture maskMap)
    {
        ClearPreview();
        ToonEmissionMaskBuilder.Result result;
        if (source == ToonEmissionMaskBuilder.Source.Automatico)
            result = ToonEmissionMaskBuilder.Automatic(baseMap, maskMap, size);
        else if (source == ToonEmissionMaskBuilder.Source.SuavidadDelMaskMap)
        {
            Texture2D readable = ToonEmissionMaskBuilder.LoadReadable(maskMap);
            result = readable == null
                ? new ToonEmissionMaskBuilder.Result { warning = "No se pudo leer el MaskMap (debe ser PNG o JPG)." }
                : ToonEmissionMaskBuilder.FromSmoothness(readable, smoothnessThreshold, size);
            if (readable != null) DestroyImmediate(readable);
        }
        else
        {
            Texture2D readable = ToonEmissionMaskBuilder.LoadReadable(baseMap);
            result = readable == null
                ? new ToonEmissionMaskBuilder.Result { warning = "No se pudo leer la textura base (debe ser PNG o JPG)." }
                : ToonEmissionMaskBuilder.FromColorKey(readable, keyColor, tolerance, size);
            if (readable != null) DestroyImmediate(readable);
        }
        preview = result.mask;
        previewCoverage = result.coverage;
        previewMethod = result.method;
        message = result.warning;
    }

    private void SaveAndAssign(Texture baseMap)
    {
        Texture2D saved = ToonEmissionMaskBuilder.SaveMaskAsset(preview, baseMap);
        if (saved == null)
        {
            message = "No se pudo guardar la máscara.";
            return;
        }
        Assign(saved);
        message = "Máscara guardada en " + AssetDatabase.GetAssetPath(saved) + " y asignada.";
    }

    private void Assign(Texture emissionMap)
    {
        Undo.RecordObject(material, "Asignar emisión toon");
        material.SetTexture(ToonEnvironmentStyle.EmissionMapId, emissionMap);
        material.SetFloat("_EmissionEnabled", 1f);
        material.EnableKeyword("_EMISSION");
        Color current = material.GetColor(ToonEnvironmentStyle.EmissionColorId);
        if (current.maxColorComponent <= 0.01f)
        {
            ToonEnvironmentStyle style = ToonEnvironmentTools.GetOrCreateStyle();
            material.SetColor(ToonEnvironmentStyle.EmissionColorId, style.WindowEmissionColor(0));
        }
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
    }

    private void ClearPreview()
    {
        if (preview != null) DestroyImmediate(preview);
        preview = null;
        previewMethod = null;
        previewCoverage = 0f;
    }
}
