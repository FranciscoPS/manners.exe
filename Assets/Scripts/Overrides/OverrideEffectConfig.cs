using UnityEngine;

public abstract class OverrideEffectConfig : ScriptableObject
{
    public abstract void ApplyTo(GameObject effectInstance);
}
