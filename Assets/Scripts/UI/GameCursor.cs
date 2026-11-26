using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class GameCursor : MonoBehaviour, ILateUpdateable
{
    private const float TwoPi = Mathf.PI * 2f;
    private const int EditorEscapeRestoreFrames = 3;

    [Header("Referencias del prefab")]
    [SerializeField] private Canvas canvas;
    [Tooltip("Grupo raíz: el cursor entero aparece y se oculta con su opacidad.")]
    [SerializeField] private CanvasGroup group;
    [Tooltip("Punto exacto del clic. El código lo mueve a la posición del mouse; la flecha, la manita y el pulso cuelgan de aquí.")]
    [SerializeField] private RectTransform pointer;
    [Tooltip("Flecha normal. Su pivote está en la punta.")]
    [SerializeField] private RectTransform arrow;
    [SerializeField] private CanvasGroup arrowGroup;
    [SerializeField] private Image arrowAccent;
    [Tooltip("Manita sobre botones y elementos clicables. Su pivote está en la punta del dedo.")]
    [SerializeField] private RectTransform hand;
    [SerializeField] private CanvasGroup handGroup;
    [Tooltip("Anillo que se expande desde la punta al hacer clic.")]
    [SerializeField] private Image pulse;

    [Header("Tamaño")]
    [Tooltip("Alto mínimo de la flecha en píxeles reales. El tamaño normal escala con la resolución como el resto de la interfaz (se diseña a 1080p en el RectTransform de Arrow y Hand); en ventanas muy pequeñas no baja de este alto.")]
    [Min(8f)] [SerializeField] private float minPixelHeight = 22f;

    [Header("Cambio entre flecha y manita")]
    [Tooltip("Velocidad del cambio entre flecha y manita. Más alto = más inmediato.")]
    [Min(1f)] [SerializeField] private float swapSharpness = 22f;
    [Tooltip("Escala con la que aparece la figura entrante antes de crecer a su tamaño normal.")]
    [Range(0.2f, 1f)] [SerializeField] private float swapStartScale = 0.55f;
    [Tooltip("Segundos del rebote con el que la manita y la flecha aparecen.")]
    [Min(0.01f)] [SerializeField] private float popDuration = 0.22f;
    [Tooltip("Cuánto se pasa del tamaño normal en el rebote. 0 = sin rebote.")]
    [Range(0f, 4f)] [SerializeField] private float popOvershoot = 2.2f;

    [Header("Manita en reposo")]
    [Tooltip("Cada cuántos segundos la manita hace un toque de invitación sobre el botón. 0 = nunca.")]
    [Min(0f)] [SerializeField] private float tapInterval = 1.1f;
    [Tooltip("Segundos que dura cada toque.")]
    [Min(0.01f)] [SerializeField] private float tapDuration = 0.24f;
    [Tooltip("Escala mínima del toque (se encoge hacia la punta del dedo).")]
    [Range(0.5f, 1f)] [SerializeField] private float tapScale = 0.9f;

    [Header("Clic")]
    [Tooltip("Escala mientras se mantiene pulsado el botón del mouse.")]
    [Range(0.5f, 1f)] [SerializeField] private float pressScale = 0.82f;
    [Tooltip("Velocidad con la que se aprieta y se suelta.")]
    [Min(1f)] [SerializeField] private float pressSharpness = 30f;
    [Tooltip("Segundos que tarda el anillo del clic en expandirse y desaparecer.")]
    [Min(0.01f)] [SerializeField] private float pulseDuration = 0.32f;
    [Tooltip("Escala inicial y final del anillo del clic.")]
    [SerializeField] private Vector2 pulseScaleRange = new Vector2(0.3f, 1.25f);

    [Header("Destello de la flecha")]
    [Tooltip("Segundos de cada ciclo de brillo de la franja cian de la flecha. 0 = sin destello.")]
    [Min(0f)] [SerializeField] private float glintPeriod = 2.6f;
    [Tooltip("Opacidad mínima de la franja durante el destello.")]
    [Range(0f, 1f)] [SerializeField] private float glintMinAlpha = 0.45f;

    [Header("Ocultar en reposo")]
    [Tooltip("Segundos sin mover el mouse tras los que el cursor se desvanece mientras la partida corre (no en pausa ni sobre botones). 0 = siempre visible.")]
    [Min(0f)] [SerializeField] private float hideAfterIdleSeconds = 3f;
    [Tooltip("Segundos del fundido al ocultarse o reaparecer.")]
    [Min(0.01f)] [SerializeField] private float fadeDuration = 0.18f;

    [Header("Fluidez en menús")]
    [Tooltip("Segundos que el menú principal y la pausa mantienen los FPS elegidos en Gráficos después de mover el mouse. Pasado ese tiempo vuelven a 30 FPS para ahorrar energía. 0 = siempre a 30.")]
    [Min(0f)] [SerializeField] private float responsiveSeconds = 1.5f;

    private static GameCursor instance;
    private static bool isQuitting;

    private readonly List<RaycastResult> results = new List<RaycastResult>(16);
    private EventSystem pointerSystem;
    private PointerEventData pointerData;
    private Vector2 lastPosition = new Vector2(float.NaN, float.NaN);
    private float handBlend;
    private float pressBlend;
    private float popTime = float.MaxValue;
    private float pulseTime = float.MaxValue;
    private float hoverTime;
    private float lastMoveTime;
    private float visibility = 1f;
    private float sizeScale = 1f;
    private float lastScaleFactor = -1f;
    private float glintClock;
    private Color accentColor;
    private Color pulseColor;
    private bool pointing;
    private bool hasFocus = true;
    private int restoreHideFrames;

    public bool IsActive => isActiveAndEnabled;
    public bool IsPointing => pointing;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        isQuitting = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (isQuitting || instance != null) return;
        instance = RuntimeUIPrefabs.Spawn(p => p.gameCursor);
    }

    public static float MinimumSizeScale(float designHeight, float scaleFactor, float minimumPixels)
    {
        float pixels = designHeight * scaleFactor;
        return pixels > 0f && pixels < minimumPixels ? minimumPixels / pixels : 1f;
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
        accentColor = arrowAccent.color;
        pulseColor = pulse.color;
        handGroup.alpha = 0f;
        hand.localScale = Uniform(swapStartScale);
        pulse.enabled = false;
        lastMoveTime = Time.unscaledTime;
    }

    private void OnEnable()
    {
        if (UpdateManager.Instance != null)
            UpdateManager.Instance.Register(this);
        ApplySystemCursor();
    }

    private void OnDisable()
    {
        Cursor.visible = true;
        if (isQuitting) return;
        if (UpdateManager.Instance != null)
            UpdateManager.Instance.Unregister(this);
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void OnApplicationQuit()
    {
        isQuitting = true;
        Cursor.visible = true;
    }

    private void OnApplicationFocus(bool focus)
    {
        hasFocus = focus;
        ApplySystemCursor();
    }

    private void ApplySystemCursor()
    {
        Cursor.visible = !hasFocus || Mouse.current == null;
    }

    public void OnLateUpdate(float deltaTime)
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || !hasFocus)
        {
            group.alpha = 0f;
            return;
        }

        RestoreHiddenSystemCursorInEditor();
        Refresh(mouse.position.ReadValue(), mouse.leftButton.wasPressedThisFrame, mouse.leftButton.isPressed, Time.unscaledDeltaTime);
    }

    private void RestoreHiddenSystemCursorInEditor()
    {
#if UNITY_EDITOR
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) restoreHideFrames = EditorEscapeRestoreFrames;
        if (restoreHideFrames <= 0) return;
        restoreHideFrames--;
        UnityEditor.Unsupported.SetAllowCursorHide(true);
        Cursor.visible = true;
        Cursor.visible = false;
