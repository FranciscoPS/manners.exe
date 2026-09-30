using UnityEngine;

public class InvisibleWallWarning : MonoBehaviour
{
    private static readonly int RevealId = Shader.PropertyToID("_Reveal");
    private static readonly int FadeId = Shader.PropertyToID("_Fade");
    private static readonly int RevealCenterId = Shader.PropertyToID("_RevealCenter");

    [Header("Muro")]
    [Tooltip("BoxCollider del muro invisible que vigila este aviso. Lo asigna 'Tools > Manners > Muros invisibles > 2'. El aviso se coloca sobre la cara del muro que mira al jugador, sin importar la rotación ni la escala del muro.")]
    [SerializeField] private BoxCollider wall;
    [Tooltip("Metros del muro, desde su extremo inicial (esfera azul al seleccionar este aviso en la escena), donde el aviso no aparece. Úsalo en esquinas donde el muro sigue por fuera del mapa.")]
    [SerializeField, Min(0f)] private float trimStart;
    [Tooltip("Metros del muro, desde su extremo final (esfera roja al seleccionar este aviso en la escena), donde el aviso no aparece.")]
    [SerializeField, Min(0f)] private float trimEnd;

    [Header("Referencias")]
    [Tooltip("Quad con el material 'Custom/WallWarningHex' pegado a la pared (campo de hexágonos). Se escala al largo del campo (config) y al alto de la zona en cada frame; el tamaño de cada hexágono, colores, brillo y animaciones se ajustan en el material, en metros.")]
    [SerializeField] private Renderer hexPanel;
    [Tooltip("Quad acostado en el piso, en la base de la pared, con el mismo material de hexágonos. Es lo que se lee en muros que la cámara ve de canto. Su profundidad se ajusta en el config (Banda en el piso). Opcional.")]
    [SerializeField] private Renderer groundBand;
    [Tooltip("Canvas en World Space con las franjas de WARNING. Su Width/Height se ajusta a la zona alrededor del jugador usando la escala del propio canvas (0.01 = 100 unidades de canvas por metro). Las franjas se anclan arriba y abajo: su alto, posición y márgenes se editan en sus RectTransform.")]
    [SerializeField] private RectTransform stripesCanvas;
    [Tooltip("CanvasGroup del canvas de franjas. Su alpha lo controla el aviso según la cercanía.")]
    [SerializeField] private CanvasGroup stripesGroup;
    [Tooltip("Textos que se desplazan dentro de las franjas.")]
    [SerializeField] private WarningMarquee[] marquees;

    [Header("Orden de dibujo")]
    [Tooltip("Order in Layer del campo de hexágonos y de la banda del piso. Mayor que 0 para dibujarse encima del agua y de los demás transparentes (si no, el agua los tapa).")]
    [SerializeField] private int panelSortingOrder = 1;
    [Tooltip("Order in Layer del canvas de franjas. Mayor que el del campo para que el WARNING quede siempre encima de los hexágonos.")]
    [SerializeField] private int stripesSortingOrder = 2;

    private Canvas stripesCanvasComponent;
    private MaterialPropertyBlock properties;
    private Vector3 wallCenter;
    private Vector3 normalAxis;
    private Vector3 alongAxis;
    private float halfThickness;
    private float halfLength;
    private bool wallReady;
    private bool initialized;
    private bool visible = true;
    private float intensity;
    private float smoothedLead;
    private float hiddenTimer;
    private Vector4 appliedSize;
    private float appliedIntensity = -1f;
    private float appliedPanelFade = -1f;
    private float appliedStripesFade = -1f;
    private float appliedPanelFocus = -1f;
    private float appliedGroundFocus = -1f;

    public BoxCollider Wall => wall;

    private void Awake()
    {
        if (!initialized)
            SetVisible(false);
    }

