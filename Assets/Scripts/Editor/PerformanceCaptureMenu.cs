using UnityEditor;
using UnityEngine;

public static class PerformanceCaptureMenu
{
    [MenuItem("Tools/Manners/Performance/Start CSV Capture (Play Mode)", false, 62)]
    public static void StartCapture() => PerformanceCapture.BeginCapture();

    [MenuItem("Tools/Manners/Performance/Start CSV Capture (Play Mode)", true)]
    private static bool CanStart() => EditorApplication.isPlaying && !PerformanceCapture.IsCapturing;

    [MenuItem("Tools/Manners/Performance/Stop CSV Capture", false, 63)]
    public static void StopCapture() => PerformanceCapture.EndCapture();

    [MenuItem("Tools/Manners/Performance/Stop CSV Capture", true)]
    private static bool CanStop() => PerformanceCapture.IsCapturing;

    [MenuItem("Tools/Manners/Performance/Open Capture Folder", false, 64)]
    private static void OpenFolder() => EditorUtility.RevealInFinder(Application.persistentDataPath);
}
