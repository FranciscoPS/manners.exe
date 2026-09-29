using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class OverrideSetupTools
{
    private const string ConfigFolder = "Assets/Configurations/Overrides";
    private const string PrefabFolder = "Assets/Prefabs/Overrides";
    private const string DatabasePath = "Assets/Resources/OverrideDatabase.asset";

    [MenuItem("Tools/Manners/Overrides/Crear sistema de sobrecargas", false, 20)]
    public static void CreateOverrideSystem()
    {
        EditorAssetUtility.EnsureFolder(ConfigFolder);
        EditorAssetUtility.EnsureFolder(PrefabFolder);

        GameObject cryoPrefab = CreateEffectPrefab<CryoFieldEffect>("CryoFieldEffect");
        GameObject laserPrefab = CreateEffectPrefab<LaserBeamEffect>("LaserBeamEffect");
        GameObject empPrefab = CreateEffectPrefab<EmpPulseEffect>("EmpPulseEffect");

        CryoFieldConfig cryoConfig = CreateEffectConfig<CryoFieldConfig>("CryoFieldConfig");
        LaserBeamConfig laserConfig = CreateEffectConfig<LaserBeamConfig>("LaserBeamConfig");
        EmpPulseConfig empConfig = CreateEffectConfig<EmpPulseConfig>("EmpPulseConfig");

        OverrideData cryo = CreateOverrideData("Override_CryoField", "Área Criogénica",
            "Un campo que te sigue ralentiza y daña. Multi disparo añade pulsos; con Balas explosivas y Cadena de impacto, cada tick explota y empuja. Potencia el láser y prolonga el EMP.",
            UpgradeType.MoveSpeed, 5, UpgradeType.MagnetRange, 5, cryoPrefab, cryoConfig);

        OverrideData laser = CreateOverrideData("Override_LaserBeam", "Rayo Láser",
            "Un rayo perforante barre al enemigo más cercano. Multi disparo abre un abanico; con Balas explosivas y Cadena de impacto, cada rayo explota y empuja por tick. Con frío o EMP, causa más daño.",
            UpgradeType.AttackRange, 5, UpgradeType.AttackSpeed, 5, laserPrefab, laserConfig);

        OverrideData emp = CreateOverrideData("Override_EmpPulse", "Pulso Electromagnético",
            "Un pulso congela y contagia a enemigos cercanos. Multi disparo repite la onda; con Balas explosivas y Cadena de impacto, cada onda explota y empuja. El láser añade daño y el frío prolonga la congelación.",
            UpgradeType.AttackRange, 5, UpgradeType.MagnetRange, 5, empPrefab, empConfig);

        OverrideDatabase database = AssetDatabase.LoadAssetAtPath<OverrideDatabase>(DatabasePath);
        if (database == null)
        {
            database = ScriptableObject.CreateInstance<OverrideDatabase>();
            AssetDatabase.CreateAsset(database, DatabasePath);
        }

        database.allOverrides = new List<OverrideData> { cryo, laser, emp };
        EditorUtility.SetDirty(database);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[OverrideSetup] Sistema de sobrecargas listo: 3 OverrideData + 3 configs de efecto en {ConfigFolder}, 3 prefabs en {PrefabFolder}, base de datos en {DatabasePath}. Ejecuta 'Tools > Manners > Sandbox > 1. Crear assets del sandbox' para duplicarlas al sandbox.");
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

    private static OverrideData CreateOverrideData(string assetName, string displayName, string description,
        UpgradeType upgradeA, int levelA, UpgradeType upgradeB, int levelB, GameObject effectPrefab, OverrideEffectConfig effectConfig)
    {
        string path = $"{ConfigFolder}/{assetName}.asset";
        OverrideData data = AssetDatabase.LoadAssetAtPath<OverrideData>(path);

        if (data == null)
        {
            data = ScriptableObject.CreateInstance<OverrideData>();
            AssetDatabase.CreateAsset(data, path);
        }

        data.overrideName = displayName;
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

    private static T CreateEffectConfig<T>(string name) where T : OverrideEffectConfig
    {
        string path = $"{ConfigFolder}/{name}.asset";
        T config = AssetDatabase.LoadAssetAtPath<T>(path);
        if (config != null) return config;

        config = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(config, path);
        return config;
    }

}
