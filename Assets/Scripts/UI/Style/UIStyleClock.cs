using UnityEngine;

public sealed class UIStyleClock : MonoBehaviour, IUpdateable
{
    private const float WrapSeconds = 3600f;

    private static UIStyleClock instance;
    private static bool isQuitting;

    public bool IsActive => true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        isQuitting = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (isQuitting || instance != null) return;

        GameObject go = new GameObject("UIStyleClock");
        instance = go.AddComponent<UIStyleClock>();
        DontDestroyOnLoad(go);
    }

    private void OnEnable()
    {
        if (UpdateManager.Instance != null)
            UpdateManager.Instance.Register(this);
    }

    private void OnDisable()
    {
        if (isQuitting) return;

        if (UpdateManager.Instance != null)
            UpdateManager.Instance.Unregister(this);
    }

    private void OnApplicationQuit()
    {
        isQuitting = true;
    }

    public void OnUpdate(float deltaTime)
    {
        Shader.SetGlobalFloat(UIStyle.TimeId, Mathf.Repeat(Time.unscaledTime, WrapSeconds));
    }
}
