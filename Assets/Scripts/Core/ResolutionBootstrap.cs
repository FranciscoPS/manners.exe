using UnityEngine;

public static class ResolutionBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplySavedGraphics()
    {
        GameGraphicsSettings.Initialize();
    }
}
