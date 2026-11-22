using System;
using UnityEngine;

public static class BroadcastMode
{
    public static event Action Changed;

    public static bool Enabled { get; private set; } = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Enabled = true;
        Changed = null;
    }

    public static void Set(bool value)
    {
        if (Enabled == value) return;

        Enabled = value;
        Changed?.Invoke();
    }

    public static void Toggle() => Set(!Enabled);
}
