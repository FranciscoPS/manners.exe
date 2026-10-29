using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Effects/UI Shard")]
[RequireComponent(typeof(CanvasRenderer))]
public class UIShard : MaskableGraphic, IUpdateable
{
    private const float GoldenAngle = 2.399963f;
    private const float PulseScale = 0.94f;
    private const float MaxMiter = 2.5f;

    [Tooltip("Vértices del recorte en proporción al rectángulo (0 a 1). Pueden salirse del rectángulo para formar picos.")]
    [SerializeField] private Vector2[] points = new Vector2[0];
    [Tooltip("0 = forma rellena. Mayor que 0 = solo la línea de contorno, con ese grosor.")]
    [SerializeField] private float stroke;
    [Tooltip("Cuánto se agita este recorte: 0 queda quieto (papel del diálogo), 1 usa todo el movimiento definido en el estilo.")]
    [Range(0f, 1f)] [SerializeField] private float motion = 1f;
    [Tooltip("Desfase del movimiento, para que los recortes de un mismo fondo no se muevan a la vez.")]
    [SerializeField] private float phase;
    [Tooltip("Retraso de la entrada al mostrarse su pantalla, en segundos.")]
    [SerializeField] private float enterDelay;

    private static readonly List<UIShard> Active = new List<UIShard>();
    private static readonly List<Vector2> Positions = new List<Vector2>();
    private static readonly List<Vector2> Miters = new List<Vector2>();

    private float popStart;
    private float popFrom;
    private int lastStep = -1;
    private bool popping;

