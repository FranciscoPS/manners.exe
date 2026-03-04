using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;
using UnityEngine.UI;
using System.Collections.Generic;

public class MainMenuUIManager : MonoBehaviour
{
    public static MainMenuUIManager Instance;

    [Header("Main Screens")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject upgradesPanel;
    [SerializeField] private GameObject tiendaPanel;
    [SerializeField] private GameObject personalizacionPanel;
    [SerializeField] private GameObject creditosPanel;

    [Header("Options Subscreens")]
    [SerializeField] private GameObject helpPanel;
    [SerializeField] private GameObject audioPanel;
    [SerializeField] private GameObject controlesPanel;

    [Header("Upgrades Subscreens")]
    [SerializeField] private GameObject habilidadesPanel;
    //[SerializeField] private GameObject arbolDeHabilidesPanel;
    //[SerializeField] private GameObject detallesPanel;

    [Header("Tienda Subscreens")]
    [SerializeField] private GameObject atuendosPanel;
    //[SerializeField] private GameObject efectosPanel;
    //[SerializeField] private GameObject monedasPanel;

    [Header("Personalizacion Subscreens")]
    [SerializeField] private GameObject skinsPanel;
    //[SerializeField] private GameObject attackEffectsPanel;

    [Header("Help Subscreens")]
    [SerializeField] private GameObject movimientoPanel;
    [SerializeField] private GameObject experienciaPanel;
    [SerializeField] private GameObject enemigosPanel;
    [SerializeField] private GameObject mejorasPanel;

    [Header("Fade")]
    [SerializeField] private float fadeDuration = 0.8f;
    [SerializeField] private int overlaySortingOrder = 1000;

    private Dictionary<MenuScreen, GameObject> screenDictionary;

    private GameObject currentScreen;
    private GameObject currentOptionsSubPanel;
    private GameObject currentHelpSubPanel;
    private GameObject currentUpgradesSubPanel;
    private GameObject currentTiendaSubPanel;
    private GameObject currentPersonalizacionSubPanel;

    private GameObject fadeOverlay;
    private CanvasGroup fadeCanvasGroup;

    private void Awake()
    {
        Instance = this;

        screenDictionary = new Dictionary<MenuScreen, GameObject>
        {
            // Main
            { MenuScreen.Main, mainPanel },
            { MenuScreen.Options, optionsPanel },
            { MenuScreen.Upgrades, upgradesPanel },
            { MenuScreen.Tienda, tiendaPanel },
            { MenuScreen.Personalizacion, personalizacionPanel },
            { MenuScreen.Creditos, creditosPanel },

            // Options Subscreens
            { MenuScreen.Help, helpPanel },
            { MenuScreen.Audio, audioPanel },
            { MenuScreen.Controles, controlesPanel },

            // Upgrades
            { MenuScreen.UpgradesHabilidades, habilidadesPanel },
            //{ MenuScreen.UpgradesArbolDeHabilidades, arbolDeHabilidesPanel },
            //{ MenuScreen.UpgradesDetalles, detallesPanel },

            // Tienda
            { MenuScreen.TiendaAtuendos, atuendosPanel },
            //{ MenuScreen.TiendaEfectos, efectosPanel },
            //{ MenuScreen.TiendaMonedas, monedasPanel },

            // Personalizacion
            { MenuScreen.PersonalizacionSkins, skinsPanel },
            //{ MenuScreen.PersonalizacionAttackEffects, attackEffectsPanel },

            // Help Subscreens
            { MenuScreen.HelpMovimiento, movimientoPanel },
            { MenuScreen.HelpExperiencia, experienciaPanel },
            { MenuScreen.HelpEnemigos, enemigosPanel },
            { MenuScreen.HelpMejoras, mejorasPanel }
        };

        foreach (var screen in screenDictionary.Values)
        {
            if (screen != null)
                screen.SetActive(false);
        }

        currentHelpSubPanel = null;
        currentOptionsSubPanel = null;
        currentUpgradesSubPanel = null;
        currentTiendaSubPanel = null;
        currentPersonalizacionSubPanel = null;

        ShowScreen(MenuScreen.Main);
    }

    // Helpers para identificar grupos de subscreens
    private bool IsOptionsSubscreen(MenuScreen screen)
    {
        return screen == MenuScreen.Help
            || screen == MenuScreen.Audio
            || screen == MenuScreen.Controles;
    }

    private bool IsHelpSubscreen(MenuScreen screen)
    {
        return screen == MenuScreen.HelpMovimiento
            || screen == MenuScreen.HelpExperiencia
            || screen == MenuScreen.HelpEnemigos
            || screen == MenuScreen.HelpMejoras;
    }

    private bool IsUpgradesSubscreen(MenuScreen screen)
    {
        return screen == MenuScreen.UpgradesHabilidades
            || screen == MenuScreen.UpgradesArbolDeHabilidades
            || screen == MenuScreen.UpgradesDetalles;
    }

    private bool IsTiendaSubscreen(MenuScreen screen)
    {
        return screen == MenuScreen.TiendaAtuendos
            || screen == MenuScreen.TiendaEfectos
            || screen == MenuScreen.TiendaMonedas;
    }

    private bool IsPersonalizacionSubscreen(MenuScreen screen)
    {
        return screen == MenuScreen.PersonalizacionSkins
            || screen == MenuScreen.PersonalizacionAttackEffects;
    }

    public void ShowScreen(MenuScreen screen)
    {
        // --- HELP subscreens (open inside helpPanel, keep helpPanel active) ---
        if (IsHelpSubscreen(screen))
        {
            if (!screenDictionary.TryGetValue(screen, out GameObject helpSub) || helpSub == null)
                return;

            // Ensure options container hidden
            if (optionsPanel != null && optionsPanel.activeSelf)
                optionsPanel.SetActive(false);

            // Deactivate other top-level if it's not help container
            if (currentScreen != null && currentScreen != helpPanel)
                currentScreen.SetActive(false);

            // Activate help container
            if (helpPanel != null && !helpPanel.activeSelf)
                helpPanel.SetActive(true);

            currentScreen = helpPanel;
            currentOptionsSubPanel = helpPanel;

            // Deactivate previous help subpanel
            if (currentHelpSubPanel != null && currentHelpSubPanel != helpSub)
                currentHelpSubPanel.SetActive(false);

            helpSub.SetActive(true);
            currentHelpSubPanel = helpSub;
            return;
        }

        // --- OPTIONS subscreens (Audio/Controles/Help as panel children or separate panels) ---
        if (IsOptionsSubscreen(screen))
        {
            if (!screenDictionary.TryGetValue(screen, out GameObject subPanel) || subPanel == null)
                return;

            // If options root visible, hide it (we'll show the selected subpanel)
            if (optionsPanel != null && optionsPanel.activeSelf)
                optionsPanel.SetActive(false);

            if (currentScreen != null && currentScreen != subPanel)
                currentScreen.SetActive(false);

            subPanel.SetActive(true);
            currentScreen = subPanel;
            currentOptionsSubPanel = subPanel;

            // If opening Help as an options subpanel, clear help substate
            if (screen == MenuScreen.Help)
            {
                if (currentHelpSubPanel != null)
                {
                    currentHelpSubPanel.SetActive(false);
                    currentHelpSubPanel = null;
                }
            }

            return;
        }

        // --- UPGRADES subscreens ---
        if (IsUpgradesSubscreen(screen))
        {
            if (!screenDictionary.TryGetValue(screen, out GameObject upgradesSub) || upgradesSub == null)
                return;

            // Hide other root panels if needed
            if (currentScreen != null && currentScreen != upgradesPanel)
                currentScreen.SetActive(false);

            // Activate upgrades container
            if (upgradesPanel != null && !upgradesPanel.activeSelf)
                upgradesPanel.SetActive(true);

            currentScreen = upgradesPanel;

            // Deactivate previous upgrades subpanel
            if (currentUpgradesSubPanel != null && currentUpgradesSubPanel != upgradesSub)
                currentUpgradesSubPanel.SetActive(false);

            upgradesSub.SetActive(true);
            currentUpgradesSubPanel = upgradesSub;

            // Ensure other groups' subpanels hidden
            if (currentHelpSubPanel != null) { currentHelpSubPanel.SetActive(false); currentHelpSubPanel = null; }
            if (currentOptionsSubPanel != null) { currentOptionsSubPanel.SetActive(false); currentOptionsSubPanel = null; }
            if (currentTiendaSubPanel != null) { currentTiendaSubPanel.SetActive(false); currentTiendaSubPanel = null; }
            if (currentPersonalizacionSubPanel != null) { currentPersonalizacionSubPanel.SetActive(false); currentPersonalizacionSubPanel = null; }

            return;
        }

        // --- TIENDA subscreens ---
        if (IsTiendaSubscreen(screen))
        {
            if (!screenDictionary.TryGetValue(screen, out GameObject tiendaSub) || tiendaSub == null)
                return;

            if (currentScreen != null && currentScreen != tiendaPanel)
                currentScreen.SetActive(false);

            if (tiendaPanel != null && !tiendaPanel.activeSelf)
                tiendaPanel.SetActive(true);

            currentScreen = tiendaPanel;

            if (currentTiendaSubPanel != null && currentTiendaSubPanel != tiendaSub)
                currentTiendaSubPanel.SetActive(false);

            tiendaSub.SetActive(true);
            currentTiendaSubPanel = tiendaSub;

            // Hide other groups' subpanels
            if (currentHelpSubPanel != null) { currentHelpSubPanel.SetActive(false); currentHelpSubPanel = null; }
            if (currentOptionsSubPanel != null) { currentOptionsSubPanel.SetActive(false); currentOptionsSubPanel = null; }
            if (currentUpgradesSubPanel != null) { currentUpgradesSubPanel.SetActive(false); currentUpgradesSubPanel = null; }
            if (currentPersonalizacionSubPanel != null) { currentPersonalizacionSubPanel.SetActive(false); currentPersonalizacionSubPanel = null; }

            return;
        }

        // --- PERSONALIZACION subscreens ---
        if (IsPersonalizacionSubscreen(screen))
        {
            if (!screenDictionary.TryGetValue(screen, out GameObject persSub) || persSub == null)
                return;

            if (currentScreen != null && currentScreen != personalizacionPanel)
                currentScreen.SetActive(false);

            if (personalizacionPanel != null && !personalizacionPanel.activeSelf)
                personalizacionPanel.SetActive(true);

            currentScreen = personalizacionPanel;

            if (currentPersonalizacionSubPanel != null && currentPersonalizacionSubPanel != persSub)
                currentPersonalizacionSubPanel.SetActive(false);

            persSub.SetActive(true);
            currentPersonalizacionSubPanel = persSub;

            // Hide other groups' subpanels
            if (currentHelpSubPanel != null) { currentHelpSubPanel.SetActive(false); currentHelpSubPanel = null; }
            if (currentOptionsSubPanel != null) { currentOptionsSubPanel.SetActive(false); currentOptionsSubPanel = null; }
            if (currentUpgradesSubPanel != null) { currentUpgradesSubPanel.SetActive(false); currentUpgradesSubPanel = null; }
            if (currentTiendaSubPanel != null) { currentTiendaSubPanel.SetActive(false); currentTiendaSubPanel = null; }

            return;
        }

        // --- Top-level: show Options root ---
        if (screen == MenuScreen.Options)
        {
            if (currentScreen != null)
                currentScreen.SetActive(false);

            // Hide any open subs
            if (currentOptionsSubPanel != null) { currentOptionsSubPanel.SetActive(false); currentOptionsSubPanel = null; }
            if (currentHelpSubPanel != null) { currentHelpSubPanel.SetActive(false); currentHelpSubPanel = null; }
            if (currentUpgradesSubPanel != null) { currentUpgradesSubPanel.SetActive(false); currentUpgradesSubPanel = null; }
            if (currentTiendaSubPanel != null) { currentTiendaSubPanel.SetActive(false); currentTiendaSubPanel = null; }
            if (currentPersonalizacionSubPanel != null) { currentPersonalizacionSubPanel.SetActive(false); currentPersonalizacionSubPanel = null; }

            if (optionsPanel != null)
            {
                optionsPanel.SetActive(true);
                currentScreen = optionsPanel;
            }
            return;
        }

        // --- Default: deactivate all dictionary panels and show requested top-level screen ---
        foreach (var panel in screenDictionary.Values)
        {
            if (panel != null)
                panel.SetActive(false);
        }

        if (screenDictionary.TryGetValue(screen, out GameObject screenToShow) && screenToShow != null)
        {
            screenToShow.SetActive(true);
            currentScreen = screenToShow;
        }

        // reset subs
        currentOptionsSubPanel = null;
        currentHelpSubPanel = null;
        currentUpgradesSubPanel = null;
        currentTiendaSubPanel = null;
        currentPersonalizacionSubPanel = null;
    }

    public void BackToOptions()
    {
        ShowScreen(MenuScreen.Options);
    }

    public void ShowScreenByIndex(int index)
    {
        ShowScreen((MenuScreen)index);
    }

    public void OnPlayPressed()
    {
        CreateFadeOverlayIfNeeded();

        fadeCanvasGroup.alpha = 0f;
        fadeOverlay.SetActive(true);

        if (MusicManager.Instance != null)
            MusicManager.Instance.FadeOutMenuMusic(fadeDuration);

        fadeCanvasGroup
            .DOFade(1f, fadeDuration)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
            });
    }

    public void OnExitPressed()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void CreateFadeOverlayIfNeeded()
    {
        if (fadeCanvasGroup != null) return;

        fadeOverlay = new GameObject("MainMenu_FadeOverlay");
        DontDestroyOnLoad(fadeOverlay);

        Canvas canvas = fadeOverlay.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = overlaySortingOrder;

        fadeOverlay.AddComponent<CanvasScaler>();
        fadeOverlay.AddComponent<GraphicRaycaster>();

        fadeCanvasGroup = fadeOverlay.AddComponent<CanvasGroup>();
        fadeCanvasGroup.alpha = 0f;

        GameObject imgObj = new GameObject("FadeImage");
        imgObj.transform.SetParent(fadeOverlay.transform, false);

        RectTransform rt = imgObj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = imgObj.AddComponent<Image>();
        img.color = Color.black;

        // Asegurar que el overlay se destruya al cargar la siguiente escena
        fadeOverlay.AddComponent<FadeOverlayController>();

        fadeOverlay.SetActive(false);
    }
}

public enum MenuScreen
{
    // Main Screens
    Main,                       // 0
    Options,                    // 1
    Upgrades,                   // 2
    Tienda,                     // 3
    Personalizacion,            // 4
    Creditos,                   // 5

    // Options Subscreens
    Help,                       // 6
    Audio,                      // 7
    Controles,                  // 8

    // Upgrades
    UpgradesHabilidades,        // 9
    UpgradesArbolDeHabilidades, // 10
    UpgradesDetalles,           // 11

    // Tienda
    TiendaAtuendos,             // 12
    TiendaEfectos,              // 13
    TiendaMonedas,              // 14

    // Personalizacion
    PersonalizacionSkins,       // 15
    PersonalizacionAttackEffects, // 16

    // Help Subscreens
    HelpMovimiento,             // 17
    HelpExperiencia,            // 18
    HelpEnemigos,               // 19
    HelpMejoras                 // 20
}
