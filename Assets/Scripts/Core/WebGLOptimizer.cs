using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Optimizador automático para WebGL - fuerza configuraciones de performance
/// Se ejecuta ANTES que cualquier otro script para garantizar settings óptimos
/// </summary>
[DefaultExecutionOrder(-1000)] // Ejecuta ANTES que todo lo demás
public class WebGLOptimizer : MonoBehaviour
{
    [Header("WebGL Performance Settings")]
    [SerializeField] private bool autoApplyOnStart = true;
    [SerializeField] private bool disableShadows = true;
    [SerializeField] private bool disableRealtimeLights = true;
    [SerializeField] private bool capFrameRate = true;
    [SerializeField] private int targetFrameRate = 60;
    [SerializeField] private bool disableAntiAliasing = true;
    [SerializeField] private bool disableVSync = true;

    [Header("Render Scale (borroso)")]
    [Tooltip("Fuerza render scale = 1. Desactiva Dynamic Resolution que causa el borroso bajo carga.")]
    [SerializeField] private bool forceRenderScaleOne = true;
    [Tooltip("Comprueba cada N segundos que el render scale no haya bajado (watchdog).")]
    [SerializeField] private float renderScaleCheckInterval = 2f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;

    private void Awake()
    {
        // Aplicar en WebGL build. Para probar en Editor: activa manualmente con clic derecho → "Apply Now".
        if (Application.platform == RuntimePlatform.WebGLPlayer && autoApplyOnStart)
        {
            ApplyWebGLOptimizations();
        }

        // El watchdog de render scale corre en todas las plataformas si está activado,
        // porque el borroso puede ocurrir en cualquier build con Dynamic Resolution.
        if (forceRenderScaleOne)
        {
            StartCoroutine(RenderScaleWatchdog());
        }
    }

    /// <summary>
    /// Aplica todas las optimizaciones críticas para WebGL
    /// </summary>
    public void ApplyWebGLOptimizations()
    {
        if (showDebugInfo) Debug.Log("[WebGLOptimizer] Aplicando optimizaciones para WebGL...");

        // 1. DESACTIVAR SOMBRAS (CRÍTICO - Mayor impacto en performance)
        if (disableShadows)
        {
            QualitySettings.shadows = UnityEngine.ShadowQuality.Disable;
            QualitySettings.shadowDistance = 0f;
            
            // Desactivar sombras en URP también
            var urpAsset = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
            if (urpAsset != null)
            {
                // Las shadows se controlan a nivel de URP Renderer
                // Esto requiere acceso al renderer asset (no siempre disponible en runtime)
                if (showDebugInfo) Debug.Log("[WebGLOptimizer] URP detectado - sombras controladas por calidad");
            }
            
            if (showDebugInfo) Debug.Log("[WebGLOptimizer] ✓ Sombras desactivadas");
        }

        // 2. LIMITAR LUCES EN TIEMPO REAL
        if (disableRealtimeLights)
        {
            QualitySettings.pixelLightCount = 0; // Solo 1 luz direccional (gratis)
            
            if (showDebugInfo) Debug.Log("[WebGLOptimizer] ✓ Luces limitadas a 0 pixel lights");
        }

        // 3. CAP DE FRAME RATE (Previene throttling del browser)
        if (capFrameRate)
        {
            Application.targetFrameRate = targetFrameRate;
            
            if (showDebugInfo) Debug.Log($"[WebGLOptimizer] ✓ Frame rate limitado a {targetFrameRate} FPS");
        }

        // 4. DESACTIVAR ANTI-ALIASING (Costoso en WebGL)
        if (disableAntiAliasing)
        {
            QualitySettings.antiAliasing = 0;
            
            var urpAsset = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
            if (urpAsset != null)
            {
                // El MSAA se controla en el URP Asset (readonly en runtime)
                if (showDebugInfo) Debug.Log("[WebGLOptimizer] Anti-aliasing desactivado en Quality Settings");
            }
            
            if (showDebugInfo) Debug.Log("[WebGLOptimizer] ✓ Anti-aliasing desactivado");
        }

        // 5. DESACTIVAR VSYNC (Navegadores manejan esto internamente)
        if (disableVSync)
        {
            QualitySettings.vSyncCount = 0;
            
            if (showDebugInfo) Debug.Log("[WebGLOptimizer] ✓ VSync desactivado");
        }

        // 6. CONFIGURACIONES ADICIONALES DE WEBGL
        QualitySettings.softParticles = false; // Depth texture costoso en WebGL
        QualitySettings.realtimeReflectionProbes = false; // Muy costoso
        QualitySettings.billboardsFaceCameraPosition = false; // Cálculo extra
        
        // 7. STREAMING DE TEXTURAS (Reduce memory footprint)
        QualitySettings.streamingMipmapsActive = false; // WebGL no soporta bien streaming
        
        // 8. BUDGETS DE RENDERING
        QualitySettings.particleRaycastBudget = 128; // Reducir raycasts de partículas
        QualitySettings.asyncUploadTimeSlice = 1; // Reducir upload time
        QualitySettings.asyncUploadBufferSize = 8; // Reducir buffer

        // 9. RENDER SCALE — fuerza 1.0 para evitar el borroso por Dynamic Resolution
        if (forceRenderScaleOne)
        {
            var urpAsset = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
            if (urpAsset != null)
            {
                urpAsset.renderScale = 1.0f;
                if (showDebugInfo) Debug.Log("[WebGLOptimizer] ✓ Render scale forzado a 1.0 (fix borroso)");
            }
        }

        if (showDebugInfo)
        {
            Debug.Log("[WebGLOptimizer] ====================================");
            Debug.Log("[WebGLOptimizer] OPTIMIZACIONES APLICADAS:");
            Debug.Log($"[WebGLOptimizer] - Shadows: {QualitySettings.shadows}");
            Debug.Log($"[WebGLOptimizer] - Shadow Distance: {QualitySettings.shadowDistance}");
            Debug.Log($"[WebGLOptimizer] - Pixel Lights: {QualitySettings.pixelLightCount}");
            Debug.Log($"[WebGLOptimizer] - Target FPS: {Application.targetFrameRate}");
            Debug.Log($"[WebGLOptimizer] - Anti-Aliasing: {QualitySettings.antiAliasing}");
            Debug.Log($"[WebGLOptimizer] - VSync: {QualitySettings.vSyncCount}");
            Debug.Log("[WebGLOptimizer] ====================================");
        }
    }

