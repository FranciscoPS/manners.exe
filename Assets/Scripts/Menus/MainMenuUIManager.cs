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

    [Header("Options Screens")]
    [SerializeField] private GameObject helpPanel;
    [SerializeField] private GameObject audioPanel;
    [SerializeField] private GameObject controlesPanel;

    [Header("Upgrades Subscreens")]
    [SerializeField] private GameObject habilidadesPanel;
    [SerializeField] private GameObject arbolDeHabilidesPanel;
    [SerializeField] private GameObject detallesPanel;

    [Header("Tienda Subscreens")]
    [SerializeField] private GameObject atuendosPanel;
    [SerializeField] private GameObject efectosPanel;
    [SerializeField] private GameObject monedasPanel;

    [Header("Personalizacion Subscreens")]
    [SerializeField] private GameObject skinsPanel;
    [SerializeField] private GameObject attackEffectsPanel;

    [Header("Audio Subscreens")]
    [SerializeField] private GameObject sfxControlPanel;

    [Header("Controles Subscreens")]
    [SerializeField] private GameObject controlesHelpPanel;

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

            // Options
            { MenuScreen.Help, helpPanel },
            { MenuScreen.Audio, audioPanel },
            { MenuScreen.Controles, controlesPanel },

            // Upgrades Subscreens
            { MenuScreen.UpgradesHabilidades, habilidadesPanel },
            { MenuScreen.UpgradesArbolDeHabilidades, arbolDeHabilidesPanel },
            { MenuScreen.UpgradesDetalles, detallesPanel },

            // Tienda Subscreens
            { MenuScreen.TiendaAtuendos, atuendosPanel },
            { MenuScreen.TiendaEfectos, efectosPanel },
            { MenuScreen.TiendaMonedas, monedasPanel },

            // Personalizacion Subscreens
            { MenuScreen.PersonalizacionSkins, skinsPanel },
            { MenuScreen.PersonalizacionAttackEffects, attackEffectsPanel },

            // Audio Subscreens
            { MenuScreen.AudioSFXControl, sfxControlPanel },

            // Controles Subscreens
            { MenuScreen.ControlesHelp, controlesHelpPanel },

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
        ShowScreen(MenuScreen.Main);
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
        if (IsHelpSubscreen(screen))
        {
            if (!screenDictionary.TryGetValue(screen, out GameObject subPanel) || subPanel == null)
                return;

            if (helpPanel != null && !helpPanel.activeSelf)
            {
                if (currentScreen != null && currentScreen != helpPanel)
                {
                    currentScreen.SetActive(false);
                }
                helpPanel.SetActive(true);
                currentScreen = helpPanel;
            }

            if (currentHelpSubPanel != null && currentHelpSubPanel != subPanel)
            {
                currentHelpSubPanel.SetActive(false);
                currentHelpSubPanel = null;
            }

            subPanel.SetActive(true);
            currentHelpSubPanel = subPanel;
            return;
        }

        if (screen == MenuScreen.Help)
        {
            if (currentScreen != null && currentScreen != helpPanel)
            {
                currentScreen.SetActive(false);
            }

            if (currentHelpSubPanel != null)
            {
                currentHelpSubPanel.SetActive(false);
                currentHelpSubPanel = null;
            }

            if (helpPanel != null)
            {
                helpPanel.SetActive(true);
                currentScreen = helpPanel;
            }
            return;
        }

        if (currentScreen == helpPanel)
        {
            if (currentHelpSubPanel != null)
            {
                currentHelpSubPanel.SetActive(false);
                currentHelpSubPanel = null;
            }

            if (helpPanel != null)
            {
                helpPanel.SetActive(false);
            }
        }

        if (currentScreen != null)
        {
            currentScreen.SetActive(false);
            currentScreen = null;
        }

        if (screenDictionary.TryGetValue(screen, out GameObject screenToShow) && screenToShow != null)
        {
            screenToShow.SetActive(true);
            currentScreen = screenToShow;
        }
    }

    public void OnPlayPressed()
    {
        CreateFadeOverlayIfNeeded();

        fadeCanvasGroup.alpha = 0f;
        fadeOverlay.SetActive(true);

        fadeCanvasGroup.DOFade(1f, fadeDuration)
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

    public void ShowScreenByIndex(int screenIndex)
    {
        MenuScreen screen = (MenuScreen)screenIndex;
        ShowScreen(screen);
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

        fadeOverlay.AddComponent<FadeOverlayController>();

        fadeOverlay.SetActive(false);
    }
}

public enum MenuScreen
{
    // Main Screens
    Main,                           // 0
    Options,                        // 1
    Upgrades,                       // 2
    Tienda,                         // 3
    Personalizacion,                // 4
    Creditos,                       // 5

    // Options Screens
    Help,                           // 6
    Audio,                          // 7
    Controles,                      // 8

    // Upgrades Subscreens
    UpgradesHabilidades,            // 9
    UpgradesArbolDeHabilidades,     // 10
    UpgradesDetalles,               // 11

    // Tienda Subscreens
    TiendaAtuendos,                 // 12
    TiendaEfectos,                  // 13
    TiendaMonedas,                  // 14

    // Personalizacion Subscreens
    PersonalizacionSkins,           // 15
    PersonalizacionAttackEffects,   // 16

    // Audio Subscreens
    AudioSFXControl,                // 17

    // Controles Subscreens
    ControlesHelp,                  // 18

    // Help Subscreens
    HelpMovimiento,                 // 19
    HelpExperiencia,                // 20
    HelpEnemigos,                   // 21
    HelpMejoras                     // 22
}