    public void Initialize()
    {
        initialized = true;
        wallReady = CacheWallFrame();
        if (stripesCanvas != null)
            stripesCanvasComponent = stripesCanvas.GetComponent<Canvas>();

        if (hexPanel != null) hexPanel.sortingOrder = panelSortingOrder;
        if (groundBand != null) groundBand.sortingOrder = panelSortingOrder;
        if (stripesCanvasComponent != null) stripesCanvasComponent.sortingOrder = stripesSortingOrder;

        intensity = 0f;
        hiddenTimer = 0f;
        appliedSize = Vector4.zero;
        appliedIntensity = -1f;
        appliedPanelFade = -1f;
        appliedStripesFade = -1f;
        appliedPanelFocus = -1f;
        appliedGroundFocus = -1f;
        SetVisible(false);
    }

    public void AlignToWall(float previewHeight, Vector3 interiorReference)
    {
        if (!CacheWallFrame()) return;

        Vector3 inward = Vector3.Dot(interiorReference - wallCenter, normalAxis) >= 0f ? normalAxis : -normalAxis;
        Vector3 position = wallCenter + inward * (halfThickness + 0.05f);
        position.y = previewHeight;
        transform.SetPositionAndRotation(position, Quaternion.LookRotation(-inward, Vector3.up));
    }

    public void Tick(in InvisibleWallWarningSystem.Frame frame, float deltaTime)
    {
        if (!wallReady) return;

        InvisibleWallWarningConfig config = frame.config;
        Vector3 toPlayer = frame.playerCenter - wallCenter;
        float normalOffset = Vector3.Dot(toPlayer, normalAxis);
        Vector3 normal = normalOffset >= 0f ? normalAxis : -normalAxis;
        float along = Vector3.Dot(toPlayer, alongAxis);

        float gap = Mathf.Max(0f, Mathf.Abs(normalOffset) - halfThickness - frame.playerRadius);
        float overshoot = Mathf.Max(0f, Mathf.Abs(along) - halfLength);
        bool pastEnd = overshoot > frame.playerRadius;
        float distance = overshoot > 0f ? Mathf.Sqrt(gap * gap + overshoot * overshoot) : gap;

        float approachSpeed = Mathf.Max(0f, -Vector3.Dot(frame.playerVelocity, normal));
        float reach = config.activationDistance + Mathf.Min(approachSpeed * config.lookAheadSeconds, config.maxLookAheadDistance);
        float full = Mathf.Min(config.fullIntensityDistance + approachSpeed * config.fullIntensityLeadSeconds, reach - 0.01f);

        float target = 0f;
        if (!frame.suppressed && !pastEnd)
        {
            float t = 1f - Mathf.InverseLerp(full, reach, distance);
            target = t * t * (3f - 2f * t);
        }

        float fadeSpeed = target > intensity ? config.fadeInSpeed : config.fadeOutSpeed;
        intensity = Mathf.MoveTowards(intensity, target, fadeSpeed * deltaTime);

        if (intensity <= 0f)
        {
            if (!visible) return;

            ApplyIntensity(1f, 1f, config.stripesAppearAt, appliedPanelFocus, appliedGroundFocus);
            hiddenTimer += deltaTime;
            if (hiddenTimer >= config.hideDelay)
                SetVisible(false);
            return;
        }

        hiddenTimer = 0f;

        float wallLength = halfLength * 2f;
        float zoneWidth = Mathf.Min(frame.zoneSize.x, wallLength);
        float fieldWidth = Mathf.Min(Mathf.Max(config.fieldLength, zoneWidth), wallLength);

        float lead = 0f;
        if (config.impactLeadSeconds > 0f && approachSpeed > 0.01f)
            lead = Vector3.Dot(frame.playerVelocity, alongAxis) * Mathf.Min(gap / approachSpeed, config.impactLeadSeconds);

        bool appearing = !visible;
        SetVisible(true);

        if (appearing || config.followSharpness <= 0f)
            smoothedLead = lead;
        else
            smoothedLead = Mathf.Lerp(smoothedLead, lead, 1f - Mathf.Exp(-config.followSharpness * deltaTime));

        float focusAlong = Mathf.Clamp(along + smoothedLead, -halfLength, halfLength);
        float zoneLimit = halfLength - zoneWidth * 0.5f;
        float fieldLimit = halfLength - fieldWidth * 0.5f;
        float zoneAlong = Mathf.Clamp(focusAlong, -zoneLimit, zoneLimit);
        float fieldAlong = Mathf.Clamp(focusAlong, -fieldLimit, fieldLimit);

        float faceOffset = halfThickness + config.surfaceOffset;
        float zoneCenterHeight = frame.zoneBottom + frame.zoneSize.y * 0.5f;
        Vector3 zoneCenter = WallPoint(zoneAlong, normal, faceOffset, zoneCenterHeight);
        Vector3 fieldCenter = WallPoint(fieldAlong, normal, faceOffset, zoneCenterHeight);

        bool cameraBehindWall = frame.hasCamera && Vector3.Dot(frame.cameraPosition - zoneCenter, normal) < 0f;
        transform.SetPositionAndRotation(zoneCenter, Quaternion.LookRotation(cameraBehindWall ? normal : -normal, Vector3.up));

        if (hexPanel != null)
            hexPanel.transform.position = fieldCenter;

        PlaceGroundBand(fieldCenter, normal, frame.zoneBottom + config.bottomExtension + config.groundHeightOffset, config.groundDepth);
        ApplySize(new Vector4(zoneWidth, frame.zoneSize.y, fieldWidth, config.groundDepth));

        float focusOffset = focusAlong - fieldAlong;
        ApplyIntensity(
            cameraBehindWall ? config.panelOpacityBetweenCameraAndPlayer : 1f,
            cameraBehindWall ? config.stripesOpacityBetweenCameraAndPlayer : 1f,
            config.stripesAppearAt,
            FocusCoordinate(hexPanel, focusOffset, fieldWidth),
            FocusCoordinate(groundBand, focusOffset, fieldWidth));

        if (marquees == null || (stripesCanvasComponent != null && !stripesCanvasComponent.enabled)) return;

        for (int i = 0; i < marquees.Length; i++)
        {
            if (marquees[i] != null)
                marquees[i].Scroll(deltaTime);
        }
    }

