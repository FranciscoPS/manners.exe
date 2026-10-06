using System.Collections.Generic;
using UnityEngine;

public sealed class UIModalVisibility : MonoBehaviour
{
    [SerializeField] private GameObject[] obscuredObjects;
    private struct State { public int count; public bool wasActive; }
    private static readonly Dictionary<GameObject, State> states = new Dictionary<GameObject, State>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => states.Clear();
    private void OnEnable()
    {
        if (obscuredObjects == null) return;
        foreach (GameObject target in obscuredObjects)
        {
            if (target == null) continue;
            if (!states.TryGetValue(target, out State state)) state.wasActive = target.activeSelf;
            state.count++; states[target] = state; target.SetActive(false);
        }
    }
    private void OnDisable()
    {
        if (obscuredObjects == null) return;
        foreach (GameObject target in obscuredObjects)
        {
            if (target == null || !states.TryGetValue(target, out State state)) continue;
            state.count--;
            if (state.count > 0) states[target] = state;
            else { states.Remove(target); target.SetActive(state.wasActive); }
        }
    }
}
