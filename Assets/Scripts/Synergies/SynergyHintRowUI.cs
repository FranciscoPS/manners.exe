using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SynergyHintRowUI : MonoBehaviour
{
    [Header("Sinergia representada por esta fila")]
    [SerializeField] private SynergyData synergy;

    [Header("Requisito A")]
    [SerializeField] private Image iconA;
    [SerializeField] private TextMeshProUGUI unknownTextA;
    [SerializeField] private TextMeshProUGUI levelTextA;

    [Header("Requisito B")]
    [SerializeField] private Image iconB;
    [SerializeField] private TextMeshProUGUI unknownTextB;
    [SerializeField] private TextMeshProUGUI levelTextB;

    [Header("Resultado")]
    [SerializeField] private Image iconResult;
    [SerializeField] private TextMeshProUGUI unknownTextResult;

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (synergy == null) return;

        UpgradeData upgradeA = UpgradeDatabase.Instance != null ? UpgradeDatabase.Instance.GetUpgradeData(synergy.requiredUpgradeA) : null;
        UpgradeData upgradeB = UpgradeDatabase.Instance != null ? UpgradeDatabase.Instance.GetUpgradeData(synergy.requiredUpgradeB) : null;

        int levelA = PlayerStatsManager.Instance != null ? PlayerStatsManager.Instance.GetUpgradeLevel(synergy.requiredUpgradeA) : 0;
        int levelB = PlayerStatsManager.Instance != null ? PlayerStatsManager.Instance.GetUpgradeLevel(synergy.requiredUpgradeB) : 0;

        ApplySlot(iconA, unknownTextA, levelTextA, upgradeA != null ? upgradeA.icon : null, levelA > 0, synergy.requiredLevelA);
        ApplySlot(iconB, unknownTextB, levelTextB, upgradeB != null ? upgradeB.icon : null, levelB > 0, synergy.requiredLevelB);

        bool unlocked = levelA >= synergy.requiredLevelA && levelB >= synergy.requiredLevelB;
        ApplySlot(iconResult, unknownTextResult, null, synergy.icon, unlocked, 0);
    }

    private void ApplySlot(Image icon, TextMeshProUGUI unknownText, TextMeshProUGUI levelText, Sprite discoveredIcon, bool discovered, int requiredLevel)
    {
        bool reveal = discovered && discoveredIcon != null;

        if (icon != null)
        {
            icon.sprite = reveal ? discoveredIcon : null;
            icon.enabled = reveal;
        }

        if (unknownText != null)
            unknownText.gameObject.SetActive(!reveal);

        if (levelText != null)
        {
            levelText.gameObject.SetActive(reveal);
            levelText.text = reveal ? $"Nv. {requiredLevel}" : "";
        }
    }
}
