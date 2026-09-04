using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class SynergyHintRowUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private const string UndiscoveredTooltipTitle = "??? Sinergia sin descubrir";
    private const string UndiscoveredTooltipBody = "Sube al nivel requerido las dos mejoras de esta fila para revelar qué hace.";

    [Header("Sinergia representada por esta fila")]
    [SerializeField] private SynergyData synergy;

    [Header("Requisito A")]
    [SerializeField] private Image iconA;
    [SerializeField] private Image backdropA;
    [SerializeField] private TextMeshProUGUI unknownTextA;
    [SerializeField] private TextMeshProUGUI levelTextA;

    [Header("Requisito B")]
    [SerializeField] private Image iconB;
    [SerializeField] private Image backdropB;
    [SerializeField] private TextMeshProUGUI unknownTextB;
    [SerializeField] private TextMeshProUGUI levelTextB;

    [Header("Resultado")]
    [SerializeField] private Image iconResult;
    [SerializeField] private Image backdropResult;
    [SerializeField] private TextMeshProUGUI unknownTextResult;
    [Tooltip("Efecto premium (foil holográfico + pulso) que se enciende cuando la sinergia está desbloqueada.")]
    [SerializeField] private PremiumUpgradeVisuals resultVisuals;

    private bool isUnlocked;
    private Canvas canvas;

    private void OnEnable()
    {
        Refresh();
    }

    private void OnDisable()
    {
        if (resultVisuals != null)
            resultVisuals.SetPremium(false);

        HoverTooltipUI.Hide();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SynergyData data = ResolveSynergy();
        if (data == null) return;

        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        if (isUnlocked)
            HoverTooltipUI.Show(canvas, data.synergyName, data.description);
        else
            HoverTooltipUI.Show(canvas, UndiscoveredTooltipTitle, UndiscoveredTooltipBody);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HoverTooltipUI.Hide();
    }

    public void Refresh()
    {
        SynergyData data = ResolveSynergy();
        if (data == null) return;

        UpgradeData upgradeA = UpgradeDatabase.Instance != null ? UpgradeDatabase.Instance.GetUpgradeData(data.requiredUpgradeA) : null;
        UpgradeData upgradeB = UpgradeDatabase.Instance != null ? UpgradeDatabase.Instance.GetUpgradeData(data.requiredUpgradeB) : null;

        int currentA = CurrentLevel(data.requiredUpgradeA);
        int currentB = CurrentLevel(data.requiredUpgradeB);

        int reachedA = Mathf.Max(currentA, SynergyDiscovery.GetMaxUpgradeLevel(data.requiredUpgradeA));
        int reachedB = Mathf.Max(currentB, SynergyDiscovery.GetMaxUpgradeLevel(data.requiredUpgradeB));

        ApplySlot(iconA, backdropA, unknownTextA, levelTextA, upgradeA != null ? upgradeA.icon : null, reachedA > 0, reachedA, data.requiredLevelA);
        ApplySlot(iconB, backdropB, unknownTextB, levelTextB, upgradeB != null ? upgradeB.icon : null, reachedB > 0, reachedB, data.requiredLevelB);

        isUnlocked = SynergyDiscovery.IsSynergyUnlocked(data)
            || (currentA >= data.requiredLevelA && currentB >= data.requiredLevelB);

        bool revealResult = ApplySlot(iconResult, backdropResult, unknownTextResult, null, data.icon, isUnlocked, 0, 0);

        if (resultVisuals != null)
            resultVisuals.SetPremium(revealResult);
    }

    private SynergyData ResolveSynergy()
    {
        if (synergy == null) return null;

        SynergyDatabase database = SynergyDatabase.Instance;
        if (database == null || database.allSynergies == null) return synergy;

        for (int i = 0; i < database.allSynergies.Count; i++)
        {
            SynergyData candidate = database.allSynergies[i];
            if (candidate != null && candidate.synergyName == synergy.synergyName)
                return candidate;
        }

        return synergy;
    }

    private static int CurrentLevel(UpgradeType type)
    {
        return PlayerStatsManager.Instance != null ? PlayerStatsManager.Instance.GetUpgradeLevel(type) : 0;
    }

    private bool ApplySlot(Image icon, Image backdrop, TextMeshProUGUI unknownText, TextMeshProUGUI levelText, Sprite discoveredIcon, bool discovered, int reachedLevel, int requiredLevel)
    {
        bool reveal = discovered && discoveredIcon != null;

        if (icon != null)
        {
            icon.sprite = reveal ? discoveredIcon : null;
            icon.enabled = reveal;
        }

        if (backdrop != null)
            backdrop.gameObject.SetActive(reveal);

        if (unknownText != null)
            unknownText.gameObject.SetActive(!reveal);

        if (levelText != null)
        {
            levelText.gameObject.SetActive(reveal);
            levelText.text = reveal ? $"Nv. {Mathf.Min(reachedLevel, requiredLevel)}/{requiredLevel}" : "";
        }

        return reveal;
    }
}
