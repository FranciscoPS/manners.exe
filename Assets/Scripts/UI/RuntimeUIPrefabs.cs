using UnityEngine;

[CreateAssetMenu(menuName = "Game/UI/Runtime UI Prefabs", fileName = "RuntimeUIPrefabs_Production")]
public sealed class RuntimeUIPrefabs : ScriptableObject
{
    public ChestAnnouncement chestAnnouncement;
    public OvertimeAlert overtimeAlert;
    public ChestOpeningSequence chestOpeningSequence;
    public OverrideActivationHUD overrideActivationHUD;
    public HoverTooltipUI hoverTooltip;
    public static RuntimeUIPrefabs Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Instance = null;

    public static T Spawn<T>(System.Func<RuntimeUIPrefabs, T> select) where T : Component
    {
        if (Instance == null) Instance = Resources.Load<RuntimeUIPrefabs>("UI/RuntimeUIPrefabs_Production");
        T prefab = Instance != null ? select(Instance) : null;
        if (prefab == null) { Debug.LogError("Falta un prefab de UI en RuntimeUIPrefabs_Production."); return null; }
        T result = Instantiate(prefab);
        DontDestroyOnLoad(result.gameObject);
        return result;
    }
}
