using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Aviso de OVERTIME: al agotarse el tiempo (GameEvents.OnMatchTimeExpired) muestra
/// un texto rojo grande pulsante en la parte superior (sin tapar el centro), hace
/// parpadear la pantalla en rojo y reproduce un audio con fade in/out, todo durante
/// unos segundos. Se autocrea (genera su propio Canvas/AudioSource), no requiere
/// setup en escena. El clip se asigna en SFXDatabase.overtimeAlertSFX.
/// </summary>
public class OvertimeAlert : MonoBehaviour
{
    private static OvertimeAlert instance;
    private static bool isQuitting = false;

    [Header("Duración")]
    [Tooltip("Cuánto dura todo el aviso (texto, parpadeo y audio) en segundos.")]
    [SerializeField] private float alertDuration = 5f;

    [Header("Texto")]
    [SerializeField] private string message = "¡OVERTIME!";
    [Tooltip("Posición vertical del texto (0 = abajo, 1 = arriba). ~0.8 = arriba, sin tapar el centro.")]
    [SerializeField] private float verticalAnchor = 0.8f;
    [SerializeField] private int fontSize = 90;
    [SerializeField] private Color textColor = new Color(1f, 0.12f, 0.12f);
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

    [Header("TEST")]
    [Tooltip("TEST: dispara el aviso de overtime poco después de iniciar la partida (quitar antes de publicar).")]
    [SerializeField] private bool testTriggerOnStart = true;
    [Tooltip("TEST: segundos a esperar antes de disparar el aviso de prueba.")]
    [SerializeField] private float testDelay = 3f;

    private Canvas canvas;
    private TMP_Text text;
    private RectTransform textRect;
    private Image flashImage;
    private AudioSource audioSource;
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

        GameObject go = new GameObject("OvertimeAlert");
        instance = go.AddComponent<OvertimeAlert>();
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
        BuildAudio();
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

    private void Start()
    {
        // TEST: dispara el aviso solo (sin la oleada) para poder verlo/escucharlo rápido.
        if (testTriggerOnStart)
        {
            StartCoroutine(TestTriggerRoutine());
        }
    }

    private IEnumerator TestTriggerRoutine()
    {
        yield return new WaitForSeconds(testDelay);
        Trigger();
    }

    private void Trigger()
    {
        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(AlertRoutine());
    }

    private void BuildUI()
    {
        GameObject canvasObj = new GameObject("OvertimeAlertCanvas");
        canvasObj.transform.SetParent(transform, false);

        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 250;

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // Overlay rojo a pantalla completa para el parpadeo.
        GameObject flashObj = new GameObject("RedFlash");
        flashObj.transform.SetParent(canvasObj.transform, false);
        flashImage = flashObj.AddComponent<Image>();
        flashImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);
        flashImage.raycastTarget = false;
        RectTransform flashRect = flashImage.rectTransform;
        flashRect.anchorMin = Vector2.zero;
        flashRect.anchorMax = Vector2.one;
        flashRect.offsetMin = Vector2.zero;
        flashRect.offsetMax = Vector2.zero;

        // Texto del aviso, en la parte superior (no tapa el centro de juego).
        GameObject textObj = new GameObject("OvertimeText");
        textObj.transform.SetParent(canvasObj.transform, false);
        text = textObj.AddComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.fontSize = fontSize;
        text.color = textColor;
        text.fontStyle = FontStyles.Bold;
        text.raycastTarget = false;

        textRect = text.rectTransform;
        textRect.anchorMin = new Vector2(0.5f, verticalAnchor);
        textRect.anchorMax = new Vector2(0.5f, verticalAnchor);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = new Vector2(1200f, 200f);

        text.gameObject.SetActive(false);
    }

    private void BuildAudio()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
    }

    private IEnumerator AlertRoutine()
    {
        // Prepara el audio (clip desde SFXDatabase).
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
            text.text = message;
            text.gameObject.SetActive(true);
        }

        float elapsed = 0f;
        float fadeOutStart = Mathf.Max(0f, alertDuration - audioFadeOut);

        while (elapsed < alertDuration)
        {
            float dt = Time.unscaledDeltaTime;
            elapsed += dt;

            // Pulso del texto.
            if (textRect != null)
            {
                float tp = (Mathf.Sin(elapsed * textPulseFrequency * Mathf.PI * 2f) + 1f) * 0.5f;
                float scale = Mathf.Lerp(textPulseMinScale, textPulseMaxScale, tp);
                textRect.localScale = new Vector3(scale, scale, 1f);
            }

            // Parpadeo rojo de pantalla.
            if (flashImage != null)
            {
                float tf = (Mathf.Sin(elapsed * flashFrequency * Mathf.PI * 2f) + 1f) * 0.5f;
                float a = tf * flashMaxAlpha;
                flashImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, a);
            }

            // Fade del audio: in al principio, out al final.
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

        // Limpieza al terminar.
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
            text.gameObject.SetActive(false);

        routine = null;
    }
}
