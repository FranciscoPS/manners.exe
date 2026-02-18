using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeButton : MonoBehaviour
{
    private TextMeshProUGUI upgradeNameText;
    private TextMeshProUGUI descriptionText;
    private TextMeshProUGUI labelText;
    private TextMeshProUGUI valuesText;
    private Image iconImage;
    private Button button;
    
    private UpgradeData assignedUpgrade;
    private int currentLevel;
    private int nextLevel;
    
    private void Awake()
    {
        button = GetComponent<Button>();
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
        }
    }
    
    public void Setup(UpgradeData upgrade, int currentUpgradeLevel)
    {
        assignedUpgrade = upgrade;
        
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
        
        if (button != null)
        {
            button.interactable = true;
        }
        
        EnsureReferences();
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
        
        if (labelText != null)
        {
            labelText.color = new Color(1f, 0.9f, 0.3f, 1f);
            
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
            valuesText.color = new Color(0.4f, 1f, 0.5f, 1f);
            
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
                else if (assignedUpgrade.upgradeType == UpgradeType.ExplosiveShot || 
                         assignedUpgrade.upgradeType == UpgradeType.Knockback)
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
                else if (assignedUpgrade.upgradeType == UpgradeType.ExplosiveShot || 
                         assignedUpgrade.upgradeType == UpgradeType.Knockback)
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
        
        if (button != null)
        {
            button.interactable = false;
        }
        
        if (PlayerStatsManager.Instance != null)
        {
            PlayerStatsManager.Instance.ApplyUpgrade(assignedUpgrade);
        }
        
        LevelUpManager levelUpManager = FindFirstObjectByType<LevelUpManager>();
        if (levelUpManager != null)
        {
            levelUpManager.OnUpgradeChosen();
        }
    }
}
