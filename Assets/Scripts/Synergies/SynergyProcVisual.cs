using System.Collections.Generic;
using UnityEngine;

/// <summary>Short, bounded rings for explosions and repeated cryo damage ticks.</summary>
public sealed class SynergyProcVisual : MonoBehaviour, IUpdateable
{
    private const int MaxVisiblePulses = 24;
    private const int Segments = 32;
    private const float Duration = 0.3f;
    private static readonly List<SynergyProcVisual> active = new List<SynergyProcVisual>();
    private static Material sharedMaterial;
    private LineRenderer line;
    private MaterialPropertyBlock properties;
    private Color color;
    private float elapsed;
    private float radius;

    public bool IsActive => isActiveAndEnabled;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        active.Clear();
        if (sharedMaterial != null) Destroy(sharedMaterial);
        sharedMaterial = null;
    }

    public static GameObject Show(Vector3 center, float pulseRadius, Color pulseColor)
    {
        if (!Application.isPlaying || pulseRadius <= 0f || active.Count >= MaxVisiblePulses) return null;
        if (sharedMaterial == null)
        {
            Shader shader = SynergyVisualUtility.FindUnlitShader();
            if (shader == null) return null;
            sharedMaterial = new Material(shader) { name = "Synergy proc rings" };
            SynergyVisualUtility.MakeTransparent(sharedMaterial);
        }

        var visual = new GameObject("Synergy proc pulse");
        visual.transform.position = center + Vector3.up * 0.08f;
        var pulse = visual.AddComponent<SynergyProcVisual>();
        pulse.properties = new MaterialPropertyBlock();
        pulse.radius = pulseRadius;
        pulse.color = pulseColor;
        pulse.line = visual.AddComponent<LineRenderer>();
        pulse.line.sharedMaterial = sharedMaterial;
        pulse.line.loop = true;
        pulse.line.useWorldSpace = false;
        pulse.line.positionCount = Segments;
        pulse.line.widthMultiplier = 0.14f;
        pulse.line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        pulse.line.receiveShadows = false;
        pulse.UpdateVisual(0f);
        active.Add(pulse);
        return visual;
    }

    private void OnEnable() => UpdateManager.Instance?.Register(this);
    private void OnDisable() => UpdateManager.Instance?.Unregister(this);
    private void OnDestroy() => active.Remove(this);

    public void OnUpdate(float deltaTime)
    {
        elapsed += deltaTime;
        UpdateVisual(Mathf.Clamp01(elapsed / Duration));
        if (elapsed >= Duration) Destroy(gameObject);
    }

    private void UpdateVisual(float progress)
    {
        float currentRadius = radius * Mathf.Lerp(0.15f, 1f, progress);
        for (int i = 0; i < Segments; i++)
        {
            float angle = i * 2f * Mathf.PI / Segments;
            line.SetPosition(i, new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * currentRadius);
        }
        Color tint = color;
        tint.a *= 1f - progress;
        properties.SetColor("_BaseColor", tint);
        properties.SetColor("_Color", tint);
        line.SetPropertyBlock(properties);
    }

    public static void ClearAll()
    {
        for (int i = active.Count - 1; i >= 0; i--)
            if (active[i] != null) Destroy(active[i].gameObject);
        active.Clear();
        if (sharedMaterial != null) Destroy(sharedMaterial);
        sharedMaterial = null;
    }
}
