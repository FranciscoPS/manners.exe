using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Text;

public class PerformanceMonitor : MonoBehaviour, IUpdateable
{
    public static PerformanceMonitor Instance { get; private set; }

    [Tooltip("Activa los informes de consola en una build de lanzamiento. Normalmente deben permanecer apagados; usa Development Build para diagnosticar.")]
    [SerializeField] private bool enableReleaseLogging = false;

    [Header("Intervalos de reporte")]
    [Tooltip("Cada cuántos segundos se imprime el resumen periódico")]
    [SerializeField] private float reportInterval = 5f;

    [Header("Umbrales de alerta")]
    [Tooltip("FPS por debajo del cual se considera un drop grave")]
    [SerializeField] private float fpsCriticalThreshold = 25f;
    [Tooltip("FPS por debajo del cual se considera un drop leve")]
    [SerializeField] private float fpsWarningThreshold = 40f;
    [Tooltip("Objetos activos (enemies + orbs + coins) que se considera excesivo")]
    [SerializeField] private int activeObjectsAlertThreshold = 150;

    private float periodicTimer;
    private float frameTimeAccum;
    private int   fpsSamples;

    private float lastFrameTime;
    private int   spikeCount;
    private float sessionStart;
    private int   lastLoggedWave = -1;

    private UniversalRenderPipelineAsset urpAsset;

    public bool IsActive => enabled && gameObject.activeInHierarchy;

    private void Awake()
    {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
        if (!enableReleaseLogging)
        {
            enabled = false;
            return;
        }
#endif
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        sessionStart = Time.realtimeSinceStartup;
    }

    private void OnEnable()
    {
        UpdateManager.Instance?.Register(this);
        periodicTimer = reportInterval;
    }

    private void OnDisable()
    {
        UpdateManager.Instance?.Unregister(this);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void OnUpdate(float deltaTime)
    {
        // Rendering continues during slow motion/pause. Scaled delta time reports
        // gameplay speed, whereas frames / unscaled seconds measures actual FPS.
        deltaTime = Time.unscaledDeltaTime;
        if (deltaTime <= 0f) return;

        float fps = 1f / deltaTime;
        frameTimeAccum += deltaTime;
        fpsSamples++;

        if (fps < fpsCriticalThreshold)
        {
            spikeCount++;

            if (Time.realtimeSinceStartup - lastFrameTime > 0.5f)
            {
                int wave       = GetCurrentWave();
                int enemies    = CountActiveEnemies();
                int collectibles = CountActiveOrbs() + CountActiveCoins();
                Debug.LogWarning(
                    $"[PERF] 🔴 SPIKE SEVERO | " +
                    $"FPS: {fps:F1} | " +
                    $"Wave: {wave} | " +
                    $"Enemies: {enemies} | " +
                    $"Collectibles: {collectibles} | " +
                    $"RenderScale: {GetRenderScale():F2} | " +
                    $"t={Time.realtimeSinceStartup - sessionStart:F1}s"
                );
                lastFrameTime = Time.realtimeSinceStartup;
            }
        }

        periodicTimer -= deltaTime;
        if (periodicTimer <= 0f)
        {
            periodicTimer = reportInterval;
            PrintPeriodicReport();
        }

        int currentWaveNow = GetCurrentWave();
        if (currentWaveNow != lastLoggedWave)
        {
            lastLoggedWave = currentWaveNow;
            float avgFps = frameTimeAccum > 0f ? fpsSamples / frameTimeAccum : 0f;
            Debug.Log(
                $"[PERF] 🌊 NUEVA WAVE → Wave {currentWaveNow} | " +
                $"FPS promedio previo: {avgFps:F1} | " +
                $"Enemies activos ahora: {CountActiveEnemies()} | " +
                $"Spikes acumulados: {spikeCount} | " +
                $"RenderScale: {GetRenderScale():F2}"
            );

            frameTimeAccum = 0f;
            fpsSamples = 0;
            spikeCount = 0;
        }
    }

    private void PrintPeriodicReport()
    {
        float avgFps     = frameTimeAccum > 0f ? fpsSamples / frameTimeAccum : 0f;
        int   enemies    = CountActiveEnemies();
        int   orbs       = CountActiveOrbs();
        int   coins      = CountActiveCoins();
        int   projectiles = CountActiveProjectiles();
        int   totalActive = enemies + orbs + coins + projectiles;
        float renderScale = GetRenderScale();

        string fpsTag = avgFps < fpsCriticalThreshold ? "🔴" :
                        avgFps < fpsWarningThreshold  ? "🟡" : "🟢";

        var sb = new StringBuilder(256);
        sb.AppendLine($"[PERF] ── REPORTE t={Time.realtimeSinceStartup - sessionStart:F0}s ──────────────────────");
        sb.AppendLine($"[PERF] {fpsTag} FPS promedio: {avgFps:F1}  (muestras: {fpsSamples})");
        sb.AppendLine($"[PERF] Wave actual:     {GetCurrentWave()}");
        sb.AppendLine($"[PERF] Enemies activos: {enemies}");
        sb.AppendLine($"[PERF] Orbs activos:    {orbs}");
        sb.AppendLine($"[PERF] Coins activos:   {coins}");
        sb.AppendLine($"[PERF] Proyectiles:     {projectiles}");
        sb.AppendLine($"[PERF] Total objetos:   {totalActive}{(totalActive > activeObjectsAlertThreshold ? " ⚠️ EXCESIVO" : "")}");
        sb.AppendLine($"[PERF] Render Scale:    {renderScale:F2}");
        sb.AppendLine($"[PERF] Spikes (wave):   {spikeCount}");
        sb.AppendLine($"[PERF] ──────────────────────────────────────────────────");

        if (avgFps < fpsWarningThreshold || totalActive > activeObjectsAlertThreshold)
            Debug.LogWarning(sb.ToString());
        else
            Debug.Log(sb.ToString());

        frameTimeAccum = 0f;
        fpsSamples = 0;
    }

    private int CountActiveEnemies()
    {

        return EnemyHealth.ActiveEnemyCount;
    }

    private int CountActiveOrbs()
    {

        return CountActiveInPool(PoolManager.PoolType.ExperienceOrb);
    }

    private int CountActiveCoins()
    {
        return CountActiveInPool(PoolManager.PoolType.Coin) + CountActiveInPool(PoolManager.PoolType.Diamond);
    }

    private int CountActiveProjectiles()
    {
        return CountActiveInPool(PoolManager.PoolType.Projectile);
    }

    private static int CountActiveInPool(PoolManager.PoolType poolType)
    {
        if (PoolManager.Instance != null && PoolManager.Instance.TryGetPoolStats(poolType, out int total, out int available))
            return Mathf.Max(0, total - available);
        return 0;
    }

    private int GetCurrentWave()
    {
        return EnemySpawnManager.Instance != null ? EnemySpawnManager.Instance.CurrentWaveNumber : 0;
    }

    private float GetRenderScale()
    {
        urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urpAsset != null)
            return urpAsset.renderScale;
        return 1f;
    }

    public void LogEvent(string eventName)
    {
        if (!isActiveAndEnabled) return;
        int wave  = GetCurrentWave();
        string fpsStr = frameTimeAccum > 0f ? $"{fpsSamples / frameTimeAccum:F1}" : "N/A (inicio)";
        Debug.Log(
            $"[PERF] 📌 EVENTO: {eventName} | " +
            $"Wave: {wave} | " +
            $"FPS~: {fpsStr} | " +
            $"Enemies: {CountActiveEnemies()} | " +
            $"RenderScale: {GetRenderScale():F2}"
        );
    }
}
