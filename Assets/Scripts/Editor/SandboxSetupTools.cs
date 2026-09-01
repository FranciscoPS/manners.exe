using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SandboxSetupTools
{
    private const string SourceScenePath = "Assets/Scenes/CityTest.unity";
    private const string SandboxFolder = "Assets/Configurations/Sandbox";
    private const string UpgradesFolder = SandboxFolder + "/Upgrades";
    private const string EnemiesFolder = SandboxFolder + "/Enemies";
    private const string WavesFolder = SandboxFolder + "/Waves";
    private const string BalancePath = SandboxFolder + "/GameBalanceConfig_Sandbox.asset";
    private const string UpgradeDatabasePath = SandboxFolder + "/UpgradeDatabase_Sandbox.asset";
    private const string SynergyDatabasePath = SandboxFolder + "/SynergyDatabase_Sandbox.asset";
    private const string SynergiesFolder = SandboxFolder + "/Synergies";
    private const string ScenePath = "Assets/Scenes/Sandbox.unity";

    private static readonly string[] SourceEnemyConfigs =
    {
        "Assets/Configurations/Enemies Configurations/BasicEnemy.asset",
        "Assets/Configurations/Enemies Configurations/FastEnemy.asset"
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
        CopySynergyDatabase();
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

        SynergyDatabase synergies = AssetDatabase.LoadAssetAtPath<SynergyDatabase>(SynergyDatabasePath);
        if (synergies == null)
        {
            Debug.LogWarning("[SandboxSetup] No hay SynergyDatabase de sandbox todavía. Ejecuta 'Tools > Manners > Synergies > Crear sistema de sinergias' y luego el paso 1 de nuevo si quieres probar sinergias.");
        }

        Dictionary<EnemyConfiguration, EnemyConfiguration> enemyMap = LoadEnemyMap();
        Dictionary<WaveData, WaveData> waveMap = LoadWaveMap();

        RewireEnemySpawnManager(enemyMap, waveMap);

        GameObject sandboxRoot = GameObject.Find("[SANDBOX]");
        if (sandboxRoot == null)
            sandboxRoot = new GameObject("[SANDBOX]");

        SandboxTuning tuning = GetOrAdd<SandboxTuning>(sandboxRoot);
        SerializedObject tuningSerialized = new SerializedObject(tuning);
        SetReference(tuningSerialized, "balanceOverride", balance);
        SetReference(tuningSerialized, "upgradeDatabaseOverride", upgrades);
        SetReference(tuningSerialized, "synergyDatabaseOverride", synergies);
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

            string targetPath = $"{EnemiesFolder}/{Path.GetFileName(SourceEnemyConfigs[i])}";
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
        string[] guids = AssetDatabase.FindAssets("t:WaveData", new[] { "Assets/Configurations/Waves Configurations" });
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
            string targetPath = $"{WavesFolder}/{Path.GetFileName(sourcePaths[i])}";
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

        if (!AssetDatabase.CopyAsset("Assets/Resources/GameBalanceConfig.asset", BalancePath))
        {
            Debug.LogWarning("[SandboxSetup] No se pudo duplicar GameBalanceConfig.");
            return null;
        }

        Debug.Log($"[SandboxSetup] GameBalanceConfig duplicado en {BalancePath}");
        return AssetDatabase.LoadAssetAtPath<GameBalanceConfig>(BalancePath);
    }

    private static UpgradeDatabase CopyUpgradeDatabase()
    {
        UpgradeDatabase existing = AssetDatabase.LoadAssetAtPath<UpgradeDatabase>(UpgradeDatabasePath);
        if (existing != null) return existing;

        UpgradeDatabase source = AssetDatabase.LoadAssetAtPath<UpgradeDatabase>("Assets/Resources/UpgradeDatabase.asset");
        if (source == null)
        {
            Debug.LogWarning("[SandboxSetup] No se encontró Assets/Resources/UpgradeDatabase.asset.");
            return null;
        }

        if (!AssetDatabase.CopyAsset("Assets/Resources/UpgradeDatabase.asset", UpgradeDatabasePath))
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
            string targetPath = $"{UpgradesFolder}/{Path.GetFileName(sourcePath)}";

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

    private static SynergyDatabase CopySynergyDatabase()
    {
        SynergyDatabase source = AssetDatabase.LoadAssetAtPath<SynergyDatabase>("Assets/Resources/SynergyDatabase.asset");
        if (source == null)
        {
            Debug.LogWarning("[SandboxSetup] No se encontró Assets/Resources/SynergyDatabase.asset. Ejecuta primero 'Tools > Manners > Synergies > Crear sistema de sinergias'.");
            return null;
        }

        EditorAssetUtility.EnsureFolder(SynergiesFolder);

        SynergyDatabase copy = AssetDatabase.LoadAssetAtPath<SynergyDatabase>(SynergyDatabasePath);
        if (copy == null)
        {
            if (!AssetDatabase.CopyAsset("Assets/Resources/SynergyDatabase.asset", SynergyDatabasePath))
            {
                Debug.LogWarning("[SandboxSetup] No se pudo duplicar SynergyDatabase.");
                return null;
            }

            copy = AssetDatabase.LoadAssetAtPath<SynergyDatabase>(SynergyDatabasePath);
        }

        List<SynergyData> copiedSynergies = new List<SynergyData>();

        for (int i = 0; i < source.allSynergies.Count; i++)
        {
            SynergyData synergy = source.allSynergies[i];
            if (synergy == null) continue;

            string sourcePath = AssetDatabase.GetAssetPath(synergy);
            string targetPath = $"{SynergiesFolder}/{Path.GetFileName(sourcePath)}";

            SynergyData synergyCopy = AssetDatabase.LoadAssetAtPath<SynergyData>(targetPath);
            if (synergyCopy == null && AssetDatabase.CopyAsset(sourcePath, targetPath))
                synergyCopy = AssetDatabase.LoadAssetAtPath<SynergyData>(targetPath);

            if (synergyCopy != null && synergy.effectConfig != null)
            {
                string configSourcePath = AssetDatabase.GetAssetPath(synergy.effectConfig);
                string configTargetPath = $"{SynergiesFolder}/{Path.GetFileName(configSourcePath)}";

                SynergyEffectConfig configCopy = AssetDatabase.LoadAssetAtPath<SynergyEffectConfig>(configTargetPath);
                if (configCopy == null && AssetDatabase.CopyAsset(configSourcePath, configTargetPath))
                    configCopy = AssetDatabase.LoadAssetAtPath<SynergyEffectConfig>(configTargetPath);

                if (configCopy != null)
                {
                    synergyCopy.effectConfig = configCopy;
                    EditorUtility.SetDirty(synergyCopy);
                }
            }

            copiedSynergies.Add(synergyCopy != null ? synergyCopy : synergy);
        }

        copy.allSynergies = copiedSynergies;
        EditorUtility.SetDirty(copy);

        Debug.Log($"[SandboxSetup] SynergyDatabase duplicada con {copiedSynergies.Count} sinergias propias en {SynergiesFolder}");
        return copy;
    }

    private static Dictionary<EnemyConfiguration, EnemyConfiguration> LoadEnemyMap()
    {
        Dictionary<EnemyConfiguration, EnemyConfiguration> map = new Dictionary<EnemyConfiguration, EnemyConfiguration>();

        for (int i = 0; i < SourceEnemyConfigs.Length; i++)
        {
            EnemyConfiguration source = AssetDatabase.LoadAssetAtPath<EnemyConfiguration>(SourceEnemyConfigs[i]);
            EnemyConfiguration copy = AssetDatabase.LoadAssetAtPath<EnemyConfiguration>($"{EnemiesFolder}/{Path.GetFileName(SourceEnemyConfigs[i])}");
            if (source != null && copy != null) map[source] = copy;
        }

        return map;
    }

    private static Dictionary<WaveData, WaveData> LoadWaveMap()
    {
        Dictionary<WaveData, WaveData> map = new Dictionary<WaveData, WaveData>();

        string[] guids = AssetDatabase.FindAssets("t:WaveData", new[] { "Assets/Configurations/Waves Configurations" });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (path.Contains("/Map2Waves/")) continue;

            WaveData source = AssetDatabase.LoadAssetAtPath<WaveData>(path);
            WaveData copy = AssetDatabase.LoadAssetAtPath<WaveData>($"{WavesFolder}/{Path.GetFileName(path)}");
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
