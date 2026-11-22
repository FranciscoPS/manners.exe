using UnityEngine;

public static class HelicopterCameraMath
{
    private const float SecondWaveRatio = 2.618f;
    private const float FirstWaveWeight = 0.8f;
    private const float SecondWaveWeight = 0.2f;

    public static float DistanceForFramedHeight(float framedHeight, float fieldOfView)
    {
        float halfAngle = Mathf.Clamp(fieldOfView, 1f, 179f) * 0.5f * Mathf.Deg2Rad;
        return Mathf.Max(0f, framedHeight) * 0.5f / Mathf.Tan(halfAngle);
    }

    public static float FramedHeight(float distance, float fieldOfView)
    {
        float halfAngle = Mathf.Clamp(fieldOfView, 1f, 179f) * 0.5f * Mathf.Deg2Rad;
        return 2f * Mathf.Max(0f, distance) * Mathf.Tan(halfAngle);
    }

    public static float Orbit(float time, float amplitude, float period)
    {
        if (period <= 0.0001f || Mathf.Approximately(amplitude, 0f)) return 0f;

        float phase = time / period * Mathf.PI * 2f;
        float wave = FirstWaveWeight * Mathf.Sin(phase) + SecondWaveWeight * Mathf.Sin(phase * SecondWaveRatio);
        return amplitude * wave;
    }

    public static float PunchEnvelope(float elapsed, float attack, float hold, float release)
    {
        if (elapsed < 0f) return 0f;

        if (elapsed < attack) return Smooth(elapsed / attack);
        elapsed -= Mathf.Max(0f, attack);

        if (elapsed < hold) return 1f;
        elapsed -= Mathf.Max(0f, hold);

        if (release <= 0f || elapsed >= release) return 0f;
        return 1f - Smooth(elapsed / release);
    }

    public static float CrowdFactor(int enemies, int start, int full)
    {
        if (full <= start) return enemies >= start ? 1f : 0f;
        return Mathf.Clamp01((enemies - start) / (float)(full - start));
    }

    public static float ZoomedFieldOfView(float baseFieldOfView, float crowdWiden, float crowd, float punch)
    {
        float widened = baseFieldOfView * (1f + Mathf.Max(0f, crowdWiden) * Mathf.Clamp01(crowd));
        return Mathf.Clamp(widened * (1f - Mathf.Clamp01(punch)), 5f, 120f);
    }

    public static float AltitudeScale(float time, float amplitude, float period)
    {
        return 1f + Orbit(time, Mathf.Clamp(amplitude, 0f, 0.5f), period);
    }

    public static float IdleTime(float idleTime, float speed, float idleSpeed, float deltaTime)
    {
        return speed <= idleSpeed ? idleTime + Mathf.Max(0f, deltaTime) : 0f;
    }

    public static float RecenterZone(float current, float idleTime, float recenterDelay, float blend, float deltaTime)
    {
        float target = recenterDelay > 0f && idleTime >= recenterDelay ? 0f : 1f;
        return Mathf.MoveTowards(Mathf.Clamp01(current), target, Mathf.Max(0f, deltaTime) / Mathf.Max(0.01f, blend));
    }

    public static Vector2 Drift(float time, float frequency, float amplitude, float seed)
    {
        float t = time * frequency;
        float x = Mathf.Clamp01(Mathf.PerlinNoise(seed, t)) * 2f - 1f;
        float y = Mathf.Clamp01(Mathf.PerlinNoise(t, seed + 17.3f)) * 2f - 1f;
        return new Vector2(x, y) * amplitude;
    }

    private static float Smooth(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }
}
