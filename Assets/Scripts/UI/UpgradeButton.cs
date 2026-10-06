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
    [Header("Textos editables por idioma")]
    [SerializeField] private LocalizedString itemLabel = new LocalizedString("SPECIAL ITEM!", "¡ÍTEM ESPECIAL!");
    [SerializeField] private LocalizedString upgradeLevel = new LocalizedString("{0} Lv.{1}", "{0} lvl.{1}");
    [SerializeField] private LocalizedString costLabel = new LocalizedString("Cost: {0} {1}", "Costo: {0} {1}");
    [SerializeField] private LocalizedString coinSingular = new LocalizedString("coin", "moneda");
    [SerializeField] private LocalizedString coinPlural = new LocalizedString("coins", "monedas");
    [SerializeField] private LocalizedString multiFirst = new LocalizedString("0% → {0:F1}%\n<size=62%>+{1} bullets</size>", "0% → {0:F1}%\n<size=62%>+{1} balas</size>");
    [SerializeField] private LocalizedString chainFirst = new LocalizedString("0% → {0:F1}%\n<size=62%>pushes {1} enemies</size>", "0% → {0:F1}%\n<size=62%>empuja {1} enem.</size>");
    [SerializeField] private LocalizedString multiNext = new LocalizedString("{0:F1}% → {1:F1}%\n<size=62%>+{2} → +{3} bullets</size>", "{0:F1}% → {1:F1}%\n<size=62%>+{2} → +{3} balas</size>");
    [SerializeField] private LocalizedString chainNext = new LocalizedString("{0:F1}% → {1:F1}%\n<size=62%>pushes {2} → {3} enemies</size>", "{0:F1}% → {1:F1}%\n<size=62%>empuja {2} → {3} enem.</size>");

    [SerializeField] private TextMeshProUGUI upgradeNameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private TextMeshProUGUI valuesText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Button button;
    [SerializeField] private CanvasGroup canvasGroup;
    private RectTransform rectTransform;

    [Header("Disabled Settings")]
    [SerializeField] private float disabledAlpha = 0.5f;

    [Header("Juice")]
    [Tooltip("Duración de la animación de aparición de la card (rebote de chica a grande).")]
    [SerializeField] private float introDuration = 0.36f;
    [Tooltip("Fuerza del rebote al aparecer. Valores del estilo DOTween OutBack: más alto = más exagerado.")]
    [SerializeField] private float introOvershoot = 1.3f;

    [Header("Component References")]
    [Tooltip("Casilla de fondo del icono: se oculta junto con el icono cuando la mejora no tiene imagen.")]
    [SerializeField] private GameObject iconBackdrop;
    [SerializeField] private HoldToSelectButton holdToSelectButton;
    [SerializeField] private PremiumUpgradeVisuals premiumVisuals;
    private PurchaseEffectFeedback purchaseEffect;

    private UpgradeData assignedUpgrade;
    private ChestItemData assignedChestItem;
    private int currentLevel;
    private int nextLevel;
    private UpgradeMode currentMode = UpgradeMode.LevelUp;
    private int upgradeCost = 0;
    private bool canAfford = true;

    private static Color Accent(Color fallback, System.Func<UIStyle, Color> pick)
    {
        UIStyle style = UIStyle.Instance;
        return style != null ? pick(style) : fallback;
    }

    private static Color Dimmed(Color color)
    {
        return new Color(color.r * 0.5f, color.g * 0.5f, color.b * 0.5f, color.a);
    }

    private static Color CostColor(bool affordable)
    {
        return affordable ? Accent(new Color(1f, 0.84f, 0f), s => s.yellow) : Accent(new Color(1f, 0.3f, 0.3f), s => s.danger);
    }

    private static Color LabelColor(bool affordable)
    {
        Color color = Accent(new Color(1f, 0.9f, 0.3f, 1f), s => s.yellow);
        return affordable ? color : Dimmed(color);
    }

    private static Color ValuesColor(bool affordable)
    {
        Color color = Accent(new Color(0.4f, 1f, 0.5f), s => s.good);
        return affordable ? color : Dimmed(color);
    }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        if (canvasGroup == null)
        {
            Debug.LogError("Asigna CanvasGroup en la tarjeta.", this);
        }

        if (premiumVisuals == null)
        {
            Debug.LogError("Asigna PremiumUpgradeVisuals en la tarjeta.", this);
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

        EnsureCostTextOpaque();
        CheckAffordability();
        UpdateUI();

        if (premiumVisuals != null && assignedUpgrade != null)
        {
            premiumVisuals.SetPremium(assignedUpgrade.isPremium, currentMode);
        }

        if (holdToSelectButton != null && assignedUpgrade != null)
        {
            holdToSelectButton.SetPremiumStyle(assignedUpgrade.isPremium);
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

    public void PlayOutroAnimation(float duration, float delay = 0f)
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        rectTransform.PopOut(duration, delay: delay);
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

        if (holdToSelectButton != null)
        {
            holdToSelectButton.SetPremiumStyle(true);
        }
    }

    private void UpdateChestUI()
    {
        if (assignedChestItem == null) return;

        if (upgradeNameText != null)
        {
            upgradeNameText.text = assignedChestItem.itemName;
            upgradeNameText.color = Accent(Color.white, s => s.paper);
        }

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

        if (iconBackdrop != null && iconImage != null)
            iconBackdrop.SetActive(iconImage.gameObject.activeSelf);

        if (costText != null)
            costText.gameObject.SetActive(false);

        if (valuesText != null)
            valuesText.gameObject.SetActive(false);

        if (labelText != null)
        {
            labelText.text = itemLabel.Value;
            labelText.color = assignedChestItem.accentColor;
        }
    }

    private void EnsureCostTextOpaque()
    {
        if (costText == null) return;
        CanvasGroup cg = costText.GetComponent<CanvasGroup>();
        if (cg == null) return;
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
            upgradeNameText.text = upgradeLevel.Format(assignedUpgrade.upgradeName, nextLevel);
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

        if (iconBackdrop != null && iconImage != null)
            iconBackdrop.SetActive(iconImage.gameObject.activeSelf);

        if (costText != null)
        {
            if (currentMode == UpgradeMode.Shop)
            {
                costText.gameObject.SetActive(true);

                string coinWord = upgradeCost == 1 ? coinSingular.Value : coinPlural.Value;
                costText.text = costLabel.Format(upgradeCost, coinWord);

                costText.color = CostColor(canAfford);
            }
            else
            {
                costText.gameObject.SetActive(false);
            }
        }

        if (labelText != null)
        {
            labelText.color = LabelColor(canAfford);

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
            valuesText.color = ValuesColor(canAfford);

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
                    valuesText.text = multiFirst.Format(nextValue, nextBullets);
                }
                else if (assignedUpgrade.upgradeType == UpgradeType.Knockback)
                {
                    int nextEnemies = PlayerStatsManager.Instance.GetKnockbackChainJumpsForLevel(nextLevel) + 1;
                    valuesText.text = chainFirst.Format(nextValue, nextEnemies);
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
                    valuesText.text = multiNext.Format(currentUpgradeValue, nextUpgradeValue, currentBullets, nextBullets);
                }
                else if (assignedUpgrade.upgradeType == UpgradeType.Knockback)
                {
                    int currentEnemies = PlayerStatsManager.Instance.GetKnockbackChainJumpsForLevel(currentLevel) + 1;
                    int nextEnemies = PlayerStatsManager.Instance.GetKnockbackChainJumpsForLevel(nextLevel) + 1;
                    valuesText.text = chainNext.Format(currentUpgradeValue, nextUpgradeValue, currentEnemies, nextEnemies);
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

        if (upgradeNameText != null)
        {
            bool reachesMax = nextLevel >= assignedUpgrade.maxLevel;
            upgradeNameText.color = reachesMax
                ? Accent(new Color(1f, 0.84f, 0f), s => s.yellow)
                : Accent(Color.white, s => s.paper);
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
            costText.color = CostColor(canAfford);
        }

        if (labelText != null)
        {
            labelText.color = LabelColor(canAfford);
        }

        if (valuesText != null)
        {
            valuesText.color = ValuesColor(canAfford);
        }
    }
    private void OnEnable() => GameLocalization.LanguageChanged += RefreshLanguage;
    private void OnDisable() => GameLocalization.LanguageChanged -= RefreshLanguage;
    private void RefreshLanguage()
    {
        if (assignedChestItem != null) UpdateChestUI();
        else UpdateUI();
    }
}
