using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class ScreenTransition : MonoBehaviour
{
    private const float MaxStep = 1f / 30f;

    private static readonly int OpenXId = Shader.PropertyToID("_CRTOpenX");
    private static readonly int OpenYId = Shader.PropertyToID("_CRTOpenY");
    private static readonly int FlashId = Shader.PropertyToID("_CRTFlash");
    private static readonly int GlowId = Shader.PropertyToID("_CRTGlow");
    private static readonly int SignalId = Shader.PropertyToID("_CRTSignal");
    private static readonly int LiveId = Shader.PropertyToID("_CRTLive");
    private static readonly int AspectId = Shader.PropertyToID("_CRTAspect");
    private static readonly int OffColorId = Shader.PropertyToID("_CRTOffColor");
    private static readonly int LineColorId = Shader.PropertyToID("_CRTLineColor");
    private static readonly int GlowColorId = Shader.PropertyToID("_CRTGlowColor");
    private static readonly int LineThicknessId = Shader.PropertyToID("_CRTLineThickness");
    private static readonly int DotSizeId = Shader.PropertyToID("_CRTDotSize");
    private static readonly int GlowSizeId = Shader.PropertyToID("_CRTGlowSize");
    private static readonly int ScanlineCountId = Shader.PropertyToID("_CRTScanlineCount");
    private static readonly int ScanlineStrengthId = Shader.PropertyToID("_CRTScanlineStrength");
    private static readonly int EdgeSoftnessId = Shader.PropertyToID("_CRTEdgeSoftness");
    private static readonly WaitForEndOfFrame EndOfFrame = new WaitForEndOfFrame();

    [Header("Referencias del prefab")]
    [Tooltip("Canvas de la transición. Queda apagado mientras no hay una transición en curso.")]
    [SerializeField] private Canvas canvas;
    [Tooltip("Imagen a pantalla completa con el material CRTPower: muestra la captura de la pantalla al apagarse y deja ver la escena nueva al encenderse. Bloquea los clics durante la transición.")]
    [SerializeField] private RawImage screen;

    private static ScreenTransition instance;
    private static bool isQuitting;

    private ScreenTransitionConfig config;
    private Coroutine routine;
    private Texture2D capture;
    private bool sceneReady;

    public static bool IsRunning => instance != null && instance.routine != null;

    public static float PowerOffDuration => ScreenTransitionConfig.Instance.PowerOffDuration;

    private static bool CanQuit => Application.platform != RuntimePlatform.WebGLPlayer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        isQuitting = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        EnsureExists();
    }

    private static bool EnsureExists()
    {
        if (instance == null && !isQuitting)
            instance = RuntimeUIPrefabs.Spawn(p => p.screenTransition);

        return instance != null;
    }

    public static void LoadScene(int buildIndex, Action beforeLoad = null)
    {
        Begin(() => SceneManager.LoadScene(buildIndex, LoadSceneMode.Single), beforeLoad, false);
    }

    public static void LoadScene(string sceneName, Action beforeLoad = null)
    {
        Begin(() => SceneManager.LoadScene(sceneName, LoadSceneMode.Single), beforeLoad, false);
    }

    public static void QuitGame()
    {
        Begin(null, null, true);
    }

    private static void Begin(Action load, Action beforeLoad, bool quit)
    {
        if (IsRunning) return;

        if (!EnsureExists())
        {
            beforeLoad?.Invoke();
            if (quit) QuitApplication();
            else load();
            return;
        }

        instance.routine = instance.StartCoroutine(instance.PowerCycle(load, beforeLoad, quit));
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        Hide();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnApplicationQuit()
    {
        isQuitting = true;
    }

    private void OnDestroy()
    {
        ReleaseCapture();
        if (instance == this)
            instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        sceneReady = true;
    }

    [ContextMenu("Probar apagado y encendido")]
    private void Preview()
    {
        if (!Application.isPlaying || IsRunning) return;
        routine = StartCoroutine(PowerCycle(null, null, false));
    }

    private IEnumerator PowerCycle(Action load, Action beforeLoad, bool quit)
    {
        config = ScreenTransitionConfig.Instance;

        if (!Application.isBatchMode)
        {
            yield return EndOfFrame;
            capture = ScreenCapture.CaptureScreenshotAsTexture();
            if (capture != null) capture.wrapMode = TextureWrapMode.Clamp;
        }

        Show(capture);
        PlaySound(config.powerOffSFX);
        yield return PowerOff();

        beforeLoad?.Invoke();
        ReleaseCapture();

        if (quit && CanQuit)
        {
            QuitApplication();
            yield break;
        }

        if (load != null)
        {
            sceneReady = false;
            load();
            while (!sceneReady) yield return null;
        }

        config = ScreenTransitionConfig.Instance;
        yield return PowerOn();

        Hide();
        routine = null;
    }

    private IEnumerator PowerOff()
    {
        yield return Animate(config.collapseDuration, t =>
        {
            float k = config.collapseCurve.Evaluate(t);
            Apply(1f, 1f - k, k * config.flashStrength, k, 1f);
        });
        yield return Animate(config.shrinkDuration, t => Apply(1f - config.shrinkCurve.Evaluate(t), 0f, config.flashStrength, 1f, 1f));
        yield return Hold(config.dotHoldDuration);
        yield return Animate(config.dotFadeDuration, t => Apply(0f, 0f, config.flashStrength, 1f - t, 1f - t));
        yield return Hold(config.offHoldDuration);
    }

    private IEnumerator PowerOn()
    {
        Shader.SetGlobalFloat(LiveId, 1f);
        Apply(0f, 0f, config.flashStrength, 0f, 0f);
        yield return Hold(config.powerOnDelay);

        PlaySound(config.powerOnSFX);
        yield return Animate(config.dotInDuration, t => Apply(0f, 0f, config.flashStrength, t, t));
        yield return Animate(config.expandDuration, t => Apply(config.expandCurve.Evaluate(t), 0f, config.flashStrength, 1f, 1f));
        yield return Animate(config.openDuration, t =>
        {
            float k = config.openCurve.Evaluate(t);
            Apply(1f, k, (1f - k) * config.flashStrength, 1f - k, 1f);
        });
    }

    private static IEnumerator Animate(float duration, Action<float> apply)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            apply(elapsed / duration);
            yield return null;
            elapsed += Step;
        }

        apply(1f);
    }

    private IEnumerator Hold(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            ApplyLook();
            yield return null;
            elapsed += Step;
        }
    }

    private static float Step => Mathf.Min(Time.unscaledDeltaTime, MaxStep);

    private void Show(Texture texture)
    {
        screen.texture = texture;
        Shader.SetGlobalFloat(LiveId, texture != null ? 0f : 1f);
        Apply(1f, 1f, 0f, 0f, 1f);
        screen.gameObject.SetActive(true);
        canvas.enabled = true;
    }

    private void Hide()
    {
        if (canvas != null) canvas.enabled = false;
        if (screen != null)
        {
            screen.gameObject.SetActive(false);
            screen.texture = null;
        }
    }

    private void ReleaseCapture()
    {
        if (screen != null) screen.texture = null;
        if (capture != null) Destroy(capture);
        capture = null;
    }

    private void Apply(float openX, float openY, float flash, float glow, float signal)
    {
        Shader.SetGlobalFloat(OpenXId, Mathf.Clamp01(openX));
        Shader.SetGlobalFloat(OpenYId, Mathf.Clamp01(openY));
        Shader.SetGlobalFloat(FlashId, Mathf.Clamp01(flash));
        Shader.SetGlobalFloat(GlowId, Mathf.Clamp01(glow));
        Shader.SetGlobalFloat(SignalId, Mathf.Clamp01(signal));
        ApplyLook();
    }

    private void ApplyLook()
    {
        Shader.SetGlobalFloat(AspectId, Screen.height > 0 ? (float)Screen.width / Screen.height : 1f);
        Shader.SetGlobalVector(OffColorId, config.offColor.linear);
        Shader.SetGlobalVector(LineColorId, config.lineColor.linear * config.intensity);
        Shader.SetGlobalVector(GlowColorId, config.glowColor.linear * config.intensity);
        Shader.SetGlobalFloat(LineThicknessId, config.lineThickness);
        Shader.SetGlobalFloat(DotSizeId, config.dotSize);
        Shader.SetGlobalFloat(GlowSizeId, config.glowSize);
        Shader.SetGlobalFloat(ScanlineCountId, config.scanlineCount);
        Shader.SetGlobalFloat(ScanlineStrengthId, config.scanlineStrength);
        Shader.SetGlobalFloat(EdgeSoftnessId, config.edgeSoftness);
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && MusicManager.Instance != null)
            MusicManager.Instance.PlaySFXOneShot(clip, config.sfxVolume);
    }

    private static void QuitApplication()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
