using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class LevelUpManager : MonoBehaviour
{
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

    [Header("Upgrade Buttons")]
    [SerializeField] private UpgradeButton upgradeButton1;
    [SerializeField] private UpgradeButton upgradeButton2;
    [SerializeField] private UpgradeButton upgradeButton3;

    [Header("Rainbow Text Settings")]
    [SerializeField] private float colorSpeed = 1f;

    private bool levelUpActive = false;
    private int currentPlayerLevel = 1;
    private UpgradeMode currentMode = UpgradeMode.LevelUp;
    private List<UpgradeButton> allButtons = new List<UpgradeButton>();
    private float lastPurchaseTime = -999f;
    private bool shopOnCooldown = false;
    private ShopScript connectedShop;

    private bool cooldownPaused = false;
    private float pausedCooldownTimeRemaining = 0f;

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

        if (ExperienceManager.Instance != null)
            ExperienceManager.Instance.OnLevelUp += HandleLevelUp;

        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCoinsChanged += OnCurrencyChanged;
        }
    }

    private void OnDisable()
    {
        if (ExperienceManager.Instance != null)
            ExperienceManager.Instance.OnLevelUp -= HandleLevelUp;

        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCoinsChanged -= OnCurrencyChanged;
        }
    }

    private void Update()
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

        if (levelUpActive && levelUpText != null)
        {
            float hue = Mathf.PingPong(Time.unscaledTime * colorSpeed, 1f);
            levelUpText.color = Color.HSVToRGB(hue, 1f, 1f);
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

    private void HandleLevelUp(int newLevel)
    {
        if (levelUpActive)
            return;

        levelUpActive = true;
        currentPlayerLevel = newLevel;
        currentMode = UpgradeMode.LevelUp;

        Time.timeScale = 0f;

        if (levelUpText != null)
            levelUpText.text = $"Nivel {newLevel}!";

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
            cooldownWarningText.text = $"Próxima compra en: {minutes:00}:{seconds:00}";
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
            levelUpText.text = "Tienda";

        // Mostrar sólo la instrucción de la tienda y ocultar la del cofre
        if (levelUpPanel != null)
            levelUpPanel.SetActive(true);

        if (closeInstructionText != null)
        {
            closeInstructionText.text = "Presiona Espacio para cerrar la tienda";
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

        GameEvents.TriggerShopOpened();
    }

    /// <summary>
    /// Abre la selección de un Cofre reutilizando el panel premium existente,
    /// mostrando ítems de efecto único (no mejoras de stats).
    /// Si se proporciona 'chestItem', se muestra ese item (persistente para el cofre).
    /// </summary>
    public void ShowChestSelection(ChestItemData chestItem = null)
    {
        Debug.Log("ShowChestSelection ejecutado");

        if (levelUpActive)
            return;

        levelUpActive = true;
        currentMode = UpgradeMode.Chest;

        Time.timeScale = 0f;

        if (levelUpText != null)
            levelUpText.text = "\u00a1Cofre!";

        if (cooldownWarningText != null)
            cooldownWarningText.gameObject.SetActive(false);

        if (levelUpPanel != null)
            levelUpPanel.SetActive(true);

        if (closeInstructionText != null)
        {
            closeInstructionText.text =
                "Si deseas usar el ítem en otro momento, presiona Espacio para cerrar el cofre";

            closeInstructionText.gameObject.SetActive(true);
        }

        GenerateChestOptions(chestItem);

        if (levelUpPanel != null)
            levelUpPanel.SetActive(true);
    }

    private void GenerateChestOptions(ChestItemData chestItem = null)
    {
        // Si se pasó un ChestItem explícito lo mostramos; si no, seleccionamos aleatorio.
        if (chestItem != null)
        {
            if (upgradeButton1 != null)
            {
                upgradeButton1.SetupChest(chestItem);
            }

            if (upgradeButton2 != null)
                upgradeButton2.gameObject.SetActive(false);

            if (upgradeButton3 != null)
                upgradeButton3.gameObject.SetActive(false);

            return;
        }

        // Comportamiento previo: elegir al azar (esto se usa sólo si no se pasa item).
        List<ChestItemData> items = ChestItemProvider.GetRandomItems(1);

        if (upgradeButton1 != null)
        {
            if (items.Count > 0) upgradeButton1.SetupChest(items[0]);
            else upgradeButton1.gameObject.SetActive(false);
        }

        if (upgradeButton2 != null)
            upgradeButton2.gameObject.SetActive(false);

        // El cofre nunca usa el tercer botón.
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

        // Si se cerró la UI mientras el modo era Chest, notificar al ChestPickup
        // para que restaure su estado (no destruir el cofre).
        if (currentMode == UpgradeMode.Chest)
        {
            ChestSpawner.NotifyChestSelectionClosed();
        }

        if (closeInstructionText != null)
        {
            closeInstructionText.gameObject.SetActive(false);

            // Dejamos preparado el texto para la próxima tienda.
            closeInstructionText.text = "Presiona Espacio para cerrar la tienda";
        }

        if (currentMode == UpgradeMode.Shop && connectedShop != null)
        {
            connectedShop.OnShopClosed();
        }
    }

    public void OnUpgradeChosen()
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
            // El jugador confirmó la mejora del Cofre: cerrar UI y eliminar el cofre del mapa.
            CloseLevelUp();

            // Destruye el cofre activo y reinicia temporizador.
            ChestSpawner.CollectActiveChest();

            // Notificar (si corresponde) que el cofre fue recogido.
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
        text.enableAutoSizing = false;

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
}
