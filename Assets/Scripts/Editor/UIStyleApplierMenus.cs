using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static partial class UIStyleApplier
{
    public const string OptionsMenuPrefabPath = "Assets/Prefabs/UI/Settings/OptionsMenu.prefab";
    public const string AccessibilityPanelPrefabPath = "Assets/Prefabs/UI/Settings/AccessibilitySettingsPanel.prefab";
    public const string MenusReportPath = "Logs/ui-menus-report.txt";

    private const string GraphicsPanelPrefabPath = "Assets/Prefabs/UI/Settings/GraphicsSettingsPanel.prefab";
    private const string AccessibilityPanelName = "AccessibilitySettingsPanel";
    private const string AccessibilitySliderName = "Slider";
    private const float AccessibilityControlsTop = 0.57f;
    private const float AccessibilityValueSplit = 0.72f;
    private const string PauseTagline = "The calm before the storm";
    private const string OptionsMenuName = "OptionsMenu";
    private const float OptionsColumnHeight = 680f;
    private const float OptionsColumnPadding = 50f;
    private const float OptionsColumnSpacing = 24f;
    private const float HelpColumnHeight = 530f;
    private const float HelpColumnPadding = 70f;
    private const float HelpColumnSpacing = 35f;
    private const float OverridesBackHeight = 150f;
    private const float OverridesBackY = -294f;
    private const float PauseColumnHeight = 600f;
    private const float PauseColumnY = -95f;
    private const float PauseColumnPadding = 60f;
    private const float PauseColumnSpacing = 20f;

    private static readonly string[] OptionsScreens = { "OptionsPanel", "AudioPanel", "ControlesPanel", "AyudaPanel", "SobrecargasMenuPanel", "GraphicsSettingsPanel", AccessibilityPanelName };

    private static readonly string[] AccessibilityRowNames =
    {
        "Perfil de cámara", "Paneos de cámara", "Movimiento de cámara", "Zooms automáticos", "Sacudidas por impactos",
        "Fijar cámara al robot", "Zona muerta", "Velocidad de seguimiento", "Distancia de cámara",
    };

    private static readonly string[,] AccessibilityLabels =
    {
        { "Camera profile", "Perfil de cámara" },
        { "Camera pans", "Paneos de cámara" },
        { "Camera sway", "Movimiento de cámara" },
        { "Automatic zooms", "Zooms automáticos" },
        { "Impact shake", "Sacudidas por impactos" },
        { "Lock camera on robot", "Fijar cámara al robot" },
        { "Dead zone", "Zona muerta" },
        { "Follow speed", "Velocidad de seguimiento" },
        { "Camera distance", "Distancia de cámara" },
    };

    private static readonly string[,] AccessibilityHints =
    {
        { "Helicopter: every camera effect. Gentle: no pans or sway. Fixed: the camera stays still on the robot.", "Helicóptero: todos los efectos de cámara. Suave: sin paneos ni movimiento. Fija: la cámara queda quieta sobre el robot." },
        { "The helicopter circles the robot from side to side.", "El helicóptero rodea al robot de lado a lado." },
        { "Gentle floating and altitude changes, like a handheld camera.", "Vaivén suave y cambios de altura, como una cámara en mano." },
        { "The camera zooms in on big moments and pulls back when crowds grow.", "La cámara se acerca en momentos clave y se aleja cuando crecen las hordas." },
        { "Screen shakes from explosions, hits and chests.", "Sacudidas de pantalla por explosiones, golpes y cofres." },
        { "The robot always stays in the center of the screen, with no delay.", "El robot siempre queda al centro de la pantalla, sin retraso." },
        { "The robot can move a little before the camera starts to follow it.", "El robot puede moverse un poco antes de que la cámara lo siga." },
        { "How quickly the camera catches up with the robot.", "Qué tan rápido alcanza la cámara al robot." },
        { "How close or far the camera is from the robot. Zooms scale with it.", "Qué tan cerca o lejos está la cámara del robot. Los zooms se ajustan a esa distancia." },
    };
    private static readonly string[] HelpSectionNames = { "MovimientoPanel", "ExperienciaPanel", "EnemigosPanel", "MejorasPanel" };

    [MenuItem("Tools/Manners/UI/5. Compartir opciones con la pausa (idioma, sobrecargas, HUD y tutorial)", false, 115)]
    public static void ApplyMenusFromMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string original = SceneManager.GetActiveScene().path;
        var menusReport = new StringBuilder();
        ApplyMenus(UIStyleTools.GetOrCreateStyle(), menusReport);
        AssetDatabase.SaveAssets();
        if (!string.IsNullOrEmpty(original) && File.Exists(original))
            EditorSceneManager.OpenScene(original, OpenSceneMode.Single);
        Directory.CreateDirectory("Logs");
        File.WriteAllText(MenusReportPath, menusReport.ToString());
        Debug.Log(menusReport.ToString());
    }

    public static void ApplyMenusBatch()
    {
        var menusReport = new StringBuilder();
        bool passed = true;
        try
        {
            ApplyMenus(UIStyleTools.GetOrCreateStyle(), menusReport);
            AssetDatabase.SaveAssets();
        }
        catch (Exception exception)
        {
            menusReport.AppendLine("EXCEPTION: " + exception);
            passed = false;
        }

        Directory.CreateDirectory("Logs");
        File.WriteAllText(MenusReportPath, menusReport.ToString());
        if (passed) Debug.Log(menusReport.ToString());
        else Debug.LogError(menusReport.ToString());
        if (Application.isBatchMode) EditorApplication.Exit(passed ? 0 : 1);
    }

    public static void ApplyMenus(UIStyle targetStyle, StringBuilder targetReport)
    {
        style = targetStyle;
        report = targetReport;

        if (style.faceShop == null)
        {
            UIStyleSpriteBuilder.BuildFaceShop(style, report);
            AssetDatabase.SaveAssets();
        }

        EnsureOptionsMenuPrefab();
        EnsureAccessibilityPanelPrefab();
        AdjustOptionsMenuAsset();

        foreach (string path in UIAuthoringTools.Scenes)
        {
            if (!File.Exists(path)) continue;
            ApplyMenusToScene(path);
        }

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    private static Transform FindCanvas(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.name == "Canvas" && root.GetComponent<Canvas>() != null) return root.transform;
        return null;
    }

    private static void EnsureOptionsMenuPrefab()
    {
        if (File.Exists(OptionsMenuPrefabPath)) return;

        string menuScene = UIAuthoringTools.Scenes[0];
        Scene scene = EditorSceneManager.OpenScene(menuScene, OpenSceneMode.Single);
        Canvas.ForceUpdateCanvases();
        Transform canvas = FindCanvas(scene);
        Transform options = canvas != null ? canvas.Find("OptionsPanel") : null;
        if (options == null)
        {
            report.AppendLine($"MENÚS: {menuScene} no tiene Canvas/OptionsPanel; el prefab compartido no se creó.");
            return;
        }

        ResetCounters();
        var rootObject = new GameObject(OptionsMenuName, typeof(RectTransform));
        rootObject.layer = canvas.gameObject.layer;
        var root = (RectTransform)rootObject.transform;
        root.SetParent(canvas, false);
        Stretch(root, 0f, 0f, 0f, 0f);
        root.SetSiblingIndex(options.GetSiblingIndex());

        foreach (string name in OptionsScreens)
        {
            Transform screen = canvas.Find(name);
            if (screen != null) screen.SetParent(root, false);
        }

        AdjustOptionsMenuContent(root);
        PrefabUtility.SaveAsPrefabAssetAndConnect(rootObject, OptionsMenuPrefabPath, InteractionMode.AutomatedAction);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        report.AppendLine($"MENÚS: prefab compartido creado en {OptionsMenuPrefabPath} con las pantallas de opciones de {menuScene} ({createdCount} objetos nuevos).");
    }

    private static void AdjustOptionsMenuAsset()
    {
        if (!File.Exists(OptionsMenuPrefabPath)) return;

        ResetCounters();
        GameObject root = PrefabUtility.LoadPrefabContents(OptionsMenuPrefabPath);
        try
        {
            AdjustOptionsMenuContent(root.transform);
            PrefabUtility.SaveAsPrefabAsset(root, OptionsMenuPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        report.AppendLine($"MENÚS {OptionsMenuPrefabPath}: {buttonCount} botones revisados, {createdCount} objetos nuevos.");
    }

    private static void EnsureAccessibilityPanelPrefab()
    {
        bool exists = File.Exists(AccessibilityPanelPrefabPath);
        string source = exists ? AccessibilityPanelPrefabPath : GraphicsPanelPrefabPath;
        if (!File.Exists(source))
        {
            report.AppendLine($"MENÚS: falta {GraphicsPanelPrefabPath}; no se creó la pantalla de accesibilidad.");
            return;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(source);
        GameObject template = exists && File.Exists(GraphicsPanelPrefabPath) ? PrefabUtility.LoadPrefabContents(GraphicsPanelPrefabPath) : null;
        try
        {
            root.name = AccessibilityPanelName;
            BuildAccessibilityContent(root.transform, template != null ? template.transform : root.transform);
            PrefabUtility.SaveAsPrefabAsset(root, AccessibilityPanelPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
            if (template != null) PrefabUtility.UnloadPrefabContents(template);
        }

        report.AppendLine(exists
            ? $"MENÚS {AccessibilityPanelPrefabPath}: pantalla de accesibilidad revisada."
            : $"MENÚS: pantalla de accesibilidad creada en {AccessibilityPanelPrefabPath} a partir de la de gráficos.");
    }

    private static void BuildAccessibilityContent(Transform root, Transform template)
    {
        GraphicsSettingsMenu graphicsMenu = root.GetComponent<GraphicsSettingsMenu>();
        if (graphicsMenu != null) Object.DestroyImmediate(graphicsMenu);
        AccessibilitySettingsMenu menu = GetOrAdd<AccessibilitySettingsMenu>(root.gameObject);

        Transform card = FindDeep(root, "AccessibilityCard");
        if (card == null)
        {
            card = FindDeep(root, "GraphicsCard");
            if (card != null) card.name = "AccessibilityCard";
        }
        Transform controls = card != null ? DirectChild(card, "Controls") : null;
        if (controls == null)
        {
            report.AppendLine("MENÚS: la pantalla de accesibilidad no tiene AccessibilityCard/Controls; no se tocó.");
            return;
        }

        Transform templateCard = FindDeep(template, "GraphicsCard") ?? FindDeep(template, "AccessibilityCard");
        Transform templateControls = templateCard != null ? DirectChild(templateCard, "Controls") : null;

        Remove(DirectChild(card, "DisplayConfirmation"), "accesibilidad: la confirmación de pantalla de gráficos no aplica aquí.");
        SetLabel(DirectChild(controls, "Title"), "Accessibility", "Accesibilidad");
        SetLabel(DirectChild(controls, "Introduction"), "Make the camera comfortable: turn off its movements or lock it on the robot.", "Haz la cámara más cómoda: apaga sus movimientos o fíjala al robot.");

        var rows = new List<Transform>();
        foreach (Transform child in controls)
            if (DirectChild(child, "Label") != null && DirectChild(child, "Value") != null) rows.Add(child);
        rows.Sort((a, b) =>
        {
            var left = (RectTransform)a;
            var right = (RectTransform)b;
            int column = left.anchorMin.x.CompareTo(right.anchorMin.x);
            return column != 0 ? column : right.anchorMin.y.CompareTo(left.anchorMin.y);
        });

        int count = AccessibilitySettingsMenu.RowCount;
        for (int i = rows.Count - 1; i >= count; i--)
        {
            Remove(rows[i], $"accesibilidad: la fila '{rows[i].name}' de gráficos sobra.");
            rows.RemoveAt(i);
        }
        if (rows.Count < count)
        {
            report.AppendLine($"MENÚS: la pantalla de accesibilidad necesita {count} filas y solo tiene {rows.Count}.");
            return;
        }

        var serialized = new SerializedObject(menu);
        SerializedProperty list = serialized.FindProperty("rows");
        list.arraySize = count;
        for (int i = 0; i < count; i++)
        {
            Transform row = rows[i];
            row.name = AccessibilityRowNames[i];
            Touch(row.gameObject);
            SetLabel(DirectChild(row, "Label"), AccessibilityLabels[i, 0], AccessibilityLabels[i, 1]);
            if (i == AccessibilitySettingsMenu.DistanceRow) BuildAccessibilitySliderRow(row);

            Transform previous = DirectChild(row, "Previous");
            Transform next = DirectChild(row, "Next");
            Transform slider = DirectChild(row, AccessibilitySliderName);
            SerializedProperty element = list.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("value").objectReferenceValue = DirectChild(row, "Value").GetComponent<TextMeshProUGUI>();
            element.FindPropertyRelative("previous").objectReferenceValue = previous != null ? previous.GetComponent<Button>() : null;
            element.FindPropertyRelative("next").objectReferenceValue = next != null ? next.GetComponent<Button>() : null;
            element.FindPropertyRelative("slider").objectReferenceValue = slider != null ? slider.GetComponent<Slider>() : null;
            SerializedProperty hint = element.FindPropertyRelative("hint");
            hint.FindPropertyRelative("english").stringValue = AccessibilityHints[i, 0];
            hint.FindPropertyRelative("spanish").stringValue = AccessibilityHints[i, 1];
        }

        Transform hintText = DirectChild(controls, "Hint");
        if (hintText == null)
        {
            hintText = DirectChild(controls, "SyncHint");
            if (hintText != null) hintText.name = "Hint";
        }
        Transform status = DirectChild(controls, "Status");
        Transform reset = DirectChild(controls, "Reset");
        Transform back = DirectChild(controls, "Back");
        Transform templateReset = templateControls != null ? DirectChild(templateControls, "Reset") : null;
        if (reset != null && templateReset != null && templateReset != reset) CopyRect(templateReset, reset);
        Transform apply = EnsureAccessibilityApply(controls, templateControls, reset);
        SetReference(serialized, "hintText", hintText != null ? hintText.GetComponent<TextMeshProUGUI>() : null);
        SetReference(serialized, "statusText", status != null ? status.GetComponent<TextMeshProUGUI>() : null);
        SetReference(serialized, "applyButton", apply != null ? apply.GetComponent<Button>() : null);
        SetReference(serialized, "resetButton", reset != null ? reset.GetComponent<Button>() : null);
        SetReference(serialized, "backButton", back != null ? back.GetComponent<Button>() : null);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        Touch(menu);
    }

    private static Transform EnsureAccessibilityApply(Transform controls, Transform templateControls, Transform reset)
    {
        Transform apply = DirectChild(controls, "Apply");
        Transform source = templateControls != null ? DirectChild(templateControls, "Apply") : null;
        if (apply == null && source != null)
        {
            GameObject copy = Object.Instantiate(source.gameObject, controls, false);
            copy.name = "Apply";
            apply = copy.transform;
            apply.SetSiblingIndex(reset != null ? reset.GetSiblingIndex() + 1 : controls.childCount - 1);
            createdCount++;
            report.AppendLine("MENÚS: accesibilidad recupera el botón Aplicar de la pantalla de gráficos.");
        }
        if (apply == null) return null;

        if (source != null && source != apply) CopyRect(source, apply);
        Button button = apply.GetComponent<Button>();
        if (button != null)
        {
            button.navigation = new Navigation { mode = Navigation.Mode.Explicit };
            Touch(button);
        }
        SetLabel(DirectChild(apply, "Text"), "Apply", "Aplicar");
        return apply;
    }

    private static void BuildAccessibilitySliderRow(Transform row)
    {
        Transform existing = DirectChild(row, AccessibilitySliderName);
        Slider slider = existing != null ? existing.GetComponent<Slider>() : null;
        if (slider == null)
        {
            GameObject created = DefaultControls.CreateSlider(new DefaultControls.Resources());
            created.name = AccessibilitySliderName;
            foreach (Transform part in created.GetComponentsInChildren<Transform>(true)) part.gameObject.layer = row.gameObject.layer;
            created.transform.SetParent(row, false);
            Transform label = DirectChild(row, "Label");
            created.transform.SetSiblingIndex(label != null ? label.GetSiblingIndex() + 1 : 0);
            slider = created.GetComponent<Slider>();
            createdCount++;
            report.AppendLine("MENÚS: accesibilidad cambia los botones de distancia de cámara por un slider.");
        }

        var sliderRect = (RectTransform)slider.transform;
        sliderRect.anchorMin = new Vector2(0f, 0f);
        sliderRect.anchorMax = new Vector2(1f, AccessibilityControlsTop);
        sliderRect.pivot = new Vector2(0.5f, 0.5f);
        sliderRect.offsetMin = Vector2.zero;
        sliderRect.offsetMax = Vector2.zero;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.SetValueWithoutNotify(GameAccessibilitySettings.DefaultCameraDistance);
        StyleSlider(slider);
        Touch(sliderRect);

        Remove(DirectChild(row, "Previous"), "accesibilidad: la distancia de cámara se ajusta con el slider, sin flecha izquierda.");
        Remove(DirectChild(row, "Next"), "accesibilidad: la distancia de cámara se ajusta con el slider, sin flecha derecha.");
        Remove(DirectChild(row, "ValuePlate"), "accesibilidad: la lectura de distancia pasa junto a la etiqueta.");

        if (DirectChild(row, "Label") is RectTransform labelRect)
        {
            labelRect.anchorMax = new Vector2(AccessibilityValueSplit, labelRect.anchorMax.y);
            Touch(labelRect);

            if (DirectChild(row, "Value") is RectTransform valueRect)
            {
                valueRect.anchorMin = new Vector2(AccessibilityValueSplit, labelRect.anchorMin.y);
                valueRect.anchorMax = new Vector2(1f, labelRect.anchorMax.y);
                valueRect.offsetMin = Vector2.zero;
                valueRect.offsetMax = Vector2.zero;
                TMP_Text value = valueRect.GetComponent<TMP_Text>();
                if (value != null)
                {
                    value.alignment = TextAlignmentOptions.Right;
                    value.text = Mathf.RoundToInt(GameAccessibilitySettings.DefaultCameraDistance * 100f) + "%";
                    Touch(value);
                }
                Touch(valueRect);
            }
        }
    }

    private static void CopyRect(Transform source, Transform target)
    {
        if (!(source is RectTransform from) || !(target is RectTransform to)) return;
        to.anchorMin = from.anchorMin;
        to.anchorMax = from.anchorMax;
        to.pivot = from.pivot;
        to.anchoredPosition = from.anchoredPosition;
        to.sizeDelta = from.sizeDelta;
        Touch(to);
    }

    private static Transform EnsureAccessibilityPanelInstance(Transform root, Transform graphicsPanel)
    {
        Transform existing = root.Find(AccessibilityPanelName);
        if (existing != null) return existing;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AccessibilityPanelPrefabPath);
        if (prefab == null) return null;

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
        instance.name = AccessibilityPanelName;
        if (graphicsPanel is RectTransform source && instance.transform is RectTransform target)
        {
            target.anchorMin = source.anchorMin;
            target.anchorMax = source.anchorMax;
            target.pivot = source.pivot;
            target.anchoredPosition = source.anchoredPosition;
            target.sizeDelta = source.sizeDelta;
            instance.transform.SetSiblingIndex(graphicsPanel.GetSiblingIndex() + 1);
        }
        createdCount++;
        report.AppendLine("MENÚS: pantalla de accesibilidad añadida al menú de opciones compartido.");
        return instance.transform;
    }

    private static void SetLabel(Transform target, string english, string spanish)
    {
        TMP_Text text = target != null ? target.GetComponent<TMP_Text>() : null;
        if (text == null) return;

        LocalizedText localized = GetOrAdd<LocalizedText>(target.gameObject);
        var serialized = new SerializedObject(localized);
        SerializedProperty content = serialized.FindProperty("content");
        content.FindPropertyRelative("english").stringValue = english;
        content.FindPropertyRelative("spanish").stringValue = spanish;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        text.text = english;
        Touch(localized);
        Touch(text);
    }

    private static void AdjustOptionsMenuContent(Transform root)
    {
        OptionsMenu menu = GetOrAdd<OptionsMenu>(root.gameObject);
        Transform hub = root.Find("OptionsPanel");
        Transform audio = root.Find("AudioPanel");
        Transform controls = root.Find("ControlesPanel");
        Transform help = root.Find("AyudaPanel");
        Transform overrides = root.Find("SobrecargasMenuPanel");
        Transform graphicsPanel = root.Find("GraphicsSettingsPanel");
        GraphicsSettingsMenu graphics = graphicsPanel != null ? graphicsPanel.GetComponent<GraphicsSettingsMenu>() : null;
        Transform accessibilityPanel = EnsureAccessibilityPanelInstance(root, graphicsPanel);
        AccessibilitySettingsMenu accessibility = accessibilityPanel != null ? accessibilityPanel.GetComponent<AccessibilitySettingsMenu>() : null;

        foreach (Transform screen in new[] { hub, audio, controls, help, overrides })
        {
            if (screen == null) continue;
            Touch(GetOrAdd<UIOverlay>(screen.gameObject));
            Deactivate(screen.gameObject);
        }
        if (graphicsPanel != null) Deactivate(graphicsPanel.gameObject);
        if (accessibilityPanel != null) Deactivate(accessibilityPanel.gameObject);

        GameObject languagePanel = null;
        Transform graphicsButton = null;
        Transform accessibilityButton = null;
        if (hub != null)
        {
            Transform buttons = DirectChild(hub, "ButtonsPanel");
            if (buttons != null)
            {
                Transform audioButton = buttons.Find("Audio");
                Transform controlsButton = buttons.Find("Controles");
                graphicsButton = buttons.Find("GraphicsButton");
                Transform helpButton = buttons.Find("Ayuda");
                Transform backButton = buttons.Find("Regresar");
                Transform template = audioButton != null ? audioButton : FirstButton(buttons);
                accessibilityButton = EnsureMenuButton(buttons, "Accesibilidad", template, "Accessibility", "Accesibilidad", false);
                Transform languageButton = EnsureMenuButton(buttons, "Idioma", template, "Language", "Idioma", false);
                Transform overridesButton = EnsureMenuButton(buttons, "Sobrecargas", template, "Overrides", "Sobrecargas", true);
                Order(audioButton, controlsButton, graphicsButton, accessibilityButton, languageButton, overridesButton, helpButton, backButton);
                Column(buttons, OptionsColumnHeight, OptionsColumnPadding, OptionsColumnSpacing);
                Wire(audioButton, menu.ShowAudio);
                Wire(controlsButton, menu.ShowControls);
                Wire(graphicsButton, menu.ShowGraphics);
                Wire(accessibilityButton, menu.ShowAccessibility);
                Wire(languageButton, menu.ToggleLanguage);
                Wire(overridesButton, menu.ShowOverrides);
                Wire(helpButton, menu.ShowHelp);
                Wire(backButton, menu.Close);
            }

            Transform selector = FindDeep(hub, "LanguageSelector");
            if (selector != null)
            {
                languagePanel = selector.gameObject;
                Deactivate(languagePanel);
            }
        }

        foreach (Transform screen in new[] { audio, controls })
        {
            Transform buttons = screen != null ? DirectChild(screen, "ButtonsPanel") : null;
            if (buttons == null) continue;
            Wire(buttons.Find("Audio"), menu.ShowAudio);
            Wire(buttons.Find("Controles"), menu.ShowControls);
            Wire(buttons.Find("Ayuda"), menu.ShowHelp);
            Wire(buttons.Find("Regresar"), menu.ShowHub);
        }

        var sections = new GameObject[HelpSectionNames.Length];
        if (help != null)
        {
            Transform buttons = DirectChild(help, "ButtonsPanel");
            if (buttons != null)
            {
                Remove(buttons.Find("Sobrecargas"), "el botón de sobrecargas sale de Ayuda; ahora está en Opciones y en la pausa.");
                for (int i = 0; i < HelpSectionNames.Length; i++)
                    Wire(buttons.Find(HelpSectionNames[i].Replace("Panel", string.Empty)), menu.ShowHelpSection, i);
                Wire(buttons.Find("Regresar"), menu.ShowHub);
                Column(buttons, HelpColumnHeight, HelpColumnPadding, HelpColumnSpacing);
            }

            Remove(DirectChild(help, "SobrecargasPanel"), "la copia de la colección de sobrecargas dentro de Ayuda se retira; la colección se abre desde Opciones.");
            for (int i = 0; i < HelpSectionNames.Length; i++)
            {
                Transform section = DirectChild(help, HelpSectionNames[i]);
                sections[i] = section != null ? section.gameObject : null;
            }
        }

        if (overrides != null)
        {
            Transform buttons = DirectChild(overrides, "ButtonsPanel");
            if (buttons != null)
            {
                Transform back = buttons.Find("Regresar");
                Wire(back, menu.Back);
                var rect = (RectTransform)buttons;
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
                rect.pivot = new Vector2(0f, 0.5f);
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, OverridesBackY);
                Column(buttons, OverridesBackHeight, OptionsColumnPadding, 0f);
                CenterLabel(back);
            }
        }

        if (graphics != null && graphicsButton != null)
        {
            var serializedGraphics = new SerializedObject(graphics);
            SetReference(serializedGraphics, "entryButton", graphicsButton.GetComponent<Button>());
            serializedGraphics.ApplyModifiedPropertiesWithoutUndo();
            Touch(graphics);
        }

        if (accessibility != null && accessibilityButton != null)
        {
            var serializedAccessibility = new SerializedObject(accessibility);
            SetReference(serializedAccessibility, "entryButton", accessibilityButton.GetComponent<Button>());
            serializedAccessibility.ApplyModifiedPropertiesWithoutUndo();
            Touch(accessibility);
        }

        var serialized = new SerializedObject(menu);
        SetReference(serialized, "hub", hub != null ? hub.gameObject : null);
        SetReference(serialized, "audioScreen", audio != null ? audio.gameObject : null);
        SetReference(serialized, "controlsScreen", controls != null ? controls.gameObject : null);
        SetReference(serialized, "helpScreen", help != null ? help.gameObject : null);
        SetReference(serialized, "overridesScreen", overrides != null ? overrides.gameObject : null);
        SetReference(serialized, "graphics", graphics);
        SetReference(serialized, "accessibility", accessibility);
        SetReference(serialized, "languagePanel", languagePanel);
        SerializedProperty list = serialized.FindProperty("helpSections");
        list.arraySize = sections.Length;
        for (int i = 0; i < sections.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = sections[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
        Touch(menu);

        foreach (UIScreenIntro intro in root.GetComponentsInChildren<UIScreenIntro>(true))
            AuthorIntroBlocks(intro);
    }

    private static void ApplyMenusToScene(string path)
    {
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        Transform canvas = FindCanvas(scene);
        if (canvas == null)
        {
            report.AppendLine($"MENÚS {path}: sin Canvas; no se tocó.");
            return;
        }

        ResetCounters();
        Canvas.ForceUpdateCanvases();
        bool menuScene = path == UIAuthoringTools.Scenes[0];
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(OptionsMenuPrefabPath);
        Transform menuRoot = canvas.Find(OptionsMenuName);
        if (menuRoot == null && prefab != null)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            menuRoot = instance.transform;
            menuRoot.SetParent(canvas, false);
            Stretch((RectTransform)menuRoot, 0f, 0f, 0f, 0f);
            Transform anchor = menuScene ? canvas.Find("OptionsPanel") : canvas.Find("InitialsEntryUI") ?? canvas.Find("PausePanel");
            int index = anchor == null ? canvas.childCount - 1 : menuScene ? anchor.GetSiblingIndex() : anchor.GetSiblingIndex() + 1;
            menuRoot.SetSiblingIndex(index);
            createdCount++;
            report.AppendLine($"MENÚS {path}: instancia del menú de opciones compartido añadida al Canvas.");
        }

        if (menuScene && menuRoot != null)
        {
            foreach (string name in OptionsScreens)
                Remove(canvas.Find(name), $"{path}: el panel suelto '{name}' se retira; ya vive dentro del menú de opciones compartido.");
        }

        OptionsMenu menu = menuRoot != null ? menuRoot.GetComponent<OptionsMenu>() : null;
        if (menuScene)
        {
            MainMenuUIManager manager = canvas.GetComponent<MainMenuUIManager>();
            if (manager != null)
            {
                var serialized = new SerializedObject(manager);
                SetReference(serialized, "optionsMenu", menu);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Touch(manager);
            }
        }
        else
        {
            PauseMenu pause = canvas.GetComponent<PauseMenu>();
            if (pause != null)
            {
                var serialized = new SerializedObject(pause);
                SetReference(serialized, "optionsMenu", menu);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Touch(pause);

                Transform pausePanel = canvas.Find("PausePanel");
                if (pausePanel != null) AdjustPausePanel(pausePanel, pause);
            }

            Remove(canvas.Find("AudioPanel"), $"{path}: el panel de audio propio de la escena se retira; lo sustituye el del menú de opciones compartido.");
            Remove(canvas.Find("GraphicsSettingsPanel"), $"{path}: la instancia suelta de gráficos se retira; va dentro del menú de opciones compartido.");
            FixTutorialScrim(scene);

            Transform health = canvas.Find("HealthBarContainer");
            if (health != null)
            {
                StyleHealthBar((RectTransform)health);
                KeepOverridePanelBelowHealth(canvas, (RectTransform)health);
            }

            Transform timerPlate = canvas.Find(StylePrefix + "TimerPlate");
            Transform timer = timerPlate != null ? timerPlate.Find("Timer") : canvas.Find("Timer");
            if (timer != null) StyleTimer((RectTransform)timer);

            Transform shop = canvas.Find("ShopText");
            if (shop != null)
            {
                Touch(GetOrAdd<CanvasGroup>(shop.gameObject));
                Touch(GetOrAdd<UIHideUnderOverlay>(shop.gameObject));
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        report.AppendLine($"MENÚS {path}: listo ({createdCount} objetos nuevos).");
    }

    private static void AdjustPausePanel(Transform pausePanel, PauseMenu pause)
    {
        Remove(FindDeep(pausePanel, "LanguageSelector"), "el selector de idioma sale de la pausa; se abre desde Opciones > Idioma.");
        Remove(DirectChild(pausePanel, "AudioPanel"), "el botón de sonido sale de la pausa; el audio se ajusta desde Opciones > Audio.");
        RemovePauseTagline(pausePanel);

        Transform buttons = DirectChild(pausePanel, "ButtonsPanel");
        if (buttons != null)
        {
            Transform resume = buttons.Find("Reanudar");
            Transform restart = buttons.Find("Reiniciar");
            Transform mainMenu = buttons.Find("Volver al Menu");
            Transform options = buttons.Find("Opciones");
            if (options == null)
            {
                options = buttons.Find("GraphicsButton");
                if (options != null)
                {
                    options.name = "Opciones";
                    Touch(options.gameObject);
                }
            }

            Transform template = options != null ? options : restart != null ? restart : resume;
            options = EnsureMenuButton(buttons, "Opciones", template, "Options", "Opciones", false);
            Transform overrides = EnsureMenuButton(buttons, "Sobrecargas", options, "Overrides", "Sobrecargas", true);
            Order(resume, restart, options, overrides, mainMenu);
            Wire(options, pause.OnOptionsButtonPressed);
            Wire(overrides, pause.OnOverridesButtonPressed);

            var rect = (RectTransform)buttons;
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, PauseColumnY);
            Column(buttons, PauseColumnHeight, PauseColumnPadding, PauseColumnSpacing);
        }

        UIScreenIntro intro = pausePanel.GetComponent<UIScreenIntro>();
        if (intro != null) AuthorIntroBlocks(intro);
    }

    private static void RemovePauseTagline(Transform pausePanel)
    {
        foreach (TMP_Text text in pausePanel.GetComponentsInChildren<TMP_Text>(true))
        {
            LocalizedText localized = text.GetComponent<LocalizedText>();
            string english = localized != null ? localized.Content.Get(GameLanguage.English) : text.text;
            if (english != PauseTagline) continue;

            Transform host = text.transform.parent;
            Remove(text.transform, "la pausa ya no muestra el subtítulo «" + PauseTagline + "».");
            if (host != null) Remove(DirectChild(host, StylePrefix + "SubRibbon"), "la pausa retira la placa de ese subtítulo.");
            return;
        }
    }

    private static void FixTutorialScrim(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name != "TutorialCanvas") continue;

            Transform canvas = root.transform;
            Transform frame = canvas.Find("ContentFrame");
            Transform panel = canvas.Find("Panel");
            if (panel == null && frame != null) panel = frame.Find("Panel");
            if (panel == null) return;
            if (frame == null) frame = panel.Find("ContentFrame");
            if (frame == null) return;

            bool changed = false;
            if (panel.parent != canvas)
            {
                int index = frame.parent == canvas ? frame.GetSiblingIndex() : 0;
                panel.SetParent(canvas, false);
                panel.SetSiblingIndex(index);
                changed = true;
            }

            Stretch((RectTransform)panel, 0f, 0f, 0f, 0f);
            if (frame.parent != panel)
            {
                frame.SetParent(panel, false);
                changed = true;
            }

            var contents = new List<Transform>();
            foreach (Transform child in panel)
                if (child != frame) contents.Add(child);
            foreach (Transform child in contents)
            {
                child.SetParent(frame, false);
                changed = true;
            }

            frame.SetAsFirstSibling();
            if (!changed) return;

            Touch(panel);
            Touch(frame);
            report.AppendLine($"MENÚS {scene.path}: el velo del tutorial cubre toda la pantalla y el diálogo queda dentro del marco 16:9.");
            return;
        }
    }

    private static Transform EnsureMenuButton(Transform parent, string name, Transform template, string english, string spanish, bool glitch)
    {
        if (parent == null) return null;

        Transform button = parent.Find(name);
        if (button == null)
        {
            if (template == null) return null;
            GameObject copy = Object.Instantiate(template.gameObject, parent);
            copy.name = name;
            button = copy.transform;
            createdCount++;
            report.AppendLine($"MENÚS: botón '{name}' creado en {parent.name} a partir de '{template.name}'.");
        }

        TMP_Text label = DirectChildText(button);
        if (label != null)
        {
            LocalizedText localized = GetOrAdd<LocalizedText>(label.gameObject);
            var serialized = new SerializedObject(localized);
            SerializedProperty content = serialized.FindProperty("content");
            content.FindPropertyRelative("english").stringValue = english;
            content.FindPropertyRelative("spanish").stringValue = spanish;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            label.text = english;
            Touch(localized);

            if (glitch)
            {
                GlitchTextUI effect = GetOrAdd<GlitchTextUI>(label.gameObject);
                var glitchSerialized = new SerializedObject(effect);
                SetReference(glitchSerialized, "target", label);
                SetReference(glitchSerialized, "companion", button.GetComponent<UISkew>());
                SetColor(glitchSerialized, "flickerColorA", style.cyan);
                SetColor(glitchSerialized, "flickerColorB", style.alert);
                glitchSerialized.ApplyModifiedPropertiesWithoutUndo();
                Touch(effect);
            }
        }

        Image image = button.GetComponent<Image>();
        if (image != null) StyleImage(image);
        if (label != null) StyleText(label);
        CenterLabel(button);
        return button;
    }

    private static void CenterLabel(Transform button)
    {
        TMP_Text label = button != null ? DirectChildText(button) : null;
        if (label == null) return;

        RectTransform rect = label.rectTransform;
        rect.localScale = Vector3.one;
        Stretch(rect, 0f, 0f, 0f, 0f);
        label.alignment = TextAlignmentOptions.Center;
        Touch(rect);
        Finish(label);
    }

    private static Transform FirstButton(Transform parent)
    {
        foreach (Transform child in parent)
            if (child.GetComponent<Button>() != null) return child;
        return null;
    }

    private static void Order(params Transform[] children)
    {
        int index = 0;
        foreach (Transform child in children)
        {
            if (child == null) continue;
            child.SetSiblingIndex(index++);
            Touch(child);
        }
    }

    private static void Column(Transform panel, float height, float padding, float spacing)
    {
        var rect = panel as RectTransform;
        if (rect == null) return;

        rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);
        VerticalLayoutGroup group = rect.GetComponent<VerticalLayoutGroup>();
        if (group != null)
        {
            group.padding = new RectOffset(group.padding.left, group.padding.right, Mathf.RoundToInt(padding), Mathf.RoundToInt(padding));
            group.spacing = spacing;
            Touch(group);
        }
        Touch(rect);
    }

    private static void Wire(Transform button, UnityAction action)
    {
        Button component = button != null ? button.GetComponent<Button>() : null;
        if (component == null) return;

        ClearListeners(component.onClick);
        UnityEventTools.AddVoidPersistentListener(component.onClick, action);
        Touch(component);
    }

    private static void Wire(Transform button, UnityAction<int> action, int argument)
    {
        Button component = button != null ? button.GetComponent<Button>() : null;
        if (component == null) return;

        ClearListeners(component.onClick);
        UnityEventTools.AddIntPersistentListener(component.onClick, action, argument);
        Touch(component);
    }

    private static void ClearListeners(UnityEventBase target)
    {
        for (int i = target.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(target, i);
    }

    private static void Deactivate(GameObject target)
    {
        if (target == null || !target.activeSelf) return;
        target.SetActive(false);
        Touch(target);
    }

    private static void Remove(Transform target, string reason)
    {
        if (target == null) return;
        Object.DestroyImmediate(target.gameObject);
        report.AppendLine("MENÚS: " + reason);
    }
}
