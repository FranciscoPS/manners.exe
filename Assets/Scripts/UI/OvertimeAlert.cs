using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OvertimeAlert : MonoBehaviour
{
    private static OvertimeAlert instance;
    private static bool isQuitting = false;

    [Header("Duración")]
    [Tooltip("Cuánto dura todo el aviso (texto, parpadeo y audio) en segundos.")]
    [SerializeField] private float alertDuration = 5f;

    [Header("Texto")]
    [Tooltip("Veces por segundo que pulsa el texto (grande/pequeño).")]
    [SerializeField] private float textPulseFrequency = 2f;
    [SerializeField] private float textPulseMinScale = 0.9f;
    [SerializeField] private float textPulseMaxScale = 1.3f;

    [Header("Parpadeo de pantalla")]
    [SerializeField] private Color flashColor = new Color(1f, 0f, 0f, 1f);
    [Tooltip("Veces por segundo que parpadea la pantalla en rojo.")]
    [SerializeField] private float flashFrequency = 2.5f;
    [Tooltip("Opacidad máxima del parpadeo rojo (0-1). Bajo para no cegar.")]
    [SerializeField] private float flashMaxAlpha = 0.35f;

    [Header("Audio (fade)")]
    [Tooltip("Tiempo de fade in del audio (seg).")]
    [SerializeField] private float audioFadeIn = 0.4f;
    [Tooltip("Tiempo de fade out del audio (seg).")]
    [SerializeField] private float audioFadeOut = 0.7f;

    [Header("Referencias del prefab")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private TMP_Text text;
    [SerializeField] private RectTransform textRect;
    [SerializeField] private Image flashImage;
    [SerializeField] private AudioSource audioSource;
    private Coroutine routine;

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

    private static void EnsureExists()
    {
        if (isQuitting || instance != null) return;

        instance = RuntimeUIPrefabs.Spawn(p => p.overtimeAlert);
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

    }

    private void OnEnable()
    {
        GameEvents.OnMatchTimeExpired += Trigger;
    }

    private void OnDisable()
    {
        GameEvents.OnMatchTimeExpired -= Trigger;
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

    private void Trigger()
    {
        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(AlertRoutine());
    }

    private IEnumerator AlertRoutine()
    {

        AudioClip clip = SFXDatabase.Instance != null ? SFXDatabase.Instance.overtimeAlertSFX : null;
        float targetVolume = SFXDatabase.Instance != null ? SFXDatabase.Instance.overtimeAlertVolume : 0.9f;

        if (clip != null && audioSource != null)
        {
            audioSource.clip = clip;
            audioSource.volume = 0f;
            audioSource.Play();
        }

        if (text != null)
        {

            textRect.gameObject.SetActive(true);
        }

        float elapsed = 0f;
        float fadeOutStart = Mathf.Max(0f, alertDuration - audioFadeOut);

        while (elapsed < alertDuration)
        {
            float dt = Time.unscaledDeltaTime;
            elapsed += dt;

            if (textRect != null)
            {
                float tp = (Mathf.Sin(elapsed * textPulseFrequency * Mathf.PI * 2f) + 1f) * 0.5f;
                float scale = Mathf.Lerp(textPulseMinScale, textPulseMaxScale, tp);
                textRect.localScale = new Vector3(scale, scale, 1f);
            }

            if (flashImage != null)
            {
                float tf = (Mathf.Sin(elapsed * flashFrequency * Mathf.PI * 2f) + 1f) * 0.5f;
                float a = tf * flashMaxAlpha;
                flashImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, a);
            }

            if (clip != null && audioSource != null)
            {
                float vol;
                if (elapsed < audioFadeIn)
                    vol = Mathf.Lerp(0f, targetVolume, elapsed / Mathf.Max(0.0001f, audioFadeIn));
                else if (elapsed > fadeOutStart)
                    vol = Mathf.Lerp(targetVolume, 0f, (elapsed - fadeOutStart) / Mathf.Max(0.0001f, audioFadeOut));
                else
                    vol = targetVolume;

                audioSource.volume = vol;
            }

            yield return null;
        }

        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.volume = 0f;
        }

        if (flashImage != null)
            flashImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);

        if (textRect != null)
            textRect.localScale = Vector3.one;

        if (text != null)
            textRect.gameObject.SetActive(false);

        routine = null;
    }
}
