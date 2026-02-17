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
            button.onClick.AddListener(OnUpgradeSelected);
        
        // Buscar componentes en hijos - más flexible que Find()
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
    
    /// <summary>
    /// Configura el botón con los datos del upgrade
    /// </summary>
    public void Setup(UpgradeData upgrade, int playerCurrentLevel)
    {
        assignedUpgrade = upgrade;
        
        if (upgrade == null)
        {
            gameObject.SetActive(false);
            return;
        }
        
        currentLevel = playerCurrentLevel;
        nextLevel = currentLevel + 1;
        
        // Si ya está al máximo, no mostrar
        if (currentLevel >= upgrade.maxLevel)
        {
            gameObject.SetActive(false);
            return;
        }
        
        gameObject.SetActive(true);
        
        // Asegurarse de tener referencias antes de actualizar UI
        EnsureReferences();
        UpdateUI();
    }
    
    private void EnsureReferences()
    {
        // Si ya las tenemos, salir
        if (upgradeNameText != null && descriptionText != null) return;
        
        if (button == null)
        {
            button = GetComponent<Button>();
            if (button != null)
                button.onClick.AddListener(OnUpgradeSelected);
        }
        
        // Buscar componentes en hijos
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
        
        // Nombre del upgrade
        if (upgradeNameText != null)
        {
            upgradeNameText.text = $"{assignedUpgrade.upgradeName} lvl.{nextLevel}";
        }
        
        // Descripción
        if (descriptionText != null)
        {
            descriptionText.text = assignedUpgrade.description;
        }
        
        // Icono
        if (iconImage != null && assignedUpgrade.icon != null)
        {
            iconImage.sprite = assignedUpgrade.icon;
            iconImage.gameObject.SetActive(true);
        }
        else if (iconImage != null)
        {
            iconImage.gameObject.SetActive(false);
        }
        
        // LabelText: Muestra el porcentaje formateado (ej: -8%, +15%)
        if (labelText != null)
        {
            labelText.color = new Color(1f, 0.9f, 0.3f, 1f); // Yellow
            
            string formattedValue = assignedUpgrade.GetFormattedValue(nextLevel);
            labelText.text = formattedValue;
        }
        
        // ValuesText: Muestra el valor numérico absoluto (ej: 15 → 18)
        if (valuesText != null)
        {
            valuesText.color = new Color(0.4f, 1f, 0.5f, 1f); // Green
            
            if (currentLevel == 0)
            {
                // Primera mejora: obtener valor base del juego
                float baseValue = 0f;
                if (PlayerStatsManager.Instance != null)
                {
                    baseValue = PlayerStatsManager.Instance.GetBaseGameValue(assignedUpgrade.upgradeType);
                }
                
                float nextValue = assignedUpgrade.CalculateValueAtLevel(nextLevel);
                
                // Si es porcentaje, calculamos el valor real que tendrá
                if (assignedUpgrade.isPercentage)
                {
                    float finalValue;
                    if (assignedUpgrade.isReduction)
                    {
                        // Para reducciones (ej: -8% cooldown): 1.0 * (1 - 0.08) = 0.92
                        finalValue = baseValue * (1f - nextValue / 100f);
                    }
                    else
                    {
                        // Para aumentos (ej: +15% daño): 10 * (1 + 0.15) = 11.5
                        finalValue = baseValue * (1f + nextValue / 100f);
                    }
                    
                    // Usar más decimales para valores pequeños (< 10)
                    string format = baseValue < 10f ? "F2" : "F1";
                    valuesText.text = $"{baseValue.ToString(format)} → {finalValue.ToString(format)}";
                }
                else
                {
                    // Para valores absolutos (como +20 HP)
                    float finalValue = assignedUpgrade.isReduction 
                        ? baseValue - nextValue 
                        : baseValue + nextValue;
                    valuesText.text = $"{baseValue:F0} → {finalValue:F0}";
                }
            }
            else
            {
                // Para niveles superiores, solo mostramos los valores del upgrade
                float currentValue = assignedUpgrade.CalculateValueAtLevel(currentLevel);
                float nextValue = assignedUpgrade.CalculateValueAtLevel(nextLevel);
                valuesText.text = $"{currentValue:F1} → {nextValue:F1}";
            }
        }
        
        // Color especial si va a ser max level
        if (nextLevel >= assignedUpgrade.maxLevel && upgradeNameText != null)
        {
            upgradeNameText.color = new Color(1f, 0.84f, 0f); // Gold
        }
    }
    
    private void OnUpgradeSelected()
    {
        if (assignedUpgrade == null) return;
        
        // Aplicar upgrade a través del PlayerStatsManager
        if (PlayerStatsManager.Instance != null)
        {
            PlayerStatsManager.Instance.ApplyUpgrade(assignedUpgrade);
        }
        
        // Notificar al LevelUpManager que cerramos
        LevelUpManager levelUpManager = FindFirstObjectByType<LevelUpManager>();
        if (levelUpManager != null)
        {
            levelUpManager.OnUpgradeChosen();
        }
    }
}
