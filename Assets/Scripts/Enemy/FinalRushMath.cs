using UnityEngine;

public static class FinalRushMath
{
    public const float MaxHealth = 1000000000f;

    public static float DamageTakenScale(float elapsed, float nearInvincibleSeconds, float nearInvincibleDamage, float indestructibleSeconds)
    {
        if (elapsed <= 0f) return 1f;
        if (indestructibleSeconds > 0f && elapsed >= indestructibleSeconds) return 0f;

        float near = Mathf.Clamp01(nearInvincibleDamage);
        if (nearInvincibleSeconds <= 0f) return near;
        if (elapsed <= nearInvincibleSeconds)
        {
            float progress = elapsed / nearInvincibleSeconds;
            return near > 0f ? Mathf.Pow(near, progress) : 1f - progress;
        }

        if (indestructibleSeconds <= nearInvincibleSeconds) return near;
        return Mathf.Lerp(near, 0f, (elapsed - nearInvincibleSeconds) / (indestructibleSeconds - nearInvincibleSeconds));
    }

    public static int Cap(float elapsed, int start, int end, float rampSeconds, int normalCap)
    {
        int ceiling = Mathf.Max(1, end);
        float progress = rampSeconds > 0f ? Mathf.Clamp01(elapsed / rampSeconds) : 1f;
        int ramped = Mathf.RoundToInt(Mathf.Lerp(Mathf.Min(start, ceiling), ceiling, progress));
        return Mathf.Min(ceiling, Mathf.Max(normalCap, ramped));
    }

    public static float Health(float baseHealth, float tierMultiplier, int tier)
    {
        float health = baseHealth * Mathf.Pow(Mathf.Max(0f, tierMultiplier), Mathf.Max(0, tier));
        return float.IsNaN(health) || health > MaxHealth ? MaxHealth : health;
    }
}
