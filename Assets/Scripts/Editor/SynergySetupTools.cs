using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class SynergySetupTools
{
    private const string ConfigFolder = "Assets/Configurations/Synergies";
    private const string PrefabFolder = "Assets/Prefabs/Synergies";
    private const string DatabasePath = "Assets/Resources/SynergyDatabase.asset";

    [MenuItem("Tools/Manners/Synergies/Crear sistema de sinergias", false, 20)]
    public static void CreateSynergySystem()
    {
        EnsureFolder(ConfigFolder);
        EnsureFolder(PrefabFolder);

        GameObject cryoPrefab = CreateEffectPrefab<CryoFieldEffect>("CryoFieldEffect");
        GameObject laserPrefab = CreateEffectPrefab<LaserBeamEffect>("LaserBeamEffect");
        GameObject empPrefab = CreateEffectPrefab<EmpPulseEffect>("EmpPulseEffect");

        CryoFieldConfig cryoConfig = CreateEffectConfig<CryoFieldConfig>("CryoFieldConfig");
        LaserBeamConfig laserConfig = CreateEffectConfig<LaserBeamConfig>("LaserBeamConfig");
        EmpPulseConfig empConfig = CreateEffectConfig<EmpPulseConfig>("EmpPulseConfig");

        SynergyData cryo = CreateSynergyData("Synergy_CryoField", "Área Criogénica",
            "Un área que se mueve contigo ralentiza y daña levemente a los enemigos que la tocan.",
            UpgradeType.MoveSpeed, 5, UpgradeType.MagnetRange, 5, cryoPrefab, cryoConfig);

        SynergyData laser = CreateSynergyData("Synergy_LaserBeam", "Rayo Láser",
            "Cada cierto tiempo disparas un rayo perforante en línea recta hacia donde miras.",
            UpgradeType.AttackRange, 5, UpgradeType.AttackSpeed, 5, laserPrefab, laserConfig);

        SynergyData emp = CreateSynergyData("Synergy_EmpPulse", "Pulso Electromagnético",
            "Un pulso periódico congela a los enemigos cercanos; el congelamiento se contagia entre ellos sin acumularse.",
            UpgradeType.AttackRange, 5, UpgradeType.MagnetRange, 5, empPrefab, empConfig);

        SynergyDatabase database = AssetDatabase.LoadAssetAtPath<SynergyDatabase>(DatabasePath);
        if (database == null)
        {
            database = ScriptableObject.CreateInstance<SynergyDatabase>();
            AssetDatabase.CreateAsset(database, DatabasePath);
        }

        database.allSynergies = new List<SynergyData> { cryo, laser, emp };
        EditorUtility.SetDirty(database);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[SynergySetup] Sistema de sinergias listo: 3 SynergyData + 3 configs de efecto en {ConfigFolder}, 3 prefabs en {PrefabFolder}, base de datos en {DatabasePath}. Ejecuta 'Tools > Manners > Sandbox > 1. Crear assets del sandbox' para duplicarlas al sandbox.");
    }

    private static GameObject CreateEffectPrefab<T>(string name) where T : Component
    {
        string path = $"{PrefabFolder}/{name}.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        GameObject temp = new GameObject(name);
        temp.AddComponent<T>();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, path);
        Object.DestroyImmediate(temp);

        return prefab;
    }

    private static SynergyData CreateSynergyData(string assetName, string displayName, string description,
        UpgradeType upgradeA, int levelA, UpgradeType upgradeB, int levelB, GameObject effectPrefab, SynergyEffectConfig effectConfig)
    {
        string path = $"{ConfigFolder}/{assetName}.asset";
        SynergyData data = AssetDatabase.LoadAssetAtPath<SynergyData>(path);

        if (data == null)
        {
            data = ScriptableObject.CreateInstance<SynergyData>();
            AssetDatabase.CreateAsset(data, path);
        }

        data.synergyName = displayName;
        data.description = description;
        data.requiredUpgradeA = upgradeA;
        data.requiredLevelA = levelA;
        data.requiredUpgradeB = upgradeB;
        data.requiredLevelB = levelB;
        data.effectPrefab = effectPrefab;
        data.effectConfig = effectConfig;

        EditorUtility.SetDirty(data);
        return data;
    }

    private static T CreateEffectConfig<T>(string name) where T : SynergyEffectConfig
    {
        string path = $"{ConfigFolder}/{name}.asset";
        T config = AssetDatabase.LoadAssetAtPath<T>(path);
        if (config != null) return config;

        config = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(config, path);
        return config;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