    /// <summary>
    /// Desactiva sombras de todas las luces en la escena (runtime)
    /// </summary>
    public void DisableAllLightShadows()
    {
        Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        
        foreach (Light light in lights)
        {
            if (light.shadows != LightShadows.None)
            {
                light.shadows = LightShadows.None;
                if (showDebugInfo) Debug.Log($"[WebGLOptimizer] Sombras desactivadas en luz: {light.name}");
            }
        }
        
        if (showDebugInfo) Debug.Log($"[WebGLOptimizer] ✓ {lights.Length} luces procesadas");
    }

    /// <summary>
    /// Reduce calidad de sombras si están habilitadas
    /// </summary>
    public void ReduceShadowQuality()
    {
        QualitySettings.shadows = UnityEngine.ShadowQuality.HardOnly;
        QualitySettings.shadowResolution = UnityEngine.ShadowResolution.Low;
        QualitySettings.shadowDistance = 20f; // Reducir a la mitad
        QualitySettings.shadowCascades = 0; // Sin cascadas
        
        if (showDebugInfo) Debug.Log("[WebGLOptimizer] ✓ Calidad de sombras reducida (Hard, Low, 20m)");
    }

    // Método para llamar desde Inspector o otros scripts
    [ContextMenu("Apply WebGL Optimizations Now")]
    public void ApplyOptimizationsManually()
    {
        ApplyWebGLOptimizations();
        DisableAllLightShadows();
    }

    /// <summary>
    /// Watchdog: comprueba periódicamente que el render scale no haya bajado.
    /// Dynamic Resolution en URP puede bajarlo bajo carga — eso es lo que causa el borroso.
    /// </summary>
    private System.Collections.IEnumerator RenderScaleWatchdog()
    {
        var wait = new WaitForSecondsRealtime(renderScaleCheckInterval);
        while (true)
        {
            yield return wait;

            var urpAsset = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
            if (urpAsset != null && urpAsset.renderScale < 0.99f)
            {
                Debug.LogWarning(
                    $"[WebGLOptimizer] ⚠️ Render scale detectó caída: {urpAsset.renderScale:F2} → reseteando a 1.0. " +
                    "CAUSA DEL BORROSO. Desactiva Dynamic Resolution en el URP Asset.");
                urpAsset.renderScale = 1.0f;

                PerformanceMonitor.Instance?.LogEvent($"RenderScale watchdog reset desde {urpAsset.renderScale:F2}");
            }
        }
    }

    // Método de diagnóstico
    [ContextMenu("Show Current Quality Settings")]
    public void ShowCurrentSettings()
    {
        Debug.Log("====== QUALITY SETTINGS ======");
        Debug.Log($"Quality Level: {QualitySettings.names[QualitySettings.GetQualityLevel()]}");
        Debug.Log($"Shadows: {QualitySettings.shadows}");
        Debug.Log($"Shadow Distance: {QualitySettings.shadowDistance}");
        Debug.Log($"Shadow Resolution: {QualitySettings.shadowResolution}");
        Debug.Log($"Pixel Light Count: {QualitySettings.pixelLightCount}");
        Debug.Log($"Anti-Aliasing: {QualitySettings.antiAliasing}");
        Debug.Log($"VSync Count: {QualitySettings.vSyncCount}");
        Debug.Log($"Target FPS: {Application.targetFrameRate}");
        Debug.Log($"Platform: {Application.platform}");
        Debug.Log("==============================");
    }
}
