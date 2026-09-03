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
    private const string SynergyConfigFolder = "Assets/Configurations/Synergies";

    private const string MenuScreenName = "SinergiasMenuPanel";
    private const string MenuTitle = "Sinergias";
    private const string MenuSubtitle = "Mejora + Mejora = ?";
    private const string LegacyCloseButtonName = "CerrarButton";

    private static readonly string[] GameOverScenePaths =
    {
        "Assets/Scenes/Final Levels/LEVEL 1/LEVEL 1.unity",
        "Assets/Scenes/CityTest.unity",
    };

    private static readonly (string rowName, string assetName)[] RowToSynergyAsset =
    {
        ("AreaFriaSynergyPanel", "Synergy_CryoField"),
        ("PEMSynergyPanel", "Synergy_EmpPulse"),
        ("RayoLaserSynergyPanel", "Synergy_LaserBeam"),
    };

    private static readonly (string upgradeIconField, string upgradeLevelTextField, string sceneIconName)[] GameOverIconMap =
    {
        ("damageUpgradeIcon", "damageUpgradeLevelText", "Damage"),
        ("attackSpeedUpgradeIcon", "attackSpeedUpgradeLevelText", "AttackSpeed"),
        ("attackRangeUpgradeIcon", "attackRangeUpgradeLevelText", "Range"),
        ("moveSpeedUpgradeIcon", "moveSpeedUpgradeLevelText", "Speed"),
        ("magnetRangeUpgradeIcon", "magnetRangeUpgradeLevelText", "Magnet"),
        ("multiShotUpgradeIcon", "multiShotUpgradeLevelText", "MultiShot"),
        ("explosiveShotUpgradeIcon", "explosiveShotUpgradeLevelText", "ExplosiveShot"),
        ("knockbackUpgradeIcon", "knockbackUpgradeLevelText", "Knockback"),
    };

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

        GameObject existingMenuScreen = (GameObject)managerSO.FindProperty("sinergiasMenuPanel").objectReferenceValue;
        GameObject menuScreen = EnsureMenuScreen(existingMenuScreen, creditosPanel, prefabAsset);

        managerSO.FindProperty("sinergiasMenuPanel").objectReferenceValue = menuScreen;
        managerSO.ApplyModifiedProperties();

        RewireMainMenuButton(mainPanel.transform, manager);
        RewireHelpButton(helpPanel.transform, manager);

        helpInstance.SetActive(false);
        menuScreen.SetActive(false);

        EditorUtility.SetDirty(manager);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log($"[SynergyPanelSetup] Listo: subpantalla de Ayuda restaurada dentro de '{helpPanel.name}', pantalla '{MenuScreenName}' creada a partir de '{creditosPanel.name}' con una instancia del prefab '{PrefabPath}', botón 'Sinergias' del menú principal y botón 'Sinergias' de Ayuda conectados.");
    }

    [MenuItem("Tools/Manners/Synergies/3. Conectar iconos dinamicos en pantalla final", false, 22)]
    public static void ConnectGameOverIcons()
    {
        Scene activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.isDirty)
        {
            Debug.LogError("[SynergyPanelSetup] Guarda la escena actual antes de ejecutar este paso (recorre varias escenas y las guarda).");
            return;
        }

        string originalScenePath = activeScene.path;

        foreach (string scenePath in GameOverScenePaths)
        {
            if (!System.IO.File.Exists(scenePath))
            {
                Debug.LogWarning($"[SynergyPanelSetup] No existe '{scenePath}', se omite.");
                continue;
            }

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            ConnectGameOverIconsInScene(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        if (!string.IsNullOrEmpty(originalScenePath))
            EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);

        Debug.Log("[SynergyPanelSetup] Iconos de la pantalla final conectados dinámicamente a UpgradeDatabase donde fue posible. Revisa el log por cada escena para ver qué campos quedaron sin asignar.");
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
        Transform titlePanel = screen.Find("TitlePanel");
        if (titlePanel == null)
        {
            Debug.LogWarning("[SynergyPanelSetup] La pantalla clonada no tiene 'TitlePanel'; ajusta el título manualmente.");
            return;
        }

        var texts = titlePanel
            .GetComponentsInChildren<TextMeshProUGUI>(true)
            .OrderByDescending(t => ((RectTransform)t.transform).anchoredPosition.y)
            .ToList();

        if (texts.Count > 0) texts[0].text = MenuTitle;
        if (texts.Count > 1) texts[1].text = MenuSubtitle;
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
        SetLabel(button, MenuTitle);
        WireShowScreen(button, manager, MenuScreen.Sinergias);
    }

    private static void RewireHelpButton(Transform helpPanel, MainMenuUIManager manager)
    {
        Button button = FindButtonByName(helpPanel, MenuTitle) ?? FindButtonWithText(helpPanel, MenuTitle);
        if (button == null)
        {
            Debug.LogWarning("[SynergyPanelSetup] No se encontró el botón 'Sinergias' dentro del menú de Ayuda.");
            return;
        }

        SetLabel(button, MenuTitle);
        WireShowScreen(button, manager, MenuScreen.HelpSinergias);
    }

    private static void WireShowScreen(Button button, MainMenuUIManager manager, MenuScreen screen)
    {
        button.onClick = new Button.ButtonClickedEvent();
        UnityEventTools.AddIntPersistentListener(button.onClick, manager.ShowScreenByIndex, (int)screen);
        EditorUtility.SetDirty(button);
    }

    private static void SetLabel(Button button, string text)
    {
        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label == null) return;

        label.text = text;
        EditorUtility.SetDirty(label);
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

    private static void ConnectGameOverIconsInScene(Scene scene)
    {
        GameOverUI gameOverUI = Object.FindFirstObjectByType<GameOverUI>(FindObjectsInactive.Include);
        if (gameOverUI == null)
        {
            Debug.LogWarning($"[SynergyPanelSetup] '{scene.path}' no tiene GameOverUI, se omite.");
            return;
        }

        SerializedObject so = new SerializedObject(gameOverUI);

        foreach (var entry in GameOverIconMap)
        {
            SerializedProperty levelTextProp = so.FindProperty(entry.upgradeLevelTextField);
            SerializedProperty iconProp = so.FindProperty(entry.upgradeIconField);
            if (levelTextProp == null || iconProp == null) continue;

            TextMeshProUGUI levelText = levelTextProp.objectReferenceValue as TextMeshProUGUI;
            if (levelText == null) continue;

            Image icon = FindSiblingImageByName(levelText.transform, entry.sceneIconName);
            if (icon == null)
            {
                Debug.LogWarning($"[SynergyPanelSetup] '{scene.path}': no se encontró un ícono llamado '{entry.sceneIconName}' junto a '{entry.upgradeLevelTextField}'. Asigna '{entry.upgradeIconField}' manualmente si corresponde.");
                continue;
            }

            iconProp.objectReferenceValue = icon;
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(gameOverUI);
    }

    private static Image FindSiblingImageByName(Transform textTransform, string iconName)
    {
        Transform parent = textTransform.parent;
        if (parent == null) return null;

        Transform match = parent.Find(iconName);
        if (match == null) return null;

        return match.GetComponent<Image>();
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

    private static void SetupRow(Transform row, SynergyData synergy)
    {
        Transform template = row.Find("Plus_Txt");

        Transform squareA = row.Find("EmptySquare1");
        Transform squareB = row.Find("EmptySquare2");
        Transform squareResult = row.Find("EmptySquare3");

        (Image iconA, TextMeshProUGUI unknownA, TextMeshProUGUI levelA) = SetupSquare(squareA, template, true);
        (Image iconB, TextMeshProUGUI unknownB, TextMeshProUGUI levelB) = SetupSquare(squareB, template, true);
        (Image iconResult, TextMeshProUGUI unknownResult, _) = SetupSquare(squareResult, template, false);

        SynergyHintRowUI rowUI = row.GetComponent<SynergyHintRowUI>();
        if (rowUI == null)
            rowUI = row.gameObject.AddComponent<SynergyHintRowUI>();

        SerializedObject rowSO = new SerializedObject(rowUI);
        rowSO.FindProperty("synergy").objectReferenceValue = synergy;
        rowSO.FindProperty("iconA").objectReferenceValue = iconA;
        rowSO.FindProperty("unknownTextA").objectReferenceValue = unknownA;
        rowSO.FindProperty("levelTextA").objectReferenceValue = levelA;
        rowSO.FindProperty("iconB").objectReferenceValue = iconB;
        rowSO.FindProperty("unknownTextB").objectReferenceValue = unknownB;
        rowSO.FindProperty("levelTextB").objectReferenceValue = levelB;
        rowSO.FindProperty("iconResult").objectReferenceValue = iconResult;
        rowSO.FindProperty("unknownTextResult").objectReferenceValue = unknownResult;
        rowSO.ApplyModifiedProperties();

        EditorUtility.SetDirty(rowUI);
    }

    private static (Image icon, TextMeshProUGUI unknownText, TextMeshProUGUI levelText) SetupSquare(Transform square, Transform tmpTemplate, bool needsLevelText)
    {
        if (square == null) return (null, null, null);

        Image icon = GetOrCreateIcon(square, "Icon");
        TextMeshProUGUI unknownText = GetOrCreateTmp(square, "UnknownText", tmpTemplate, "?", Stretch);

        TextMeshProUGUI levelText = null;
        if (needsLevelText)
            levelText = GetOrCreateTmp(square, "LevelText", tmpTemplate, "Nv. 0", BottomStrip);

        return (icon, unknownText, levelText);
    }

    private static Image GetOrCreateIcon(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null) return existing.GetComponent<Image>();

        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(6, 6);
        rt.offsetMax = new Vector2(-6, -6);

        Image image = go.GetComponent<Image>();
        image.sprite = null;
        image.enabled = false;
        image.preserveAspect = true;
        image.raycastTarget = false;

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
