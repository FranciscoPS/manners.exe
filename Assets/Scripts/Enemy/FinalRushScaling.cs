using UnityEngine;

public static class FinalRushScaling
{
    public static bool Active { get; private set; }
    public static float Toughness { get; private set; } = 1f;
    public static float ControlResistance { get; private set; }
    public static float MinSpeed { get; private set; }
    public static float MinContactDamage { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Clear()
    {
        Active = false;
        Toughness = 1f;
        ControlResistance = 0f;
        MinSpeed = 0f;
        MinContactDamage = 0f;
    }

    public static void Set(float toughness, float controlResistance, float minSpeed, float minContactDamage)
    {
        Active = true;
        Toughness = float.IsNaN(toughness) ? 1f : Mathf.Clamp(toughness, 1f, FinalRushMath.MaxToughness);
        ControlResistance = float.IsNaN(controlResistance) ? 0f : Mathf.Clamp01(controlResistance);
        MinSpeed = Mathf.Max(0f, minSpeed);
        MinContactDamage = Mathf.Max(0f, minContactDamage);
    }
}
