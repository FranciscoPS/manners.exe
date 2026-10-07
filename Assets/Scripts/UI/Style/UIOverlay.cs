using System;
using UnityEngine;

public class UIOverlay : MonoBehaviour
{
    public static event Action Changed;

    public static int ActiveCount { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        ActiveCount = 0;
        Changed = null;
    }

    private void OnEnable()
    {
        ActiveCount++;
        Changed?.Invoke();
    }

    private void OnDisable()
    {
        ActiveCount = Mathf.Max(0, ActiveCount - 1);
        Changed?.Invoke();
    }
}
