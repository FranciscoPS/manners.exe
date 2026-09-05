using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MusicSetupTools
{
    private static readonly Regex IntroPattern = new Regex(@"(^|[\s_\-])intro$", RegexOptions.IgnoreCase);
    private static readonly Regex OutroPattern = new Regex(@"(^|[\s_\-])outro$", RegexOptions.IgnoreCase);
    private static readonly Regex LoopPattern = new Regex(@"(^|[\s_\-])loop\s*(\d+)$", RegexOptions.IgnoreCase);
    private static readonly Regex BridgePattern = new Regex(@"(^|[\s_\-])puente\s*(\d+)$", RegexOptions.IgnoreCase);

    private class ClipSet
    {
        public AudioClip intro;
        public AudioClip outro;
        public readonly SortedDictionary<int, AudioClip> loops = new SortedDictionary<int, AudioClip>();
        public readonly SortedDictionary<int, AudioClip> bridges = new SortedDictionary<int, AudioClip>();
        public readonly List<string> ignored = new List<string>();
    }

    private struct SectionSettings
    {
        public int repeatCount;
        public float startAtSeconds;
    }

    [MenuItem("Tools/Manners/Música/Asignar loops por nombre (MusicManager de la escena abierta)", false, 50)]
    public static void AssignLoopsByName()
    {
        MusicManager manager = Object.FindFirstObjectByType<MusicManager>(FindObjectsInactive.Include);
        if (manager == null)
        {
            Debug.LogError("[MusicSetup] No hay MusicManager en la escena abierta. Abre Assets/Scenes/MainMenu.unity (ahí vive) y vuelve a ejecutar la herramienta.");
            return;
        }

        SerializedObject serialized = new SerializedObject(manager);
        SerializedProperty configs = serialized.FindProperty("sceneMusicConfigs");
        if (configs == null || configs.arraySize == 0)
        {
            Debug.LogWarning("[MusicSetup] El MusicManager no tiene entradas en 'Scene Music Configs'. Agrega una por escena (índice de build) y escribe su 'Clip Folder'.");
            return;
        }

        int filled = 0;
        for (int i = 0; i < configs.arraySize; i++)
        {
            if (FillConfig(configs.GetArrayElementAtIndex(i)))
                filled++;
        }

        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(manager);
        EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);

        Debug.Log($"[MusicSetup] {filled} escena(s) con música asignada por nombre. Guarda la escena (Ctrl/Cmd+S) para conservarlo.");
    }

    private static bool FillConfig(SerializedProperty config)
    {
        int sceneIndex = config.FindPropertyRelative("sceneIndex").intValue;
        SerializedProperty folderProperty = config.FindPropertyRelative("clipFolder");
        string folder = ResolveFolder(config, folderProperty.stringValue);

        if (string.IsNullOrEmpty(folder))
        {
            Debug.LogWarning($"[MusicSetup] Escena {sceneIndex}: no tiene 'Clip Folder' ni clips asignados de los que deducir la carpeta; se omite.");
            return false;
        }

        if (!AssetDatabase.IsValidFolder(folder))
        {
            Debug.LogWarning($"[MusicSetup] Escena {sceneIndex}: la carpeta '{folder}' no existe en el proyecto; se omite.");
            return false;
        }

        ClipSet set = ScanFolder(folder);
        if (set.loops.Count == 0)
        {
            Debug.LogWarning($"[MusicSetup] Escena {sceneIndex}: en '{folder}' no hay ningún clip llamado '... loop1', '... loop2', etc.; se omite.");
            return false;
        }

        folderProperty.stringValue = folder;

        SerializedProperty sections = config.FindPropertyRelative("loopSections");
        Dictionary<AudioClip, SectionSettings> previous = ReadPreviousSettings(sections);

        List<int> loopNumbers = new List<int>(set.loops.Keys);
        int lastNumber = loopNumbers[loopNumbers.Count - 1];
        int regularCount = loopNumbers.Count;
        AudioClip overtimeBridge = null;
        AudioClip overtimeLoop = null;

        if (set.outro != null)
        {
            overtimeBridge = set.outro;
        }
        else if (loopNumbers.Count >= 2)
        {
            int beforeLast = loopNumbers[loopNumbers.Count - 2];
            if (set.bridges.ContainsKey(beforeLast) && !set.bridges.ContainsKey(lastNumber))
            {
                overtimeBridge = set.bridges[beforeLast];
                overtimeLoop = set.loops[lastNumber];
                regularCount = loopNumbers.Count - 1;
            }
        }

        config.FindPropertyRelative("introClip").objectReferenceValue = set.intro;

        sections.arraySize = regularCount;
        StringBuilder summary = new StringBuilder();
        for (int i = 0; i < regularCount; i++)
        {
            int number = loopNumbers[i];
            AudioClip loop = set.loops[number];
            AudioClip bridge = null;
            bool lastRegular = i == regularCount - 1;
            if (!lastRegular || overtimeLoop == null)
                set.bridges.TryGetValue(number, out bridge);

            SerializedProperty section = sections.GetArrayElementAtIndex(i);
            section.FindPropertyRelative("loopClip").objectReferenceValue = loop;
            section.FindPropertyRelative("bridgeClip").objectReferenceValue = bridge;

            previous.TryGetValue(loop, out SectionSettings settings);
            section.FindPropertyRelative("repeatCount").intValue = settings.repeatCount;
            section.FindPropertyRelative("startAtSeconds").floatValue = settings.startAtSeconds;

            summary.Append(loop.name);
            if (bridge != null) summary.Append(" → ").Append(bridge.name);
            summary.Append(i < regularCount - 1 ? " → " : "");
        }

        config.FindPropertyRelative("overtimeBridgeClip").objectReferenceValue = overtimeBridge;
        config.FindPropertyRelative("overtimeLoopClip").objectReferenceValue = overtimeLoop;

        string overtimeSummary = overtimeBridge == null && overtimeLoop == null
            ? "sin overtime"
            : $"overtime: {(overtimeBridge != null ? overtimeBridge.name : "(sin puente)")} → {(overtimeLoop != null ? overtimeLoop.name : "(vuelve el último loop)")}";
        string introSummary = set.intro != null ? set.intro.name : "(sin intro)";
        string ignoredSummary = set.ignored.Count > 0 ? $" Ignorados (no siguen la convención): {string.Join(", ", set.ignored)}." : "";

        Debug.Log($"[MusicSetup] Escena {sceneIndex} ← {folder}: intro {introSummary} → {summary} · {overtimeSummary}.{ignoredSummary}");
        return true;
    }

    private static string ResolveFolder(SerializedProperty config, string configuredFolder)
    {
        if (!string.IsNullOrWhiteSpace(configuredFolder))
            return configuredFolder.Trim().TrimEnd('/');

        string fromIntro = FolderOf(config.FindPropertyRelative("introClip").objectReferenceValue);
        if (fromIntro != null) return fromIntro;

        SerializedProperty sections = config.FindPropertyRelative("loopSections");
        for (int i = 0; i < sections.arraySize; i++)
        {
            SerializedProperty section = sections.GetArrayElementAtIndex(i);
            string fromLoop = FolderOf(section.FindPropertyRelative("loopClip").objectReferenceValue);
            if (fromLoop != null) return fromLoop;
            string fromBridge = FolderOf(section.FindPropertyRelative("bridgeClip").objectReferenceValue);
            if (fromBridge != null) return fromBridge;
        }

        string fromOvertime = FolderOf(config.FindPropertyRelative("overtimeBridgeClip").objectReferenceValue)
            ?? FolderOf(config.FindPropertyRelative("overtimeLoopClip").objectReferenceValue)
            ?? FolderOf(config.FindPropertyRelative("loopClip").objectReferenceValue);
        return fromOvertime;
    }

    private static string FolderOf(Object clip)
    {
        if (clip == null) return null;
        string path = AssetDatabase.GetAssetPath(clip);
        if (string.IsNullOrEmpty(path)) return null;
        return Path.GetDirectoryName(path).Replace('\\', '/');
    }

    private static ClipSet ScanFolder(string folder)
    {
        ClipSet set = new ClipSet();
        string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { folder });

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (Path.GetDirectoryName(path).Replace('\\', '/') != folder) continue;

            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null) continue;

            string name = Path.GetFileNameWithoutExtension(path).Trim();
            Match loopMatch = LoopPattern.Match(name);
            Match bridgeMatch = BridgePattern.Match(name);

            if (loopMatch.Success)
                set.loops[int.Parse(loopMatch.Groups[2].Value)] = clip;
            else if (bridgeMatch.Success)
                set.bridges[int.Parse(bridgeMatch.Groups[2].Value)] = clip;
            else if (IntroPattern.IsMatch(name))
                set.intro = clip;
            else if (OutroPattern.IsMatch(name))
                set.outro = clip;
            else
                set.ignored.Add(name);
        }

        return set;
    }

    private static Dictionary<AudioClip, SectionSettings> ReadPreviousSettings(SerializedProperty sections)
    {
        Dictionary<AudioClip, SectionSettings> previous = new Dictionary<AudioClip, SectionSettings>();

        for (int i = 0; i < sections.arraySize; i++)
        {
            SerializedProperty section = sections.GetArrayElementAtIndex(i);
            AudioClip loop = section.FindPropertyRelative("loopClip").objectReferenceValue as AudioClip;
            if (loop == null) continue;

            previous[loop] = new SectionSettings
            {
                repeatCount = section.FindPropertyRelative("repeatCount").intValue,
                startAtSeconds = section.FindPropertyRelative("startAtSeconds").floatValue
            };
        }

        return previous;
    }
}
