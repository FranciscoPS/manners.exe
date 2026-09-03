using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class SandboxHotkeys : MonoBehaviour, IUpdateable
{
    [Header("Panel de debug")]
    [SerializeField] private SandboxDebugMonitor debugMonitor;
    [SerializeField] private Key togglePanel = Key.F1;

    [Header("Sinergias")]
    [Tooltip("Muestra/oculta el panel real de sinergias (Assets/Prefabs/UI/SynergyHintsPanel.prefab) para verificar visualmente el relleno progresivo con el estado actual de PlayerStatsManager.")]
    [SerializeField] private GameObject synergyHintsPanelPrefab;
    [SerializeField] private Key toggleSynergyHints = Key.H;
    [Tooltip("Borra el progreso guardado localmente (PlayerPrefs) de mejoras y sinergias descubiertas, como si fuera una instalación limpia.")]
    [SerializeField] private Key clearSynergyDiscoveries = Key.Delete;

    [Header("Progresión")]
    [SerializeField] private Key grantLevel = Key.F2;
    [SerializeField] private Key grantRandomUpgrade = Key.F3;
    [SerializeField] private Key grantRandomPremiumUpgrade = Key.F4;

    [Header("Enemigos")]
    [SerializeField] private Key spawnEnemyBurst = Key.F5;
    [SerializeField] private Key killAllEnemies = Key.F6;
    [Tooltip("Config de enemigo que se usa para la ráfaga manual (F5).")]
    [SerializeField] private EnemyConfiguration burstEnemy;
    [SerializeField] private int burstAmount = 10;
    [SerializeField] private float burstRadius = 12f;

    [Header("Economía")]
    [SerializeField] private Key addCurrency = Key.F7;
    [SerializeField] private int currencyPerPress = 500;

    [Header("Estado del jugador y del spawner")]
    [SerializeField] private Key toggleInvulnerable = Key.F8;
    [Tooltip("Mata al jugador al instante (ignora invulnerabilidad) para abrir la pantalla de Game Over sin esperar.")]
    [SerializeField] private Key killPlayer = Key.K;
    [SerializeField] private Key toggleSpawning = Key.F9;
    [SerializeField] private Key forceFinalRush = Key.F10;

    [Header("Utilidades")]
    [SerializeField] private Key cycleTimeScale = Key.F11;
    [SerializeField] private float[] timeScaleSteps = { 1f, 0.25f, 2f, 4f };
    [SerializeField] private Key spawnChest = Key.F12;
    [SerializeField] private Key reloadSandbox = Key.Backspace;

    public bool IsActive => isActiveAndEnabled;

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

        if (WasPressed(keyboard, togglePanel) && debugMonitor != null)
            debugMonitor.TogglePanel();

        if (WasPressed(keyboard, toggleSynergyHints))
            SandboxCommands.ToggleSynergyHints(synergyHintsPanelPrefab);

        if (WasPressed(keyboard, clearSynergyDiscoveries))
            SandboxCommands.ClearSynergyDiscoveries();

        if (WasPressed(keyboard, grantLevel))
            SandboxCommands.GrantLevels(1);

        if (WasPressed(keyboard, grantRandomUpgrade))
            SandboxCommands.GrantRandomUpgrade(false);

        if (WasPressed(keyboard, grantRandomPremiumUpgrade))
            SandboxCommands.GrantRandomUpgrade(true);

        if (WasPressed(keyboard, spawnEnemyBurst))
            SandboxCommands.SpawnBurst(burstAmount, burstRadius, burstEnemy);

        if (WasPressed(keyboard, killAllEnemies))
            SandboxCommands.KillAllEnemies();

        if (WasPressed(keyboard, addCurrency))
            SandboxCommands.AddCurrency(currencyPerPress, currencyPerPress);

        if (WasPressed(keyboard, toggleInvulnerable))
            SandboxCommands.ToggleInvulnerable();

        if (WasPressed(keyboard, killPlayer))
            SandboxCommands.KillPlayer();

        if (WasPressed(keyboard, toggleSpawning))
            SandboxCommands.ToggleSpawning();

        if (WasPressed(keyboard, forceFinalRush))
            SandboxCommands.ForceFinalRush();

        if (WasPressed(keyboard, cycleTimeScale))
            SandboxCommands.CycleTimeScale(timeScaleSteps);

        if (WasPressed(keyboard, spawnChest))
            SandboxCommands.SpawnChestNow();

        if (WasPressed(keyboard, reloadSandbox))
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
        return $"{togglePanel}=panel  {toggleSynergyHints}=panel de sinergias  {clearSynergyDiscoveries}=borrar progreso sinergias  {grantLevel}=+1 nivel  {grantRandomUpgrade}=mejora  {grantRandomPremiumUpgrade}=mejora premium  " +
               $"{spawnEnemyBurst}=ráfaga x{burstAmount}  {killAllEnemies}=matar todo  {addCurrency}=+{currencyPerPress} monedas/diamantes  " +
               $"{toggleInvulnerable}=invulnerable  {killPlayer}=morir  {toggleSpawning}=pausar spawns  {forceFinalRush}=oleada final  " +
               $"{cycleTimeScale}=time scale  {spawnChest}=cofre  {reloadSandbox}=reiniciar";
    }
}