    bool IUpdateable.IsActive => isActiveAndEnabled;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Active.Clear();
    }

    public static void Pulse(Transform root)
    {
        if (root == null) return;

        float now = Time.unscaledTime;
        for (int i = 0; i < Active.Count; i++)
        {
            UIShard shard = Active[i];
            if (shard.motion <= 0f || !shard.transform.IsChildOf(root)) continue;

            shard.popStart = now;
            shard.popFrom = PulseScale;
            shard.popping = true;
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        if (!Application.isPlaying) return;

        popStart = Time.unscaledTime + enterDelay;
        popFrom = 0f;
        popping = true;
        lastStep = -1;
        Active.Add(this);

        if (UpdateManager.Instance != null)
            UpdateManager.Instance.Register(this);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (!Application.isPlaying) return;

        Active.Remove(this);

        if (UpdateManager.Instance != null)
            UpdateManager.Instance.Unregister(this);
    }

    public void OnUpdate(float deltaTime)
    {
        UIStyle style = UIStyle.Instance;
        if (style == null) return;

        if (popping)
        {
            SetVerticesDirty();
            return;
        }

        if (motion <= 0f || style.shardWobble <= 0f) return;

        int step = style.shardFrameRate > 0f ? Mathf.FloorToInt(Time.unscaledTime * style.shardFrameRate) : Time.frameCount;
        if (step == lastStep) return;

        lastStep = step;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        int count = points != null ? points.Length : 0;
        if (count < 3) return;

        Rect rect = GetPixelAdjustedRect();
        Vector2 center = rect.center;
        float scale = PopScale();
        if (scale <= 0.001f) return;

        UIStyle style = Application.isPlaying ? UIStyle.Instance : null;
        float amplitude = style != null ? style.shardWobble * motion : 0f;
        float time = 0f;
        if (amplitude > 0f)
        {
            time = Time.unscaledTime;
            if (style.shardFrameRate > 0f) time = Mathf.Floor(time * style.shardFrameRate) / style.shardFrameRate;
            time *= style.shardSpeed * Mathf.PI * 2f;
        }

        Positions.Clear();
        Miters.Clear();
        float area = 0f;

        for (int i = 0; i < count; i++)
        {
            Vector2 position = new Vector2(rect.xMin + points[i].x * rect.width, rect.yMin + points[i].y * rect.height);
            if (amplitude > 0f)
            {
                float angle = time + phase + i * GoldenAngle;
                position.x += Mathf.Sin(angle) * amplitude;
                position.y += Mathf.Cos(angle * 0.77f + i) * amplitude;
            }
            Positions.Add(center + (position - center) * scale);
        }

        for (int i = 0; i < count; i++)
        {
            Vector2 a = Positions[i];
            Vector2 b = Positions[(i + 1) % count];
            area += a.x * b.y - b.x * a.y;
        }

        float sign = area >= 0f ? 1f : -1f;
        for (int i = 0; i < count; i++)
        {
            Vector2 before = (Positions[i] - Positions[(i + count - 1) % count]).normalized;
            Vector2 after = (Positions[(i + 1) % count] - Positions[i]).normalized;
            Vector2 normalBefore = new Vector2(before.y, -before.x) * sign;
            Vector2 normalAfter = new Vector2(after.y, -after.x) * sign;
            Vector2 miter = normalBefore + normalAfter;
            float length = miter.magnitude;
            miter = length > 0.0001f ? miter / length : normalBefore;
            Miters.Add(miter * Mathf.Min(1f / Mathf.Max(Vector2.Dot(miter, normalBefore), 0.0001f), MaxMiter));
        }

        float fringe = canvas != null && canvas.scaleFactor > 0f ? 1f / canvas.scaleFactor : 1f;
        Color32 solid = color;
        Color32 clear = new Color32(solid.r, solid.g, solid.b, 0);

        if (stroke > 0f)
            BuildStroke(vh, count, stroke * 0.5f, fringe, solid, clear);
        else
            BuildFill(vh, count, center, fringe, solid, clear);
    }

    private float PopScale()
    {
        if (!popping) return 1f;

        UIStyle style = UIStyle.Instance;
        float duration = style != null ? style.shardEnterDuration : 0.28f;
        float elapsed = Time.unscaledTime - popStart;
        if (elapsed < 0f) return popFrom;

        if (duration <= 0f || elapsed >= duration)
        {
            popping = false;
            return 1f;
        }

        float t = elapsed / duration - 1f;
        float eased = t * t * (2.70158f * t + 1.70158f) + 1f;
        return Mathf.LerpUnclamped(popFrom, 1f, eased);
    }

    private static void BuildFill(VertexHelper vh, int count, Vector2 center, float fringe, Color32 solid, Color32 clear)
    {
        vh.AddVert(center, solid, Vector4.zero);
        for (int i = 0; i < count; i++)
        {
            vh.AddVert(Positions[i], solid, Vector4.zero);
            vh.AddVert(Positions[i] + Miters[i] * fringe, clear, Vector4.zero);
        }

        for (int i = 0; i < count; i++)
        {
            int inner = 1 + i * 2;
            int nextInner = 1 + ((i + 1) % count) * 2;
            vh.AddTriangle(0, inner, nextInner);
            vh.AddTriangle(inner, inner + 1, nextInner + 1);
            vh.AddTriangle(inner, nextInner + 1, nextInner);
        }
    }

    private static void BuildStroke(VertexHelper vh, int count, float half, float fringe, Color32 solid, Color32 clear)
    {
        for (int i = 0; i < count; i++)
        {
            Vector2 position = Positions[i];
            Vector2 miter = Miters[i];
            vh.AddVert(position - miter * (half + fringe), clear, Vector4.zero);
            vh.AddVert(position - miter * half, solid, Vector4.zero);
            vh.AddVert(position + miter * half, solid, Vector4.zero);
            vh.AddVert(position + miter * (half + fringe), clear, Vector4.zero);
        }

        for (int i = 0; i < count; i++)
        {
            int current = i * 4;
            int next = ((i + 1) % count) * 4;
            for (int band = 0; band < 3; band++)
            {
                vh.AddTriangle(current + band, current + band + 1, next + band + 1);
                vh.AddTriangle(current + band, next + band + 1, next + band);
            }
        }
    }
}
