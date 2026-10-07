using System.Collections;
using UnityEngine;
using TMPro;

public class ChestAnnouncement : MonoBehaviour
{
    private static ChestAnnouncement instance;
    private static bool isQuitting = false;

    [Header("Timing")]
    [Tooltip("Cuánto tiempo (seg) permanece visible el mensaje.")]
    [SerializeField] private float displayDuration = 2.5f;
    [Tooltip("Cuántas veces por segundo pulsa (grande/pequeño). Más bajo = más lento.")]
    [SerializeField] private float pulseFrequency = 0.8f;
    [Tooltip("Escala mínima y máxima del pulso.")]
    [SerializeField] private float pulseMinScale = 0.9f;
    [SerializeField] private float pulseMaxScale = 1.15f;

    [Header("Referencias visuales del prefab")]
    [SerializeField] private TMP_Text text;
    [SerializeField] private RectTransform textRect;
    [SerializeField] private CanvasGroup group;
    [SerializeField] private UnityEngine.UI.Image plate;
    private Coroutine routine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        isQuitting = false;
    }

    public static void Show()
    {
        if (isQuitting) return;
        EnsureExists();
        if (instance != null) instance.ShowInternal();
    }

    private static void EnsureExists()
    {
        if (instance != null) return;

        instance = RuntimeUIPrefabs.Spawn(p => p.chestAnnouncement);
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

    private void OnApplicationQuit()
    {
        isQuitting = true;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void ShowInternal()
    {
        if (text == null) return;

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(AnimateRoutine());
    }

    private IEnumerator AnimateRoutine()
    {
        textRect.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < displayDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = (Mathf.Sin(elapsed * pulseFrequency * Mathf.PI * 2f) + 1f) * 0.5f;
            float scale = Mathf.Lerp(pulseMinScale, pulseMaxScale, t);
            textRect.localScale = new Vector3(scale, scale, 1f);

            float fadeStart = displayDuration * 0.75f;
            float alpha = elapsed < fadeStart
                ? 1f
                : Mathf.Clamp01(1f - (elapsed - fadeStart) / (displayDuration - fadeStart));
            group.alpha = alpha;

            yield return null;
        }

        textRect.localScale = Vector3.one;
        group.alpha = 1f;
        textRect.gameObject.SetActive(false);
        routine = null;
    }
}