    private bool CacheWallFrame()
    {
        if (wall == null) return false;

        Transform wallTransform = wall.transform;
        Vector3 scale = wallTransform.lossyScale;
        float halfX = Mathf.Abs(wall.size.x * scale.x) * 0.5f;
        float halfZ = Mathf.Abs(wall.size.z * scale.z) * 0.5f;
        Vector3 right = Vector3.ProjectOnPlane(wallTransform.right, Vector3.up).normalized;
        Vector3 forward = Vector3.ProjectOnPlane(wallTransform.forward, Vector3.up).normalized;

        wallCenter = wallTransform.TransformPoint(wall.center);

        if (halfX <= halfZ)
        {
            normalAxis = right;
            alongAxis = forward;
            halfThickness = halfX;
            halfLength = halfZ;
        }
        else
        {
            normalAxis = forward;
            alongAxis = right;
            halfThickness = halfZ;
            halfLength = halfX;
        }

        float alongMin = -halfLength + trimStart;
        float alongMax = halfLength - trimEnd;
        if (alongMax <= alongMin) return false;

        wallCenter += alongAxis * ((alongMin + alongMax) * 0.5f);
        halfLength = (alongMax - alongMin) * 0.5f;

        return normalAxis.sqrMagnitude > 0.5f && alongAxis.sqrMagnitude > 0.5f;
    }

    private Vector3 WallPoint(float alongPosition, Vector3 normal, float faceOffset, float height)
    {
        Vector3 point = wallCenter + alongAxis * alongPosition + normal * faceOffset;
        point.y = height;
        return point;
    }