#endif
    }

    public void Refresh(Vector2 position, bool pressedThisFrame, bool held, float unscaledDeltaTime)
    {
        if (Cursor.visible && hasFocus) Cursor.visible = false;

        float now = Time.unscaledTime;
        bool moved = position != lastPosition;
        if (moved)
        {
            bool firstSample = float.IsNaN(lastPosition.x);
            lastPosition = position;
            lastMoveTime = now;
            if (!firstSample && responsiveSeconds > 0f) GameGraphicsSettings.KeepResponsive(responsiveSeconds);
        }

        bool inside = position.x >= 0f && position.y >= 0f && position.x <= Screen.width && position.y <= Screen.height;
        bool idle = hideAfterIdleSeconds > 0f && Time.timeScale > 0f && now - lastMoveTime > hideAfterIdleSeconds;
        bool wasPointing = pointing;
        pointing = inside && visibility > 0f && IsOverInteractable(position);
        bool shown = inside && (!idle || pointing || pressedThisFrame || held);

        visibility = Mathf.MoveTowards(visibility, shown ? 1f : 0f, unscaledDeltaTime / fadeDuration);
        group.alpha = visibility;
        if (visibility <= 0f && !shown) return;

        UpdateSize();
        float scale = canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
        pointer.anchoredPosition = position / scale;

        if (pointing != wasPointing)
        {
            popTime = 0f;
            hoverTime = 0f;
        }

        if (pressedThisFrame)
        {
            pulseTime = 0f;
            pulse.enabled = true;
        }

        Animate(held, unscaledDeltaTime);
    }

    private void Animate(bool held, float dt)
    {
        handBlend = Mathf.Lerp(handBlend, pointing ? 1f : 0f, 1f - Mathf.Exp(-swapSharpness * dt));
        pressBlend = Mathf.Lerp(pressBlend, held ? 1f : 0f, 1f - Mathf.Exp(-pressSharpness * dt));
        popTime += dt;
        hoverTime += dt;

        float pop = BackOut(Mathf.Clamp01(popTime / popDuration), popOvershoot);
        float press = Mathf.Lerp(1f, pressScale, pressBlend);
        float tap = pointing && !held ? TapScale(hoverTime) : 1f;

        float arrowScale = Mathf.LerpUnclamped(1f, swapStartScale, handBlend) * (pointing ? 1f : pop) * press;
        float handScale = Mathf.LerpUnclamped(swapStartScale, 1f, handBlend) * (pointing ? pop : 1f) * press * tap;
        arrow.localScale = Uniform(arrowScale * sizeScale);
        hand.localScale = Uniform(handScale * sizeScale);
        arrowGroup.alpha = 1f - handBlend;
        handGroup.alpha = handBlend;

        if (glintPeriod > 0f)
        {
            glintClock = Mathf.Repeat(glintClock + dt / glintPeriod, 1f);
            float wave = 0.5f + 0.5f * Mathf.Cos(glintClock * TwoPi);
            Color color = accentColor;
            color.a *= Mathf.Lerp(glintMinAlpha, 1f, wave);
            arrowAccent.color = color;
        }

        if (pulse.enabled)
        {
            pulseTime += dt;
            float t = Mathf.Clamp01(pulseTime / pulseDuration);
            float ring = Mathf.Lerp(pulseScaleRange.x, pulseScaleRange.y, 1f - (1f - t) * (1f - t)) * sizeScale;
            pulse.rectTransform.localScale = Uniform(ring);
            Color color = pulseColor;
            color.a *= 1f - t;
            pulse.color = color;
            if (t >= 1f) pulse.enabled = false;
        }
    }

    private float TapScale(float time)
    {
        if (tapInterval <= 0f) return 1f;
        float phase = Mathf.Repeat(time, tapInterval + tapDuration) - tapInterval;
        if (phase < 0f) return 1f;
        return Mathf.Lerp(1f, tapScale, Mathf.Sin(phase / tapDuration * Mathf.PI));
    }

    private static Vector3 Uniform(float scale) => new Vector3(scale, scale, 1f);

    private static float BackOut(float t, float overshoot)
    {
        float u = t - 1f;
        return 1f + (overshoot + 1f) * u * u * u + overshoot * u * u;
    }

    private void UpdateSize()
    {
        float scaleFactor = canvas.scaleFactor;
        if (Mathf.Approximately(scaleFactor, lastScaleFactor)) return;
        lastScaleFactor = scaleFactor;
        sizeScale = MinimumSizeScale(arrow.rect.height, scaleFactor, minPixelHeight);
    }

    private bool IsOverInteractable(Vector2 position)
    {
        EventSystem current = EventSystem.current;
        if (current == null) return false;
        if (current != pointerSystem)
        {
            pointerSystem = current;
            pointerData = new PointerEventData(current);
        }

        pointerData.position = position;
        results.Clear();
        current.RaycastAll(pointerData, results);
        if (results.Count == 0) return false;

        Selectable selectable = results[0].gameObject.GetComponentInParent<Selectable>();
        return selectable != null && selectable.isActiveAndEnabled && selectable.IsInteractable();
    }
}
