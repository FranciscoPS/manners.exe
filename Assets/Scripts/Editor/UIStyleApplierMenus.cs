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
    public const string MenusReportPath = "Logs/ui-menus-report.txt";

    private const string OptionsMenuName = "OptionsMenu";
    private const float OptionsColumnHeight = 600f;
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

    private static readonly string[] OptionsScreens = { "OptionsPanel", "AudioPanel", "ControlesPanel", "AyudaPanel", "SobrecargasMenuPanel", "GraphicsSettingsPanel" };
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

        foreach (Transform screen in new[] { hub, audio, controls, help, overrides })
        {
            if (screen == null) continue;
            Touch(GetOrAdd<UIOverlay>(screen.gameObject));
            Deactivate(screen.gameObject);
        }
        if (graphicsPanel != null) Deactivate(graphicsPanel.gameObject);

        GameObject languagePanel = null;
        Transform graphicsButton = null;
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
                Transform languageButton = EnsureMenuButton(buttons, "Idioma", template, "Language", "Idioma", false);
                Transform overridesButton = EnsureMenuButton(buttons, "Sobrecargas", template, "Overrides", "Sobrecargas", true);
                Order(audioButton, controlsButton, graphicsButton, languageButton, overridesButton, helpButton, backButton);
                Column(buttons, OptionsColumnHeight, OptionsColumnPadding, OptionsColumnSpacing);
                Wire(audioButton, menu.ShowAudio);
                Wire(controlsButton, menu.ShowControls);
                Wire(graphicsButton, menu.ShowGraphics);
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

        var serialized = new SerializedObject(menu);
        SetReference(serialized, "hub", hub != null ? hub.gameObject : null);
        SetReference(serialized, "audioScreen", audio != null ? audio.gameObject : null);
        SetReference(serialized, "controlsScreen", controls != null ? controls.gameObject : null);
        SetReference(serialized, "helpScreen", help != null ? help.gameObject : null);
        SetReference(serialized, "overridesScreen", overrides != null ? overrides.gameObject : null);
        SetReference(serialized, "graphics", graphics);
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
