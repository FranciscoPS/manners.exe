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

        int reachedA = ReachedLevel(synergy.requiredUpgradeA);
        int reachedB = ReachedLevel(synergy.requiredUpgradeB);

        ApplySlot(iconA, unknownTextA, levelTextA, upgradeA != null ? upgradeA.icon : null, reachedA > 0, reachedA, synergy.requiredLevelA);
        ApplySlot(iconB, unknownTextB, levelTextB, upgradeB != null ? upgradeB.icon : null, reachedB > 0, reachedB, synergy.requiredLevelB);

        bool unlocked = SynergyDiscovery.IsSynergyUnlocked(synergy)
            || (SynergyManager.Instance != null && SynergyManager.Instance.IsSynergyActive(synergy))
            || (reachedA >= synergy.requiredLevelA && reachedB >= synergy.requiredLevelB);

        ApplySlot(iconResult, unknownTextResult, null, synergy.icon, unlocked, 0, 0);
    }

    private static int ReachedLevel(UpgradeType type)
    {
        int current = PlayerStatsManager.Instance != null ? PlayerStatsManager.Instance.GetUpgradeLevel(type) : 0;
        return Mathf.Max(current, SynergyDiscovery.GetMaxUpgradeLevel(type));
    }

    private void ApplySlot(Image icon, TextMeshProUGUI unknownText, TextMeshProUGUI levelText, Sprite discoveredIcon, bool discovered, int reachedLevel, int requiredLevel)
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
            levelText.text = reveal ? $"Nv. {Mathf.Min(reachedLevel, requiredLevel)}/{requiredLevel}" : "";
        }
    }
}
