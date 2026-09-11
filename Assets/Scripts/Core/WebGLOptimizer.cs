using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class WebGLOptimizer : MonoBehaviour
{
    [SerializeField] private bool autoApplyOnStart = true;

    private void Awake()
    {
        if (autoApplyOnStart) GameGraphicsSettings.Initialize();
    }

    public void ApplyWebGLOptimizations() => GameGraphicsSettings.Initialize();

    public void DisableAllLightShadows()
    {
        var settings = GameGraphicsSettings.Current;
        settings.shadows = false;
        GameGraphicsSettings.Apply(settings);
    }

    public void ReduceShadowQuality()
    {
        var settings = GameGraphicsSettings.Current;
        settings.preset = GameGraphicsSettings.GraphicsPreset.Balanced;
        GameGraphicsSettings.Apply(settings);
    }

    [ContextMenu("Apply WebGL Optimizations Now")]
    public void ApplyOptimizationsManually() => GameGraphicsSettings.Initialize();

    [ContextMenu("Show Current Quality Settings")]
    public void ShowCurrentSettings()
    {
        if (Application.isPlaying) Debug.Log(JsonUtility.ToJson(GameGraphicsSettings.Current, true));
    }
}
