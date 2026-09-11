using UnityEngine;

public sealed class GameGraphicsSettingsDriver : MonoBehaviour
{
    private void Update() => GameGraphicsSettings.Tick();
    private void OnApplicationFocus(bool focused) => GameGraphicsSettings.SetFocus(focused);
    private void OnDestroy() => GameGraphicsSettings.Shutdown();
}
