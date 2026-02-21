using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class LevelUpManager : MonoBehaviour
{
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
    
    [Header("Shop Settings")]
    [SerializeField] private float shopGlobalCooldown = 120f; // Cooldown global de la tienda en segundos

    private bool levelUpActive = false;
    private int currentPlayerLevel = 1;
    private UpgradeMode currentMode = UpgradeMode.LevelUp;
    private List<UpgradeButton> allButtons = new List<UpgradeButton>();
    private float lastPurchaseTime = -999f;
    private bool shopOnCooldown = false;
    private ShopScript connectedShop;
    
    private InputAction closeShopAction;

    private void Awake()
    {
        if (levelUpPanel != null)
            levelUpPanel.SetActive(false);
        
        // Setup ESC key input
        closeShopAction = new InputAction(
            name: "CloseShop",
            binding: "<Keyboard>/escape"
        );
        closeShopAction.Enable();
    }

    private void Start()
    {
        allButtons.Clear();
        if (upgradeButton1 != null) allButtons.Add(upgradeButton1);
        if (upgradeButton2 != null) allButtons.Add(upgradeButton2);
        if (upgradeButton3 != null) allButtons.Add(upgradeButton3);
        
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
        
        closeShopAction?.Disable();
    }
    
    private void OnDestroy()
    {
        closeShopAction?.Dispose();
    }

    private void Update()
    {
        if (levelUpActive && levelUpText != null)
        {
            float hue = Mathf.PingPong(Time.unscaledTime * colorSpeed, 1f);
            levelUpText.color = Color.HSVToRGB(hue, 1f, 1f);
        }
        
        // ESC key to close shop using Input System
        if (levelUpActive && currentMode == UpgradeMode.Shop && closeShopAction.triggered)
        {
            CloseLevelUp();
        }
        
        // Actualizar cooldown DENTRO del panel si está abierto en modo Shop (TIEMPO REAL)
        if (levelUpActive && currentMode == UpgradeMode.Shop && shopOnCooldown)
        {
            UpdateCooldownDisplay();
        }
        
        // Verificar si el cooldown terminó
        if (shopOnCooldown && Time.unscaledTime - lastPurchaseTime >= shopGlobalCooldown)
        {
            shopOnCooldown = false;
            
            // Si la tienda está abierta, habilitar botones
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
            levelUpText.text = $"LEVEL {newLevel}!";
        
        // Ocultar textos de Shop en modo LevelUp
        if (cooldownWarningText != null)
        {
            cooldownWarningText.gameObject.SetActive(false);
        }
        
        if (closeInstructionText != null)
        {
            closeInstructionText.gameObject.SetActive(false);
        }

        // Generar opciones de upgrade aleatorias
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
        
        // Obtener niveles actuales de upgrades
        Dictionary<UpgradeType, int> currentLevels = PlayerStatsManager.Instance.GetAllUpgradeLevels();
        
        // Obtener 3 upgrades aleatorios (usar ShopUpgradeDatabase si es modo Shop)
        List<UpgradeData> selectedUpgrades;
        
        if (mode == UpgradeMode.Shop && ShopUpgradeDatabase.Instance != null)
        {
            selectedUpgrades = ShopUpgradeDatabase.Instance.GetRandomShopUpgrades(currentLevels);
        }
        else
        {
            selectedUpgrades = UpgradeDatabase.Instance.GetRandomUpgrades(currentLevels, currentPlayerLevel);
        }
        
        // Configurar botones
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
    
    /// <summary>
    /// Actualiza el display del cooldown en el panel
    /// </summary>
    private void UpdateCooldownDisplay()
    {
        if (cooldownWarningText == null) return;
        
        float timeElapsed = Time.unscaledTime - lastPurchaseTime;
        float timeRemaining = shopGlobalCooldown - timeElapsed;
        
        if (timeRemaining > 0)
        {
            int minutes = Mathf.FloorToInt(timeRemaining / 60f);
            int seconds = Mathf.FloorToInt(timeRemaining % 60f);
            cooldownWarningText.text = $"Next purchase in: {minutes:00}:{seconds:00}";
            cooldownWarningText.gameObject.SetActive(true);
        }
        else
        {
            cooldownWarningText.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Registra el ShopScript que controla esta tienda
    /// </summary>
    public void RegisterShop(ShopScript shop)
    {
        connectedShop = shop;
    }
    
    /// <summary>
    /// Verifica si el panel de level up está activo
    /// </summary>
    public bool IsLevelUpActive()
    {
        return levelUpActive;
    }
    
    /// <summary>
    /// Verifica si la tienda está disponible (no en cooldown)
    /// </summary>
    public bool IsShopAvailable()
    {
        return !shopOnCooldown;
    }
    
    /// <summary>
    /// Obtiene el tiempo restante del cooldown en segundos
    /// </summary>
    public float GetShopCooldownRemaining()
    {
        if (!shopOnCooldown)
            return 0f;
        
        float timeElapsed = Time.unscaledTime - lastPurchaseTime;
        return Mathf.Max(0f, shopGlobalCooldown - timeElapsed);
    }
    
    public void ShowShop()
    {
        if (levelUpActive)
            return;
        
        levelUpActive = true;
        currentMode = UpgradeMode.Shop;
        
        Time.timeScale = 0f;
        
        if (levelUpText != null)
            levelUpText.text = "SHOP";
        
        // Mostrar instrucciones de cierre
        if (closeInstructionText != null)
        {
            closeInstructionText.gameObject.SetActive(true);
        }
        
        GenerateUpgradeOptions(UpgradeMode.Shop);
        
        // Si está en cooldown, deshabilitar botones y mostrar contador
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
        
        if (levelUpPanel != null)
            levelUpPanel.SetActive(true);
    }
    
    public void OnUpgradeChosen()
    {
        // En modo Shop, después de UNA compra, deshabilitar todo y empezar cooldown
        if (currentMode == UpgradeMode.Shop)
        {
            lastPurchaseTime = Time.unscaledTime;
            shopOnCooldown = true;
            
            // Deshabilitar todos los botones inmediatamente
            DisableAllButtons();
            
            // Mostrar el contador de cooldown
            UpdateCooldownDisplay();
        }
        else
        {
            CloseLevelUp();
        }
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
        // Only refresh affordability if in Shop mode
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
        
        // Notificar al ShopScript que se cerró
        if (currentMode == UpgradeMode.Shop && connectedShop != null)
        {
            connectedShop.OnShopClosed();
        }
    }
}

