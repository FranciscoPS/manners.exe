using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class SandboxDiffTool
{
    [MenuItem("Tools/Manners/Sandbox/4. Comparar sandbox vs producción", false, 13)]
    public static void ReportDifferences()
    {
        StringBuilder report = new StringBuilder(2048);
        int totalDiffs = 0;

        GameBalanceConfig sandboxBalance = AssetDatabase.LoadAssetAtPath<GameBalanceConfig>(SandboxSetupTools.BalancePath);
        GameBalanceConfig prodBalance = AssetDatabase.LoadAssetAtPath<GameBalanceConfig>("Assets/Resources/GameBalanceConfig.asset");
        totalDiffs += CompareAsset(sandboxBalance, prodBalance, "GameBalanceConfig", report);

        UpgradeDatabase sandboxUpgradeDb = AssetDatabase.LoadAssetAtPath<UpgradeDatabase>(SandboxSetupTools.UpgradeDatabasePath);
        UpgradeDatabase prodUpgradeDb = AssetDatabase.LoadAssetAtPath<UpgradeDatabase>("Assets/Resources/UpgradeDatabase.asset");

        if (prodUpgradeDb != null)
        {
            Dictionary<UpgradeData, UpgradeData> upgradeMap = MapByFilename(prodUpgradeDb.allUpgrades, SandboxSetupTools.UpgradesFolder);
            foreach (KeyValuePair<UpgradeData, UpgradeData> pair in upgradeMap)
                totalDiffs += CompareAsset(pair.Value, pair.Key, $"UpgradeData/{pair.Key.name}", report);
        }

        SynergyDatabase sandboxSynergyDb = AssetDatabase.LoadAssetAtPath<SynergyDatabase>(SandboxSetupTools.SynergyDatabasePath);
        SynergyDatabase prodSynergyDb = AssetDatabase.LoadAssetAtPath<SynergyDatabase>("Assets/Resources/SynergyDatabase.asset");

        if (prodSynergyDb != null)
        {
            Dictionary<SynergyData, SynergyData> synergyMap = MapByFilename(prodSynergyDb.allSynergies, SandboxSetupTools.SynergiesFolder);
            foreach (KeyValuePair<SynergyData, SynergyData> pair in synergyMap)
            {
                totalDiffs += CompareAsset(pair.Value, pair.Key, $"SynergyData/{pair.Key.name}", report);

                if (pair.Key.effectConfig == null) continue;

                string sourcePath = AssetDatabase.GetAssetPath(pair.Key.effectConfig);
                string targetPath = $"{SandboxSetupTools.SynergiesFolder}/{Path.GetFileName(sourcePath)}";
                SynergyEffectConfig sandboxConfig = AssetDatabase.LoadAssetAtPath<SynergyEffectConfig>(targetPath);

                totalDiffs += CompareAsset(sandboxConfig, pair.Key.effectConfig, $"SynergyEffectConfig/{pair.Key.effectConfig.name}", report);
            }
        }

        Dictionary<EnemyConfiguration, EnemyConfiguration> enemyMap = SandboxSetupTools.LoadEnemyMap();
        foreach (KeyValuePair<EnemyConfiguration, EnemyConfiguration> pair in enemyMap)
            totalDiffs += CompareAsset(pair.Value, pair.Key, $"EnemyConfiguration/{pair.Key.name}", report);

        Dictionary<WaveData, WaveData> waveMap = SandboxSetupTools.LoadWaveMap();
        foreach (KeyValuePair<WaveData, WaveData> pair in waveMap)
            totalDiffs += CompareAsset(pair.Value, pair.Key, $"WaveData/{pair.Key.name}", report);

        if (totalDiffs == 0)
        {
            Debug.Log("[SandboxDiff] Sin diferencias: los assets del sandbox coinciden con los de producción en todos los valores rastreados.");
            return;
        }

        Debug.Log($"[SandboxDiff] {totalDiffs} valor(es) distinto(s) entre sandbox y producción (formato: producción → sandbox). Esto es un chequeo automático de campos numéricos/simples; revísalo antes de copiar nada a mano — no compara referencias a otros assets (prefabs, sprites, etc.), solo números, texto, bool, color y enum:\n{report}");
    }

    private static Dictionary<T, T> MapByFilename<T>(IList<T> productionList, string sandboxFolder) where T : Object
    {
        Dictionary<T, T> map = new Dictionary<T, T>();
        if (productionList == null) return map;

        for (int i = 0; i < productionList.Count; i++)
        {
            T source = productionList[i];
            if (source == null) continue;

            string sourcePath = AssetDatabase.GetAssetPath(source);
            string targetPath = $"{sandboxFolder}/{Path.GetFileName(sourcePath)}";
            T sandboxCopy = AssetDatabase.LoadAssetAtPath<T>(targetPath);

            if (sandboxCopy != null) map[source] = sandboxCopy;
        }

        return map;
    }

    private static int CompareAsset(Object sandboxAsset, Object prodAsset, string label, StringBuilder report)
    {
        if (sandboxAsset == null || prodAsset == null) return 0;

        SerializedObject sandboxSO = new SerializedObject(sandboxAsset);
        SerializedObject prodSO = new SerializedObject(prodAsset);

        SerializedProperty prop = sandboxSO.GetIterator();
        bool enterChildren = true;
        int diffs = 0;

        while (prop.NextVisible(enterChildren))
        {
            enterChildren = prop.propertyType != SerializedPropertyType.ObjectReference;

            if (prop.name == "m_Script" || prop.propertyType == SerializedPropertyType.ObjectReference)
                continue;

            SerializedProperty target = prodSO.FindProperty(prop.propertyPath);
            if (target == null || target.propertyType != prop.propertyType) continue;

            if (!ValuesDiffer(prop, target)) continue;

            report.AppendLine($"  {label}.{prop.propertyPath}: {FormatValue(target)} → {FormatValue(prop)}");
            diffs++;
        }

        return diffs;
    }

    private static bool ValuesDiffer(SerializedProperty a, SerializedProperty b)
    {
        switch (a.propertyType)
        {
            case SerializedPropertyType.Integer: return a.intValue != b.intValue;
            case SerializedPropertyType.Boolean: return a.boolValue != b.boolValue;
            case SerializedPropertyType.Float: return !Mathf.Approximately(a.floatValue, b.floatValue);
            case SerializedPropertyType.String: return a.stringValue != b.stringValue;
            case SerializedPropertyType.Color: return a.colorValue != b.colorValue;
            case SerializedPropertyType.Enum: return a.enumValueIndex != b.enumValueIndex;
            case SerializedPropertyType.Vector2: return a.vector2Value != b.vector2Value;
            case SerializedPropertyType.Vector3: return a.vector3Value != b.vector3Value;
            case SerializedPropertyType.Vector4: return a.vector4Value != b.vector4Value;
            case SerializedPropertyType.ArraySize: return a.intValue != b.intValue;
            default: return false;
        }
    }

    private static string FormatValue(SerializedProperty p)
    {
        switch (p.propertyType)
        {
            case SerializedPropertyType.Integer: return p.intValue.ToString();
            case SerializedPropertyType.Boolean: return p.boolValue.ToString();
            case SerializedPropertyType.Float: return p.floatValue.ToString("F3");
            case SerializedPropertyType.String: return p.stringValue;
            case SerializedPropertyType.Color: return p.colorValue.ToString();
            case SerializedPropertyType.Enum:
                return p.enumValueIndex >= 0 && p.enumValueIndex < p.enumDisplayNames.Length
                    ? p.enumDisplayNames[p.enumValueIndex]
                    : p.enumValueIndex.ToString();
            case SerializedPropertyType.Vector2: return p.vector2Value.ToString();
            case SerializedPropertyType.Vector3: return p.vector3Value.ToString();
            case SerializedPropertyType.Vector4: return p.vector4Value.ToString();
            case SerializedPropertyType.ArraySize: return p.intValue.ToString();
            default: return "?";
        }
    }
}
