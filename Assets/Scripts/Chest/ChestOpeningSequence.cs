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

    [SerializeField] private GameObject canvasRoot;
    [SerializeField] private Image dimOverlay;
    [SerializeField] private Image flashOverlay;
    [SerializeField] private CanvasGroup promptGroup;
    [SerializeField] private CanvasGroup skipHintGroup;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private TextMeshProUGUI skipHintText;
    [SerializeField] private RadiantAuraVFX aura;
    [SerializeField] private CanvasGroup auraGroup;
    [SerializeField] private ChestShowcase showcase;
    [SerializeField] private ParticleSystem chestBurstPrefab;
    private ParticleSystem chestBurst;

    [Header("Textos de la cinemática")]
    [SerializeField] private LocalizedString skipHint = new LocalizedString("Press {0} to skip", "Pulsa {0} para saltar");
    [SerializeField] private LocalizedString spaceKey = new LocalizedString("Space", "Espacio");

    [SerializeField, Range(0, 1)] private float dimOpacity = .82f;
    private Color dimBaseColor;
    private Color flashBaseColor;
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
        if (instance != null) instance.StartCoroutine(instance.RunSequence(chestInstance, onComplete));
        else onComplete?.Invoke();
    }

    private static void EnsureExists()
    {
        if (instance != null) return;

        instance = RuntimeUIPrefabs.Spawn(p => p.chestOpeningSequence);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        dimBaseColor = dimOverlay.color;
        flashBaseColor = flashOverlay.color;
        DontDestroyOnLoad(gameObject);

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

    private string KeyLabel(Key key)
    {
        switch (key)
        {
            case Key.Space: return spaceKey.Value;
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
        dimOverlay.color = new Color(dimBaseColor.r, dimBaseColor.g, dimBaseColor.b, 0f);
        flashOverlay.color = Color.clear;
        promptGroup.alpha = 1f;
        skipHintGroup.alpha = 1f;
        auraGroup.alpha = 1f;
        promptText.GetComponent<LocalizedText>()?.Apply();
        skipHintText.text = config.allowSkip ? skipHint.Format(KeyLabel(config.skipKey)) : "";

        aura.SpinMultiplier = 0.3f;
        aura.Play();
        showcase.TryBegin(config);

        if (showcase.IsActive)
            showcase.SetAlpha(1f);

        PlayClip(config.buildupSFX, config.sfxVolume);

        if (showcase.IsActive && showcase.ClipLength > 0.05f)
            yield return RunClipTimeline(config, chestPosition);
        else
            yield return RunFallbackTimeline(config, chestPosition);

        FinishInstantly(chestAnimator);

        yield return RunFadeOutPhase(config, skipRequested ? 0.15f : Mathf.Max(0f, config.fadeOutDuration));

        canvasRoot.SetActive(false);
        aura.Stop();
        showcase.End();

        onComplete?.Invoke();
    }

    private IEnumerator RunClipTimeline(ChestOpeningConfig config, Vector3 chestPosition)
    {
        float clipLength = showcase.ClipLength;
        float burstAt = Mathf.Clamp(showcase.BurstTime, 0f, clipLength);
        float cutAt = Mathf.Clamp(clipLength - config.revealLeadTime, burstAt, clipLength);

        showcase.Seek(0f, 1f);

        float elapsed = 0f;
        float nextShake = 0f;
        bool burst = false;

        while (elapsed < cutAt)
        {
            if (CheckSkip(config)) yield break;

            if (!burst)
            {
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, burstAt));
                UpdateAnticipation(config, t);

                if (elapsed >= nextShake)
                {
                    CameraShakeManager.Instance?.Shake(config.anticipationShakeForce);
                    nextShake = elapsed + config.anticipationShakeInterval;
                }

                if (elapsed >= burstAt)
                {
                    burst = true;
                    TriggerBurst(config, chestPosition);
                }
            }

            if (burst)
            {
                UpdateFlash(config, (elapsed - burstAt) / Mathf.Max(0.01f, config.burstDuration));
                UpdatePromptPulse(1f);
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private IEnumerator RunFallbackTimeline(ChestOpeningConfig config, Vector3 chestPosition)
    {
        float elapsed = 0f;
        float nextShake = 0f;

        while (elapsed < config.anticipationDuration)
        {
            if (CheckSkip(config)) yield break;

            UpdateAnticipation(config, elapsed / Mathf.Max(0.01f, config.anticipationDuration));

            if (elapsed >= nextShake)
            {
                CameraShakeManager.Instance?.Shake(config.anticipationShakeForce);
                nextShake = elapsed + config.anticipationShakeInterval;
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        UpdateAnticipation(config, 1f);
        TriggerBurst(config, chestPosition);

        elapsed = 0f;
        while (elapsed < config.burstDuration)
        {
            if (CheckSkip(config)) yield break;

            UpdateFlash(config, elapsed / Mathf.Max(0.01f, config.burstDuration));

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        flashOverlay.color = Color.clear;

        float holdDuration = Mathf.Max(0f, config.lidOpenFallbackDuration) + config.revealHoldDuration;
        elapsed = 0f;
        while (elapsed < holdDuration)
        {
            if (CheckSkip(config)) yield break;

            UpdatePromptPulse(1f);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private void UpdateAnticipation(ChestOpeningConfig config, float t)
    {
        dimOverlay.color = new Color(dimBaseColor.r, dimBaseColor.g, dimBaseColor.b, dimOpacity * t);
        aura.SpinMultiplier = Mathf.Lerp(0.3f, 1f, t);
        UpdatePromptPulse(t);
    }

    private void TriggerBurst(ChestOpeningConfig config, Vector3 chestPosition)
    {
        CameraShakeManager.Instance?.Shake(config.burstShakeForce);
        PlayClip(config.burstSFX, config.sfxVolume);
        SpawnWorldBurst(chestPosition);
        aura.SpinMultiplier = 3f;
    }

    private void UpdateFlash(ChestOpeningConfig config, float t)
    {
        float flashT = t < 1f ? 1f - Mathf.Abs(t * 2f - 1f) : 0f;
        flashOverlay.color = new Color(flashBaseColor.r, flashBaseColor.g, flashBaseColor.b, flashT);
    }

    private IEnumerator RunFadeOutPhase(ChestOpeningConfig config, float duration)
    {
        float startDim = dimOverlay.color.a;
        float startPrompt = promptGroup.alpha;
        float startSkipHint = skipHintGroup.alpha;
        float startAura = auraGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            dimOverlay.color = new Color(dimBaseColor.r, dimBaseColor.g, dimBaseColor.b, Mathf.Lerp(startDim, 0f, t));
            promptGroup.alpha = Mathf.Lerp(startPrompt, 0f, t);
            skipHintGroup.alpha = Mathf.Lerp(startSkipHint, 0f, t);
            auraGroup.alpha = Mathf.Lerp(startAura, 0f, t);

            if (showcase.IsActive)
                showcase.SetAlpha(1f - t);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        dimOverlay.color = Color.clear;
        promptGroup.alpha = 0f;
        skipHintGroup.alpha = 0f;
        auraGroup.alpha = 0f;
    }

    private void UpdatePromptPulse(float intensity)
    {
        promptHue += Time.unscaledDeltaTime * 0.6f;
        if (promptHue > 1f) promptHue -= 1f;

        UIStyle style = UIStyle.Instance;
        promptText.color = style != null ? style.TextAccentCycle(promptHue) : Color.HSVToRGB(promptHue, 0.35f, 1f);

        float scale = 1f + Mathf.Sin(Time.unscaledTime * 6f) * 0.05f * intensity;
        promptText.rectTransform.localScale = Vector3.one * scale;
    }

    private void FinishInstantly(Animator chestAnimator)
    {
        SnapWorldChestOpen(chestAnimator);

        if (showcase.IsActive && skipRequested)
            showcase.Seek(showcase.ClipLength, 0f);

        flashOverlay.color = Color.clear;
        aura.SpinMultiplier = 1f;
    }

    private static void SnapWorldChestOpen(Animator chestAnimator)
    {
        if (chestAnimator == null) return;

        int stateHash = chestAnimator.GetCurrentAnimatorStateInfo(0).fullPathHash;
        chestAnimator.speed = 1f;
        chestAnimator.Play(stateHash, 0, 1f);
        chestAnimator.Update(0f);
        chestAnimator.speed = 0f;
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

    private void SpawnWorldBurst(Vector3 position)
    {
        if (chestBurstPrefab == null) return;
        if (chestBurst == null) chestBurst = Instantiate(chestBurstPrefab);
        chestBurst.transform.position = position + Vector3.up * 1.2f;
        chestBurst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        chestBurst.Play();
    }
}
