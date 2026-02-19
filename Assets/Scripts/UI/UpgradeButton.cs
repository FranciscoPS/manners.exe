using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum UpgradeMode
{
    LevelUp,
    Shop
}

public class UpgradeButton : MonoBehaviour
{
    private TextMeshProUGUI upgradeNameText;
    private TextMeshProUGUI descriptionText;
    private TextMeshProUGUI labelText;
    private TextMeshProUGUI valuesText;
    private TextMeshProUGUI costText;
    private Image iconImage;
    private Button button;
    private CanvasGroup canvasGroup;
    
    [Header("Disabled Settings")]
    [SerializeField] private float disabledAlpha = 0.5f;
    
    private UpgradeData assignedUpgrade;
    private int currentLevel;
    private int nextLevel;
    private UpgradeMode currentMode = UpgradeMode.LevelUp;
    private int upgradeCost = 0;
    private bool canAfford = true;
    
    private void Awake()
    {
        button = GetComponent<Button>();
        canvasGroup = GetComponent<CanvasGroup>();
        
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnUpgradeSelected);
        }
        
        TextMeshProUGUI[] allTexts = GetComponentsInChildren<TextMeshProUGUI>();
        Image[] allImages = GetComponentsInChildren<Image>();
        
        foreach (var img in allImages)
        {
            if (img.gameObject.name.Contains("Icon"))
            {
                iconImage = img;
                break;
            }
        }
        
        Debug.Log($"[{gameObject.name}] Found {allTexts.Length} TextMeshPro components:");
        
        foreach (var text in allTexts)
        {
            string name = text.gameObject.name;
            Debug.Log($"  - {name}");
            
            if (name.Contains("Name") || name.Contains("Title"))
                upgradeNameText = text;
            else if (name.Contains("Description") || name.Contains("Desc"))
                descriptionText = text;
            else if (name.Contains("Label") || name.Contains("Status"))
                labelText = text;
            else if (name.Contains("Value") || name.Contains("Stats") || name.Contains("Number"))
                valuesText = text;
            else if (name.Contains("Cost") || name.Contains("Price"))
            {
                costText = text;
                Debug.Log($"  -> COST TEXT FOUND: {name}");
            }
        }
        
        if (costText == null)
        {
            Debug.LogWarning($"[{gameObject.name}] NO COST TEXT FOUND! Add a TextMeshPro child with 'Cost' or 'Price' in the name.");
        }
    }
    
    public void Setup(UpgradeData upgrade, int currentUpgradeLevel, UpgradeMode mode = UpgradeMode.LevelUp)
    {
        assignedUpgrade = upgrade;
        currentMode = mode;
        
        if (upgrade == null)
        {
            gameObject.SetActive(false);
            return;
        }
        
        currentLevel = currentUpgradeLevel;
        nextLevel = currentLevel + 1;
        
        if (currentLevel >= upgrade.maxLevel)
        {
            gameObject.SetActive(false);
            return;
        }
        
        gameObject.SetActive(true);
        
        // Calculate cost if in shop mode
        if (currentMode == UpgradeMode.Shop)
        {
            upgradeCost = upgrade.CalculateShopCostForLevel(nextLevel);
        }
        
        EnsureReferences();
        CheckAffordability();
        UpdateUI();
    }
    
    private void EnsureReferences()
    {
        if (upgradeNameText != null && descriptionText != null) return;
        
        if (button == null)
        {
            button = GetComponent<Button>();
        }
        
        TextMeshProUGUI[] allTexts = GetComponentsInChildren<TextMeshProUGUI>();
        Image[] allImages = GetComponentsInChildren<Image>();
        
        foreach (var img in allImages)
        {
            if (img.gameObject.name.Contains("Icon"))
            {
                iconImage = img;
                break;
            }
        }
        
        foreach (var text in allTexts)
        {
            string name = text.gameObject.name;
            
            if (name.Contains("Name") || name.Contains("Title"))
                upgradeNameText = text;
            else if (name.Contains("Description") || name.Contains("Desc"))
                descriptionText = text;
            else if (name.Contains("Label") || name.Contains("Status"))
                labelText = text;
            else if (name.Contains("Value") || name.Contains("Stats") || name.Contains("Number"))
                valuesText = text;
            else if (name.Contains("Cost") || name.Contains("Price"))
                costText = text;
        }
    }
    
    private void CheckAffordability()
    {
        // In LevelUp mode, always affordable
        if (currentMode == UpgradeMode.LevelUp)
        {
            canAfford = true;
            
            if (button != null)
            {
                button.interactable = true;
            }
            
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }
            
            return;
        }
        
        // In Shop mode, check if player has enough coins
        if (CurrencyManager.Instance == null)
        {
            canAfford = false;
            return;
        }
        
        canAfford = CurrencyManager.Instance.CurrentCoins >= upgradeCost;
        
        if (button != null)
        {
            button.interactable = canAfford;
        }
        
        if (canvasGroup != null)
        {
            canvasGroup.alpha = canAfford ? 1f : disabledAlpha;
        }
    }
    
    private void UpdateUI()
    {
        if (assignedUpgrade == null) return;
        
        if (upgradeNameText != null)
        {
            upgradeNameText.text = $"{assignedUpgrade.upgradeName} lvl.{nextLevel}";
        }
        
        if (descriptionText != null)
        {
            descriptionText.text = assignedUpgrade.description;
        }
        
        if (iconImage != null && assignedUpgrade.icon != null)
        {
            iconImage.sprite = assignedUpgrade.icon;
            iconImage.gameObject.SetActive(true);
        }
        else if (iconImage != null)
        {
            iconImage.gameObject.SetActive(false);
        }
        
        // Cost text (only in Shop mode)
        if (costText != null)
        {
            if (currentMode == UpgradeMode.Shop)
            {
                costText.gameObject.SetActive(true);
                
                // Formato: "Cost: X gold coins" o "Cost: X gold coin" si es 1
                string coinWord = upgradeCost == 1 ? "gold coin" : "gold coins";
                costText.text = $"Cost: {upgradeCost} {coinWord}";
                
                costText.color = canAfford ? new Color(1f, 0.84f, 0f) : new Color(1f, 0.3f, 0.3f);
            }
            else
            {
                costText.gameObject.SetActive(false);
            }
        }
        else if (currentMode == UpgradeMode.Shop)
        {
            Debug.LogWarning($"No Cost Text found for {assignedUpgrade.upgradeName}! Add a TextMeshPro child named 'CostText' or 'PriceText'");
        }
        
        if (labelText != null)
        {
            labelText.color = canAfford ? new Color(1f, 0.9f, 0.3f, 1f) : new Color(0.5f, 0.45f, 0.15f);
            
            string formattedValue = assignedUpgrade.GetFormattedValue(nextLevel);
            
            if (assignedUpgrade.upgradeType == UpgradeType.AttackSpeed)
            {
                labelText.text = $"{formattedValue}";
            }
            else
            {
                labelText.text = formattedValue;
            }
        }
        
        if (valuesText != null)
        {
            valuesText.color = canAfford ? new Color(0.4f, 1f, 0.5f) : new Color(0.2f, 0.5f, 0.25f);
            
            if (currentLevel == 0)
            {
                float baseValue = 0f;
                if (PlayerStatsManager.Instance != null)
                {
                    baseValue = PlayerStatsManager.Instance.GetBaseGameValue(assignedUpgrade.upgradeType);
                }
                
                float nextValue = assignedUpgrade.CalculateValueAtLevel(nextLevel);
                
                if (assignedUpgrade.upgradeType == UpgradeType.AttackSpeed)
                {
                    float baseCooldown = baseValue;
                    float baseFireRate = 1f / baseCooldown;
                    float currentFireRate = baseFireRate;
                    float newFireRate = baseFireRate * (1f + nextValue / 100f);
                    
                    valuesText.text = $"{currentFireRate:F2} → {newFireRate:F2}";
                }
                else if (assignedUpgrade.upgradeType == UpgradeType.MultiShot)
                {
                    int nextBullets = 3;
                    valuesText.text = $"0% → {nextValue:F1}% (+{nextBullets} bullets)";
                }
                else if (assignedUpgrade.upgradeType == UpgradeType.Knockback)
                {
                    float nextForce = 5f;
                    valuesText.text = $"0% → {nextValue:F1}% [{nextForce:F1}F]";
                }
                else if (assignedUpgrade.upgradeType == UpgradeType.ExplosiveShot)
                {
                    valuesText.text = $"0% → {nextValue:F1}%";
                }
                else if (assignedUpgrade.isPercentage)
                {
                    float finalValue = baseValue * (1f + nextValue / 100f);
                    
                    string format;
                    if (baseValue < 1f)
                        format = "F3";
                    else if (baseValue < 10f)
                        format = "F2";
                    else
                        format = "F1";
                    
                    valuesText.text = $"{baseValue.ToString(format)} → {finalValue.ToString(format)}";
                }
                else
                {
                    float finalValue = baseValue + nextValue;
                    valuesText.text = $"{baseValue:F0} → {finalValue:F0}";
                }
            }
            else
            {
                float baseValue = 0f;
                if (PlayerStatsManager.Instance != null)
                {
                    baseValue = PlayerStatsManager.Instance.GetBaseGameValue(assignedUpgrade.upgradeType);
                }
                
                float currentUpgradeValue = assignedUpgrade.CalculateValueAtLevel(currentLevel);
                float nextUpgradeValue = assignedUpgrade.CalculateValueAtLevel(nextLevel);
                
                if (assignedUpgrade.upgradeType == UpgradeType.AttackSpeed)
                {
                    float baseCooldown = baseValue;
                    float baseFireRate = 1f / baseCooldown;
                    float currentFireRate = baseFireRate * (1f + currentUpgradeValue / 100f);
                    float nextFireRate = baseFireRate * (1f + nextUpgradeValue / 100f);
                    
                    valuesText.text = $"{currentFireRate:F2} → {nextFireRate:F2}";
                }
                else if (assignedUpgrade.upgradeType == UpgradeType.MultiShot)
                {
                    int currentBullets = PlayerStatsManager.Instance.GetMultiShotExtraBullets();
                    int nextBullets = 3 + ((nextLevel - 1) / 4) * 3;
                    valuesText.text = $"{currentUpgradeValue:F1}% (+{currentBullets}) → {nextUpgradeValue:F1}% (+{nextBullets})";
                }
                else if (assignedUpgrade.upgradeType == UpgradeType.Knockback)
                {
                    float currentForce = PlayerStatsManager.Instance.GetKnockbackForce();
                    float nextForce = 5f + (nextLevel - 1) * 0.5f;
                    valuesText.text = $"{currentUpgradeValue:F1}% [{currentForce:F1}F] → {nextUpgradeValue:F1}% [{nextForce:F1}F]";
                }
                else if (assignedUpgrade.upgradeType == UpgradeType.ExplosiveShot)
                {
                    valuesText.text = $"{currentUpgradeValue:F1}% → {nextUpgradeValue:F1}%";
                }
                else if (assignedUpgrade.isPercentage)
                {
                    float currentFinalValue = baseValue * (1f + currentUpgradeValue / 100f);
                    float nextFinalValue = baseValue * (1f + nextUpgradeValue / 100f);
                    
                    string format;
                    if (baseValue < 1f)
                        format = "F3";
                    else if (baseValue < 10f)
                        format = "F2";
                    else
                        format = "F1";
                    
                    valuesText.text = $"{currentFinalValue.ToString(format)} → {nextFinalValue.ToString(format)}";
                }
                else
                {
                    float currentFinalValue = baseValue + currentUpgradeValue;
                    float nextFinalValue = baseValue + nextUpgradeValue;
                    valuesText.text = $"{currentFinalValue:F0} → {nextFinalValue:F0}";
                }
            }
        }
        
        if (nextLevel >= assignedUpgrade.maxLevel && upgradeNameText != null)
        {
            upgradeNameText.color = new Color(1f, 0.84f, 0f);
        }
    }
    
    private void OnUpgradeSelected()
    {
        if (assignedUpgrade == null) return;
        
        LevelUpManager levelUpManager = FindFirstObjectByType<LevelUpManager>();
        
        // Shop mode: verify and spend coins
        if (currentMode == UpgradeMode.Shop)
        {
            if (!canAfford)
            {
                Debug.Log("Not enough coins!");
                return;
            }
            
            if (CurrencyManager.Instance != null)
            {
                bool success = CurrencyManager.Instance.SpendCoins(upgradeCost);
                
                if (!success)
                {
                    Debug.LogError("Failed to spend coins!");
                    return;
                }
            }
        }
        
        if (button != null)
        {
            button.interactable = false;
        }
        
        if (PlayerStatsManager.Instance != null)
        {
            PlayerStatsManager.Instance.ApplyUpgrade(assignedUpgrade);
        }
        
        if (levelUpManager != null)
        {
            levelUpManager.OnUpgradeChosen();
        }
    }
    
    public void RefreshAffordability()
    {
        CheckAffordability();
        
        // Update text colors
        if (costText != null && currentMode == UpgradeMode.Shop)
        {
            costText.color = canAfford ? new Color(1f, 0.84f, 0f) : new Color(1f, 0.3f, 0.3f);
        }
        
        if (labelText != null)
        {
            labelText.color = canAfford ? new Color(1f, 0.9f, 0.3f, 1f) : new Color(0.5f, 0.45f, 0.15f);
        }
        
        if (valuesText != null)
        {
            valuesText.color = canAfford ? new Color(0.4f, 1f, 0.5f) : new Color(0.2f, 0.5f, 0.25f);
        }
    }
}
