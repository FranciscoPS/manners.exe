using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[DefaultExecutionOrder(-1000)]
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
    private RenderPipelineAsset previousQualityPipeline;
    private UniversalRenderPipelineAsset runtimePipeline;

    private void Awake()
    {

        if (Application.platform == RuntimePlatform.WebGLPlayer && autoApplyOnStart)
        {
            ApplyWebGLOptimizations();
        }

        if (forceRenderScaleOne)
        {
            StartCoroutine(RenderScaleWatchdog());
        }
    }

    public void ApplyWebGLOptimizations()
    {
        if (runtimePipeline == null)
        {
            previousQualityPipeline = QualitySettings.renderPipeline;
            var source = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (source != null)
            {
                runtimePipeline = Instantiate(source);
                runtimePipeline.name = source.name + " (Web runtime)";
                QualitySettings.renderPipeline = runtimePipeline;
            }
        }

        if (disableShadows)
        {
            QualitySettings.shadows = UnityEngine.ShadowQuality.Disable;
            QualitySettings.shadowDistance = 0f;

            var urpAsset = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
            if (urpAsset != null)
            {
                urpAsset.shadowDistance = 0f;
            }

        }

        if (disableRealtimeLights)
        {
            QualitySettings.pixelLightCount = 0;
            if (runtimePipeline != null) runtimePipeline.maxAdditionalLightsCount = 0;
        }

        if (capFrameRate)
        {
            Application.targetFrameRate = targetFrameRate;

        }

        if (disableAntiAliasing)
        {
            QualitySettings.antiAliasing = 0;

            var urpAsset = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
            if (urpAsset != null)
            {
                urpAsset.msaaSampleCount = 1;
            }

        }

        if (disableVSync)
        {
            QualitySettings.vSyncCount = 0;

        }

        QualitySettings.softParticles = false;
        QualitySettings.realtimeReflectionProbes = false;
        QualitySettings.billboardsFaceCameraPosition = false;

        QualitySettings.streamingMipmapsActive = false;

        QualitySettings.particleRaycastBudget = 128;
        QualitySettings.asyncUploadTimeSlice = 1;
        QualitySettings.asyncUploadBufferSize = 8;

        if (forceRenderScaleOne)
        {
            var urpAsset = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
            if (urpAsset != null)
            {
                urpAsset.renderScale = 1.0f;
            }
        }

        if (showDebugInfo)
        {
            Debug.Log($"[WebGL] GPU={SystemInfo.graphicsDeviceName}; API={SystemInfo.graphicsDeviceType}; pipeline={runtimePipeline?.name}");
        }
    }

    private void OnDestroy()
    {
        if (runtimePipeline == null) return;
        if (QualitySettings.renderPipeline == runtimePipeline)
            QualitySettings.renderPipeline = previousQualityPipeline;
        Destroy(runtimePipeline);
    }

    public void DisableAllLightShadows()
    {
        Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);

        foreach (Light light in lights)
        {
            if (light.shadows != LightShadows.None)
            {
                light.shadows = LightShadows.None;
            }
        }

    }

    public void ReduceShadowQuality()
    {
        QualitySettings.shadows = UnityEngine.ShadowQuality.HardOnly;
        QualitySettings.shadowResolution = UnityEngine.ShadowResolution.Low;
        QualitySettings.shadowDistance = 20f;
        QualitySettings.shadowCascades = 0;

    }

    [ContextMenu("Apply WebGL Optimizations Now")]
    public void ApplyOptimizationsManually()
    {
        ApplyWebGLOptimizations();
        DisableAllLightShadows();
    }

    private System.Collections.IEnumerator RenderScaleWatchdog()
    {
        var wait = new WaitForSecondsRealtime(renderScaleCheckInterval);
        while (true)
        {
            yield return wait;

            var urpAsset = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
            if (urpAsset != null && urpAsset.renderScale < 0.99f)
            {
                urpAsset.renderScale = 1.0f;

                PerformanceMonitor.Instance?.LogEvent($"RenderScale watchdog reset desde {urpAsset.renderScale:F2}");
            }
        }
    }

    [ContextMenu("Show Current Quality Settings")]
    public void ShowCurrentSettings()
    {
    }
}
