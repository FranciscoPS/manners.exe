using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum UpgradeMode
{
    LevelUp,
    Shop,
    Chest
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
    private RectTransform rectTransform;

    [Header("Disabled Settings")]
    [SerializeField] private float disabledAlpha = 0.5f;

    [Header("Juice")]
    [Tooltip("Duración de la animación de aparición de la card (rebote de chica a grande).")]
    [SerializeField] private float introDuration = 0.32f;
    [Tooltip("Fuerza del rebote al aparecer. Valores del estilo DOTween OutBack: más alto = más exagerado.")]
    [SerializeField] private float introOvershoot = 0.9f;

    [Header("Component References")]
    private HoldToSelectButton holdToSelectButton;
    private PremiumUpgradeVisuals premiumVisuals;
    private PurchaseEffectFeedback purchaseEffect;

    private UpgradeData assignedUpgrade;
    private ChestItemData assignedChestItem;
    private int currentLevel;
    private int nextLevel;
    private UpgradeMode currentMode = UpgradeMode.LevelUp;
    private int upgradeCost = 0;
    private bool canAfford = true;

    private void Awake()
    {
        button = GetComponent<Button>();
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        holdToSelectButton = GetComponent<HoldToSelectButton>();
        premiumVisuals = GetComponent<PremiumUpgradeVisuals>();

        if (premiumVisuals == null)
        {
            premiumVisuals = gameObject.AddComponent<PremiumUpgradeVisuals>();
        }

        purchaseEffect = GetComponent<PurchaseEffectFeedback>();

        if (holdToSelectButton != null)
        {
            holdToSelectButton.OnHoldComplete = OnUpgradeSelected;
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
            }
        }
        else
        {
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OnUpgradeSelected);
            }
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

        if (currentMode == UpgradeMode.Shop)
        {
            upgradeCost = upgrade.CalculateShopCostForLevel(nextLevel);
        }

        EnsureReferences();
        EnsureCostTextOpaque();
        CheckAffordability();
        UpdateUI();

        if (premiumVisuals != null && assignedUpgrade != null)
        {
            premiumVisuals.SetPremium(assignedUpgrade.isPremium, currentMode);
        }
    }

    public void PlayIntroAnimation(float delay = 0f)
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        rectTransform.PopIn(introDuration, introOvershoot, delay);
    }

    public void SetupChest(ChestItemData item)
    {
        assignedChestItem = item;
        assignedUpgrade = null;
        currentMode = UpgradeMode.Chest;

        if (item == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        EnsureReferences();

        canAfford = true;
        upgradeCost = 0;

        if (button != null) button.interactable = true;
        if (canvasGroup != null) canvasGroup.alpha = 1f;
        if (holdToSelectButton != null) holdToSelectButton.SetInteractable(true);

        UpdateChestUI();

        if (premiumVisuals != null)
        {
            premiumVisuals.SetPremium(true, currentMode);
        }
    }

    private void UpdateChestUI()
    {
        if (assignedChestItem == null) return;

        if (upgradeNameText != null)
            upgradeNameText.text = assignedChestItem.itemName;

        if (descriptionText != null)
            descriptionText.text = assignedChestItem.description;

        if (iconImage != null && assignedChestItem.icon != null)
        {
            iconImage.sprite = assignedChestItem.icon;
            iconImage.gameObject.SetActive(true);
        }
        else if (iconImage != null)
        {
            iconImage.gameObject.SetActive(false);
        }

        if (costText != null)
            costText.gameObject.SetActive(false);

        if (valuesText != null)
            valuesText.gameObject.SetActive(false);

        if (labelText != null)
        {
            labelText.text = "¡ÍTEM ESPECIAL!";
            labelText.color = assignedChestItem.accentColor;
        }
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

    private void EnsureCostTextOpaque()
    {
        if (costText == null) return;
        CanvasGroup cg = costText.GetComponent<CanvasGroup>();
        if (cg == null) cg = costText.gameObject.AddComponent<CanvasGroup>();
        cg.ignoreParentGroups = true;
        cg.alpha = 1f;
    }

    private void CheckAffordability()
    {

        if (currentMode == UpgradeMode.LevelUp || currentMode == UpgradeMode.Chest)
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

            if (holdToSelectButton != null)
            {
                holdToSelectButton.SetInteractable(true);
            }

            return;
        }

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

        if (holdToSelectButton != null)
        {
            holdToSelectButton.SetInteractable(canAfford);
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

        if (costText != null)
        {
            if (currentMode == UpgradeMode.Shop)
            {
                costText.gameObject.SetActive(true);

                string coinWord = upgradeCost == 1 ? "moneda" : "monedas";
                costText.text = $"Costo: {upgradeCost} {coinWord}";

                costText.color = canAfford ? new Color(1f, 0.84f, 0f) : new Color(1f, 0.3f, 0.3f);
            }
            else
            {
                costText.gameObject.SetActive(false);
            }
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

            valuesText.gameObject.SetActive(true);
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
                    valuesText.text = $"0% → {nextValue:F1}% (+{nextBullets} balas)";
                }
                else if (assignedUpgrade.upgradeType == UpgradeType.Knockback)
                {
                    int nextEnemies = PlayerStatsManager.Instance.GetKnockbackChainJumpsForLevel(nextLevel) + 1;
                    valuesText.text = $"0% → {nextValue:F1}% · empuja {nextEnemies} enem.";
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
                else if (assignedUpgrade.upgradeType == UpgradeType.HealOnLevelUp)
                {
                    float currentHP = baseValue;
                    float afterHeal = PlayerStatsManager.Instance != null
                        ? Mathf.Min(currentHP + nextValue, FindFirstObjectByType<PlayerHealth>()?.MaxHealth ?? currentHP + nextValue)
                        : currentHP + nextValue;
                    valuesText.text = $"{currentHP:F0} → {afterHeal:F0} HP";
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
                    int currentEnemies = PlayerStatsManager.Instance.GetKnockbackChainJumpsForLevel(currentLevel) + 1;
                    int nextEnemies = PlayerStatsManager.Instance.GetKnockbackChainJumpsForLevel(nextLevel) + 1;
                    valuesText.text = $"{currentUpgradeValue:F1}% [{currentEnemies} enem.] → {nextUpgradeValue:F1}% [{nextEnemies} enem.]";
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
                else if (assignedUpgrade.upgradeType == UpgradeType.HealOnLevelUp)
                {
                    PlayerHealth ph = FindFirstObjectByType<PlayerHealth>();
                    float curHP  = ph != null ? ph.CurrentHealth : 0f;
                    float maxHP  = ph != null ? ph.MaxHealth : curHP + nextUpgradeValue;
                    float healed = Mathf.Min(curHP + nextUpgradeValue, maxHP);
                    valuesText.text = $"{curHP:F0} → {healed:F0} HP";
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
        if (currentMode == UpgradeMode.Chest)
        {
            if (button != null)
            {
                button.interactable = false;
            }

            ChestItemProvider.ApplyEffect(assignedChestItem);

            if (purchaseEffect != null)
            {
                purchaseEffect.PlayPurchaseEffect();
            }

            LevelUpManager chestManager = FindFirstObjectByType<LevelUpManager>();
            if (chestManager != null)
            {
                chestManager.OnUpgradeChosen();
            }
            return;
        }

        if (assignedUpgrade == null) return;

        LevelUpManager levelUpManager = FindFirstObjectByType<LevelUpManager>();

        if (currentMode == UpgradeMode.Shop)
        {
            if (!canAfford)
                return;

            if (CurrencyManager.Instance != null)
            {
                bool success = CurrencyManager.Instance.SpendCoins(upgradeCost);

                if (!success)
                    return;
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

        if (purchaseEffect != null)
        {
            purchaseEffect.PlayPurchaseEffect();
        }

        if (levelUpManager != null)
        {
            levelUpManager.OnUpgradeChosen();
        }
    }

    public void RefreshAffordability()
    {
        CheckAffordability();

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
