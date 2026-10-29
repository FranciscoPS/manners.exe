using System;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class OptionsMenu : MonoBehaviour, IUpdateable
{
    [Header("Pantallas del menú de opciones")]
    [Tooltip("Pantalla principal de opciones, con la columna de botones.")]
    [SerializeField] private GameObject hub;
    [SerializeField] private GameObject audioScreen;
    [SerializeField] private GameObject controlsScreen;
    [SerializeField] private GameObject helpScreen;
    [Tooltip("Secciones de la ayuda en el orden de sus botones: movimiento, experiencia, enemigos y mejoras.")]
    [SerializeField] private GameObject[] helpSections;
    [Tooltip("Colección de sobrecargas. También se abre directamente desde el menú principal y desde la pausa.")]
    [SerializeField] private GameObject overridesScreen;
    [SerializeField] private GraphicsSettingsMenu graphics;
    [Tooltip("Selector de idioma de la pantalla principal; el botón Idioma lo muestra y lo oculta.")]
    [SerializeField] private GameObject languagePanel;

    private static int lastBackFrame = -1;
    private GameObject currentScreen;
    private bool overridesOpenedDirectly;

    public event Action Closed;

    public bool IsOpen => currentScreen != null;
    public bool IsActive => isActiveAndEnabled;
    public static bool ConsumedBackThisFrame => lastBackFrame == Time.frameCount;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        lastBackFrame = -1;
    }

    private void Awake()
    {
        Hide(hub);
        Hide(audioScreen);
        Hide(controlsScreen);
        Hide(helpScreen);
        Hide(overridesScreen);
        if (graphics != null) Hide(graphics.gameObject);
        Hide(languagePanel);
    }

    private void OnEnable()
    {
        if (graphics != null) graphics.Closed += OnGraphicsClosed;
        UpdateManager.Instance?.Register(this);
    }

    private void OnDisable()
    {
        if (graphics != null) graphics.Closed -= OnGraphicsClosed;
        UpdateManager.Instance?.Unregister(this);
    }

    public void Open()
    {
        overridesOpenedDirectly = false;
        Show(hub);
    }

    public void OpenOverrides()
    {
        overridesOpenedDirectly = true;
        Show(overridesScreen);
    }

    public void ShowHub() => Show(hub);
    public void ShowAudio() => Show(audioScreen);
    public void ShowControls() => Show(controlsScreen);
    public void ShowHelp() => ShowHelpSection(0);

    public void ShowGraphics()
    {
        if (graphics != null) Show(graphics.gameObject);
    }

    public void ShowOverrides()
    {
        overridesOpenedDirectly = false;
        Show(overridesScreen);
    }

    public void ShowHelpSection(int index)
    {
        Show(helpScreen);
        if (helpSections == null) return;

        for (int i = 0; i < helpSections.Length; i++)
        {
            if (helpSections[i] != null) helpSections[i].SetActive(i == index);
        }
    }

    public void ToggleLanguage()
    {
        if (languagePanel != null) languagePanel.SetActive(!languagePanel.activeSelf);
    }

    public void Back()
    {
        if (currentScreen == null) return;

        bool leaving = currentScreen == hub || (currentScreen == overridesScreen && overridesOpenedDirectly);
        if (leaving) Close();
        else ShowHub();
    }

    public void Close()
    {
        if (currentScreen == null) return;

        Hide(currentScreen);
        currentScreen = null;
        overridesOpenedDirectly = false;
        Hide(languagePanel);
        Closed?.Invoke();
    }

    private void Show(GameObject screen)
    {
        if (screen == null) return;

        if (currentScreen != null && currentScreen != screen) Hide(currentScreen);
        if (screen != hub) Hide(languagePanel);
        currentScreen = screen;
        screen.SetActive(true);
    }

    private static void Hide(GameObject screen)
    {
        if (screen != null && screen.activeSelf) screen.SetActive(false);
    }

    private void OnGraphicsClosed()
    {
        if (graphics == null || currentScreen != graphics.gameObject) return;

        currentScreen = null;
        ShowHub();
    }

    public void OnUpdate(float deltaTime)
    {
        if (currentScreen == null) return;
        if (graphics != null && currentScreen == graphics.gameObject) return;
        if (GraphicsSettingsMenu.ConsumedBackThisFrame) return;

        bool back = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            || (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);
        if (!back) return;

        lastBackFrame = Time.frameCount;
        Back();
    }
}
