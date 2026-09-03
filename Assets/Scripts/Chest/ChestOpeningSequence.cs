using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class ChestOpeningSequence : MonoBehaviour
{
    private static ChestOpeningSequence instance;
    private static bool isQuitting = false;

    private GameObject canvasRoot;
    private Image dimOverlay;
    private Image flashOverlay;
    private CanvasGroup promptGroup;
    private TextMeshProUGUI promptText;
    private TextMeshProUGUI skipHintText;
    private RadiantAuraVFX aura;

    private bool skipRequested;
    private float promptHue;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        isQuitting = false;
    }

    public static void Play(ChestItemData item, GameObject chestInstance, Action onComplete)
    {
        if (isQuitting)
        {
            onComplete?.Invoke();
            return;
        }

        EnsureExists();
        instance.StartCoroutine(instance.RunSequence(chestInstance, onComplete));
    }

    private static void EnsureExists()
    {
        if (instance != null) return;

        GameObject go = new GameObject("ChestOpeningSequence");
        instance = go.AddComponent<ChestOpeningSequence>();
        DontDestroyOnLoad(go);
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
        BuildUI();
    }

    private void OnApplicationQuit()
    {
        isQuitting = true;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void BuildUI()
    {
        canvasRoot = new GameObject("ChestOpeningCanvas");
        canvasRoot.transform.SetParent(transform, false);

        Canvas canvas = canvasRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;

        CanvasScaler scaler = canvasRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        dimOverlay = CreateFullscreenImage("DimOverlay", Color.clear);
        dimOverlay.raycastTarget = false;
        SetupVignetteMaterial(dimOverlay);

        GameObject auraObj = new GameObject("Aura", typeof(RectTransform));
        auraObj.transform.SetParent(canvasRoot.transform, false);
        RectTransform auraRect = auraObj.GetComponent<RectTransform>();
        auraRect.anchorMin = auraRect.anchorMax = new Vector2(0.5f, 0.5f);
        auraRect.pivot = new Vector2(0.5f, 0.5f);
        auraRect.sizeDelta = new Vector2(1650f, 1650f);
        auraRect.anchoredPosition = Vector2.zero;

        aura = auraObj.AddComponent<RadiantAuraVFX>();
        aura.SizeMultiplier = 1f;
        aura.RaySegments = 18;
        aura.HoleRadius = 0.5f;
        aura.HoleSoftness = 0.35f;
        aura.Initialize(auraRect);

        BuildPromptText();

        flashOverlay = CreateFullscreenImage("FlashOverlay", Color.clear);
        flashOverlay.raycastTarget = false;

        canvasRoot.SetActive(false);
    }

    private static void SetupVignetteMaterial(Image target)
    {
        Shader shader = Shader.Find("UI/RadialVignette");
        if (shader == null) return;

        Material mat = new Material(shader);
        mat.SetFloat("_InnerRadius", 0.55f);
        mat.SetFloat("_OuterRadius", 1.7f);

        Vector2 size = target.rectTransform.rect.size;
        float minDim = Mathf.Max(1f, Mathf.Min(size.x, size.y));
        mat.SetVector("_RectSize", new Vector4(size.x / minDim, size.y / minDim, 0f, 0f));

        target.material = mat;
    }

    private Image CreateFullscreenImage(string objName, Color color)
    {
        GameObject go = new GameObject(objName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(canvasRoot.transform, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = go.GetComponent<Image>();
        img.color = color;
        return img;
    }

    private void BuildPromptText()
    {
        GameObject textObj = new GameObject("PromptText", typeof(RectTransform));
        textObj.transform.SetParent(canvasRoot.transform, false);

        promptGroup = textObj.AddComponent<CanvasGroup>();

        RectTransform rt = textObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.14f);
        rt.anchorMax = new Vector2(0.5f, 0.14f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(1200f, 160f);
        rt.anchoredPosition = Vector2.zero;

        promptText = textObj.AddComponent<TextMeshProUGUI>();
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.fontSize = 64;
        promptText.fontStyle = FontStyles.Bold;
        promptText.raycastTarget = false;
        promptText.text = "";

        GameObject hintObj = new GameObject("SkipHintText", typeof(RectTransform));
        hintObj.transform.SetParent(textObj.transform, false);

        RectTransform hintRect = hintObj.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0.5f, 0f);
        hintRect.anchorMax = new Vector2(0.5f, 0f);
        hintRect.pivot = new Vector2(0.5f, 1f);
        hintRect.sizeDelta = new Vector2(1000f, 50f);
        hintRect.anchoredPosition = new Vector2(0f, 6f);

        skipHintText = hintObj.AddComponent<TextMeshProUGUI>();
        skipHintText.alignment = TextAlignmentOptions.Center;
        skipHintText.fontSize = 30;
        skipHintText.color = new Color(0.85f, 0.88f, 0.95f, 0.9f);
        skipHintText.raycastTarget = false;
        skipHintText.text = "";
    }

    private static string KeyLabel(Key key)
    {
        switch (key)
        {
            case Key.Space: return "Espacio";
            case Key.Enter: return "Enter";
            case Key.Escape: return "Esc";
            default: return key.ToString();
        }
    }

    private IEnumerator RunSequence(GameObject chestInstance, Action onComplete)
    {
        ChestOpeningConfig config = ChestOpeningConfig.Instance;
        Animator chestAnimator = chestInstance != null ? chestInstance.GetComponentInChildren<Animator>() : null;
        Vector3 chestPosition = chestInstance != null ? chestInstance.transform.position : Vector3.zero;

        if (chestAnimator != null)
            chestAnimator.speed = 0f;

        Time.timeScale = 0f;
        skipRequested = false;
        promptHue = UnityEngine.Random.value;

        canvasRoot.SetActive(true);
        dimOverlay.color = new Color(config.dimColor.r, config.dimColor.g, config.dimColor.b, 0f);
        flashOverlay.color = Color.clear;
        promptGroup.alpha = 1f;
        promptText.text = config.promptMessage;
        skipHintText.text = config.allowSkip ? string.Format(config.skipHintMessage, KeyLabel(config.skipKey)) : "";
        aura.SpinMultiplier = 0.3f;
        aura.Play();

        PlayClip(config.buildupSFX, config.sfxVolume);

        yield return RunAnticipationPhase(config);

        if (!skipRequested)
            yield return RunBurstPhase(config, chestAnimator, chestPosition);

        if (!skipRequested)
            yield return RunRevealPhase(config, chestAnimator);

        FinishInstantly(chestAnimator);

        yield return RunFadeOutPhase(config, skipRequested ? 0.15f : 0.4f);

        canvasRoot.SetActive(false);
        aura.Stop();

        onComplete?.Invoke();
    }

    private IEnumerator RunAnticipationPhase(ChestOpeningConfig config)
    {
        float elapsed = 0f;
        float nextShake = 0f;

        while (elapsed < config.anticipationDuration)
        {
            if (CheckSkip(config)) yield break;

            float t = elapsed / Mathf.Max(0.01f, config.anticipationDuration);
            dimOverlay.color = new Color(config.dimColor.r, config.dimColor.g, config.dimColor.b, config.dimColor.a * t);
            aura.SpinMultiplier = Mathf.Lerp(0.3f, 1f, t);
            UpdatePromptPulse(t);

            if (elapsed >= nextShake)
            {
                CameraShakeManager.Instance?.Shake(config.anticipationShakeForce);
                nextShake = elapsed + config.anticipationShakeInterval;
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        dimOverlay.color = config.dimColor;
    }

    private IEnumerator RunBurstPhase(ChestOpeningConfig config, Animator chestAnimator, Vector3 chestPosition)
    {
        CameraShakeManager.Instance?.Shake(config.burstShakeForce);
        PlayClip(config.burstSFX, config.sfxVolume);
        SpawnWorldBurst(chestPosition);

        if (chestAnimator != null)
        {
            int stateHash = chestAnimator.GetCurrentAnimatorStateInfo(0).fullPathHash;
            chestAnimator.Play(stateHash, 0, 0f);
            chestAnimator.speed = 1f;
        }

        aura.SpinMultiplier = 3f;

        float elapsed = 0f;
        while (elapsed < config.burstDuration)
        {
            if (CheckSkip(config)) yield break;

            float t = elapsed / Mathf.Max(0.01f, config.burstDuration);
            float flashT = 1f - Mathf.Abs(t * 2f - 1f);
            flashOverlay.color = new Color(config.flashColor.r, config.flashColor.g, config.flashColor.b, flashT);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        flashOverlay.color = Color.clear;
    }

    private IEnumerator RunRevealPhase(ChestOpeningConfig config, Animator chestAnimator)
    {
        float lidLength = config.lidOpenFallbackDuration;

        if (chestAnimator != null)
        {
            yield return null;
            AnimatorStateInfo info = chestAnimator.GetCurrentAnimatorStateInfo(0);
            if (info.length > 0.01f)
                lidLength = info.length;
        }

        float holdDuration = Mathf.Max(0f, lidLength) + config.revealHoldDuration;
        float elapsed = 0f;
        bool frozeAnimator = false;

        while (elapsed < holdDuration)
        {
            if (CheckSkip(config)) yield break;

            if (!frozeAnimator && chestAnimator != null && elapsed >= lidLength)
            {
                chestAnimator.speed = 0f;
                frozeAnimator = true;
            }

            UpdatePromptPulse(1f);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (chestAnimator != null)
            chestAnimator.speed = 0f;
    }

    private IEnumerator RunFadeOutPhase(ChestOpeningConfig config, float duration)
    {
        float startDim = dimOverlay.color.a;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            dimOverlay.color = new Color(config.dimColor.r, config.dimColor.g, config.dimColor.b, Mathf.Lerp(startDim, 0f, t));
            promptGroup.alpha = Mathf.Lerp(1f, 0f, t);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        dimOverlay.color = Color.clear;
        promptGroup.alpha = 0f;
    }

    private void UpdatePromptPulse(float intensity)
    {
        promptHue += Time.unscaledDeltaTime * 0.6f;
        if (promptHue > 1f) promptHue -= 1f;

        promptText.color = Color.HSVToRGB(promptHue, 0.35f, 1f);

        float scale = 1f + Mathf.Sin(Time.unscaledTime * 6f) * 0.05f * intensity;
        promptText.rectTransform.localScale = Vector3.one * scale;
    }

    private void FinishInstantly(Animator chestAnimator)
    {
        if (chestAnimator != null)
        {
            int stateHash = chestAnimator.GetCurrentAnimatorStateInfo(0).fullPathHash;
            chestAnimator.speed = 1f;
            chestAnimator.Play(stateHash, 0, 1f);
            chestAnimator.Update(0f);
            chestAnimator.speed = 0f;
        }

        flashOverlay.color = Color.clear;
        aura.SpinMultiplier = 1f;
    }

    private bool CheckSkip(ChestOpeningConfig config)
    {
        if (!config.allowSkip) return false;
        if (Keyboard.current == null) return false;

        var control = Keyboard.current[config.skipKey];
        if (control != null && control.wasPressedThisFrame)
        {
            skipRequested = true;
            return true;
        }

        return false;
    }

    private static void PlayClip(AudioClip clip, float volume)
    {
        if (clip == null || MusicManager.Instance == null) return;
        MusicManager.Instance.PlaySFXOneShot(clip, volume);
    }

    private static void SpawnWorldBurst(Vector3 position)
    {
        GameObject go = new GameObject("ChestBurstVFX");
        go.transform.position = position + Vector3.up * 1.2f;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 1.2f;
        main.loop = false;
        main.useUnscaledTime = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.1f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 7f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.9f);
        main.startColor = Color.white;
        main.gravityModifier = -0.15f;
        main.maxParticles = 60;
        main.playOnAwake = false;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 40, 60) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.3f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(1f, 0.95f, 0.6f), 0f),
                new GradientColorKey(new Color(1f, 0.6f, 1f), 0.5f),
                new GradientColorKey(new Color(0.5f, 0.8f, 1f), 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 0.6f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortMode = ParticleSystemSortMode.Distance;

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");

        if (shader != null)
        {
            Material mat = new Material(shader);
            mat.mainTexture = RadiantAuraVFX.GetOrCreateSoftDotTexture();
            mat.SetFloat("_Surface", 1);
            mat.SetFloat("_BlendOp", 0);
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            mat.SetFloat("_ZWrite", 0);
            mat.renderQueue = 3000;
            renderer.material = mat;
        }

        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        ps.Play();
        Destroy(go, 2.5f);
    }
}
