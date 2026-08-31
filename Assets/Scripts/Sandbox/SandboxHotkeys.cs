using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class SandboxHotkeys : MonoBehaviour, IUpdateable
{
    private SandboxConfig.Hotkeys keys;

    public bool IsActive => isActiveAndEnabled && keys != null;

    private void Awake()
    {
        keys = SandboxBootstrapper.Instance != null && SandboxBootstrapper.Instance.Config != null
            ? SandboxBootstrapper.Instance.Config.Keys
            : null;

        if (keys == null)
            enabled = false;
    }

    private void OnEnable()
    {
        UpdateManager.Instance?.Register(this);
    }

    private void OnDisable()
    {
        UpdateManager.Instance?.Unregister(this);
    }

    public void OnUpdate(float deltaTime)
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (WasPressed(keyboard, keys.statusReport))
            SandboxStatusLogger.ReportNow();

        if (WasPressed(keyboard, keys.grantLevel))
            SandboxCommands.GrantLevels(1);

        if (WasPressed(keyboard, keys.grantRandomUpgrade))
            SandboxCommands.GrantRandomUpgrade(false);

        if (WasPressed(keyboard, keys.grantRandomPremiumUpgrade))
            SandboxCommands.GrantRandomUpgrade(true);

        if (WasPressed(keyboard, keys.spawnEnemyBurst))
            SandboxCommands.SpawnBurst(keys.burstAmount, keys.burstRadius);

        if (WasPressed(keyboard, keys.killAllEnemies))
            SandboxCommands.KillAllEnemies();

        if (WasPressed(keyboard, keys.addCurrency))
            SandboxCommands.AddCurrency(keys.currencyPerPress, keys.currencyPerPress);

        if (WasPressed(keyboard, keys.toggleInvulnerable))
            SandboxCommands.ToggleInvulnerable();

        if (WasPressed(keyboard, keys.toggleSpawning))
            SandboxCommands.ToggleSpawning();

        if (WasPressed(keyboard, keys.forceFinalRush))
            SandboxCommands.ForceFinalRush();

        if (WasPressed(keyboard, keys.cycleTimeScale))
            SandboxCommands.CycleTimeScale(keys.timeScaleSteps);

        if (WasPressed(keyboard, keys.spawnChest))
            SandboxCommands.SpawnChestNow();

        if (WasPressed(keyboard, keys.reloadSandbox))
            SandboxCommands.ReloadSandbox();
    }

    private static bool WasPressed(Keyboard keyboard, Key key)
    {
        if (key == Key.None) return false;

        KeyControl control = keyboard[key];
        return control != null && control.wasPressedThisFrame;
    }

    public string BuildHelpText()
    {
        if (keys == null) return "sin teclas";

        return $"{keys.statusReport}=informe  {keys.grantLevel}=+1 nivel  {keys.grantRandomUpgrade}=mejora  {keys.grantRandomPremiumUpgrade}=mejora premium  " +
               $"{keys.spawnEnemyBurst}=ráfaga x{keys.burstAmount}  {keys.killAllEnemies}=matar todo  {keys.addCurrency}=+{keys.currencyPerPress} monedas/diamantes  " +
               $"{keys.toggleInvulnerable}=invulnerable  {keys.toggleSpawning}=pausar spawns  {keys.forceFinalRush}=oleada final  " +
               $"{keys.cycleTimeScale}=time scale  {keys.spawnChest}=cofre  {keys.reloadSandbox}=reiniciar";
    }
}
