using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeButton : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI upgradeNameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI currentValueText;
    [SerializeField] private TextMeshProUGUI nextValueText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Button button;
    
    [Header("Visual Settings")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = new Color(1f, 1f, 0.7f);
    [SerializeField] private Color maxLevelColor = new Color(1f, 0.84f, 0f); // Gold
    
    private UpgradeData assignedUpgrade;
    private int currentLevel;
    private int nextLevel;
    
    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();
        
        if (button != null)
            button.onClick.AddListener(OnUpgradeSelected);
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
        UpdateUI();
    }
    
    private void UpdateUI()
    {
        if (assignedUpgrade == null) return;
        
        // Nombre del upgrade
        if (upgradeNameText != null)
        {
            upgradeNameText.text = assignedUpgrade.upgradeName;
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
        
        // Nivel actual (si es 0, es "NEW!")
        if (levelText != null)
        {
            if (currentLevel == 0)
            {
                levelText.text = "NEW!";
                levelText.color = Color.green;
            }
            else
            {
                levelText.text = $"Lvl {currentLevel} → {nextLevel}";
                levelText.color = normalColor;
            }
        }
        
        // Valor actual
        if (currentValueText != null)
        {
            if (currentLevel == 0)
            {
                currentValueText.text = "—";
            }
            else
            {
                currentValueText.text = assignedUpgrade.GetFormattedValue(currentLevel);
            }
        }
        
        // Valor siguiente (con flecha)
        if (nextValueText != null)
        {
            nextValueText.text = $"→ {assignedUpgrade.GetFormattedValue(nextLevel)}";
            nextValueText.color = Color.green;
        }
        
        // Color especial si va a ser max level
        if (nextLevel >= assignedUpgrade.maxLevel && upgradeNameText != null)
        {
            upgradeNameText.color = maxLevelColor;
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
    
    /// <summary>
    /// Efecto visual al pasar el ratón (opcional)
    /// </summary>
    public void OnPointerEnter()
    {
        if (upgradeNameText != null && currentLevel < assignedUpgrade.maxLevel - 1)
        {
            upgradeNameText.color = hoverColor;
        }
    }
    
    public void OnPointerExit()
    {
        if (upgradeNameText != null && currentLevel < assignedUpgrade.maxLevel - 1)
        {
            upgradeNameText.color = normalColor;
        }
    }
}
