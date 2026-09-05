using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class SandboxDiffTool
{
    public enum SyncDirection
    {
        SandboxToProduction,
        ProductionToSandbox
    }

    public class AssetPair
    {
        public string label;
        public Object production;
        public Object sandbox;

        public Object Source(SyncDirection direction)
        {
            return direction == SyncDirection.SandboxToProduction ? sandbox : production;
        }

        public Object Target(SyncDirection direction)
        {
            return direction == SyncDirection.SandboxToProduction ? production : sandbox;
        }
    }

    public class DiffEntry
    {
        public AssetPair pair;
        public string propertyPath;
        public string displayName;
        public string productionValue;
        public string sandboxValue;
        public bool isArray;
        public bool isReference;
        public bool selected = true;
    }

    public class DiffResult
    {
        public readonly List<AssetPair> pairs = new List<AssetPair>();
        public readonly List<DiffEntry> entries = new List<DiffEntry>();
        public readonly Dictionary<Object, Object> productionToSandbox = new Dictionary<Object, Object>();
        public readonly Dictionary<Object, Object> sandboxToProduction = new Dictionary<Object, Object>();
    }

    private const string ProductionBalancePath = "Assets/Resources/GameBalanceConfig.asset";
    private const string ProductionUpgradeDatabasePath = "Assets/Resources/UpgradeDatabase.asset";
    private const string ProductionSynergyDatabasePath = "Assets/Resources/SynergyDatabase.asset";
    private const string ProductionChestOpeningConfigPath = "Assets/Resources/ChestOpeningConfig.asset";
    private const string ProductionSynergiesFolder = "Assets/Configurations/Synergies";
    private const string ProductionConfigurationsFolder = "Assets/Configurations/";
    private const string ProductionResourcesFolder = "Assets/Resources/";

    public static DiffResult Compare()
    {
        DiffResult result = new DiffResult();
        CollectPairs(result);

        result.pairs.Sort((a, b) => string.CompareOrdinal(a.label, b.label));

        for (int i = 0; i < result.pairs.Count; i++)
            CollectDiffs(result, result.pairs[i]);

        return result;
    }

    public static int Apply(DiffResult result, IList<DiffEntry> entries, SyncDirection direction)
    {
        Dictionary<Object, Object> referenceMap = direction == SyncDirection.SandboxToProduction
            ? result.sandboxToProduction
            : result.productionToSandbox;

        Dictionary<AssetPair, List<DiffEntry>> byPair = new Dictionary<AssetPair, List<DiffEntry>>();
        for (int i = 0; i < entries.Count; i++)
        {
            if (!byPair.TryGetValue(entries[i].pair, out List<DiffEntry> list))
            {
                list = new List<DiffEntry>();
                byPair[entries[i].pair] = list;
            }

            list.Add(entries[i]);
        }

        int applied = 0;
        int skipped = 0;

        foreach (KeyValuePair<AssetPair, List<DiffEntry>> group in byPair)
        {
            Object source = group.Key.Source(direction);
            Object target = group.Key.Target(direction);
            if (source == null || target == null) continue;

            SerializedObject sourceSO = new SerializedObject(source);
            SerializedObject targetSO = new SerializedObject(target);
            bool changed = false;

            for (int i = 0; i < group.Value.Count; i++)
            {
                DiffEntry entry = group.Value[i];
                SerializedProperty sourceProperty = sourceSO.FindProperty(entry.propertyPath);
                if (sourceProperty == null) continue;

                if (entry.isReference)
                {
                    Object resolved = ResolveReference(sourceProperty.objectReferenceValue, referenceMap, direction, out string problem);
                    if (problem != null)
                    {
                        Debug.LogWarning($"[SandboxSync] {group.Key.label}.{entry.displayName} no se copió: {problem}");
                        skipped++;
                        continue;
                    }

                    targetSO.FindProperty(entry.propertyPath).objectReferenceValue = resolved;
                }
                else
                {
                    targetSO.CopyFromSerializedProperty(sourceProperty);

                    if (entry.isArray)
                        skipped += RemapReferences(group.Key.label, targetSO.FindProperty(entry.propertyPath), referenceMap, direction);
                }

                changed = true;
                applied++;
            }

            if (changed)
            {
                targetSO.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
            }
        }

        if (applied > 0)
            AssetDatabase.SaveAssets();

        if (skipped > 0)
            Debug.LogWarning($"[SandboxSync] {skipped} referencia(s) se dejaron sin copiar porque apuntan a assets exclusivos del otro lado sin contraparte (detalle arriba).");

        return applied;
    }

    public static string BuildReport(DiffResult result)
    {
        if (result.entries.Count == 0)
            return "[SandboxDiff] Sin diferencias: los assets del sandbox coinciden con los de producción en todos los valores rastreados.";

        StringBuilder report = new StringBuilder(2048);
        report.AppendLine($"[SandboxDiff] {result.entries.Count} valor(es) distinto(s) entre sandbox y producción (formato: producción → sandbox). Compara números, texto, bool, color, enum, vectores, tamaños de listas y referencias a otros assets (prefabs, materiales, sprites, clips); una referencia a la copia sandbox de un asset cuenta como igual a la referencia a su original de producción:");

        for (int i = 0; i < result.entries.Count; i++)
        {
            DiffEntry entry = result.entries[i];
            report.AppendLine($"  {entry.pair.label}.{entry.displayName}: {entry.productionValue} → {entry.sandboxValue}");
        }

        return report.ToString();
    }

    private static void CollectPairs(DiffResult result)
    {
        AddPair(result, "GameBalanceConfig",
            Load<GameBalanceConfig>(ProductionBalancePath),
            Load<GameBalanceConfig>(SandboxSetupTools.BalancePath));

        AddPair(result, "ChestOpeningConfig",
            Load<ChestOpeningConfig>(ProductionChestOpeningConfigPath),
            Load<ChestOpeningConfig>(SandboxSetupTools.ChestOpeningConfigPath));

        UpgradeDatabase productionUpgradeDb = Load<UpgradeDatabase>(ProductionUpgradeDatabasePath);
        AddPair(result, "UpgradeDatabase", productionUpgradeDb, Load<UpgradeDatabase>(SandboxSetupTools.UpgradeDatabasePath));

        if (productionUpgradeDb != null)
        {
            foreach (KeyValuePair<UpgradeData, UpgradeData> pair in MapByFilename(productionUpgradeDb.allUpgrades, SandboxSetupTools.UpgradesFolder))
                AddPair(result, $"UpgradeData/{pair.Key.name}", pair.Key, pair.Value);
        }

        AddPair(result, "SynergyDatabase",
            Load<SynergyDatabase>(ProductionSynergyDatabasePath),
            Load<SynergyDatabase>(SandboxSetupTools.SynergyDatabasePath));

        foreach (SynergyData synergy in LoadAllInFolder<SynergyData>(ProductionSynergiesFolder))
            AddPair(result, $"SynergyData/{synergy.name}", synergy, LoadCounterpart<SynergyData>(synergy, SandboxSetupTools.SynergiesFolder));

        foreach (SynergyEffectConfig config in LoadAllInFolder<SynergyEffectConfig>(ProductionSynergiesFolder))
            AddPair(result, $"SynergyEffectConfig/{config.name}", config, LoadCounterpart<SynergyEffectConfig>(config, SandboxSetupTools.SynergiesFolder));

        foreach (KeyValuePair<EnemyConfiguration, EnemyConfiguration> pair in SandboxSetupTools.LoadEnemyMap())
            AddPair(result, $"EnemyConfiguration/{pair.Key.name}", pair.Key, pair.Value);

        foreach (KeyValuePair<WaveData, WaveData> pair in SandboxSetupTools.LoadWaveMap())
            AddPair(result, $"WaveData/{pair.Key.name}", pair.Key, pair.Value);
    }

    private static void AddPair(DiffResult result, string label, Object production, Object sandbox)
    {
        if (production == null || sandbox == null) return;

        result.pairs.Add(new AssetPair { label = label, production = production, sandbox = sandbox });
        result.productionToSandbox[production] = sandbox;
        result.sandboxToProduction[sandbox] = production;
    }

    private static T Load<T>(string path) where T : Object
    {
        return AssetDatabase.LoadAssetAtPath<T>(path);
    }

    private static List<T> LoadAllInFolder<T>(string folder) where T : Object
    {
        List<T> assets = new List<T>();
        if (!AssetDatabase.IsValidFolder(folder)) return assets;

        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder });
        for (int i = 0; i < guids.Length; i++)
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[i]));
            if (asset != null) assets.Add(asset);
        }

        assets.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return assets;
    }

    private static T LoadCounterpart<T>(Object source, string folder) where T : Object
    {
        string fileName = Path.GetFileName(AssetDatabase.GetAssetPath(source));
        return AssetDatabase.LoadAssetAtPath<T>($"{folder}/{fileName}");
    }

    private static Dictionary<T, T> MapByFilename<T>(IList<T> productionList, string sandboxFolder) where T : Object
    {
        Dictionary<T, T> map = new Dictionary<T, T>();
        if (productionList == null) return map;

        for (int i = 0; i < productionList.Count; i++)
        {
            T source = productionList[i];
            if (source == null) continue;

            T sandboxCopy = LoadCounterpart<T>(source, sandboxFolder);
            if (sandboxCopy != null) map[source] = sandboxCopy;
        }

        return map;
    }

    private static void CollectDiffs(DiffResult result, AssetPair pair)
    {
        SerializedObject productionSO = new SerializedObject(pair.production);
        SerializedObject sandboxSO = new SerializedObject(pair.sandbox);

        SerializedProperty property = productionSO.GetIterator();
        bool enterChildren = true;

        while (property.NextVisible(enterChildren))
        {
            enterChildren = false;

            if (property.name == "m_Script")
                continue;

            SerializedProperty other = sandboxSO.FindProperty(property.propertyPath);
            if (other == null || other.propertyType != property.propertyType)
                continue;

            if (property.propertyType == SerializedPropertyType.ObjectReference)
            {
                if (!ReferencesDiffer(result, property.objectReferenceValue, other.objectReferenceValue))
                    continue;

                result.entries.Add(new DiffEntry
                {
                    pair = pair,
                    propertyPath = property.propertyPath,
                    displayName = DisplayName(property.propertyPath),
                    productionValue = FormatReference(property.objectReferenceValue),
                    sandboxValue = FormatReference(other.objectReferenceValue),
                    isReference = true
                });
                continue;
            }

            if (property.isArray && property.propertyType != SerializedPropertyType.String)
            {
                if (property.arraySize != other.arraySize)
                {
                    result.entries.Add(new DiffEntry
                    {
                        pair = pair,
                        propertyPath = property.propertyPath,
                        displayName = DisplayName(property.propertyPath),
                        productionValue = $"{property.arraySize} elemento(s)",
                        sandboxValue = $"{other.arraySize} elemento(s)",
                        isArray = true
                    });
                    continue;
                }

                enterChildren = true;
                continue;
            }

            if (property.propertyType == SerializedPropertyType.Generic)
            {
                enterChildren = true;
                continue;
            }

            if (property.propertyType == SerializedPropertyType.ArraySize || !ValuesDiffer(property, other))
                continue;

            result.entries.Add(new DiffEntry
            {
                pair = pair,
                propertyPath = property.propertyPath,
                displayName = DisplayName(property.propertyPath),
                productionValue = FormatValue(property),
                sandboxValue = FormatValue(other)
            });
        }
    }

    private static bool ReferencesDiffer(DiffResult result, Object production, Object sandbox)
    {
        if (production == sandbox) return false;
        if (production == null || sandbox == null) return true;

        return !(result.productionToSandbox.TryGetValue(production, out Object mapped) && mapped == sandbox);
    }

    private static Object ResolveReference(Object source, Dictionary<Object, Object> map, SyncDirection direction, out string problem)
    {
        problem = null;
        if (source == null) return null;
        if (map.TryGetValue(source, out Object mapped)) return mapped;

        string path = AssetDatabase.GetAssetPath(source);
        if (IsExclusiveToSourceSide(path, direction))
        {
            string side = direction == SyncDirection.SandboxToProduction ? "del sandbox" : "de producción";
            problem = $"'{path}' es un asset exclusivo {side} sin copia en el otro lado. Crea primero su contraparte (Tools > Manners > Sandbox > 1) y vuelve a sincronizar.";
            return null;
        }

        return source;
    }

    private static bool IsExclusiveToSourceSide(string path, SyncDirection direction)
    {
        if (string.IsNullOrEmpty(path)) return false;

        bool insideSandbox = path.StartsWith(SandboxSetupTools.SandboxFolder + "/");
        if (direction == SyncDirection.SandboxToProduction)
            return insideSandbox;

        if (insideSandbox) return false;
        return path.StartsWith(ProductionConfigurationsFolder) || path.StartsWith(ProductionResourcesFolder);
    }

    private static int RemapReferences(string label, SerializedProperty root, Dictionary<Object, Object> map, SyncDirection direction)
    {
        if (root == null) return 0;

        int skipped = 0;
        SerializedProperty iterator = root.Copy();
        SerializedProperty end = iterator.GetEndProperty();

        while (iterator.NextVisible(true) && !SerializedProperty.EqualContents(iterator, end))
        {
            if (iterator.propertyType != SerializedPropertyType.ObjectReference) continue;

            Object resolved = ResolveReference(iterator.objectReferenceValue, map, direction, out string problem);
            if (problem != null)
            {
                Debug.LogWarning($"[SandboxSync] {label}.{DisplayName(iterator.propertyPath)} quedó vacío: {problem}");
                skipped++;
            }

            iterator.objectReferenceValue = resolved;
        }

        return skipped;
    }

    private static string DisplayName(string propertyPath)
    {
        return propertyPath.Replace(".Array.data[", "[");
    }

    private static string FormatReference(Object reference)
    {
        if (reference == null) return "(ninguno)";

        string path = AssetDatabase.GetAssetPath(reference);
        return string.IsNullOrEmpty(path) ? reference.name : Path.GetFileName(path);
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
            case SerializedPropertyType.Vector2Int: return a.vector2IntValue != b.vector2IntValue;
            case SerializedPropertyType.Vector3Int: return a.vector3IntValue != b.vector3IntValue;
            default: return false;
        }
    }

    private static string FormatValue(SerializedProperty p)
    {
        switch (p.propertyType)
        {
            case SerializedPropertyType.Integer: return p.intValue.ToString();
            case SerializedPropertyType.Boolean: return p.boolValue.ToString();
            case SerializedPropertyType.Float: return p.floatValue.ToString("0.###");
            case SerializedPropertyType.String: return string.IsNullOrEmpty(p.stringValue) ? "(vacío)" : p.stringValue;
            case SerializedPropertyType.Color: return p.colorValue.ToString();
            case SerializedPropertyType.Enum:
                return p.enumValueIndex >= 0 && p.enumValueIndex < p.enumDisplayNames.Length
                    ? p.enumDisplayNames[p.enumValueIndex]
                    : p.enumValueIndex.ToString();
            case SerializedPropertyType.Vector2: return p.vector2Value.ToString();
            case SerializedPropertyType.Vector3: return p.vector3Value.ToString();
            case SerializedPropertyType.Vector4: return p.vector4Value.ToString();
            case SerializedPropertyType.Vector2Int: return p.vector2IntValue.ToString();
            case SerializedPropertyType.Vector3Int: return p.vector3IntValue.ToString();
            default: return "?";
        }
    }
}
