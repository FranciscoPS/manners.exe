using System;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public sealed class PerformanceCapture : MonoBehaviour
{
    private static PerformanceCapture instance;
    public static bool IsCapturing => instance != null;
    private const int Capacity = 4096;
    private readonly double[] frameMilliseconds = new double[Capacity];
    private readonly FrameTiming[] timing = new FrameTiming[1];
    private StreamWriter writer;
    private int samples;
    private double elapsed;
    private double windowStart;
    private double cpuSum;
    private double gpuSum;
    private int cpuSamples;
    private int gpuSamples;
    private ulong lastTimestamp;
    private string capturedScene;
    private float capturedScale;
    private int capturedWidth, capturedHeight, capturedTarget, capturedVSync;
    private bool capturedFocus, capturedPause;
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartIfRequested()
    {
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-manners-perf-capture") < 0) return;
        BeginCapture();
    }

    public static void BeginCapture()
    {
        if (!Application.isPlaying || instance != null) return;
        var go = new GameObject("PerformanceCapture");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<PerformanceCapture>();
    }

    public static void EndCapture()
    {
        if (instance != null) Destroy(instance.gameObject);
    }

    private void Start()
    {
        try
        {
            string directory = Path.Combine(Application.persistentDataPath, "Performance");
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff", Invariant) + ".csv");
            writer = new StreamWriter(path);
            writer.WriteLine("utc,scene,seconds,frames,mean_fps,p95_frame_ms,p99_frame_ms,cpu_frame_ms_including_wait,gpu_ms,width,height,render_scale,fps_target,vsync,enemies,focused,paused");
            File.WriteAllText(Path.ChangeExtension(path, ".txt"),
                "Unity: " + Application.unityVersion + "\nGPU: " + SystemInfo.graphicsDeviceName +
                "\nAPI: " + SystemInfo.graphicsDeviceType + "\nCPU: " + SystemInfo.processorType +
                "\nRAM MB: " + SystemInfo.systemMemorySize + "\nGraphics memory MB: " + SystemInfo.graphicsMemorySize +
                "\nOS: " + SystemInfo.operatingSystem +
                "\nMissing CPU/GPU timings are left blank. Capture is opt-in and adds measurement overhead. No temperatures are measured.\n");
            windowStart = Time.realtimeSinceStartupAsDouble;
            Debug.Log("[PerformanceCapture] " + path);
        }
        catch (Exception error)
        {
            Debug.LogWarning("[PerformanceCapture] Cannot open capture: " + error.Message);
            enabled = false;
        }
    }

    private void Update()
    {
        if (writer == null) return;
        string scene = SceneManager.GetActiveScene().name;
        float scale = GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp ? urp.renderScale : 1f;
        if (samples > 0 && (scene != capturedScene || scale != capturedScale || Screen.width != capturedWidth ||
            Screen.height != capturedHeight || Application.targetFrameRate != capturedTarget ||
            QualitySettings.vSyncCount != capturedVSync || Application.isFocused != capturedFocus ||
            (Time.timeScale == 0) != capturedPause)) FlushWindow();
        if (samples == 0)
        {
            capturedScene = scene;
            capturedScale = scale;
            capturedWidth = Screen.width;
            capturedHeight = Screen.height;
            capturedTarget = Application.targetFrameRate;
            capturedVSync = QualitySettings.vSyncCount;
            capturedFocus = Application.isFocused;
            capturedPause = Time.timeScale == 0;
        }
        double delta = Time.unscaledDeltaTime;
        frameMilliseconds[samples++] = delta * 1000.0;
        elapsed += delta;
        FrameTimingManager.CaptureFrameTimings();
        if (FrameTimingManager.GetLatestTimings(1, timing) > 0 && timing[0].frameStartTimestamp != lastTimestamp)
        {
            lastTimestamp = timing[0].frameStartTimestamp;
            if (timing[0].cpuFrameTime > 0) { cpuSum += timing[0].cpuFrameTime; cpuSamples++; }
            if (timing[0].gpuFrameTime > 0) { gpuSum += timing[0].gpuFrameTime; gpuSamples++; }
        }
        if (samples == Capacity || Time.realtimeSinceStartupAsDouble - windowStart >= 10.0) FlushWindow();
    }

    private void FlushWindow()
    {
        if (writer == null || samples == 0) return;
        Array.Sort(frameMilliseconds, 0, samples);
        double p95 = frameMilliseconds[Mathf.Clamp(Mathf.CeilToInt(samples * .95f) - 1, 0, samples - 1)];
        double p99 = frameMilliseconds[Mathf.Clamp(Mathf.CeilToInt(samples * .99f) - 1, 0, samples - 1)];
        writer.WriteLine(string.Join(",", DateTime.UtcNow.ToString("O", Invariant),
            "\"" + capturedScene.Replace("\"", "\"\"") + "\"",
            elapsed.ToString("F3", Invariant), samples.ToString(Invariant),
            (samples / Math.Max(elapsed, .000001)).ToString("F2", Invariant),
            p95.ToString("F3", Invariant), p99.ToString("F3", Invariant),
            cpuSamples > 0 ? (cpuSum / cpuSamples).ToString("F3", Invariant) : "",
            gpuSamples > 0 ? (gpuSum / gpuSamples).ToString("F3", Invariant) : "",
            capturedWidth.ToString(Invariant), capturedHeight.ToString(Invariant), capturedScale.ToString("F2", Invariant),
            capturedTarget.ToString(Invariant), capturedVSync.ToString(Invariant),
            EnemyHealth.ActiveEnemies.Count.ToString(Invariant), capturedFocus ? "1" : "0", capturedPause ? "1" : "0"));
        writer.Flush();
        samples = 0;
        elapsed = cpuSum = gpuSum = 0;
        cpuSamples = gpuSamples = 0;
        windowStart = Time.realtimeSinceStartupAsDouble;
    }

    private void OnDestroy()
    {
        FlushWindow();
        writer?.Dispose();
        writer = null;
        if (instance == this) instance = null;
    }
}