    private float FocusCoordinate(Renderer target, float offsetAlong, float width)
    {
        if (target == null) return 0.5f;

        float direction = Vector3.Dot(alongAxis, target.transform.right) >= 0f ? 1f : -1f;
        return Mathf.Clamp01(0.5f + direction * offsetAlong / Mathf.Max(0.01f, width));
    }

    private void SetVisible(bool value)
    {
        if (visible == value) return;
        visible = value;
        gameObject.SetActive(value);
    }

    private void PlaceGroundBand(Vector3 wallBase, Vector3 normal, float height, float depth)
    {
        if (groundBand == null) return;

        bool show = depth > 0.01f;
        if (groundBand.enabled != show)
            groundBand.enabled = show;
        if (!show) return;

        Vector3 center = wallBase + normal * (depth * 0.5f);
        center.y = height;
        groundBand.transform.SetPositionAndRotation(center, Quaternion.LookRotation(Vector3.down, normal));
    }

    private void ApplySize(Vector4 size)
    {
        if ((size - appliedSize).sqrMagnitude < 0.0001f) return;
        appliedSize = size;

        if (hexPanel != null)
            hexPanel.transform.localScale = new Vector3(size.z, size.y, 1f);

        if (groundBand != null)
            groundBand.transform.localScale = new Vector3(size.z, Mathf.Max(0.01f, size.w), 1f);

        if (stripesCanvas != null)
        {
            Vector3 canvasScale = stripesCanvas.localScale;
            stripesCanvas.sizeDelta = new Vector2(
                size.x / Mathf.Max(0.0001f, canvasScale.x),
                size.y / Mathf.Max(0.0001f, canvasScale.y));
        }
    }

    private void ApplyIntensity(float panelFade, float stripesFade, float stripesAppearAt, float panelFocus, float groundFocus)
    {
        if (Mathf.Abs(intensity - appliedIntensity) < 0.002f
            && Mathf.Abs(panelFade - appliedPanelFade) < 0.002f
            && Mathf.Abs(stripesFade - appliedStripesFade) < 0.002f
            && Mathf.Abs(panelFocus - appliedPanelFocus) < 0.001f
            && Mathf.Abs(groundFocus - appliedGroundFocus) < 0.001f) return;

        appliedIntensity = intensity;
        appliedPanelFade = panelFade;
        appliedStripesFade = stripesFade;
        appliedPanelFocus = panelFocus;
        appliedGroundFocus = groundFocus;

        SetRendererValues(hexPanel, panelFade, panelFocus);
        SetRendererValues(groundBand, 1f, groundFocus);

        float stripesAlpha = Mathf.Clamp01((intensity - stripesAppearAt) / Mathf.Max(0.01f, 1f - stripesAppearAt)) * stripesFade;

        if (stripesGroup != null)
            stripesGroup.alpha = stripesAlpha;

        if (stripesCanvasComponent != null)
            stripesCanvasComponent.enabled = stripesAlpha > 0.001f;
    }

    private void SetRendererValues(Renderer target, float fade, float focus)
    {
        if (target == null) return;

        if (properties == null)
            properties = new MaterialPropertyBlock();

        target.GetPropertyBlock(properties);
        properties.SetFloat(RevealId, intensity);
        properties.SetFloat(FadeId, fade);
        properties.SetFloat(RevealCenterId, Mathf.Clamp01(focus));
        target.SetPropertyBlock(properties);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!CacheWallFrame()) return;

        Vector3 start = wallCenter - alongAxis * halfLength;
        Vector3 end = wallCenter + alongAxis * halfLength;
        Gizmos.color = new Color(1f, 0.25f, 0.2f, 1f);
        Gizmos.DrawLine(start, end);
        Gizmos.color = new Color(0.2f, 0.6f, 1f, 1f);
        Gizmos.DrawSphere(start, 0.6f);
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 1f);
        Gizmos.DrawSphere(end, 0.6f);
    }
#endif
}
