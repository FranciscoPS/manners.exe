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
    [SerializeField] private GameObject accesibilidadPanel;

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
            { MenuScreen.Accesibilidad, accesibilidadPanel },

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

    public void ShowScreen(MenuScreen screen)
    {
        if (currentScreen != null)
            currentScreen.SetActive(false);

        if (screenDictionary.TryGetValue(screen, out GameObject screenToShow))
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
                // Cargar la siguiente escena; el overlay se destruirá en el evento sceneLoaded
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
    Main,
    Options,
    Upgrades,
    Tienda,
    Personalizacion,
    Creditos,

    // Options Screens
    Help,
    Audio,
    Controles,
    Accesibilidad,

    // Help Subscreens
    HelpMovimiento,
    HelpExperiencia,
    HelpEnemigos,
    HelpMejoras
}
