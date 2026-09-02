using UnityEngine;

public abstract class SynergyEffectConfig : ScriptableObject
{
    public abstract void ApplyTo(GameObject effectInstance);
}
