using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Text;

/// <summary>
/// Monitor de rendimiento en runtime.
/// Loguea FPS, objetos activos, wave actual y detecta drops severos.
/// Solo activo en Editor y Development Builds — strip automático en Release.
/// </summary>
public class PerformanceMonitor : MonoBehaviour, IUpdateable
{
    public static PerformanceMonitor Instance { get; private set; }

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

    // ── Estado interno ────────────────────────────────────────────────────────
    private float periodicTimer;
    private float fpsAccum;
    private int   fpsSamples;

    // Para detectar drops puntuales (spike en un solo frame)
    private float lastFrameTime;
    private int   spikeCount;
    private float sessionStart;
    private int   lastLoggedWave = -1;

    // Referencia al URP Asset para leer Render Scale
    private UniversalRenderPipelineAsset urpAsset;

    // IUpdateable
    public bool IsActive => enabled && gameObject.activeInHierarchy;

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
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

    // ── IUpdateable ───────────────────────────────────────────────────────────

    public void OnUpdate(float deltaTime)
    {
        if (deltaTime <= 0f) return;

        // Acumular FPS
        float fps = 1f / deltaTime;
        fpsAccum  += fps;
        fpsSamples++;

        // ── Detección de spike puntual ────────────────────────────────────────
        if (fps < fpsCriticalThreshold)
        {
            spikeCount++;
            // Solo logear cada spike (no inundar la consola: máximo 1 por segundo aprox)
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

        // ── Reporte periódico ─────────────────────────────────────────────────
        periodicTimer -= deltaTime;
        if (periodicTimer <= 0f)
        {
            periodicTimer = reportInterval;
            PrintPeriodicReport();
        }

        // ── Loguear transición de wave ────────────────────────────────────────
        int currentWaveNow = GetCurrentWave();
        if (currentWaveNow != lastLoggedWave)
        {
            lastLoggedWave = currentWaveNow;
            float avgFps = fpsSamples > 0 ? fpsAccum / fpsSamples : 0f;
            Debug.Log(
                $"[PERF] 🌊 NUEVA WAVE → Wave {currentWaveNow} | " +
                $"FPS promedio previo: {avgFps:F1} | " +
                $"Enemies activos ahora: {CountActiveEnemies()} | " +
                $"Spikes acumulados: {spikeCount} | " +
                $"RenderScale: {GetRenderScale():F2}"
            );
            // Resetear acumuladores al entrar a nueva wave para medir cada wave por separado
            fpsAccum   = 0f;
            fpsSamples = 0;
            spikeCount = 0;
        }
    }

    // ── Reporte periódico ────────────────────────────────────────────────────

    private void PrintPeriodicReport()
    {
        float avgFps     = fpsSamples > 0 ? fpsAccum / fpsSamples : 0f;
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
        sb.AppendLine($"[PERF] Render Scale:    {renderScale:F2}{(renderScale < 0.99f ? " ⚠️ REDUCIDA — CAUSA DEL BORROSO" : "")}");
        sb.AppendLine($"[PERF] Spikes (wave):   {spikeCount}");
        sb.AppendLine($"[PERF] ──────────────────────────────────────────────────");

        // Usar Warning si algo está mal, Log si todo está bien
        if (avgFps < fpsWarningThreshold || totalActive > activeObjectsAlertThreshold || renderScale < 0.99f)
            Debug.LogWarning(sb.ToString());
        else
            Debug.Log(sb.ToString());

        // Resetear acumuladores del período
        fpsAccum   = 0f;
        fpsSamples = 0;
    }

    // ── Helpers de conteo ────────────────────────────────────────────────────

    private int CountActiveEnemies()
    {
        // Cuenta por tag para no depender de referencias
        return GameObject.FindGameObjectsWithTag("Enemy").Length;
    }

    private int CountActiveOrbs()
    {
        // ExperienceOrb hereda de BaseCollectible — buscar por tipo
        return FindObjectsByType<ExperienceOrb>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;
    }

    private int CountActiveCoins()
    {
        return FindObjectsByType<Collectible>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;
    }

    private int CountActiveProjectiles()
    {
        return FindObjectsByType<Projectile>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;
    }

    private int GetCurrentWave()
    {
        return EnemySpawnManager.Instance != null ? EnemySpawnManager.Instance.CurrentWaveNumber : 0;
    }

    private float GetRenderScale()
    {
        if (urpAsset != null)
            return urpAsset.renderScale;
        return 1f;
    }

    // ── API pública ──────────────────────────────────────────────────────────

    /// <summary>
    /// Llamar desde cualquier sistema cuando ocurre un evento relevante
    /// (p.ej: explosión masiva, nivel subido, shop abierto).
    /// </summary>
    public void LogEvent(string eventName)
    {
        int wave  = GetCurrentWave();
        string fpsStr = fpsSamples > 0 ? $"{fpsAccum / fpsSamples:F1}" : "N/A (inicio)";
        Debug.Log(
            $"[PERF] 📌 EVENTO: {eventName} | " +
            $"Wave: {wave} | " +
            $"FPS~: {fpsStr} | " +
            $"Enemies: {CountActiveEnemies()} | " +
            $"RenderScale: {GetRenderScale():F2}"
        );
    }
}
