using System.Collections.Generic;
using UnityEngine;

public sealed class UIModalVisibility : MonoBehaviour
{
    [SerializeField] private GameObject[] obscuredObjects;
    [Tooltip("Oculta el panel de sobrecargas mientras este modal está visible.")]
    [SerializeField] private bool obscureOverrideHudPanel;

    private struct State { public int count; public bool wasActive; }
    private static readonly Dictionary<GameObject, State> states = new Dictionary<GameObject, State>();
    private GameObject overrideHudPanel;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => states.Clear();

    private void OnEnable()
    {
        if (obscuredObjects != null)
        {
            foreach (GameObject target in obscuredObjects)
                Obscure(target);
        }

        if (!obscureOverrideHudPanel) return;

        OverrideHudPanel panel = OverrideHudPanel.Instance;
        overrideHudPanel = panel != null ? panel.gameObject : null;
        Obscure(overrideHudPanel);
    }

    private void OnDisable()
    {
        if (obscuredObjects != null)
        {
            foreach (GameObject target in obscuredObjects)
                Restore(target);
        }

        Restore(overrideHudPanel);
        overrideHudPanel = null;
    }

    private static void Obscure(GameObject target)
    {
        if (target == null) return;

        if (!states.TryGetValue(target, out State state))
            state.wasActive = target.activeSelf;

        state.count++;
        states[target] = state;
        target.SetActive(false);
    }

    private static void Restore(GameObject target)
    {
        if (target == null || !states.TryGetValue(target, out State state)) return;

        state.count--;
        if (state.count > 0)
            states[target] = state;
        else
        {
            states.Remove(target);
            target.SetActive(state.wasActive);
        }
    }
}
