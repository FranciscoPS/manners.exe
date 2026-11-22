using UnityEngine;

public static class FinalRushMath
{
    public const float MaxToughness = 1000000000f;

    public static float Tiers(float elapsed, float tierInterval)
    {
        return tierInterval > 0f ? Mathf.Max(0f, elapsed) / tierInterval : 0f;
    }

    public static float Toughness(float tiers, float multiplierPerTier)
    {
        float toughness = Mathf.Pow(Mathf.Max(1f, multiplierPerTier), Mathf.Max(0f, tiers));
        return float.IsNaN(toughness) || toughness > MaxToughness ? MaxToughness : toughness;
    }

    public static float ControlResistance(float tiers, float perTier, float maxResistance)
    {
        return Mathf.Clamp01(Mathf.Min(maxResistance, perTier * Mathf.Max(0f, tiers)));
    }

    public static float Speed(float tiers, float baseSpeed, float perTier, float maxSpeed)
    {
        return Mathf.Min(maxSpeed, baseSpeed + perTier * Mathf.Max(0f, tiers));
    }

    public static float ChaseSpeed(float tiers, float playerSpeed, float ratioStart, float ratioPerTier, float ratioMax)
    {
        return Mathf.Max(0f, playerSpeed) * Mathf.Min(ratioMax, ratioStart + ratioPerTier * Mathf.Max(0f, tiers));
    }

    public static float ContactDamage(float tiers, float baseDamage, float perTier)
    {
        return baseDamage + perTier * Mathf.Max(0f, tiers);
    }

    public static int Cap(float elapsed, int start, int end, float rampSeconds, int normalCap)
    {
        int ceiling = Mathf.Max(1, end);
        float progress = rampSeconds > 0f ? Mathf.Clamp01(elapsed / rampSeconds) : 1f;
        int ramped = Mathf.RoundToInt(Mathf.Lerp(Mathf.Min(start, ceiling), ceiling, progress));
        return Mathf.Min(ceiling, Mathf.Max(normalCap, ramped));
    }
}
