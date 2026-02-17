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
            // Asegurarse de que solo haya UN listener
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnUpgradeSelected);
        }
        
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
        
        // Si ya está al máximo, no mostrar
        if (currentLevel >= upgrade.maxLevel)
        {
            gameObject.SetActive(false);
            return;
        }
        
        gameObject.SetActive(true);
        
        // Re-habilitar el botón
        if (button != null)
        {
            button.interactable = true;
        }
        
        // Asegurarse de tener referencias antes de actualizar UI
        EnsureReferences();
        UpdateUI();
    }
    
    private void EnsureReferences()
    {
        // Si ya las tenemos, salir
        if (upgradeNameText != null && descriptionText != null) return;
        
        // Solo obtener el botón si aún no lo tenemos (no agregar listener aquí)
        if (button == null)
        {
            button = GetComponent<Button>();
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
            // Mostrar el nivel que vas a obtener (número de veces comprado + 1)
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
                    
                    // Usar formato apropiado según el tamaño del valor
                    string format;
                    if (baseValue < 1f)
                        format = "F3"; // 3 decimales para valores muy pequeños (0.001 - 0.999)
                    else if (baseValue < 10f)
                        format = "F2"; // 2 decimales para valores pequeños (1.00 - 9.99)
                    else
                        format = "F1"; // 1 decimal para valores grandes (10.0+)
                    
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
                // Para niveles superiores, calcular el valor real del stat
                float baseValue = 0f;
                if (PlayerStatsManager.Instance != null)
                {
                    baseValue = PlayerStatsManager.Instance.GetBaseGameValue(assignedUpgrade.upgradeType);
                }
                
                float currentUpgradeValue = assignedUpgrade.CalculateValueAtLevel(currentLevel);
                float nextUpgradeValue = assignedUpgrade.CalculateValueAtLevel(nextLevel);
                
                if (assignedUpgrade.isPercentage)
                {
                    float currentFinalValue, nextFinalValue;
                    
                    if (assignedUpgrade.isReduction)
                    {
                        // Reducción: base × (1 - porcentaje/100)
                        currentFinalValue = baseValue * (1f - currentUpgradeValue / 100f);
                        nextFinalValue = baseValue * (1f - nextUpgradeValue / 100f);
                    }
                    else
                    {
                        // Aumento: base × (1 + porcentaje/100)
                        currentFinalValue = baseValue * (1f + currentUpgradeValue / 100f);
                        nextFinalValue = baseValue * (1f + nextUpgradeValue / 100f);
                    }
                    
                    // Usar formato apropiado según el tamaño del valor
                    string format;
                    if (baseValue < 1f)
                        format = "F3"; // 3 decimales para valores muy pequeños (0.001 - 0.999)
                    else if (baseValue < 10f)
                        format = "F2"; // 2 decimales para valores pequeños (1.00 - 9.99)
                    else
                        format = "F1"; // 1 decimal para valores grandes (10.0+)
                    
                    valuesText.text = $"{currentFinalValue.ToString(format)} → {nextFinalValue.ToString(format)}";
                }
                else
                {
                    // Valores absolutos
                    float currentFinalValue = assignedUpgrade.isReduction 
                        ? baseValue - currentUpgradeValue 
                        : baseValue + currentUpgradeValue;
                    float nextFinalValue = assignedUpgrade.isReduction 
                        ? baseValue - nextUpgradeValue 
                        : baseValue + nextUpgradeValue;
                    valuesText.text = $"{currentFinalValue:F0} → {nextFinalValue:F0}";
                }
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
        
        // Deshabilitar el botón inmediatamente para evitar doble clic
        if (button != null)
        {
            button.interactable = false;
        }
        
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
