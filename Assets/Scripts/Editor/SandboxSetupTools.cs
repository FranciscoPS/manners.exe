using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SandboxSetupTools
{
    private const string SandboxFolder = "Assets/Configurations/Sandbox";
    private const string UpgradesFolder = SandboxFolder + "/Upgrades";
    private const string EnemiesFolder = SandboxFolder + "/Enemies";
    private const string WavesFolder = SandboxFolder + "/Waves";
    private const string ConfigPath = SandboxFolder + "/SandboxConfig.asset";
    private const string BalancePath = SandboxFolder + "/GameBalanceConfig_Sandbox.asset";
    private const string UpgradeDatabasePath = SandboxFolder + "/UpgradeDatabase_Sandbox.asset";
    private const string ScenePath = "Assets/Scenes/Sandbox.unity";

    private static readonly string[] ProjectilePrefabs = { "Assets/Prefabs/Resources/Bullet_VFX.prefab" };
    private static readonly string[] OrbPrefabs = { "Assets/Prefabs/Resources/ExperienceOrb.prefab" };
    private static readonly string[] CoinPrefabs = { "Assets/Prefabs/Resources/Coin.prefab" };
    private static readonly string[] DiamondPrefabs = { "Assets/Prefabs/Resources/Diamond.prefab" };

    private static readonly string[] BasicEnemyPrefabs =
    {
        "Assets/Prefabs/Characters/Basic Enemy.prefab",
        "Assets/Prefabs/Characters/BEnemy2.prefab",
        "Assets/Prefabs/Characters/BEnemy3.prefab"
    };

    private static readonly string[] FastEnemyPrefabs =
    {
        "Assets/Prefabs/Characters/Fast Enemy.prefab",
        "Assets/Prefabs/Characters/FEnemy2.prefab",
        "Assets/Prefabs/Characters/FEnemy3.prefab"
    };

    private static readonly string[] ObstaclePrefabs =
    {
        "Assets/Prefabs/Buildings/Building.prefab",
        "Assets/Prefabs/Buildings/Building2.prefab",
        "Assets/Prefabs/Buildings/Building3.prefab",
        "Assets/Prefabs/Buildings/Bulding4.prefab"
    };

    private static readonly string[] SourceEnemyConfigs =
    {
        "Assets/Configurations/Enemies Configurations/BasicEnemy.asset",
        "Assets/Configurations/Enemies Configurations/FastEnemy.asset"
    };

    [MenuItem("Tools/Manners/Sandbox/1. Crear assets del sandbox", false, 10)]
    public static void CreateSandboxAssets()
    {
        EnsureFolder(SandboxFolder);
        EnsureFolder(UpgradesFolder);
        EnsureFolder(EnemiesFolder);
        EnsureFolder(WavesFolder);

        GameBalanceConfig balance = CopyBalanceConfig();
        UpgradeDatabase upgrades = CopyUpgradeDatabase();
        Dictionary<EnemyConfiguration, EnemyConfiguration> enemyMap = CopyEnemyConfigs();
        WaveData[] waves = CopyWaves(enemyMap);

        SandboxConfig config = AssetDatabase.LoadAssetAtPath<SandboxConfig>(ConfigPath);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<SandboxConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            Debug.Log($"[SandboxSetup] SandboxConfig creado en {ConfigPath}");
        }
        else
        {
            Debug.Log($"[SandboxSetup] SandboxConfig ya existía en {ConfigPath}, se refrescan sus referencias.");
        }

        FillConfig(config, balance, upgrades, enemyMap, waves);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = config;
        EditorGUIUtility.PingObject(config);
    }

    [MenuItem("Tools/Manners/Sandbox/2. Crear escena Sandbox", false, 11)]
    public static void CreateSandboxScene()
    {
        SandboxConfig config = AssetDatabase.LoadAssetAtPath<SandboxConfig>(ConfigPath);
        if (config == null)
        {
            Debug.LogError($"[SandboxSetup] No existe {ConfigPath}. Ejecuta antes 'Tools > Manners > Sandbox > 1. Crear assets del sandbox'.");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject lightObject = new GameObject("Directional Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.1f;
        light.shadows = LightShadows.Soft;
        lightObject.transform.rotation = Quaternion.Euler(52f, -34f, 0f);

        GameObject sandboxObject = new GameObject("[SANDBOX]");
        SandboxBootstrapper bootstrapper = sandboxObject.AddComponent<SandboxBootstrapper>();

        SerializedObject serialized = new SerializedObject(bootstrapper);
        serialized.FindProperty("config").objectReferenceValue = config;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);

        RegisterSceneInBuildSettings();

        AssetDatabase.Refresh();
        Debug.Log($"[SandboxSetup] Escena creada en {ScenePath} y añadida a Build Settings. Dale a Play.");
    }

    [MenuItem("Tools/Manners/Sandbox/3. Refrescar referencias del config", false, 12)]
    public static void RefreshConfig()
    {
        SandboxConfig config = AssetDatabase.LoadAssetAtPath<SandboxConfig>(ConfigPath);
        if (config == null)
        {
            Debug.LogError($"[SandboxSetup] No existe {ConfigPath}.");
            return;
        }

        GameBalanceConfig balance = AssetDatabase.LoadAssetAtPath<GameBalanceConfig>(BalancePath);
        UpgradeDatabase upgrades = AssetDatabase.LoadAssetAtPath<UpgradeDatabase>(UpgradeDatabasePath);

        Dictionary<EnemyConfiguration, EnemyConfiguration> enemyMap = new Dictionary<EnemyConfiguration, EnemyConfiguration>();
        for (int i = 0; i < SourceEnemyConfigs.Length; i++)
        {
            EnemyConfiguration source = AssetDatabase.LoadAssetAtPath<EnemyConfiguration>(SourceEnemyConfigs[i]);
            EnemyConfiguration copy = AssetDatabase.LoadAssetAtPath<EnemyConfiguration>($"{EnemiesFolder}/{Path.GetFileName(SourceEnemyConfigs[i])}");
            if (source != null && copy != null) enemyMap[source] = copy;
        }

        List<WaveData> waves = new List<WaveData>();
        string[] waveGuids = AssetDatabase.FindAssets("t:WaveData", new[] { WavesFolder });
        for (int i = 0; i < waveGuids.Length; i++)
        {
            WaveData wave = AssetDatabase.LoadAssetAtPath<WaveData>(AssetDatabase.GUIDToAssetPath(waveGuids[i]));
            if (wave != null) waves.Add(wave);
        }
        waves.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

        FillConfig(config, balance, upgrades, enemyMap, waves.ToArray());

        AssetDatabase.SaveAssets();
        Debug.Log("[SandboxSetup] Referencias del SandboxConfig refrescadas.");
    }

    private static GameBalanceConfig CopyBalanceConfig()
    {
        GameBalanceConfig existing = AssetDatabase.LoadAssetAtPath<GameBalanceConfig>(BalancePath);
        if (existing != null) return existing;

        if (!AssetDatabase.CopyAsset("Assets/Resources/GameBalanceConfig.asset", BalancePath))
        {
            Debug.LogWarning("[SandboxSetup] No se pudo duplicar GameBalanceConfig. El sandbox usará el de producción.");
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

    private static WaveData[] CopyWaves(Dictionary<EnemyConfiguration, EnemyConfiguration> enemyMap)
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

        List<WaveData> result = new List<WaveData>();

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

            result.Add(copy);
        }

        Debug.Log($"[SandboxSetup] {result.Count} WaveData duplicadas en {WavesFolder}");
        return result.ToArray();
    }

    private static void FillConfig(SandboxConfig config, GameBalanceConfig balance, UpgradeDatabase upgrades,
                                   Dictionary<EnemyConfiguration, EnemyConfiguration> enemyMap, WaveData[] waves)
    {
        SerializedObject serialized = new SerializedObject(config);

        SetReference(serialized, "balanceOverride", balance);
        SetReference(serialized, "upgradeDatabaseOverride", upgrades);
        SetReference(serialized, "playerPrefab", LoadPrefab("Assets/Prefabs/Characters/Player.prefab"));

        FillObjectArray(serialized.FindProperty("obstacles").FindPropertyRelative("prefabs"), LoadPrefabs(ObstaclePrefabs));

        BuildPools(serialized.FindProperty("pools"));

        FillObjectArray(serialized.FindProperty("waveQueue"), waves);

        List<EnemyConfiguration> enemies = new List<EnemyConfiguration>(enemyMap.Values);
        FillObjectArray(serialized.FindProperty("continuousEnemyTypes"), enemies.ToArray());

        if (enemies.Count > 0)
            SetReference(serialized, "manualBurstEnemy", enemies[0]);

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(config);
    }

    private static void BuildPools(SerializedProperty pools)
    {
        if (pools.arraySize > 0) return;

        pools.arraySize = 6;

        ConfigurePool(pools.GetArrayElementAtIndex(0), PoolManager.PoolType.Projectile, ProjectilePrefabs, 200);
        ConfigurePool(pools.GetArrayElementAtIndex(1), PoolManager.PoolType.ExperienceOrb, OrbPrefabs, 300);
        ConfigurePool(pools.GetArrayElementAtIndex(2), PoolManager.PoolType.Coin, CoinPrefabs, 100);
        ConfigurePool(pools.GetArrayElementAtIndex(3), PoolManager.PoolType.Diamond, DiamondPrefabs, 50);
        ConfigurePool(pools.GetArrayElementAtIndex(4), PoolManager.PoolType.BasicEnemy, BasicEnemyPrefabs, 150);
        ConfigurePool(pools.GetArrayElementAtIndex(5), PoolManager.PoolType.FastEnemy, FastEnemyPrefabs, 150);
    }

    private static void ConfigurePool(SerializedProperty element, PoolManager.PoolType type, string[] prefabPaths, int prewarmCount)
    {
        GameObject[] prefabs = LoadPrefabs(prefabPaths);

        element.FindPropertyRelative("poolType").enumValueIndex = (int)type;
        element.FindPropertyRelative("prewarmCount").intValue = prewarmCount;
        element.FindPropertyRelative("preventGrow").boolValue = false;
        element.FindPropertyRelative("defaultCapacity").intValue = prewarmCount;
        element.FindPropertyRelative("maxSize").intValue = prewarmCount * 2;
        element.FindPropertyRelative("prefab").objectReferenceValue = prefabs.Length > 0 ? prefabs[0] : null;

        FillObjectArray(element.FindPropertyRelative("prefabs"), prefabs);
    }

    private static void FillObjectArray<T>(SerializedProperty property, T[] values) where T : Object
    {
        if (property == null) return;

        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }

    private static void SetReference(SerializedObject serialized, string fieldName, Object value)
    {
        SerializedProperty property = serialized.FindProperty(fieldName);
        if (property != null && value != null)
            property.objectReferenceValue = value;
    }

    private static GameObject LoadPrefab(string path)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) Debug.LogWarning($"[SandboxSetup] No se encontró el prefab {path}");
        return prefab;
    }

    private static GameObject[] LoadPrefabs(string[] paths)
    {
        List<GameObject> result = new List<GameObject>(paths.Length);

        for (int i = 0; i < paths.Length; i++)
        {
            GameObject prefab = LoadPrefab(paths[i]);
            if (prefab != null) result.Add(prefab);
        }

        return result.ToArray();
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
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
