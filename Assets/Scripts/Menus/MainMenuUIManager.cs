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

    //[Header("Upgrades Subscreens")]
    //[SerializeField] private GameObject habilidadesPanel;
    //[SerializeField] private GameObject arbolDeHabilidesPanel;
    //[SerializeField] private GameObject detallesPanel;

    //[Header("Tienda Subscreens")]
    //[SerializeField] private GameObject atuendosPanel;
    //[SerializeField] private GameObject efectosPanel;
    //[SerializeField] private GameObject monedasPanel;

    //[Header("Personalizacion Subscreens")]
    //[SerializeField] private GameObject skinsPanel;
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

            //// Upgrades
            //{ MenuScreen.UpgradesHabilidades, habilidadesPanel },
            //{ MenuScreen.UpgradesArbolDeHabilidades, arbolDeHabilidesPanel },
            //{ MenuScreen.UpgradesDetalles, detallesPanel },

            //// Tienda
            //{ MenuScreen.TiendaAtuendos, atuendosPanel },
            //{ MenuScreen.TiendaEfectos, efectosPanel },
            //{ MenuScreen.TiendaMonedas, monedasPanel },

            //// Personalizacion
            //{ MenuScreen.PersonalizacionSkins, skinsPanel },
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

        ShowScreen(MenuScreen.Main);
    }

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

    public void ShowScreen(MenuScreen screen)
    {
        // Manejo de subpantallas de Help (HelpMovimiento, HelpExperiencia, ...)
        if (IsHelpSubscreen(screen))
        {
            if (!screenDictionary.TryGetValue(screen, out GameObject helpSub) || helpSub == null)
                return;

            // Apagar optionsPanel si estaba visible
            if (optionsPanel != null && optionsPanel.activeSelf)
                optionsPanel.SetActive(false);

            // Si había otra pantalla activa y no es helpPanel, apagarla
            if (currentScreen != null && currentScreen != helpPanel)
            {
                currentScreen.SetActive(false);
            }

            // Activar helpPanel (contenedor)
            if (helpPanel != null && !helpPanel.activeSelf)
            {
                helpPanel.SetActive(true);
            }

            currentScreen = helpPanel;
            currentOptionsSubPanel = helpPanel;

            // Apagar subhelp anterior
            if (currentHelpSubPanel != null && currentHelpSubPanel != helpSub)
            {
                currentHelpSubPanel.SetActive(false);
            }

            helpSub.SetActive(true);
            currentHelpSubPanel = helpSub;
            return;
        }

        // Manejo de subpantallas de Options (Audio, Controles, Help)
        if (IsOptionsSubscreen(screen))
        {
            if (!screenDictionary.TryGetValue(screen, out GameObject subPanel) || subPanel == null)
                return;

            // Apagar optionsPanel si estaba visible
            if (optionsPanel != null && optionsPanel.activeSelf)
                optionsPanel.SetActive(false);

            // Si había otra pantalla activa distinta del subpanel, apagarla
            if (currentScreen != null && currentScreen != subPanel)
            {
                currentScreen.SetActive(false);
            }

            // Activar el subpanel seleccionado (Audio o Controles o Help)
            subPanel.SetActive(true);
            currentScreen = subPanel;
            currentOptionsSubPanel = subPanel;

            // Si abrimos 'Help' como subpanel, resetear currentHelpSubPanel (se gestionará si se abre un sub-sub)
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

        // Mostrar la pantalla Options (raíz): apagar subscreens y mostrar optionsPanel
        if (screen == MenuScreen.Options)
        {
            // Apagar cualquier pantalla activa
            if (currentScreen != null)
            {
                currentScreen.SetActive(false);
            }

            // Apagar subscreens de options y help
            if (currentOptionsSubPanel != null)
            {
                currentOptionsSubPanel.SetActive(false);
                currentOptionsSubPanel = null;
            }

            if (currentHelpSubPanel != null)
            {
                currentHelpSubPanel.SetActive(false);
                currentHelpSubPanel = null;
            }

            if (optionsPanel != null)
            {
                optionsPanel.SetActive(true);
                currentScreen = optionsPanel;
            }
            return;
        }

        // Caso general: apagar todo el diccionario y mostrar la pantalla solicitada
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

        currentOptionsSubPanel = null;
        currentHelpSubPanel = null;
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
