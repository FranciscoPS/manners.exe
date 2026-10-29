using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class LevelUpManager : MonoBehaviour, IUpdateable
{
    [Header("Textos editables por idioma")]
    [SerializeField] private LocalizedString levelTitle = new LocalizedString("Level {0}!", "Nivel {0}!");
    [SerializeField] private LocalizedString cooldownLabel = new LocalizedString("Next purchase in: {0:00}:{1:00}", "Próxima compra en: {0:00}:{1:00}");
    [SerializeField] private LocalizedString shopTitle = new LocalizedString("Shop", "Tienda");
    [SerializeField] private LocalizedString closeShop = new LocalizedString("Press Space to close the shop", "Presiona Espacio para cerrar la tienda");
    [SerializeField] private LocalizedString chestTitle = new LocalizedString("Chest!", "¡Cofre!");
    [SerializeField] private LocalizedString closeChest = new LocalizedString("Press Space to close the chest and use the item later", "Si deseas usar el ítem en otro momento, presiona Espacio para cerrar el cofre");

    private static LevelUpManager instance;
    public static LevelUpManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<LevelUpManager>();
            }
            return instance;
        }
    }

    [Header("UI")]
    [SerializeField] private GameObject levelUpPanel;
    [SerializeField] private TextMeshProUGUI levelUpText;
    [SerializeField] private TextMeshProUGUI cooldownWarningText;
    [SerializeField] private TextMeshProUGUI closeInstructionText;
    [Tooltip("Placa de fondo de la instrucción de cierre: se muestra y se oculta junto con el texto.")]
    [SerializeField] private GameObject closeInstructionPlate;

    [Header("Upgrade Buttons")]
    [SerializeField] private UpgradeButton upgradeButton1;
    [SerializeField] private UpgradeButton upgradeButton2;
    [SerializeField] private UpgradeButton upgradeButton3;

    [Header("Rainbow Text Settings")]
    [SerializeField] private float colorSpeed = 1f;
    [Tooltip("Fondo del título (estallido). Si está asignado, es el fondo el que recorre los colores de acento y el texto queda fijo para leerse mejor.")]
    [SerializeField] private Graphic titleBackdrop;

    [Header("Juice")]
    [Tooltip("Retraso entre la aparición de cada card de mejora, para un efecto de cascada.")]
    [SerializeField] private float cardIntroStagger = 0.07f;
    [Tooltip("Duración de la animación de cierre de las cards (de grande a chica) al elegir una mejora.")]
    [SerializeField] private float cardOutroDuration = 0.2f;

    private bool levelUpActive = false;
    private int currentPlayerLevel = 1;
    private UpgradeMode currentMode = UpgradeMode.LevelUp;
    private List<UpgradeButton> allButtons = new List<UpgradeButton>();
    private float lastPurchaseTime = -999f;
    private bool shopOnCooldown = false;
    private ShopScript connectedShop;

    private bool cooldownPaused = false;
    private float pausedCooldownTimeRemaining = 0f;
    private Coroutine closeAfterOutroRoutine;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        if (levelUpPanel != null)
            levelUpPanel.SetActive(false);
    }

    private void Start()
    {
        allButtons.Clear();
        if (upgradeButton1 != null) allButtons.Add(upgradeButton1);
        if (upgradeButton2 != null) allButtons.Add(upgradeButton2);
        if (upgradeButton3 != null) allButtons.Add(upgradeButton3);

        if (closeInstructionText != null)
        {
            closeInstructionText.gameObject.SetActive(false);
        }
        SyncInstructionPlate();

        if (ExperienceManager.Instance != null)
            ExperienceManager.Instance.OnLevelUp += HandleLevelUp;

        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCoinsChanged += OnCurrencyChanged;
        }
    }

    private void OnDisable()
    {
        UpdateManager.Instance?.Unregister(this);
        GameLocalization.LanguageChanged -= RefreshLanguage;
        if (ExperienceManager.Instance != null)
            ExperienceManager.Instance.OnLevelUp -= HandleLevelUp;

        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCoinsChanged -= OnCurrencyChanged;
        }
    }

    public bool IsActive => isActiveAndEnabled;
    public void OnUpdate(float deltaTime)
    {
        // Permitir cerrar la selección del cofre con la tecla Espacio.
        if (levelUpActive && currentMode == UpgradeMode.Chest)
        {
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                CloseLevelUp();
                return;
            }
        }

        if (levelUpActive)
        {
            UpdateTitleColor();
        }

        if (levelUpActive && currentMode == UpgradeMode.Shop && shopOnCooldown)
        {
            UpdateCooldownDisplay();
        }

        if (shopOnCooldown && !cooldownPaused && ShopUpgradeDatabase.Instance != null && Time.unscaledTime - lastPurchaseTime >= ShopUpgradeDatabase.Instance.ShopGlobalCooldown)
        {
            shopOnCooldown = false;

            if (levelUpActive && currentMode == UpgradeMode.Shop)
            {
                EnableAllButtons();

                if (cooldownWarningText != null)
                {
                    cooldownWarningText.gameObject.SetActive(false);
                }
            }
        }
    }

    private void UpdateTitleColor()
    {
        UIStyle style = UIStyle.Instance;
        if (titleBackdrop != null && style != null)
        {
            titleBackdrop.color = style.AccentCycle(Time.unscaledTime * style.accentCycleSpeed * colorSpeed);
            return;
        }

        if (levelUpText != null)
        {
            float hue = Mathf.PingPong(Time.unscaledTime * colorSpeed, 1f);
            levelUpText.color = Color.HSVToRGB(hue, 1f, 1f);
        }
    }

    private void PlayTitleIntro()
    {
        levelUpText.rectTransform.PopIn();

        if (titleBackdrop != null)
            titleBackdrop.rectTransform.PopIn(0.36f, 1.5f);
    }

    private void HandleLevelUp(int newLevel)
    {
        if (levelUpActive)
            return;

        levelUpActive = true;
        currentPlayerLevel = newLevel;
        currentMode = UpgradeMode.LevelUp;

        Time.timeScale = 0f;

        if (levelUpText != null)
        {
            levelUpText.text = levelTitle.Format(newLevel);
            PlayTitleIntro();
        }

        if (cooldownWarningText != null)
        {
            cooldownWarningText.gameObject.SetActive(false);
        }

        if (closeInstructionText != null)
        {
            closeInstructionText.gameObject.SetActive(false);
        }

        GenerateUpgradeOptions(UpgradeMode.LevelUp);

        if (levelUpPanel != null)
            levelUpPanel.SetActive(true);

        SyncInstructionPlate();
    }

    private void SyncInstructionPlate()
    {
        if (closeInstructionPlate != null && closeInstructionText != null)
            closeInstructionPlate.SetActive(closeInstructionText.gameObject.activeSelf);
    }

    private void GenerateUpgradeOptions(UpgradeMode mode)
    {
        if (UpgradeDatabase.Instance == null)
        {
            Debug.LogError("UpgradeDatabase not found! Cannot generate upgrade options.");
            return;
        }

        if (PlayerStatsManager.Instance == null)
        {
            Debug.LogError("PlayerStatsManager not found! Cannot generate upgrade options.");
            return;
        }

        Dictionary<UpgradeType, int> currentLevels = PlayerStatsManager.Instance.GetAllUpgradeLevels();

        List<UpgradeData> selectedUpgrades;

        if (mode == UpgradeMode.Shop && ShopUpgradeDatabase.Instance != null)
        {
            selectedUpgrades = ShopUpgradeDatabase.Instance.GetRandomShopUpgrades(currentLevels);
        }
        else
        {
            selectedUpgrades = UpgradeDatabase.Instance.GetRandomUpgrades(currentLevels, currentPlayerLevel);
        }

        if (upgradeButton1 != null)
        {
            if (selectedUpgrades.Count > 0)
            {
                int currentLevel = currentLevels.ContainsKey(selectedUpgrades[0].upgradeType)
                    ? currentLevels[selectedUpgrades[0].upgradeType]
                    : 0;
                upgradeButton1.Setup(selectedUpgrades[0], currentLevel, mode);
                upgradeButton1.PlayIntroAnimation(0f);
            }
            else
            {
                upgradeButton1.gameObject.SetActive(false);
            }
        }

        if (upgradeButton2 != null)
        {
            if (selectedUpgrades.Count > 1)
            {
                int currentLevel = currentLevels.ContainsKey(selectedUpgrades[1].upgradeType)
                    ? currentLevels[selectedUpgrades[1].upgradeType]
                    : 0;
                upgradeButton2.Setup(selectedUpgrades[1], currentLevel, mode);
                upgradeButton2.PlayIntroAnimation(cardIntroStagger);
            }
            else
            {
                upgradeButton2.gameObject.SetActive(false);
            }
        }

        if (upgradeButton3 != null)
        {
            if (selectedUpgrades.Count > 2)
            {
                int currentLevel = currentLevels.ContainsKey(selectedUpgrades[2].upgradeType)
                    ? currentLevels[selectedUpgrades[2].upgradeType]
                    : 0;
                upgradeButton3.Setup(selectedUpgrades[2], currentLevel, mode);
                upgradeButton3.PlayIntroAnimation(cardIntroStagger * 2f);
            }
            else
            {
                upgradeButton3.gameObject.SetActive(false);
            }
        }
    }

    private void UpdateCooldownDisplay()
    {
        if (cooldownWarningText == null) return;

        float timeRemaining;
        if (cooldownPaused)
        {
            timeRemaining = pausedCooldownTimeRemaining;
        }
        else
        {
            float timeElapsed = Time.unscaledTime - lastPurchaseTime;
            float cooldown = ShopUpgradeDatabase.Instance != null ? ShopUpgradeDatabase.Instance.ShopGlobalCooldown : 120f;
            timeRemaining = cooldown - timeElapsed;
        }

        if (timeRemaining > 0)
        {
            int minutes = Mathf.FloorToInt(timeRemaining / 60f);
            int seconds = Mathf.FloorToInt(timeRemaining % 60f);
            cooldownWarningText.text = cooldownLabel.Format(minutes, seconds);
            cooldownWarningText.gameObject.SetActive(true);
        }
        else
        {
            cooldownWarningText.gameObject.SetActive(false);
        }
    }

    public void RegisterShop(ShopScript shop)
    {
        connectedShop = shop;
    }

    public bool IsLevelUpActive()
    {
        return levelUpActive;
    }

    public bool IsShopAvailable()
    {
        return !shopOnCooldown;
    }

    public float GetShopCooldownRemaining()
    {
        if (!shopOnCooldown)
            return 0f;

        if (cooldownPaused)
            return pausedCooldownTimeRemaining;

        float timeElapsed = Time.unscaledTime - lastPurchaseTime;
        float cooldown = ShopUpgradeDatabase.Instance != null ? ShopUpgradeDatabase.Instance.ShopGlobalCooldown : 120f;
        return Mathf.Max(0f, cooldown - timeElapsed);
    }

    public void ShowShop()
    {
        Debug.Log("ShowShop ejecutado");

        if (levelUpActive)
            return;

        levelUpActive = true;
        currentMode = UpgradeMode.Shop;

        Time.timeScale = 0f;

        if (levelUpText != null)
        {
            levelUpText.text = shopTitle.Value;
            PlayTitleIntro();
        }

        // Mostrar sólo la instrucción de la tienda y ocultar la del cofre
        if (levelUpPanel != null)
            levelUpPanel.SetActive(true);

        if (closeInstructionText != null)
        {
            closeInstructionText.text = closeShop.Value;
            closeInstructionText.gameObject.SetActive(true);
        }

        if (shopOnCooldown)
        {
            float timeElapsed = Time.unscaledTime - lastPurchaseTime;
            float cooldown = ShopUpgradeDatabase.Instance != null ? ShopUpgradeDatabase.Instance.ShopGlobalCooldown : 120f;
            pausedCooldownTimeRemaining = Mathf.Max(0f, cooldown - timeElapsed);
            cooldownPaused = true;
        }

        GenerateUpgradeOptions(UpgradeMode.Shop);

        if (shopOnCooldown)
        {
            DisableAllButtons();
            UpdateCooldownDisplay();
        }
        else
        {
            if (cooldownWarningText != null)
            {
                cooldownWarningText.gameObject.SetActive(false);
            }
        }

        SyncInstructionPlate();
        GameEvents.TriggerShopOpened();
    }

    public bool ShowChestSelection(ChestItemData chestItem = null)
    {
        Debug.Log("ShowChestSelection ejecutado");

        if (levelUpActive)
            return false;

        levelUpActive = true;
        currentMode = UpgradeMode.Chest;

        Time.timeScale = 0f;

        if (levelUpText != null)
        {
            levelUpText.text = chestTitle.Value;
            PlayTitleIntro();
        }

        if (cooldownWarningText != null)
            cooldownWarningText.gameObject.SetActive(false);

        if (levelUpPanel != null)
            levelUpPanel.SetActive(true);

        if (closeInstructionText != null)
        {
            closeInstructionText.text =
                closeChest.Value;

            closeInstructionText.gameObject.SetActive(true);
        }

        GenerateChestOptions(chestItem);

        if (levelUpPanel != null)
            levelUpPanel.SetActive(true);

        SyncInstructionPlate();
        return true;
    }

    private void GenerateChestOptions(ChestItemData chestItem = null)
    {

        if (chestItem != null)
        {
            if (upgradeButton1 != null)
            {
                upgradeButton1.SetupChest(chestItem);
                upgradeButton1.PlayIntroAnimation(0f);
            }

            if (upgradeButton2 != null)
                upgradeButton2.gameObject.SetActive(false);

            if (upgradeButton3 != null)
                upgradeButton3.gameObject.SetActive(false);

            return;
        }

        List<ChestItemData> items = ChestItemProvider.GetRandomItems(1);

        if (upgradeButton1 != null)
        {
            if (items.Count > 0)
            {
                upgradeButton1.SetupChest(items[0]);
                upgradeButton1.PlayIntroAnimation(0f);
            }
            else
            {
                upgradeButton1.gameObject.SetActive(false);
            }
        }

        if (upgradeButton2 != null)
            upgradeButton2.gameObject.SetActive(false);

        if (upgradeButton3 != null)
            upgradeButton3.gameObject.SetActive(false);
    }

    private void DisableAllButtons()
    {
        foreach (var button in allButtons)
        {
            if (button != null && button.gameObject.activeSelf)
            {
                Button btn = button.GetComponent<Button>();
                if (btn != null)
                {
                    btn.interactable = false;
                }

                CanvasGroup cg = button.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.alpha = 0.3f;
                }
            }
        }
    }

    private void EnableAllButtons()
    {
        foreach (var button in allButtons)
        {
            if (button != null && button.gameObject.activeSelf)
            {
                button.RefreshAffordability();
            }
        }
    }

    private void OnCurrencyChanged(int newAmount)
    {

        if (currentMode == UpgradeMode.Shop)
        {
            foreach (var button in allButtons)
            {
                if (button != null && button.gameObject.activeSelf)
                {
                    button.RefreshAffordability();
                }
            }
        }
    }

    public void CloseLevelUp()
    {
        levelUpActive = false;

        if (levelUpPanel != null)
            levelUpPanel.SetActive(false);

        Time.timeScale = 1f;

        if (cooldownPaused)
        {
            cooldownPaused = false;

            float cooldown = ShopUpgradeDatabase.Instance != null ? ShopUpgradeDatabase.Instance.ShopGlobalCooldown : 120f;
            lastPurchaseTime = Time.unscaledTime - (cooldown - pausedCooldownTimeRemaining);
        }

        if (currentMode == UpgradeMode.Chest)
        {
            ChestSpawner.NotifyChestSelectionClosed();

            if (ShopManager.Instance != null && ShopManager.Instance.GetActiveShop() != null)
            {
                ShopManager.Instance.GetActiveShop().OnShopClosed();
            }
        }

        if (closeInstructionText != null)
        {
            closeInstructionText.gameObject.SetActive(false);

            // Dejamos preparado el texto para la próxima tienda.
            closeInstructionText.text = closeShop.Value;
        }
        SyncInstructionPlate();

        if (currentMode == UpgradeMode.Shop && connectedShop != null)
        {
            connectedShop.OnShopClosed();
        }
    }

    public void OnUpgradeChosen()
    {
        foreach (var button in allButtons)
        {
            if (button == null || !button.gameObject.activeSelf) continue;

            Button btn = button.GetComponent<Button>();
            if (btn != null) btn.interactable = false;

            button.PlayOutroAnimation(cardOutroDuration);
        }

        if (closeAfterOutroRoutine != null)
            StopCoroutine(closeAfterOutroRoutine);

        closeAfterOutroRoutine = StartCoroutine(CloseAfterOutroRoutine());
    }

    private IEnumerator CloseAfterOutroRoutine()
    {
        yield return new WaitForSecondsRealtime(cardOutroDuration);

        closeAfterOutroRoutine = null;
        FinishUpgradeChosen();
    }

    private void FinishUpgradeChosen()
    {
        if (currentMode == UpgradeMode.Shop)
        {
            lastPurchaseTime = Time.unscaledTime;
            shopOnCooldown = true;

            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.OnShopPurchaseMade();
            }

            CloseLevelUp();
            GameEvents.TriggerShopAutoClosed();
        }
        else if (currentMode == UpgradeMode.Chest)
        {

            CloseLevelUp();

            ChestSpawner.CollectActiveChest();

            ChestSpawner.NotifyChestCollected();
        }
        else
        {
            CloseLevelUp();
        }
    }

    // Helper: configura visibilidad + CanvasGroup y fuerza alpha del texto para evitar estar oculto por UI.
    private void SetInstructionVisible(TextMeshProUGUI text, string message, bool visible)
    {
        if (text == null)
        {
            Debug.LogError("Instruction TMP no asignado.");
            return;
        }

        text.gameObject.SetActive(true);

        text.text = message ?? "";

        // FORZAR VISIBILIDAD REAL

        Color c = text.color;
        c.a = visible ? 1f : 0f;
        text.color = c;

        text.ForceMeshUpdate();

        CanvasGroup cg = text.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = visible ? 1f : 0f;
            cg.interactable = visible;
            cg.blocksRaycasts = visible;
        }

        Debug.Log($"Instruction FINAL: '{message}' visible={visible}");
    }

    // Asegura que el textMeshPro quede bajo el mismo canvas/panel para evitar estar detrás.
    private void EnsureInstructionParent(TextMeshProUGUI text)
    {
        if (text == null)
            return;

        text.transform.SetAsLastSibling();
    }
    private void OnEnable() { GameLocalization.LanguageChanged += RefreshLanguage; UpdateManager.Instance?.Register(this); }
    private void RefreshLanguage()
    {
        if (!levelUpActive) return;
        if (levelUpText != null) levelUpText.text = currentMode == UpgradeMode.Chest ? chestTitle.Value : currentMode == UpgradeMode.Shop ? shopTitle.Value : levelTitle.Format(currentPlayerLevel);
        if (closeInstructionText != null && currentMode != UpgradeMode.LevelUp) closeInstructionText.text = currentMode == UpgradeMode.Chest ? closeChest.Value : closeShop.Value;
    }
}
