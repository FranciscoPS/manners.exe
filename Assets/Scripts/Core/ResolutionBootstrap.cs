using UnityEngine;

public static class ResolutionBootstrap
{
    private const int TargetWidth = 1920;
    private const int TargetHeight = 1080;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ForceResolution()
    {
#if !UNITY_WEBGL
        if (Screen.width != TargetWidth || Screen.height != TargetHeight)
        {
            Screen.SetResolution(TargetWidth, TargetHeight, Screen.fullScreenMode);
        }
#endif
    }
}
