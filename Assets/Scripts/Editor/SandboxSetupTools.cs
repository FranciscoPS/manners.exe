using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SandboxSetupTools
{
    private const string SourceScenePath = "Assets/Scenes/Final Levels/LEVEL 1/LEVEL 1.unity";
    internal const string SandboxFolder = "Assets/Configurations/Sandbox";
    internal const string UpgradesFolder = SandboxFolder + "/Upgrades";
    internal const string EnemiesFolder = SandboxFolder + "/Enemies";
    internal const string WavesFolder = SandboxFolder + "/Waves";
    internal const string BalancePath = SandboxFolder + "/GameBalanceConfig_Sandbox.asset";
    internal const string UpgradeDatabasePath = SandboxFolder + "/UpgradeDatabase_Sandbox.asset";
    internal const string OverrideDatabasePath = SandboxFolder + "/OverrideDatabase_Sandbox.asset";
    internal const string OverridesFolder = SandboxFolder + "/Overrides";
    internal const string ChestOpeningConfigPath = SandboxFolder + "/ChestOpeningConfig_Sandbox.asset";
    internal const string InvisibleWallWarningConfigPath = SandboxFolder + "/InvisibleWallWarningConfig_Sandbox.asset";
    private const string ScenePath = "Assets/Scenes/Sandbox.unity";

    private static readonly string[] SourceEnemyConfigs =
    {
        "Assets/Configurations/Production/Enemies/BasicEnemy_Production.asset",
        "Assets/Configurations/Production/Enemies/FastEnemy_Production.asset"
    };

    [MenuItem("Tools/Manners/Sandbox/1. Crear assets del sandbox", false, 10)]
    public static void CreateSandboxAssets()
    {
        EditorAssetUtility.EnsureFolder(SandboxFolder);
        EditorAssetUtility.EnsureFolder(UpgradesFolder);
        EditorAssetUtility.EnsureFolder(EnemiesFolder);
        EditorAssetUtility.EnsureFolder(WavesFolder);

        CopyBalanceConfig();
        CopyUpgradeDatabase();
        CopyOverrideDatabase();
        CopyChestOpeningConfig();
        CopyInvisibleWallWarningConfig();
        Dictionary<EnemyConfiguration, EnemyConfiguration> enemyMap = CopyEnemyConfigs();
        CopyWaves(enemyMap);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[SandboxSetup] Assets del sandbox listos en " + SandboxFolder + ". Edítalos directamente (son independientes de los de producción).");
    }

    [MenuItem("Tools/Manners/Sandbox/2. Duplicar Nivel 1 en escena Sandbox", false, 11)]
    public static void DuplicateLevelScene()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SourceScenePath) == null)
        {
            Debug.LogError($"[SandboxSetup] No se encontró {SourceScenePath}.");
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
        {
            bool overwrite = EditorUtility.DisplayDialog(
                "Sandbox ya existe",
                $"Ya existe {ScenePath}.\n\n¿Sobrescribirla con una copia nueva de {SourceScenePath}?\nSe perderá cualquier cambio manual hecho en Sandbox.unity (el historial de git no se pierde).",
                "Sobrescribir", "Cancelar");

            if (!overwrite) return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            AssetDatabase.DeleteAsset(ScenePath);
        }
        else if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        if (!AssetDatabase.CopyAsset(SourceScenePath, ScenePath))
        {
            Debug.LogError($"[SandboxSetup] No se pudo duplicar {SourceScenePath} a {ScenePath}.");
            return;
        }

        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        RegisterSceneInBuildSettings();

        Debug.Log($"[SandboxSetup] {ScenePath} es una copia real y completa de {SourceScenePath}: edificios, spawn points, cámara, Canvas y managers son los mismos objetos, ya funcionales. Ejecuta el paso 3 para conectarle el balance independiente del sandbox.");
    }

    [MenuItem("Tools/Manners/Sandbox/3. Conectar sandbox a la escena abierta", false, 12)]
    public static void WireSandboxScene()
    {
        Scene active = EditorSceneManager.GetActiveScene();
        if (active.path != ScenePath)
        {
            Debug.LogError($"[SandboxSetup] Abre primero {ScenePath} (paso 2) y vuelve a ejecutar este paso con esa escena activa.");
            return;
        }

        GameBalanceConfig balance = AssetDatabase.LoadAssetAtPath<GameBalanceConfig>(BalancePath);
        UpgradeDatabase upgrades = AssetDatabase.LoadAssetAtPath<UpgradeDatabase>(UpgradeDatabasePath);

        if (balance == null || upgrades == null)
        {
            Debug.LogError("[SandboxSetup] Faltan los assets del sandbox. Ejecuta primero el paso 1.");
            return;
        }

        OverrideDatabase overrides = AssetDatabase.LoadAssetAtPath<OverrideDatabase>(OverrideDatabasePath);
        if (overrides == null)
        {
            Debug.LogWarning("[SandboxSetup] No hay OverrideDatabase de sandbox todavía. Ejecuta 'Tools > Manners > Overrides > Crear sistema de sobrecargas' y luego el paso 1 de nuevo si quieres probar sobrecargas.");
        }

        ChestOpeningConfig chestOpening = AssetDatabase.LoadAssetAtPath<ChestOpeningConfig>(ChestOpeningConfigPath);
        if (chestOpening == null)
        {
            Debug.LogWarning("[SandboxSetup] No hay ChestOpeningConfig de sandbox todavía. Ejecuta 'Tools > Manners > VFX > Crear configuración de apertura de cofre' y luego el paso 1 de nuevo si quieres tunearla por separado.");
        }

        InvisibleWallWarningConfig wallWarning = AssetDatabase.LoadAssetAtPath<InvisibleWallWarningConfig>(InvisibleWallWarningConfigPath);
        if (wallWarning == null)
        {
            Debug.LogWarning("[SandboxSetup] No hay InvisibleWallWarningConfig de sandbox todavía. Ejecuta 'Tools > Manners > Muros invisibles > 1. Crear assets del aviso' y luego este paso de nuevo.");
        }

        Dictionary<EnemyConfiguration, EnemyConfiguration> enemyMap = LoadEnemyMap();
        Dictionary<WaveData, WaveData> waveMap = LoadWaveMap();

        RewireEnemySpawnManager(enemyMap, waveMap);

        if (AssetDatabase.LoadAssetAtPath<GameObject>(InvisibleWallWarningSetupTools.PrefabPath) != null)
            InvisibleWallWarningSetupTools.PlaceWarnings(active);
        else
            Debug.LogWarning("[SandboxSetup] Avisos de muros invisibles sin colocar: falta el prefab. Ejecuta 'Tools > Manners > Muros invisibles > 1. Crear assets del aviso' y luego este paso de nuevo.");

        GameObject sandboxRoot = GameObject.Find("[SANDBOX]");
        if (sandboxRoot == null)
            sandboxRoot = new GameObject("[SANDBOX]");

        SandboxTuning tuning = GetOrAdd<SandboxTuning>(sandboxRoot);
        SerializedObject tuningSerialized = new SerializedObject(tuning);
        SetReference(tuningSerialized, "balanceOverride", balance);
        SetReference(tuningSerialized, "upgradeDatabaseOverride", upgrades);
        SetReference(tuningSerialized, "overrideDatabaseOverride", overrides);
        SetReference(tuningSerialized, "chestOpeningConfigOverride", chestOpening);
        SetReference(tuningSerialized, "invisibleWallWarningConfigOverride", wallWarning);
        tuningSerialized.ApplyModifiedPropertiesWithoutUndo();

        GameObject panelRoot = BuildOrFindDebugPanel(sandboxRoot.transform);

        SandboxDebugMonitor monitor = GetOrAdd<SandboxDebugMonitor>(sandboxRoot);
        SerializedObject monitorSerialized = new SerializedObject(monitor);
        SetReference(monitorSerialized, "panelRoot", panelRoot);
        monitorSerialized.ApplyModifiedPropertiesWithoutUndo();

        SandboxHotkeys hotkeys = GetOrAdd<SandboxHotkeys>(sandboxRoot);
        SerializedObject hotkeysSerialized = new SerializedObject(hotkeys);
        SetReference(hotkeysSerialized, "debugMonitor", monitor);

        if (hotkeysSerialized.FindProperty("burstEnemy").objectReferenceValue == null && enemyMap.Count > 0)
        {
            IEnumerator<EnemyConfiguration> enumerator = enemyMap.Values.GetEnumerator();
            if (enumerator.MoveNext())
                SetReference(hotkeysSerialized, "burstEnemy", enumerator.Current);
        }

        hotkeysSerialized.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(active);
        EditorSceneManager.SaveScene(active);

        Debug.Log("[SandboxSetup] Sandbox conectado: balance independiente inyectado, panel de debug creado, teclas activas. Dale a Play.");
    }

    private static Dictionary<EnemyConfiguration, EnemyConfiguration> CopyEnemyConfigs()
    {
        Dictionary<EnemyConfiguration, EnemyConfiguration> map = new Dictionary<EnemyConfiguration, EnemyConfiguration>();

        for (int i = 0; i < SourceEnemyConfigs.Length; i++)
        {
            EnemyConfiguration source = AssetDatabase.LoadAssetAtPath<EnemyConfiguration>(SourceEnemyConfigs[i]);
            if (source == null)
            {
                Debug.LogWarning($"[SandboxSetup] No se encontró {SourceEnemyConfigs[i]}");
                continue;
            }

            string targetPath = $"{EnemiesFolder}/{GameAssetPaths.SandboxFileName(SourceEnemyConfigs[i])}";
            EnemyConfiguration copy = AssetDatabase.LoadAssetAtPath<EnemyConfiguration>(targetPath);

            if (copy == null && AssetDatabase.CopyAsset(SourceEnemyConfigs[i], targetPath))
                copy = AssetDatabase.LoadAssetAtPath<EnemyConfiguration>(targetPath);

            if (copy != null) map[source] = copy;
        }

        Debug.Log($"[SandboxSetup] {map.Count} EnemyConfiguration duplicadas en {EnemiesFolder}");
        return map;
    }

    private static void CopyWaves(Dictionary<EnemyConfiguration, EnemyConfiguration> enemyMap)
    {
        string[] guids = AssetDatabase.FindAssets("t:WaveData", new[] { "Assets/Configurations/Production/Waves" });
        List<string> sourcePaths = new List<string>();

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (path.Contains("/Map2Waves/")) continue;
            sourcePaths.Add(path);
        }

        sourcePaths.Sort(string.CompareOrdinal);

        int created = 0;
        for (int i = 0; i < sourcePaths.Count; i++)
        {
            string targetPath = $"{WavesFolder}/{GameAssetPaths.SandboxFileName(sourcePaths[i])}";
            WaveData copy = AssetDatabase.LoadAssetAtPath<WaveData>(targetPath);

            if (copy == null && AssetDatabase.CopyAsset(sourcePaths[i], targetPath))
                copy = AssetDatabase.LoadAssetAtPath<WaveData>(targetPath);

            if (copy == null) continue;

            if (copy.enemyDistribution != null)
            {
                for (int e = 0; e < copy.enemyDistribution.Length; e++)
                {
                    EnemySpawnEntry entry = copy.enemyDistribution[e];
                    if (entry?.enemyConfig == null) continue;

                    if (enemyMap.TryGetValue(entry.enemyConfig, out EnemyConfiguration mapped))
                        entry.enemyConfig = mapped;
                }

                EditorUtility.SetDirty(copy);
            }

            created++;
        }

        Debug.Log($"[SandboxSetup] {created} WaveData duplicadas en {WavesFolder}");
    }

    private static GameBalanceConfig CopyBalanceConfig()
    {
        GameBalanceConfig existing = AssetDatabase.LoadAssetAtPath<GameBalanceConfig>(BalancePath);
        if (existing != null) return existing;

        if (!AssetDatabase.CopyAsset("Assets/Configurations/Production/Resources/GameBalanceConfig_Production.asset", BalancePath))
        {
            Debug.LogWarning("[SandboxSetup] No se pudo duplicar GameBalanceConfig.");
            return null;
        }

        Debug.Log($"[SandboxSetup] GameBalanceConfig duplicado en {BalancePath}");
        return AssetDatabase.LoadAssetAtPath<GameBalanceConfig>(BalancePath);
    }

    private static ChestOpeningConfig CopyChestOpeningConfig()
    {
        ChestOpeningConfig existing = AssetDatabase.LoadAssetAtPath<ChestOpeningConfig>(ChestOpeningConfigPath);
        if (existing != null) return existing;

        if (!AssetDatabase.LoadAssetAtPath<ChestOpeningConfig>("Assets/Configurations/Production/Resources/ChestOpeningConfig_Production.asset"))
        {
            Debug.LogWarning("[SandboxSetup] No se encontró Assets/Configurations/Production/Resources/ChestOpeningConfig_Production.asset. Ejecuta primero 'Tools > Manners > VFX > Crear configuración de apertura de cofre' y luego este paso de nuevo si quieres tunear la cinemática por separado en el sandbox.");
            return null;
        }

        if (!AssetDatabase.CopyAsset("Assets/Configurations/Production/Resources/ChestOpeningConfig_Production.asset", ChestOpeningConfigPath))
        {
            Debug.LogWarning("[SandboxSetup] No se pudo duplicar ChestOpeningConfig.");
            return null;
        }

        Debug.Log($"[SandboxSetup] ChestOpeningConfig duplicado en {ChestOpeningConfigPath}");
        return AssetDatabase.LoadAssetAtPath<ChestOpeningConfig>(ChestOpeningConfigPath);
    }

    internal static InvisibleWallWarningConfig CopyInvisibleWallWarningConfig()
    {
        InvisibleWallWarningConfig existing = AssetDatabase.LoadAssetAtPath<InvisibleWallWarningConfig>(InvisibleWallWarningConfigPath);
        if (existing != null) return existing;

        if (AssetDatabase.LoadAssetAtPath<InvisibleWallWarningConfig>(InvisibleWallWarningSetupTools.ConfigPath) == null)
        {
            Debug.LogWarning($"[SandboxSetup] No se encontró {InvisibleWallWarningSetupTools.ConfigPath}. Ejecuta primero 'Tools > Manners > Muros invisibles > 1. Crear assets del aviso' si quieres tunear los avisos de muros invisibles por separado en el sandbox.");
            return null;
        }

        EditorAssetUtility.EnsureFolder(SandboxFolder);

        if (!AssetDatabase.CopyAsset(InvisibleWallWarningSetupTools.ConfigPath, InvisibleWallWarningConfigPath))
        {
            Debug.LogWarning("[SandboxSetup] No se pudo duplicar InvisibleWallWarningConfig.");
            return null;
        }

        Debug.Log($"[SandboxSetup] InvisibleWallWarningConfig duplicado en {InvisibleWallWarningConfigPath}");
        return AssetDatabase.LoadAssetAtPath<InvisibleWallWarningConfig>(InvisibleWallWarningConfigPath);
    }

    private static UpgradeDatabase CopyUpgradeDatabase()
    {
        UpgradeDatabase existing = AssetDatabase.LoadAssetAtPath<UpgradeDatabase>(UpgradeDatabasePath);

        UpgradeDatabase source = AssetDatabase.LoadAssetAtPath<UpgradeDatabase>("Assets/Configurations/Production/Resources/UpgradeDatabase_Production.asset");
        if (source == null)
        {
            Debug.LogWarning("[SandboxSetup] No se encontró Assets/Configurations/Production/Resources/UpgradeDatabase_Production.asset.");
            return null;
        }

        if (existing == null && !AssetDatabase.CopyAsset("Assets/Configurations/Production/Resources/UpgradeDatabase_Production.asset", UpgradeDatabasePath))
        {
            Debug.LogWarning("[SandboxSetup] No se pudo duplicar UpgradeDatabase.");
            return null;
        }

        UpgradeDatabase copy = AssetDatabase.LoadAssetAtPath<UpgradeDatabase>(UpgradeDatabasePath);
        List<UpgradeData> copiedUpgrades = new List<UpgradeData>();

        for (int i = 0; i < source.allUpgrades.Count; i++)
        {
            UpgradeData upgrade = source.allUpgrades[i];
            if (upgrade == null) continue;

            string sourcePath = AssetDatabase.GetAssetPath(upgrade);
            string targetPath = $"{UpgradesFolder}/{GameAssetPaths.SandboxFileName(sourcePath)}";

            UpgradeData upgradeCopy = AssetDatabase.LoadAssetAtPath<UpgradeData>(targetPath);
            if (upgradeCopy == null && AssetDatabase.CopyAsset(sourcePath, targetPath))
                upgradeCopy = AssetDatabase.LoadAssetAtPath<UpgradeData>(targetPath);

            copiedUpgrades.Add(upgradeCopy != null ? upgradeCopy : upgrade);
        }

        copy.allUpgrades = copiedUpgrades;
        EditorUtility.SetDirty(copy);

        Debug.Log($"[SandboxSetup] UpgradeDatabase duplicada con {copiedUpgrades.Count} mejoras propias en {UpgradesFolder}");
        return copy;
    }

    private static OverrideDatabase CopyOverrideDatabase()
    {
        OverrideDatabase source = AssetDatabase.LoadAssetAtPath<OverrideDatabase>("Assets/Configurations/Production/Resources/OverrideDatabase_Production.asset");
        if (source == null)
        {
            Debug.LogWarning("[SandboxSetup] No se encontró Assets/Configurations/Production/Resources/OverrideDatabase_Production.asset. Ejecuta primero 'Tools > Manners > Overrides > Crear sistema de sobrecargas'.");
            return null;
        }

        EditorAssetUtility.EnsureFolder(OverridesFolder);

        OverrideDatabase copy = AssetDatabase.LoadAssetAtPath<OverrideDatabase>(OverrideDatabasePath);
        if (copy == null)
        {
            if (!AssetDatabase.CopyAsset("Assets/Configurations/Production/Resources/OverrideDatabase_Production.asset", OverrideDatabasePath))
            {
                Debug.LogWarning("[SandboxSetup] No se pudo duplicar OverrideDatabase.");
                return null;
            }

            copy = AssetDatabase.LoadAssetAtPath<OverrideDatabase>(OverrideDatabasePath);
        }

        List<OverrideData> copiedOverrides = new List<OverrideData>();

        for (int i = 0; i < source.allOverrides.Count; i++)
        {
            OverrideData overrideData = source.allOverrides[i];
            if (overrideData == null) continue;

            string sourcePath = AssetDatabase.GetAssetPath(overrideData);
            string targetPath = $"{OverridesFolder}/{GameAssetPaths.SandboxFileName(sourcePath)}";

            OverrideData overrideCopy = AssetDatabase.LoadAssetAtPath<OverrideData>(targetPath);
            if (overrideCopy == null && AssetDatabase.CopyAsset(sourcePath, targetPath))
                overrideCopy = AssetDatabase.LoadAssetAtPath<OverrideData>(targetPath);

            if (overrideCopy != null && overrideData.effectConfig != null)
            {
                string configSourcePath = AssetDatabase.GetAssetPath(overrideData.effectConfig);
                string configTargetPath = $"{OverridesFolder}/{GameAssetPaths.SandboxFileName(configSourcePath)}";

                OverrideEffectConfig configCopy = AssetDatabase.LoadAssetAtPath<OverrideEffectConfig>(configTargetPath);
                if (configCopy == null && AssetDatabase.CopyAsset(configSourcePath, configTargetPath))
                    configCopy = AssetDatabase.LoadAssetAtPath<OverrideEffectConfig>(configTargetPath);

                if (configCopy != null)
                {
                    overrideCopy.effectConfig = configCopy;
                    EditorUtility.SetDirty(overrideCopy);
                }
            }

            copiedOverrides.Add(overrideCopy != null ? overrideCopy : overrideData);
        }

        copy.allOverrides = copiedOverrides;
        EditorUtility.SetDirty(copy);

        Debug.Log($"[SandboxSetup] OverrideDatabase duplicada con {copiedOverrides.Count} sobrecargas propias en {OverridesFolder}");
        return copy;
    }

    internal static Dictionary<EnemyConfiguration, EnemyConfiguration> LoadEnemyMap()
    {
        Dictionary<EnemyConfiguration, EnemyConfiguration> map = new Dictionary<EnemyConfiguration, EnemyConfiguration>();

        for (int i = 0; i < SourceEnemyConfigs.Length; i++)
        {
            EnemyConfiguration source = AssetDatabase.LoadAssetAtPath<EnemyConfiguration>(SourceEnemyConfigs[i]);
            EnemyConfiguration copy = AssetDatabase.LoadAssetAtPath<EnemyConfiguration>($"{EnemiesFolder}/{GameAssetPaths.SandboxFileName(SourceEnemyConfigs[i])}");
            if (source != null && copy != null) map[source] = copy;
        }

        return map;
    }

    internal static Dictionary<WaveData, WaveData> LoadWaveMap()
    {
        Dictionary<WaveData, WaveData> map = new Dictionary<WaveData, WaveData>();

        string[] guids = AssetDatabase.FindAssets("t:WaveData", new[] { "Assets/Configurations/Production/Waves" });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (path.Contains("/Map2Waves/")) continue;

            WaveData source = AssetDatabase.LoadAssetAtPath<WaveData>(path);
            WaveData copy = AssetDatabase.LoadAssetAtPath<WaveData>($"{WavesFolder}/{GameAssetPaths.SandboxFileName(path)}");
            if (source != null && copy != null) map[source] = copy;
        }

        return map;
    }

    private static void RewireEnemySpawnManager(Dictionary<EnemyConfiguration, EnemyConfiguration> enemyMap, Dictionary<WaveData, WaveData> waveMap)
    {
        EnemySpawnManager manager = Object.FindFirstObjectByType<EnemySpawnManager>(FindObjectsInactive.Include);
        if (manager == null)
        {
            Debug.LogWarning("[SandboxSetup] No se encontró EnemySpawnManager en la escena.");
            return;
        }

        SerializedObject serialized = new SerializedObject(manager);
        int waveRewired = RewireArray(serialized.FindProperty("waveQueue"), waveMap);
        int enemyRewired = RewireArray(serialized.FindProperty("continuousEnemyTypes"), enemyMap);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(manager);

        Debug.Log($"[SandboxSetup] EnemySpawnManager: {waveRewired} WaveData y {enemyRewired} EnemyConfiguration remapeados a copias del sandbox.");
    }

    private static int RewireArray<T>(SerializedProperty arrayProperty, Dictionary<T, T> map) where T : Object
    {
        if (arrayProperty == null) return 0;

        int rewired = 0;
        for (int i = 0; i < arrayProperty.arraySize; i++)
        {
            SerializedProperty element = arrayProperty.GetArrayElementAtIndex(i);
            T current = element.objectReferenceValue as T;

            if (current != null && map.TryGetValue(current, out T mapped))
            {
                element.objectReferenceValue = mapped;
                rewired++;
            }
        }

        return rewired;
    }

    private static GameObject BuildOrFindDebugPanel(Transform parent)
    {
        Transform existingCanvas = parent.Find("SandboxDebugCanvas");
        if (existingCanvas != null)
        {
            Transform existingPanel = existingCanvas.Find("Panel");
            if (existingPanel != null) return existingPanel.gameObject;
        }

        GameObject canvasObject = new GameObject("SandboxDebugCanvas");
        canvasObject.transform.SetParent(parent, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject panelObject = new GameObject("Panel");
        panelObject.transform.SetParent(canvasObject.transform, false);

        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.65f);

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(12f, -12f);
        panelRect.sizeDelta = new Vector2(540f, 700f);

        return panelObject;
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }

    private static void SetReference(SerializedObject serialized, string fieldName, Object value)
    {
        SerializedProperty property = serialized.FindProperty(fieldName);
        if (property != null && value != null)
            property.objectReferenceValue = value;
    }

    private static void RegisterSceneInBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

        for (int i = 0; i < scenes.Count; i++)
        {
            if (scenes[i].path == ScenePath) return;
        }

        scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
