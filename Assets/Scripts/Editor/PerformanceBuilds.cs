using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class PerformanceBuilds
{
    public static void WindowsPerformanceReview()
    {
        PerformanceValidation.RunChecks();
        WebBuildRenderingChecks.Run();
        bool previousTiming = PlayerSettings.enableFrameTimingStats;
        try
        {
            PlayerSettings.enableFrameTimingStats = true;
            Build(BuildTarget.StandaloneWindows64, "Builds/PerformanceReview/Windows/manners.exe");
        }
        finally { PlayerSettings.enableFrameTimingStats = previousTiming; }
    }

    public static void Windows()
    {
        WebBuildRenderingChecks.Run();
        Build(BuildTarget.StandaloneWindows64, "Builds/Compatibility-2026-09-07/Windows/manners.exe");
    }

    public static void Web()
    {
        Build(BuildTarget.WebGL, "Builds/Compatibility-2026-09-07/Web");
    }

    private static void Build(BuildTarget target, string path)
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Use a separate batch-mode Editor.");
        var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
            target = target,
            locationPathName = path,
            options = BuildOptions.None
        });
        Directory.CreateDirectory("Logs");
        File.WriteAllText($"Logs/performance-build-{target}.txt", $"{result.summary.result}\n{result.summary.outputPath}\nErrors: {result.summary.totalErrors}\nDuration: {result.summary.totalTime}\nBytes: {result.summary.totalSize}");
        if (result.summary.result != BuildResult.Succeeded)
            throw new Exception($"{target} build failed. See the build log.");
    }
}
