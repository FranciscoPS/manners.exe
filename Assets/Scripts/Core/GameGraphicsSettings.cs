using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class GameGraphicsSettings
{
    public enum GraphicsPreset { Eco, Balanced, High }

    [Serializable]
    public sealed class SettingsData
    {
        public GraphicsPreset preset = GraphicsPreset.Balanced;
        public int frameRate = 60;
        public bool vSync;
        public float renderScale = 1f;
        public int textureMipmapLimit;
        public bool shadows = true;
        public bool ambientOcclusion;
        public bool postProcessing = true;
        public int resolutionHeight = 1080;
        public bool fullScreen = true;
        public SettingsData Clone() => (SettingsData)MemberwiseClone();
    }

    private sealed class CameraState
    {
        public UniversalAdditionalCameraData data;
        public bool postProcessing;
        public int rendererIndex;
        public bool usesMainRenderer;
    }

    private const string PrefsKey = "GraphicsSettings.v1";
    private const float ConfirmationDuration = 15f;
    private static readonly int[] FrameRates = { 30, 45, 60, 90, 120 };
    private static readonly int[] ResolutionHeights = { 0, 720, 900, 1080, 1440, 2160 };
    private static readonly Dictionary<Camera, CameraState> Cameras = new Dictionary<Camera, CameraState>();
    private static readonly List<Camera> RemovedCameras = new List<Camera>();
    private static SettingsData current;
    private static SettingsData confirmedDisplay;
    private static UniversalRenderPipelineAsset sourcePipeline;
    private static UniversalRenderPipelineAsset runtimePipeline;
    private static RenderPipelineAsset originalQualityPipeline;
    private static int noAoRendererIndex = -1;
    private static int originalVSync;
    private static int originalFrameRate;
    private static int originalMipmapLimit;
    private static float displayDeadline;
    private static bool initialized;
    private static bool isFocused = true;
    private static bool idle;

    public static event Action Changed;
    public static SettingsData Current { get { Initialize(); return (current ?? CreatePreset(GraphicsPreset.Balanced)).Clone(); } }
    public static bool IsDisplayChangePending => confirmedDisplay != null;
    public static float DisplayConfirmationSecondsRemaining => IsDisplayChangePending
        ? Mathf.Max(0f, displayDeadline - Time.realtimeSinceStartup) : 0f;
    public static bool SupportsDisplayChanges => !Application.isEditor && !Application.isMobilePlatform
        && Application.platform != RuntimePlatform.WebGLPlayer;
    public static bool SupportsAmbientOcclusion { get { Initialize(); return noAoRendererIndex >= 0; } }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Shutdown();
        Changed = null;
        current = null;
    }

    public static SettingsData CreatePreset(GraphicsPreset preset)
    {
        var settings = new SettingsData { preset = preset };
        if (preset == GraphicsPreset.Eco)
        {
            settings.frameRate = 30;
            settings.renderScale = 0.75f;
            settings.textureMipmapLimit = 1;
            settings.shadows = false;
            settings.postProcessing = false;
        }
        else if (preset == GraphicsPreset.High) settings.ambientOcclusion = true;
        return settings;
    }

    public static SettingsData Sanitize(SettingsData settings)
    {
        var result = settings != null ? settings.Clone() : CreatePreset(GraphicsPreset.Balanced);
        if (!Enum.IsDefined(typeof(GraphicsPreset), result.preset)) result.preset = GraphicsPreset.Balanced;
        if (Array.IndexOf(FrameRates, result.frameRate) < 0) result.frameRate = 60;
        if (float.IsNaN(result.renderScale) || float.IsInfinity(result.renderScale)) result.renderScale = 1f;
        result.renderScale = Mathf.Clamp(result.renderScale, 0.5f, 1f);
        result.textureMipmapLimit = Mathf.Clamp(result.textureMipmapLimit, 0, 2);
        if (Array.IndexOf(ResolutionHeights, result.resolutionHeight) < 0) result.resolutionHeight = 1080;
        return result;
    }

    public static void Initialize()
    {
        if (initialized || !Application.isPlaying) return;
        initialized = true;
        originalVSync = QualitySettings.vSyncCount;
        originalFrameRate = Application.targetFrameRate;
        originalMipmapLimit = QualitySettings.globalTextureMipmapLimit;
        originalQualityPipeline = QualitySettings.renderPipeline;
        sourcePipeline = (originalQualityPipeline != null ? originalQualityPipeline : GraphicsSettings.defaultRenderPipeline)
            as UniversalRenderPipelineAsset;
        current = CreatePreset(GraphicsPreset.Balanced);
        if (PlayerPrefs.HasKey(PrefsKey))
        {
            try { JsonUtility.FromJsonOverwrite(PlayerPrefs.GetString(PrefsKey), current); }
            catch (ArgumentException) { current = CreatePreset(GraphicsPreset.Balanced); }
        }
        current = Sanitize(current);
        if (sourcePipeline != null)
        {
            runtimePipeline = UnityEngine.Object.Instantiate(sourcePipeline);
            runtimePipeline.name = sourcePipeline.name + " (Runtime Graphics)";
            runtimePipeline.hideFlags = HideFlags.DontSave;
            for (int i = 0; i < runtimePipeline.rendererDataList.Length; i++)
            {
                var data = runtimePipeline.rendererDataList[i];
                if (data != null && data.name == "PC_NoAO_Renderer") noAoRendererIndex = i;
            }
            QualitySettings.renderPipeline = runtimePipeline;
        }
        var go = new GameObject("GameGraphicsSettings");
        UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<GameGraphicsSettingsDriver>();
        isFocused = Application.isFocused;
        SceneManager.sceneLoaded += OnSceneLoaded;
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        Application.quitting += Shutdown;
        ApplyRendering();
        ApplyDisplay();
    }

    public static void Apply(SettingsData settings)
    {
        Initialize();
        if (!initialized) return;
        var next = Sanitize(settings);
        bool displayChanged = SupportsDisplayChanges &&
            (next.resolutionHeight != current.resolutionHeight || next.fullScreen != current.fullScreen);
        if (displayChanged)
        {
            if (confirmedDisplay == null) confirmedDisplay = current.Clone();
            displayDeadline = Time.realtimeSinceStartup + ConfirmationDuration;
        }
        current = next;
        ApplyRendering();
        if (displayChanged) ApplyDisplay();
        Save();
        Changed?.Invoke();
    }

    public static void ResetToDefaults() => Apply(CreatePreset(GraphicsPreset.Balanced));

    public static void ConfirmDisplayChange()
    {
        if (!IsDisplayChangePending) return;
        confirmedDisplay = null;
        Save();
        Changed?.Invoke();
    }

    public static void RevertDisplayChange()
    {
        if (!IsDisplayChangePending) return;
        current.resolutionHeight = confirmedDisplay.resolutionHeight;
        current.fullScreen = confirmedDisplay.fullScreen;
        confirmedDisplay = null;
        ApplyDisplay();
        Save();
        Changed?.Invoke();
    }

    private static void Save()
    {
        var saved = current.Clone();
        if (confirmedDisplay != null)
        {
            saved.resolutionHeight = confirmedDisplay.resolutionHeight;
            saved.fullScreen = confirmedDisplay.fullScreen;
        }
        PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(saved));
        PlayerPrefs.Save();
    }

    private static void ApplyRendering()
    {
        ApplyFramePacing();
        QualitySettings.globalTextureMipmapLimit = current.textureMipmapLimit;
        if (runtimePipeline != null)
        {
            runtimePipeline.renderScale = current.renderScale;
            runtimePipeline.shadowDistance = current.shadows
                ? Mathf.Min(sourcePipeline.shadowDistance, current.preset == GraphicsPreset.High ? 28f : 20f) : 0f;
            runtimePipeline.shadowCascadeCount = current.preset == GraphicsPreset.High
                ? sourcePipeline.shadowCascadeCount : 1;
        }
        foreach (var pair in Cameras)
            if (pair.Key != null && pair.Value.data != null) ApplyCamera(pair.Value);
    }

    private static void ApplyFramePacing()
    {
        if (current == null) return;
        int budget = !isFocused ? 15 : idle ? Mathf.Min(30, current.frameRate) : current.frameRate;
        bool sync = current.vSync && isFocused && !idle;
        QualitySettings.vSyncCount = sync ? 1 : 0;
        Application.targetFrameRate = budget;
    }

    private static void ApplyDisplay()
    {
        if (!SupportsDisplayChanges) return;
        var display = Screen.mainWindowDisplayInfo;
        int nativeWidth = display.width > 0 ? display.width : Screen.currentResolution.width;
        int nativeHeight = display.height > 0 ? display.height : Screen.currentResolution.height;
        if (nativeWidth <= 0 || nativeHeight <= 0) { nativeWidth = 1920; nativeHeight = 1080; }
        Vector2Int size = CalculateResolution(nativeWidth, nativeHeight, current.resolutionHeight);
        Screen.SetResolution(size.x, size.y,
            current.fullScreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
    }

    public static Vector2Int CalculateResolution(int nativeWidth, int nativeHeight, int requestedHeight)
    {
        nativeWidth = Mathf.Max(1, nativeWidth);
        nativeHeight = Mathf.Max(1, nativeHeight);
        int height = requestedHeight <= 0 ? nativeHeight : Mathf.Min(requestedHeight, nativeHeight);
        int width = Mathf.Clamp(Mathf.RoundToInt(height * ((float)nativeWidth / nativeHeight)), 1, nativeWidth);
        return new Vector2Int(width, height);
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RemovedCameras.Clear();
        foreach (var pair in Cameras)
            if (pair.Key == null || pair.Value.data == null) RemovedCameras.Add(pair.Key);
        foreach (var camera in RemovedCameras) Cameras.Remove(camera);
        RemovedCameras.Clear();
        idle = scene.name == "MainMenu" || Time.timeScale == 0f;
        ApplyFramePacing();
        foreach (var root in scene.GetRootGameObjects())
            foreach (var camera in root.GetComponentsInChildren<Camera>(true)) RegisterCamera(camera);
    }

    private static void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (!Cameras.ContainsKey(camera)) RegisterCamera(camera);
    }

    private static void RegisterCamera(Camera camera)
    {
        if (camera.cameraType != CameraType.Game || Cameras.ContainsKey(camera) || runtimePipeline == null) return;
        if (!camera.TryGetComponent<UniversalAdditionalCameraData>(out var data)) return;
        int rendererIndex = -1;
        for (int i = 0; i < runtimePipeline.rendererDataList.Length; i++)
        {
            if (runtimePipeline.rendererDataList[i] != null && data.scriptableRenderer == runtimePipeline.GetRenderer(i))
            {
                rendererIndex = i;
                break;
            }
        }
        var state = new CameraState { data = data, postProcessing = data.renderPostProcessing,
            rendererIndex = rendererIndex, usesMainRenderer = rendererIndex == 0 && camera.targetTexture == null };
        Cameras.Add(camera, state);
        ApplyCamera(state);
    }

    private static void ApplyCamera(CameraState state)
    {
        state.data.renderPostProcessing = state.postProcessing && current.postProcessing;
        if (state.usesMainRenderer && noAoRendererIndex >= 0)
            state.data.SetRenderer(current.ambientOcclusion ? state.rendererIndex : noAoRendererIndex);
    }

    internal static void Tick()
    {
        if (!initialized) return;
        if (IsDisplayChangePending && Time.realtimeSinceStartup >= displayDeadline) RevertDisplayChange();
        bool nextIdle = Time.timeScale == 0f || SceneManager.GetActiveScene().name == "MainMenu";
        if (nextIdle != idle) { idle = nextIdle; ApplyFramePacing(); }
    }

    internal static void SetFocus(bool focused)
    {
        isFocused = focused;
        ApplyFramePacing();
    }

    internal static void Shutdown()
    {
        if (!initialized) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        Application.quitting -= Shutdown;
        foreach (var pair in Cameras)
        {
            if (pair.Key == null || pair.Value.data == null) continue;
            pair.Value.data.renderPostProcessing = pair.Value.postProcessing;
            if (pair.Value.usesMainRenderer) pair.Value.data.SetRenderer(pair.Value.rendererIndex);
        }
        Cameras.Clear();
        RemovedCameras.Clear();
        if (QualitySettings.renderPipeline == runtimePipeline) QualitySettings.renderPipeline = originalQualityPipeline;
        QualitySettings.vSyncCount = originalVSync;
        Application.targetFrameRate = originalFrameRate;
        QualitySettings.globalTextureMipmapLimit = originalMipmapLimit;
        if (runtimePipeline != null) UnityEngine.Object.Destroy(runtimePipeline);
        runtimePipeline = null;
        sourcePipeline = null;
        noAoRendererIndex = -1;
        confirmedDisplay = null;
        idle = false;
        initialized = false;
    }
}
