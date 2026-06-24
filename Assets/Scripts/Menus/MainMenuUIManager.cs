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
    [SerializeField] private GameObject mapSelection;
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
    [SerializeField] private GameObject habilidadesnNormalesPanel;
    [SerializeField] private GameObject habilidadesPremiumPanel;

    [Header("Tienda Subscreens")]
    [SerializeField] private GameObject atuendosPanel;
    [SerializeField] private GameObject gemasPanel;

    [Header("Personalizacion Subscreens")]
    [SerializeField] private GameObject skinsPanel;

    [Header("Help Subscreens")]
    [SerializeField] private GameObject movimientoPanel;
    [SerializeField] private GameObject experienciaPanel;
    [SerializeField] private GameObject enemigosPanel;
    [SerializeField] private GameObject mejorasPanel;

    [Header("Normal SubScreens")]
    [SerializeField] private GameObject rpPanel;
    [SerializeField] private GameObject rPanel;
    [SerializeField] private GameObject dPanel;
    [SerializeField] private GameObject mPanel;
    [SerializeField] private GameObject msPanel;

    [Header("Premium Subscreens")]
    [SerializeField] private GameObject kbPanel;
    [SerializeField] private GameObject exPanel;
    [SerializeField] private GameObject mlsPanel;

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
            { MenuScreen.Main, mainPanel },
            { MenuScreen.Options, optionsPanel },
            { MenuScreen.Upgrades, upgradesPanel },
            { MenuScreen.Tienda, tiendaPanel },
            { MenuScreen.Personalizacion, personalizacionPanel },
            { MenuScreen.Creditos, creditosPanel },
            { MenuScreen.mapSelection, mapSelection },

            { MenuScreen.Help, helpPanel },
            { MenuScreen.Audio, audioPanel },
            { MenuScreen.Controles, controlesPanel },

            { MenuScreen.UpgradesHabilidadesNormales, habilidadesnNormalesPanel },
            { MenuScreen.UpgradesHabilidadesPremium,  habilidadesPremiumPanel },

            { MenuScreen.TiendaAtuendos, atuendosPanel },
            { MenuScreen.TiendaMonedas, gemasPanel },

            { MenuScreen.PersonalizacionSkins, skinsPanel },

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

        DeactivateAllUpgradeSubpanels();

        currentHelpSubPanel = null;
        currentOptionsSubPanel = null;
        currentUpgradesSubPanel = null;
        currentTiendaSubPanel = null;
        currentPersonalizacionSubPanel = null;

        ShowScreen(MenuScreen.Main);
    }

    private void Start()
    {
        if (mapSelection != null)
        {
            foreach (Button btn in mapSelection.GetComponentsInChildren<Button>(true))
            {
                if (btn.GetComponent<MenuButtonHover>() == null)
                    btn.gameObject.AddComponent<MenuButtonHover>();
            }
        }
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

    private bool IsUpgradesSubscreen(MenuScreen screen)
    {
        return screen == MenuScreen.UpgradesHabilidadesNormales
            || screen == MenuScreen.UpgradesHabilidadesPremium
            || screen == MenuScreen.UpgradesDetalles

            || screen == MenuScreen.UpgradesNormal_RP
            || screen == MenuScreen.UpgradesNormal_R
            || screen == MenuScreen.UpgradesNormal_D
            || screen == MenuScreen.UpgradesNormal_M
            || screen == MenuScreen.UpgradesNormal_MS
            || screen == MenuScreen.UpgradesPremium_KB
            || screen == MenuScreen.UpgradesPremium_EX
            || screen == MenuScreen.UpgradesPremium_MLS;
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

        switch (screen)
        {
            case MenuScreen.UpgradesNormal_RP:
                ShowUpgradeNormalSubpanel(0);
                return;
            case MenuScreen.UpgradesNormal_R:
                ShowUpgradeNormalSubpanel(1);
                return;
            case MenuScreen.UpgradesNormal_D:
                ShowUpgradeNormalSubpanel(2);
                return;
            case MenuScreen.UpgradesNormal_M:
                ShowUpgradeNormalSubpanel(3);
                return;
            case MenuScreen.UpgradesNormal_MS:
                ShowUpgradeNormalSubpanel(4);
                return;
            case MenuScreen.UpgradesPremium_KB:
                ShowUpgradePremiumSubpanel(0);
                return;
            case MenuScreen.UpgradesPremium_EX:
                ShowUpgradePremiumSubpanel(1);
                return;
            case MenuScreen.UpgradesPremium_MLS:
                ShowUpgradePremiumSubpanel(2);
                return;
        }

        if (IsHelpSubscreen(screen))
        {
            if (!screenDictionary.TryGetValue(screen, out GameObject helpSub) || helpSub == null)
                return;

            if (optionsPanel != null && optionsPanel.activeSelf)
                optionsPanel.SetActive(false);

            if (currentScreen != null && currentScreen != helpPanel)
                currentScreen.SetActive(false);

            if (helpPanel != null && !helpPanel.activeSelf)
                helpPanel.SetActive(true);

            currentScreen = helpPanel;
            currentOptionsSubPanel = helpPanel;

            if (currentHelpSubPanel != null && currentHelpSubPanel != helpSub)
                currentHelpSubPanel.SetActive(false);

            helpSub.SetActive(true);
            currentHelpSubPanel = helpSub;
            return;
        }

        if (IsOptionsSubscreen(screen))
        {
            if (!screenDictionary.TryGetValue(screen, out GameObject subPanel) || subPanel == null)
                return;

            if (optionsPanel != null && optionsPanel.activeSelf)
                optionsPanel.SetActive(false);

            if (currentScreen != null && currentScreen != subPanel)
                currentScreen.SetActive(false);

            subPanel.SetActive(true);
            currentScreen = subPanel;
            currentOptionsSubPanel = subPanel;

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

        if (IsUpgradesSubscreen(screen))
        {

            if (currentScreen != null && currentScreen != upgradesPanel)
                currentScreen.SetActive(false);

            if (upgradesPanel != null && !upgradesPanel.activeSelf)
                upgradesPanel.SetActive(true);

            currentScreen = upgradesPanel;

            if (screen == MenuScreen.UpgradesHabilidadesNormales)
            {

                if (habilidadesnNormalesPanel != null && !habilidadesnNormalesPanel.activeSelf)
                    habilidadesnNormalesPanel.SetActive(true);

                if (habilidadesPremiumPanel != null && habilidadesPremiumPanel.activeSelf)
                    habilidadesPremiumPanel.SetActive(false);

                rpPanel?.SetActive(false);
                rPanel?.SetActive(false);
                dPanel?.SetActive(false);
                mPanel?.SetActive(false);
                msPanel?.SetActive(false);

                currentUpgradesSubPanel = null;
            }

            else if (screen == MenuScreen.UpgradesHabilidadesPremium)
            {
                if (habilidadesPremiumPanel != null && !habilidadesPremiumPanel.activeSelf)
                    habilidadesPremiumPanel.SetActive(true);

                if (habilidadesnNormalesPanel != null && habilidadesnNormalesPanel.activeSelf)
                    habilidadesnNormalesPanel.SetActive(false);

                SetButtonsActive(kbPanel, true);
                SetButtonsActive(exPanel, true);
                SetButtonsActive(mlsPanel, true);

                kbPanel?.SetActive(false);
                exPanel?.SetActive(false);
                mlsPanel?.SetActive(false);

                currentUpgradesSubPanel = null;
            }
            else
            {

                if (screenDictionary.TryGetValue(screen, out GameObject upgradesSub) && upgradesSub != null)
                {
                    if (currentUpgradesSubPanel != null && currentUpgradesSubPanel != upgradesSub)
                        currentUpgradesSubPanel.SetActive(false);

                    upgradesSub.SetActive(true);
                    currentUpgradesSubPanel = upgradesSub;
                }

            }

            if (currentHelpSubPanel != null) { currentHelpSubPanel.SetActive(false); currentHelpSubPanel = null; }
            if (currentOptionsSubPanel != null) { currentOptionsSubPanel.SetActive(false); currentOptionsSubPanel = null; }
            if (currentTiendaSubPanel != null) { currentTiendaSubPanel.SetActive(false); currentTiendaSubPanel = null; }
            if (currentPersonalizacionSubPanel != null) { currentPersonalizacionSubPanel.SetActive(false); currentPersonalizacionSubPanel = null; }

            return;
        }

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

            if (currentHelpSubPanel != null) { currentHelpSubPanel.SetActive(false); currentHelpSubPanel = null; }
            if (currentOptionsSubPanel != null) { currentOptionsSubPanel.SetActive(false); currentOptionsSubPanel = null; }
            if (currentUpgradesSubPanel != null) { currentUpgradesSubPanel.SetActive(false); currentUpgradesSubPanel = null; }
            if (currentPersonalizacionSubPanel != null) { currentPersonalizacionSubPanel.SetActive(false); currentPersonalizacionSubPanel = null; }

            return;
        }

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

            if (currentHelpSubPanel != null) { currentHelpSubPanel.SetActive(false); currentHelpSubPanel = null; }
            if (currentOptionsSubPanel != null) { currentOptionsSubPanel.SetActive(false); currentOptionsSubPanel = null; }
            if (currentUpgradesSubPanel != null) { currentUpgradesSubPanel.SetActive(false); currentUpgradesSubPanel = null; }
            if (currentTiendaSubPanel != null) { currentTiendaSubPanel.SetActive(false); currentTiendaSubPanel = null; }

            return;
        }

        if (screen == MenuScreen.Options)
        {
            if (currentScreen != null)
                currentScreen.SetActive(false);

            if (currentOptionsSubPanel != null) { currentOptionsSubPanel.SetActive(false); currentOptionsSubPanel = null; }
            if (currentHelpSubPanel != null) { currentHelpSubPanel.SetActive(false); currentHelpSubPanel = null; }
            if (currentUpgradesSubPanel != null) { currentUpgradesSubPanel.SetActive(false); currentUpgradesSubPanel = null; }
            if (currentTiendaSubPanel != null) { currentTiendaSubPanel?.SetActive(false); currentTiendaSubPanel = null; }
            if (currentPersonalizacionSubPanel != null) { currentPersonalizacionSubPanel?.SetActive(false); currentPersonalizacionSubPanel = null; }

            if (optionsPanel != null)
            {
                optionsPanel.SetActive(true);
                currentScreen = optionsPanel;
            }
            return;
        }

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

    public void ShowUpgradeNormalSubpanel(int index)
    {
        GameObject[] normals = new GameObject[] { rpPanel, rPanel, dPanel, mPanel, msPanel };
        if (index < 0 || index >= normals.Length) return;

        if (upgradesPanel != null && !upgradesPanel.activeSelf) upgradesPanel.SetActive(true);
        if (habilidadesnNormalesPanel != null && !habilidadesnNormalesPanel.activeSelf) habilidadesnNormalesPanel.SetActive(true);

        if (habilidadesPremiumPanel != null && habilidadesPremiumPanel.activeSelf) habilidadesPremiumPanel.SetActive(false);
        SetButtonsActive(kbPanel, false);
        SetButtonsActive(exPanel, false);
        SetButtonsActive(mlsPanel, false);
        kbPanel?.SetActive(false);
        exPanel?.SetActive(false);
        mlsPanel?.SetActive(false);

        GameObject selected = normals[index];
        if (selected == null) return;

        if (currentUpgradesSubPanel != null && currentUpgradesSubPanel != selected)
        {
            SetButtonsActive(currentUpgradesSubPanel, false);
            currentUpgradesSubPanel.SetActive(false);
        }

        selected.SetActive(true);
        SetButtonsActive(selected, true);
        currentUpgradesSubPanel = selected;
    }

    public void ShowUpgradePremiumSubpanel(int index)
    {
        GameObject[] premiums = new GameObject[] { kbPanel, exPanel, mlsPanel };
        if (index < 0 || index >= premiums.Length) return;

        if (upgradesPanel != null && !upgradesPanel.activeSelf) upgradesPanel.SetActive(true);
        if (habilidadesPremiumPanel != null && !habilidadesPremiumPanel.activeSelf) habilidadesPremiumPanel.SetActive(true);

        if (habilidadesnNormalesPanel != null && habilidadesnNormalesPanel.activeSelf) habilidadesnNormalesPanel.SetActive(false);
        SetButtonsActive(rpPanel, false);
        SetButtonsActive(rPanel, false);
        SetButtonsActive(dPanel, false);
        SetButtonsActive(mPanel, false);
        SetButtonsActive(msPanel, false);
        rpPanel?.SetActive(false);
        rPanel?.SetActive(false);
        dPanel?.SetActive(false);
        mPanel?.SetActive(false);
        msPanel?.SetActive(false);

        GameObject selected = premiums[index];
        if (selected == null) return;

        if (currentUpgradesSubPanel != null && currentUpgradesSubPanel != selected)
        {
            SetButtonsActive(currentUpgradesSubPanel, false);
            currentUpgradesSubPanel.SetActive(false);
        }

        selected.SetActive(true);
        SetButtonsActive(selected, true);
        currentUpgradesSubPanel = selected;
    }

    private void DeactivateAllUpgradeSubpanels()
    {

        rpPanel?.SetActive(false);
        rPanel?.SetActive(false);
        dPanel?.SetActive(false);
        mPanel?.SetActive(false);
        msPanel?.SetActive(false);

        kbPanel?.SetActive(false);
        exPanel?.SetActive(false);
        mlsPanel?.SetActive(false);

        habilidadesnNormalesPanel?.SetActive(false);
        habilidadesPremiumPanel?.SetActive(false);
    }

    private void SetButtonsActive(GameObject panel, bool active)
    {
        if (panel == null) return;

        foreach (var btn in panel.GetComponentsInChildren<Button>(true))
        {
            if (btn == null) continue;
            btn.gameObject.SetActive(active);
            btn.interactable = active;
        }
    }

    public void LevelSelection(int sceneIndex)
    {
        MusicManager.Instance?.PlayUISound(MusicManager.Instance.clickSFX);
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
                SceneManager.LoadScene(sceneIndex, LoadSceneMode.Single);
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

        fadeOverlay.AddComponent<FadeOverlayController>();

        fadeOverlay.SetActive(false);
    }
}

public enum MenuScreen
{
    Main,
    Options,
    Upgrades,
    Tienda,
    Personalizacion,
    Creditos,

    Help,
    Audio,
    Controles,

    UpgradesHabilidadesNormales,
    UpgradesHabilidadesPremium,
    UpgradesDetalles,

    TiendaAtuendos,
    TiendaEfectos,
    TiendaMonedas,

    PersonalizacionSkins,
    PersonalizacionAttackEffects,

    HelpMovimiento,
    HelpExperiencia,
    HelpEnemigos,
    HelpMejoras,

    mapSelection,

    UpgradesNormal_RP,
    UpgradesNormal_R,
    UpgradesNormal_D,
    UpgradesNormal_M,
    UpgradesNormal_MS,

    UpgradesPremium_KB,
    UpgradesPremium_EX,
    UpgradesPremium_MLS,
}
