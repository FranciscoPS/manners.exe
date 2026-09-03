using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public static class SynergyPanelSetupTools
{
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string PrefabFolder = "Assets/Prefabs/UI";
    private const string PrefabPath = PrefabFolder + "/SynergyHintsPanel.prefab";
    private const string InitialsPrefabPath = PrefabFolder + "/InitialsEntryUI.prefab";
    private const string SynergyConfigFolder = "Assets/Configurations/Synergies";

    private const string MenuScreenName = "SinergiasMenuPanel";
    private const string MenuTitle = "Sinergias";
    private const string MenuSubtitle = "Mejora + Mejora = ?";
    private const string LegacyCloseButtonName = "CerrarButton";

    private const string GlitchPrefix = "Sin";

    private const string GameOverSynergyInstanceName = "SynergyHintsPanel";
    private const string GameOverSynergyTitleName = "SinergiasTitle";
    private const string GameOverDiscoveryTextName = "SynergyDiscoveryText";
    private const float GameOverSynergyScale = 0.58f;

    private static readonly string[] GameOverScenePaths =
    {
        "Assets/Scenes/Final Levels/LEVEL 1/LEVEL 1.unity",
        "Assets/Scenes/CityTest.unity",
        "Assets/Scenes/Sandbox.unity",
        "Assets/Scenes/MilitaryBase.unity",
    };

    private static readonly (string rowName, string assetName)[] RowToSynergyAsset =
    {
        ("AreaFriaSynergyPanel", "Synergy_CryoField"),
        ("PEMSynergyPanel", "Synergy_EmpPulse"),
        ("RayoLaserSynergyPanel", "Synergy_LaserBeam"),
    };

    private static readonly (string rowName, string placeholder)[] GameOverStatRows =
    {
        ("Time", "Tiempo sobrevivido: 00:00"),
        ("XP", "Nivel alcanzado: 0"),
        ("Enemigos", "Enemigos eliminados: 0"),
        ("Edificios", "Edificios destruidos: 0"),
        ("Coins", "Monedas recolectadas: 0"),
        ("Gems", "Gemas recolectadas: 0"),
    };

    private static readonly Color DiscoveryBannerColor = new Color(1f, 0.95f, 0.6f, 1f);

    [MenuItem("Tools/Manners/Synergies/2. Configurar pantallas de sinergias en el menu", false, 21)]
    public static void ConfigureSynergyScreens()
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        if (scene.path != MainMenuScenePath)
        {
            Debug.LogError($"[SynergyPanelSetup] Abre '{MainMenuScenePath}' como escena activa antes de ejecutar este paso.");
            return;
        }

        MainMenuUIManager manager = Object.FindFirstObjectByType<MainMenuUIManager>(FindObjectsInactive.Include);
        if (manager == null)
        {
            Debug.LogError("[SynergyPanelSetup] No se encontró MainMenuUIManager en la escena.");
            return;
        }

        SerializedObject managerSO = new SerializedObject(manager);
        GameObject helpInstance = (GameObject)managerSO.FindProperty("sinergiasPanel").objectReferenceValue;
        GameObject helpPanel = (GameObject)managerSO.FindProperty("helpPanel").objectReferenceValue;
        GameObject creditosPanel = (GameObject)managerSO.FindProperty("creditosPanel").objectReferenceValue;
        GameObject mainPanel = (GameObject)managerSO.FindProperty("mainPanel").objectReferenceValue;

        if (helpInstance == null || helpPanel == null || creditosPanel == null || mainPanel == null)
        {
            Debug.LogError("[SynergyPanelSetup] Faltan referencias (sinergiasPanel/helpPanel/creditosPanel/mainPanel) en MainMenuUIManager.");
            return;
        }

        RemoveLegacyCloseButton(helpInstance.transform);

        if (!PrefabUtility.IsPartOfPrefabInstance(helpInstance))
        {
            SetupRows(helpInstance.transform);
            EditorAssetUtility.EnsureFolder(PrefabFolder);
            PrefabUtility.SaveAsPrefabAssetAndConnect(helpInstance, PrefabPath, InteractionMode.AutomatedAction);
        }

        RestoreHelpInstanceParent(helpInstance, helpPanel.transform);

        GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefabAsset == null)
        {
            Debug.LogError($"[SynergyPanelSetup] No se encontró el prefab en '{PrefabPath}'.");
            return;
        }

        UpdatePrefabContents();

        GameObject existingMenuScreen = (GameObject)managerSO.FindProperty("sinergiasMenuPanel").objectReferenceValue;
        GameObject menuScreen = EnsureMenuScreen(existingMenuScreen, creditosPanel, prefabAsset);

        managerSO.FindProperty("sinergiasMenuPanel").objectReferenceValue = menuScreen;
        managerSO.ApplyModifiedProperties();

        EnsureGlitch(FindTitleText(menuScreen.transform));
        RewireMainMenuButton(mainPanel.transform, manager);
        RewireHelpButton(helpPanel.transform, manager);

        helpInstance.SetActive(false);
        menuScreen.SetActive(false);

        EditorUtility.SetDirty(manager);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log($"[SynergyPanelSetup] Listo: subpantalla de Ayuda restaurada dentro de '{helpPanel.name}', pantalla '{MenuScreenName}' creada a partir de '{creditosPanel.name}' con una instancia del prefab '{PrefabPath}', botón 'Sinergias' del menú principal y botón 'Sinergias' de Ayuda conectados.");
    }

    [MenuItem("Tools/Manners/Synergies/3. Redisenar pantalla de Game Over", false, 22)]
    public static void RedesignGameOverScreens()
    {
        Scene activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.isDirty)
        {
            Debug.LogError("[SynergyPanelSetup] Guarda la escena actual antes de ejecutar este paso (recorre varias escenas y las guarda).");
            return;
        }

        GameObject synergyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (synergyPrefab == null)
        {
            Debug.LogError($"[SynergyPanelSetup] No existe '{PrefabPath}'. Ejecuta primero el paso 2 en MainMenu.");
            return;
        }

        string originalScenePath = activeScene.path;
        Sprite frameSprite = null;

        foreach (string scenePath in GameOverScenePaths)
        {
            if (!System.IO.File.Exists(scenePath))
            {
                Debug.LogWarning($"[SynergyPanelSetup] No existe '{scenePath}', se omite.");
                continue;
            }

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Sprite sceneFrame = RedesignGameOverInScene(scene, synergyPrefab);
            if (frameSprite == null) frameSprite = sceneFrame;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        RedesignInitialsEntryPrefab(frameSprite);

        if (!string.IsNullOrEmpty(originalScenePath))
            EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);

        Debug.Log("[SynergyPanelSetup] Game Over rediseñado en todas las escenas: stats a la izquierda, panel de sinergias a la derecha con aviso de descubrimiento, QR y mejoras eliminados, diálogo de récord reencuadrado.");
    }

    [MenuItem("Tools/Manners/Synergies/Borrar progreso guardado de sinergias", false, 40)]
    public static void ClearSavedSynergyDiscoveries()
    {
        SynergyDiscovery.Clear();

        foreach (string guid in AssetDatabase.FindAssets("t:SynergyData"))
        {
            SynergyData synergy = AssetDatabase.LoadAssetAtPath<SynergyData>(AssetDatabase.GUIDToAssetPath(guid));
            SynergyDiscovery.Forget(synergy);
        }

        Debug.Log("[SynergyPanelSetup] Progreso guardado de sinergias borrado: todas las mejoras y sinergias vuelven a '?'.");
    }

    private static Sprite RedesignGameOverInScene(Scene scene, GameObject synergyPrefab)
    {
        GameOverUI gameOverUI = Object.FindFirstObjectByType<GameOverUI>(FindObjectsInactive.Include);
        if (gameOverUI == null)
        {
            Debug.LogWarning($"[SynergyPanelSetup] '{scene.path}' no tiene GameOverUI, se omite.");
            return null;
        }

        SerializedObject so = new SerializedObject(gameOverUI);
        GameObject panel = so.FindProperty("gameOverPanel").objectReferenceValue as GameObject;
        if (panel == null)
        {
            Debug.LogWarning($"[SynergyPanelSetup] '{scene.path}': GameOverUI.gameOverPanel no está asignado, se omite.");
            return null;
        }

        Transform statsPanel = panel.transform.Find("StatsPanel");
        Transform right = statsPanel != null ? statsPanel.Find("Combat stats") : null;
        Transform left = statsPanel != null ? statsPanel.Find("RunStats") : null;
        Transform leftList = left != null ? left.Find("StatsText") : null;

        if (right == null || leftList == null)
        {
            Debug.LogWarning($"[SynergyPanelSetup] '{scene.path}': la jerarquía del Game Over no coincide (StatsPanel/Combat stats/RunStats/StatsText), se omite.");
            return null;
        }

        DestroyChild(panel.transform, "QR");

        Transform rightStats = right.Find("StatsText");
        if (rightStats != null)
        {
            MoveChild(rightStats, "Enemigos", leftList);
            MoveChild(rightStats, "Edificios", leftList);
        }

        ConfigureStatsList(leftList);

        TextMeshProUGUI rowTemplate = leftList.GetComponentInChildren<TextMeshProUGUI>(true);
        Transform premiumTitle = right.Find("PremiumPanel/Title");
        TextMeshProUGUI titleTemplate = premiumTitle != null ? premiumTitle.GetComponent<TextMeshProUGUI>() : rowTemplate;

        TextMeshProUGUI title = GetOrCloneText(right, GameOverSynergyTitleName, titleTemplate);
        if (title != null)
        {
            RectTransform rt = title.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -12f);
            rt.sizeDelta = new Vector2(700f, 46f);
            StyleText(title, MenuTitle, TextAlignmentOptions.Center, 20f, 36f);
            EnsureGlitch(title);
        }

        DestroyChild(right, "PremiumPanel");
        DestroyChild(right, "StatsPanel");
        DestroyChild(right, "StatsText");

        Transform synergyInstance = right.Find(GameOverSynergyInstanceName);
        if (synergyInstance == null)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(synergyPrefab, right);
            instance.name = GameOverSynergyInstanceName;
            synergyInstance = instance.transform;
        }

        RectTransform synergyRect = (RectTransform)synergyInstance;
        synergyRect.anchorMin = new Vector2(0.5f, 0.5f);
        synergyRect.anchorMax = new Vector2(0.5f, 0.5f);
        synergyRect.pivot = new Vector2(0.5f, 0.5f);
        synergyRect.anchoredPosition = new Vector2(0f, -6f);
        synergyRect.localScale = new Vector3(GameOverSynergyScale, GameOverSynergyScale, 1f);
        synergyInstance.SetSiblingIndex(right.childCount - 1);
        synergyInstance.gameObject.SetActive(true);

        TextMeshProUGUI banner = GetOrCloneText(right, GameOverDiscoveryTextName, rowTemplate);
        if (banner != null)
        {
            RectTransform rt = banner.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 12f);
            rt.sizeDelta = new Vector2(780f, 40f);
            StyleText(banner, "¡Nueva pista de sinergia encontrada!", TextAlignmentOptions.Center, 14f, 26f);
            banner.color = DiscoveryBannerColor;
            banner.transform.SetAsLastSibling();
            banner.gameObject.SetActive(false);
        }

        so.FindProperty("synergyDiscoveryText").objectReferenceValue = banner;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(gameOverUI);

        Image frame = panel.GetComponent<Image>();
        return frame != null ? frame.sprite : null;
    }

    private static void ConfigureStatsList(Transform list)
    {
        VerticalLayoutGroup layout = list.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
            layout = list.gameObject.AddComponent<VerticalLayoutGroup>();

        layout.padding = new RectOffset(36, 36, 28, 28);
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        for (int i = 0; i < GameOverStatRows.Length; i++)
        {
            Transform row = list.Find(GameOverStatRows[i].rowName);
            if (row == null) continue;

            row.SetSiblingIndex(i);

            LayoutElement element = row.GetComponent<LayoutElement>();
            if (element == null)
                element = row.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = 58f;
            element.flexibleHeight = 0f;

            TextMeshProUGUI tmp = row.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
                StyleText(tmp, GameOverStatRows[i].placeholder, TextAlignmentOptions.MidlineLeft, 14f, 28f);
        }
    }

    private static void RedesignInitialsEntryPrefab(Sprite frameSprite)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(InitialsPrefabPath);
        if (root == null)
        {
            Debug.LogWarning($"[SynergyPanelSetup] No se pudo abrir '{InitialsPrefabPath}'.");
            return;
        }

        Transform rootT = root.transform;
        Transform background = rootT.Find("Background");
        Transform title = rootT.Find("Title") ?? FindDeep(rootT, "Title");
        Transform row = rootT.Find("InitialsRow") ?? FindDeep(rootT, "InitialsRow");
        Transform grid = rootT.Find("Grid") ?? FindDeep(rootT, "Grid");
        Transform cursor = rootT.Find("Cursor") ?? FindDeep(rootT, "Cursor");

        Transform dialog = rootT.Find("Dialog");
        if (dialog == null)
        {
            GameObject dialogObj = new GameObject("Dialog", typeof(RectTransform), typeof(Image));
            dialogObj.transform.SetParent(rootT, false);
            dialog = dialogObj.transform;
        }

        dialog.SetSiblingIndex(background != null ? background.GetSiblingIndex() + 1 : 0);

        RectTransform dialogRect = (RectTransform)dialog;
        dialogRect.anchorMin = new Vector2(0.5f, 0.5f);
        dialogRect.anchorMax = new Vector2(0.5f, 0.5f);
        dialogRect.pivot = new Vector2(0.5f, 0.5f);
        dialogRect.anchoredPosition = Vector2.zero;
        dialogRect.sizeDelta = new Vector2(1000f, 660f);

        Image dialogImage = dialog.GetComponent<Image>();
        if (frameSprite != null)
        {
            dialogImage.sprite = frameSprite;
            dialogImage.type = Image.Type.Sliced;
            dialogImage.color = Color.white;
        }
        else
        {
            dialogImage.color = new Color(0.1f, 0.12f, 0.3f, 0.98f);
        }
        dialogImage.raycastTarget = true;

        Reparent(title, dialog, new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(900f, 80f));
        Reparent(row, dialog, new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(660f, 100f));
        Reparent(grid, dialog, new Vector2(0.5f, 1f), new Vector2(0f, -270f), new Vector2(720f, 280f));

        if (cursor != null)
        {
            cursor.SetParent(dialog, false);
            cursor.SetAsLastSibling();
        }

        TextMeshProUGUI titleText = title != null ? title.GetComponent<TextMeshProUGUI>() : null;
        if (titleText != null)
            StyleText(titleText, "¡Nuevo récord! Escribe tus iniciales", TextAlignmentOptions.Center, 24f, 44f);

        PrefabUtility.SaveAsPrefabAsset(root, InitialsPrefabPath);
        PrefabUtility.UnloadPrefabContents(root);
    }

    private static void Reparent(Transform target, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
    {
        if (target == null) return;

        if (target.parent != parent)
            target.SetParent(parent, false);

        RectTransform rt = (RectTransform)target;
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        rt.localScale = Vector3.one;
    }

    private static Transform FindDeep(Transform root, string name)
    {
        return root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name && t != root);
    }

    private static void StyleText(TextMeshProUGUI tmp, string text, TextAlignmentOptions alignment, float minSize, float maxSize)
    {
        tmp.text = text;
        tmp.alignment = alignment;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = minSize;
        tmp.fontSizeMax = maxSize;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.raycastTarget = false;
        EditorUtility.SetDirty(tmp);
    }

    private static TextMeshProUGUI GetOrCloneText(Transform parent, string name, TextMeshProUGUI template)
    {
        Transform existing = parent.Find(name);
        if (existing != null) return existing.GetComponent<TextMeshProUGUI>();
        if (template == null) return null;

        GameObject go = Object.Instantiate(template.gameObject, parent);
        go.name = name;
        go.SetActive(true);

        LayoutElement stray = go.GetComponent<LayoutElement>();
        if (stray != null) Object.DestroyImmediate(stray);

        return go.GetComponent<TextMeshProUGUI>();
    }

    private static void MoveChild(Transform from, string name, Transform to)
    {
        Transform child = from.Find(name);
        if (child == null || to.Find(name) != null) return;

        child.SetParent(to, false);
    }

    private static void DestroyChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child == null) return;

        Object.DestroyImmediate(child.gameObject);
    }

    private static void UpdatePrefabContents()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);

        SetupRows(root.transform);
        PatchLevelTexts(root);

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        PrefabUtility.UnloadPrefabContents(root);
    }

    private static void PatchLevelTexts(GameObject root)
    {
        foreach (TextMeshProUGUI tmp in root.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (tmp.gameObject.name != "LevelText") continue;

            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 10f;
            tmp.fontSizeMax = 22f;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;
        }
    }

    private static void RemoveLegacyCloseButton(Transform panel)
    {
        Transform legacy = panel.Find(LegacyCloseButtonName);
        if (legacy == null) return;

        if (PrefabUtility.IsPartOfAnyPrefab(legacy.gameObject))
        {
            Debug.LogWarning($"[SynergyPanelSetup] '{LegacyCloseButtonName}' vive dentro del prefab; bórralo manualmente desde '{PrefabPath}'.");
            return;
        }

        Object.DestroyImmediate(legacy.gameObject);
    }

    private static void RestoreHelpInstanceParent(GameObject helpInstance, Transform helpPanel)
    {
        if (helpInstance.transform.parent == helpPanel) return;

        helpInstance.transform.SetParent(helpPanel, false);
        helpInstance.transform.SetAsLastSibling();
    }

    private static GameObject EnsureMenuScreen(GameObject existing, GameObject creditosPanel, GameObject prefabAsset)
    {
        Transform screensRoot = creditosPanel.transform.parent;

        if (existing == null)
        {
            Transform byName = screensRoot.Find(MenuScreenName);
            if (byName != null) existing = byName.gameObject;
        }

        if (existing != null) return existing;

        GameObject screen = Object.Instantiate(creditosPanel, screensRoot);
        screen.name = MenuScreenName;
        screen.transform.SetSiblingIndex(creditosPanel.transform.GetSiblingIndex() + 1);

        ApplyTitle(screen.transform);
        ReplaceContentWithPrefab(screen.transform, prefabAsset);

        return screen;
    }

    private static void ApplyTitle(Transform screen)
    {
        var texts = TitleTexts(screen);
        if (texts == null)
        {
            Debug.LogWarning("[SynergyPanelSetup] La pantalla clonada no tiene 'TitlePanel'; ajusta el título manualmente.");
            return;
        }

        if (texts.Count > 0) texts[0].text = MenuTitle;
        if (texts.Count > 1) texts[1].text = MenuSubtitle;
    }

    private static TextMeshProUGUI FindTitleText(Transform screen)
    {
        var texts = TitleTexts(screen);
        return texts != null && texts.Count > 0 ? texts[0] : null;
    }

    private static List<TextMeshProUGUI> TitleTexts(Transform screen)
    {
        Transform titlePanel = screen.Find("TitlePanel");
        if (titlePanel == null) return null;

        return titlePanel
            .GetComponentsInChildren<TextMeshProUGUI>(true)
            .OrderByDescending(t => ((RectTransform)t.transform).anchoredPosition.y)
            .ToList();
    }

    private static void ReplaceContentWithPrefab(Transform screen, GameObject prefabAsset)
    {
        Transform oldContent = screen.Find("Text");
        int siblingIndex = oldContent != null ? oldContent.GetSiblingIndex() : screen.childCount;

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset, screen);
        instance.transform.SetSiblingIndex(siblingIndex);
        instance.SetActive(true);

        if (oldContent != null)
            Object.DestroyImmediate(oldContent.gameObject);
    }

    private static void RewireMainMenuButton(Transform mainPanel, MainMenuUIManager manager)
    {
        Button button = FindButtonWithText(mainPanel, "Robots")
            ?? FindButtonWithText(mainPanel, MenuTitle)
            ?? FindButtonByName(mainPanel, "Personalizacion");

        if (button == null)
        {
            Debug.LogWarning("[SynergyPanelSetup] No se encontró el botón 'Robots' en el menú principal.");
            return;
        }

        button.gameObject.name = MenuTitle;
        button.interactable = true;
        EnsureGlitch(SetLabel(button, MenuTitle));
        WireShowScreen(button, manager, MenuScreen.Sinergias);
        DisableComingSoonHover(button);
    }

    private static void RewireHelpButton(Transform helpPanel, MainMenuUIManager manager)
    {
        Button button = FindButtonByName(helpPanel, MenuTitle) ?? FindButtonWithText(helpPanel, MenuTitle);
        if (button == null)
        {
            Debug.LogWarning("[SynergyPanelSetup] No se encontró el botón 'Sinergias' dentro del menú de Ayuda.");
            return;
        }

        EnsureGlitch(SetLabel(button, MenuTitle));
        WireShowScreen(button, manager, MenuScreen.HelpSinergias);
    }

    private static void DisableComingSoonHover(Button button)
    {
        MenuButtonHover hover = button.GetComponent<MenuButtonHover>();
        if (hover == null) return;

        SerializedObject hoverSO = new SerializedObject(hover);
        hoverSO.FindProperty("changeTextOnHover").boolValue = false;
        hoverSO.ApplyModifiedProperties();
        EditorUtility.SetDirty(hover);
    }

    private static void EnsureGlitch(TextMeshProUGUI label)
    {
        if (label == null) return;

        GlitchTextUI glitch = label.GetComponent<GlitchTextUI>();
        if (glitch == null)
            glitch = label.gameObject.AddComponent<GlitchTextUI>();

        SerializedObject glitchSO = new SerializedObject(glitch);
        glitchSO.FindProperty("target").objectReferenceValue = label;
        glitchSO.FindProperty("prefix").stringValue = GlitchPrefix;
        glitchSO.ApplyModifiedProperties();

        EditorUtility.SetDirty(glitch);
    }

    private static void WireShowScreen(Button button, MainMenuUIManager manager, MenuScreen screen)
    {
        button.onClick = new Button.ButtonClickedEvent();
        UnityEventTools.AddIntPersistentListener(button.onClick, manager.ShowScreenByIndex, (int)screen);
        EditorUtility.SetDirty(button);
    }

    private static TextMeshProUGUI SetLabel(Button button, string text)
    {
        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label == null) return null;

        label.text = text;
        EditorUtility.SetDirty(label);
        return label;
    }

    private static Button FindButtonByName(Transform root, string name)
    {
        return root
            .GetComponentsInChildren<Button>(true)
            .FirstOrDefault(b => b.gameObject.name == name);
    }

    private static Button FindButtonWithText(Transform root, string text)
    {
        TextMeshProUGUI match = root
            .GetComponentsInChildren<TextMeshProUGUI>(true)
            .FirstOrDefault(t => t.text.Trim() == text);

        if (match == null) return null;

        return match.GetComponentInParent<Button>();
    }

    private static void SetupRows(Transform panel)
    {
        foreach (var entry in RowToSynergyAsset)
        {
            Transform row = panel.Find(entry.rowName);
            if (row == null)
            {
                Debug.LogWarning($"[SynergyPanelSetup] No se encontró la fila '{entry.rowName}' dentro de '{panel.name}'.");
                continue;
            }

            SynergyData synergy = AssetDatabase.LoadAssetAtPath<SynergyData>($"{SynergyConfigFolder}/{entry.assetName}.asset");
            if (synergy == null)
                Debug.LogWarning($"[SynergyPanelSetup] No se encontró el asset '{entry.assetName}' en {SynergyConfigFolder}.");

            SetupRow(row, synergy);
        }
    }

    private static readonly Color RequirementBackdropColor = new Color(0.93f, 0.96f, 1f, 1f);
    private static readonly Color ResultBackdropColor = new Color(1f, 0.95f, 0.6f, 1f);
    private const float BackdropInset = 8f;
    private const float IconInset = 14f;

    private struct SquareParts
    {
        public Image icon;
        public Image backdrop;
        public TextMeshProUGUI unknownText;
        public TextMeshProUGUI levelText;
    }

    private static void SetupRow(Transform row, SynergyData synergy)
    {
        EnsureHoverHitArea(row);

        Transform template = row.Find("Plus_Txt");

        Transform squareA = row.Find("EmptySquare1");
        Transform squareB = row.Find("EmptySquare2");
        Transform squareResult = row.Find("EmptySquare3");

        SquareParts a = SetupSquare(squareA, template, true, RequirementBackdropColor);
        SquareParts b = SetupSquare(squareB, template, true, RequirementBackdropColor);
        SquareParts result = SetupSquare(squareResult, template, false, ResultBackdropColor);

        PremiumUpgradeVisuals resultVisuals = null;
        if (squareResult != null)
        {
            resultVisuals = squareResult.GetComponent<PremiumUpgradeVisuals>();
            if (resultVisuals == null)
                resultVisuals = squareResult.gameObject.AddComponent<PremiumUpgradeVisuals>();
        }

        SynergyHintRowUI rowUI = row.GetComponent<SynergyHintRowUI>();
        if (rowUI == null)
            rowUI = row.gameObject.AddComponent<SynergyHintRowUI>();

        SerializedObject rowSO = new SerializedObject(rowUI);
        rowSO.FindProperty("synergy").objectReferenceValue = synergy;
        rowSO.FindProperty("iconA").objectReferenceValue = a.icon;
        rowSO.FindProperty("backdropA").objectReferenceValue = a.backdrop;
        rowSO.FindProperty("unknownTextA").objectReferenceValue = a.unknownText;
        rowSO.FindProperty("levelTextA").objectReferenceValue = a.levelText;
        rowSO.FindProperty("iconB").objectReferenceValue = b.icon;
        rowSO.FindProperty("backdropB").objectReferenceValue = b.backdrop;
        rowSO.FindProperty("unknownTextB").objectReferenceValue = b.unknownText;
        rowSO.FindProperty("levelTextB").objectReferenceValue = b.levelText;
        rowSO.FindProperty("iconResult").objectReferenceValue = result.icon;
        rowSO.FindProperty("backdropResult").objectReferenceValue = result.backdrop;
        rowSO.FindProperty("unknownTextResult").objectReferenceValue = result.unknownText;
        rowSO.FindProperty("resultVisuals").objectReferenceValue = resultVisuals;
        rowSO.ApplyModifiedProperties();

        EditorUtility.SetDirty(rowUI);
    }

    private static void EnsureHoverHitArea(Transform row)
    {
        Transform existing = row.Find("HoverHitArea");
        GameObject go = existing != null ? existing.gameObject : new GameObject("HoverHitArea", typeof(RectTransform), typeof(Image));

        if (existing == null)
            go.transform.SetParent(row, false);

        go.transform.SetSiblingIndex(0);

        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image image = go.GetComponent<Image>();
        image.color = Color.clear;
        image.raycastTarget = true;

        LayoutElement layoutElement = go.GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = go.AddComponent<LayoutElement>();
        layoutElement.ignoreLayout = true;
    }

    private static SquareParts SetupSquare(Transform square, Transform tmpTemplate, bool needsLevelText, Color backdropColor)
    {
        SquareParts parts = new SquareParts();
        if (square == null) return parts;

        parts.backdrop = GetOrCreateBackdrop(square, "IconBackdrop", backdropColor);
        parts.icon = GetOrCreateIcon(square, "Icon");
        parts.unknownText = GetOrCreateTmp(square, "UnknownText", tmpTemplate, "?", Stretch);

        if (needsLevelText)
            parts.levelText = GetOrCreateTmp(square, "LevelText", tmpTemplate, "Nv. 0", BottomStrip);

        return parts;
    }

    private static Image GetOrCreateBackdrop(Transform parent, string name, Color color)
    {
        Transform existing = parent.Find(name);
        GameObject go = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(Image));

        if (existing == null)
        {
            go.transform.SetParent(parent, false);
            go.SetActive(false);
        }

        go.transform.SetSiblingIndex(0);

        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(BackdropInset, BackdropInset);
        rt.offsetMax = new Vector2(-BackdropInset, -BackdropInset);

        Image image = go.GetComponent<Image>();
        image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        image.type = Image.Type.Sliced;
        image.color = color;
        image.raycastTarget = false;

        return image;
    }

    private static Image GetOrCreateIcon(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        GameObject go = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(Image));

        if (existing == null)
            go.transform.SetParent(parent, false);

        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(IconInset, IconInset);
        rt.offsetMax = new Vector2(-IconInset, -IconInset);

        Image image = go.GetComponent<Image>();
        image.preserveAspect = true;
        image.raycastTarget = false;

        if (existing == null)
        {
            image.sprite = null;
            image.enabled = false;
        }

        return image;
    }

    private static TextMeshProUGUI GetOrCreateTmp(Transform parent, string name, Transform tmpTemplate, string defaultText, System.Action<RectTransform> layout)
    {
        Transform existing = parent.Find(name);
        if (existing != null) return existing.GetComponent<TextMeshProUGUI>();

        GameObject go;
        if (tmpTemplate != null)
        {
            go = Object.Instantiate(tmpTemplate.gameObject, parent);
        }
        else
        {
            go = new GameObject(name, typeof(RectTransform));
            go.AddComponent<TextMeshProUGUI>();
        }

        go.name = name;

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = defaultText;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        layout((RectTransform)go.transform);

        return tmp;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    private static void BottomStrip(RectTransform rt)
    {
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -2f);
        rt.sizeDelta = new Vector2(0f, 18f);
        rt.localScale = Vector3.one;
    }
}
