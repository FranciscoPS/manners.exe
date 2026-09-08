using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public class PerformanceMonitor : MonoBehaviour, IUpdateable
{
    public static PerformanceMonitor Instance { get; private set; }
    [SerializeField, Min(5f)] private float reportInterval = 15f;
    private double windowStart;
    private int frames;
    private float worstFrame;
    private bool showOverlay;
    private string deviceReport;
    private string frameReport = "Collecting frame timings...";
    private string saveMessage = "";
    public bool IsActive => isActiveAndEnabled;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Instance = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureExists()
    {
        if (Instance == null)
            new GameObject("[PerformanceMonitor]").AddComponent<PerformanceMonitor>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        deviceReport = $"manners.exe {Application.version} | Unity {Application.unityVersion}\n" +
            $"GPU actually used: {SystemInfo.graphicsDeviceName}\n" +
            $"API / driver: {SystemInfo.graphicsDeviceType} | {SystemInfo.graphicsDeviceVersion}\n" +
            $"GPU memory reported: {SystemInfo.graphicsMemorySize} MB | RAM: {SystemInfo.systemMemorySize} MB\n" +
            $"CPU: {SystemInfo.processorType} ({SystemInfo.processorCount} threads)\n" +
            $"OS: {SystemInfo.operatingSystem} | Platform: {Application.platform}";
        Debug.Log("[PERF DEVICE]\n" + deviceReport);
    }

    private void OnEnable()
    {
        if (Instance != this) return;
        windowStart = Time.realtimeSinceStartupAsDouble;
        frames = 0;
        worstFrame = 0;
        UpdateManager.Instance?.Register(this);
    }

    private void OnDisable() => UpdateManager.Instance?.Unregister(this);
    private void OnDestroy() { if (Instance == this) Instance = null; }

    public void OnUpdate(float deltaTime)
    {
        if (Keyboard.current != null && Keyboard.current.f8Key.wasPressedThisFrame)
            showOverlay = !showOverlay;

        frames++;
        worstFrame = Mathf.Max(worstFrame, Time.unscaledDeltaTime);
        double elapsed = Time.realtimeSinceStartupAsDouble - windowStart;
        if (elapsed < Mathf.Max(5f, reportInterval)) return;

        var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        int quality = QualitySettings.GetQualityLevel();
        frameReport = $"Scene: {SceneManager.GetActiveScene().name} | t={Time.realtimeSinceStartup:F0}s\n" +
            $"FPS: {frames / elapsed:F1} | average frame: {elapsed * 1000 / frames:F1} ms | worst: {worstFrame * 1000:F1} ms\n" +
            $"Output: {Screen.width} x {Screen.height} | scale: {(urp != null ? urp.renderScale : 1):F2} | quality: {QualitySettings.names[quality]}\n" +
            $"Pipeline: {(urp != null ? urp.name : "none")} | renderer: {(urp != null ? urp.scriptableRenderer.GetType().Name : "none")}\n" +
            $"Enemies: {EnemyHealth.ActiveEnemyCount} | orbs: {ActivePool(PoolManager.PoolType.ExperienceOrb)} | projectiles: {ActivePool(PoolManager.PoolType.Projectile)}\n" +
            $"Unity allocated memory: {Profiler.GetTotalAllocatedMemoryLong() / 1048576} MB | managed: {System.GC.GetTotalMemory(false) / 1048576} MB\n" +
            $"VSync: {QualitySettings.vSyncCount} | FPS cap: {Application.targetFrameRate} | focus: {Application.isFocused} | battery: {SystemInfo.batteryStatus}";
        Debug.Log("[PERF FRAME]\n" + frameReport);
        windowStart = Time.realtimeSinceStartupAsDouble;
        frames = 0;
        worstFrame = 0;
    }

    private static int ActivePool(PoolManager.PoolType type)
    {
        if (PoolManager.Instance != null && PoolManager.Instance.TryGetPoolStats(type, out int total, out int available))
            return Mathf.Max(0, total - available);
        return 0;
    }

    public void LogEvent(string eventName)
    {
        if (Debug.isDebugBuild) Debug.Log($"[PERF EVENT] {eventName} | enemies={EnemyHealth.ActiveEnemyCount}");
    }

    private void OnGUI()
    {
        if (!showOverlay) return;
        GUILayout.BeginArea(new Rect(12, 12, Mathf.Min(Screen.width - 24, 860), Mathf.Min(Screen.height - 24, 520)), GUI.skin.box);
        GUILayout.Label("Performance report — F8 to close");
        GUILayout.Label(deviceReport + "\n" + frameReport);
#if !UNITY_WEBGL || UNITY_EDITOR
        if (GUILayout.Button("Save performance report"))
        {
            try
            {
                string path = Path.Combine(Application.persistentDataPath, "performance-report.txt");
                File.WriteAllText(path, deviceReport + "\n" + frameReport);
                saveMessage = path;
            }
            catch (System.Exception exception) { saveMessage = exception.Message; }
        }
#endif
        GUILayout.Label(saveMessage);
        GUILayout.EndArea();
    }
}