using UnityEngine;

public class InvisibleWallWarning : MonoBehaviour
{
    private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
    private static readonly int OpacityId = Shader.PropertyToID("_Opacity");

    [Header("Muro")]
    [Tooltip("BoxCollider del muro invisible que vigila este aviso. Lo asigna 'Tools > Manners > Muros invisibles > 2'. El aviso se coloca sobre la cara del muro que mira al jugador, sin importar la rotación ni la escala del muro.")]
    [SerializeField] private BoxCollider wall;

    [Header("Referencias")]
    [Tooltip("Quad con el material 'Custom/WallWarningHex' (panel de hexágonos). Se escala al tamaño de la zona en cada frame; el tamaño de cada hexágono, colores y animaciones se ajustan en el material, en metros.")]
    [SerializeField] private Renderer hexPanel;
    [Tooltip("Canvas en World Space con las franjas de WARNING. Su Width/Height se ajusta a la zona usando la escala del propio canvas (0.01 = 100 unidades de canvas por metro). Las franjas se anclan arriba y abajo: su alto, posición y márgenes se editan en sus RectTransform.")]
    [SerializeField] private RectTransform stripesCanvas;
    [Tooltip("CanvasGroup del canvas de franjas. Su alpha lo controla el aviso según la cercanía.")]
    [SerializeField] private CanvasGroup stripesGroup;
    [Tooltip("Textos que se desplazan dentro de las franjas.")]
    [SerializeField] private WarningMarquee[] marquees;

    private Canvas stripesCanvasComponent;
    private MaterialPropertyBlock properties;
    private Vector3 wallCenter;
    private Vector3 normalAxis;
    private Vector3 alongAxis;
    private float halfThickness;
    private float halfLength;
    private bool wallReady;
    private bool visible = true;
    private float intensity;
    private float alongPosition;
    private Vector2 appliedSize;
    private float appliedIntensity = -1f;
    private float appliedOpacity = -1f;

    public BoxCollider Wall => wall;

    public void Initialize()
    {
        wallReady = CacheWallFrame();
        if (stripesCanvas != null)
            stripesCanvasComponent = stripesCanvas.GetComponent<Canvas>();

        intensity = 0f;
        appliedSize = Vector2.zero;
        appliedIntensity = -1f;
        appliedOpacity = -1f;
        SetVisible(false);
    }

    public void AlignToWall(float previewHeight)
    {
        if (!CacheWallFrame()) return;

        Vector3 position = wallCenter + normalAxis * (halfThickness + 0.05f);
        position.y = previewHeight;
        transform.SetPositionAndRotation(position, Quaternion.LookRotation(-normalAxis, Vector3.up));
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
        float distance = overshoot > 0f ? Mathf.Sqrt(gap * gap + overshoot * overshoot) : gap;

        float approachSpeed = Mathf.Max(0f, -Vector3.Dot(frame.playerVelocity, normal));
        float reach = config.activationDistance + Mathf.Min(approachSpeed * config.lookAheadSeconds, config.maxLookAheadDistance);
        float full = Mathf.Min(config.fullIntensityDistance, reach - 0.01f);

        float target = 0f;
        if (!frame.suppressed)
        {
            float t = 1f - Mathf.InverseLerp(full, reach, distance);
            target = t * t * (3f - 2f * t);
        }

        float fadeSpeed = target > intensity ? config.fadeInSpeed : config.fadeOutSpeed;
        intensity = Mathf.MoveTowards(intensity, target, fadeSpeed * deltaTime);

        if (intensity <= 0f)
        {
            SetVisible(false);
            return;
        }

        float lead = 0f;
        if (config.impactLeadSeconds > 0f && approachSpeed > 0.01f)
        {
            float timeToImpact = gap / approachSpeed;
            lead = Vector3.Dot(frame.playerVelocity, alongAxis) * Mathf.Min(timeToImpact, config.impactLeadSeconds);
        }

        float targetAlong = Mathf.Clamp(along + lead, -halfLength, halfLength);
        bool appearing = !visible;
        SetVisible(true);

        if (appearing || config.followSharpness <= 0f)
            alongPosition = targetAlong;
        else
            alongPosition = Mathf.Lerp(alongPosition, targetAlong, 1f - Mathf.Exp(-config.followSharpness * deltaTime));

        Vector3 position = wallCenter + alongAxis * alongPosition + normal * (halfThickness + config.surfaceOffset);
        position.y = frame.zoneBottom + frame.zoneSize.y * 0.5f;

        Vector3 forward = -normal;
        float opacity = 1f;
        if (frame.hasCamera && Vector3.Dot(frame.cameraPosition - position, normal) < 0f)
        {
            forward = normal;
            opacity = config.opacityBetweenCameraAndPlayer;
        }

        transform.SetPositionAndRotation(position, Quaternion.LookRotation(forward, Vector3.up));
        ApplySize(frame.zoneSize);
        ApplyIntensity(opacity, config.stripesAppearAt);

        if (marquees == null) return;

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

        return normalAxis.sqrMagnitude > 0.5f && alongAxis.sqrMagnitude > 0.5f;
    }

    private void SetVisible(bool value)
    {
        if (visible == value) return;
        visible = value;
        gameObject.SetActive(value);
    }

    private void ApplySize(Vector2 size)
    {
        if ((size - appliedSize).sqrMagnitude < 0.0001f) return;
        appliedSize = size;

        if (hexPanel != null)
            hexPanel.transform.localScale = new Vector3(size.x, size.y, 1f);

        if (stripesCanvas != null)
        {
            Vector3 canvasScale = stripesCanvas.localScale;
            stripesCanvas.sizeDelta = new Vector2(
                size.x / Mathf.Max(0.0001f, canvasScale.x),
                size.y / Mathf.Max(0.0001f, canvasScale.y));
        }
    }

    private void ApplyIntensity(float opacity, float stripesAppearAt)
    {
        if (Mathf.Abs(intensity - appliedIntensity) < 0.002f && Mathf.Abs(opacity - appliedOpacity) < 0.002f) return;
        appliedIntensity = intensity;
        appliedOpacity = opacity;

        if (hexPanel != null)
        {
            if (properties == null)
                properties = new MaterialPropertyBlock();

            hexPanel.GetPropertyBlock(properties);
            properties.SetFloat(IntensityId, intensity);
            properties.SetFloat(OpacityId, opacity);
            hexPanel.SetPropertyBlock(properties);
        }

        float stripesAlpha = Mathf.Clamp01((intensity - stripesAppearAt) / Mathf.Max(0.01f, 1f - stripesAppearAt)) * opacity;

        if (stripesGroup != null)
            stripesGroup.alpha = stripesAlpha;

        if (stripesCanvasComponent != null)
            stripesCanvasComponent.enabled = stripesAlpha > 0.001f;
    }
}
