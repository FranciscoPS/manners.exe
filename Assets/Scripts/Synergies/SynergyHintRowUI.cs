using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public enum SynergyResultDisplayMode
{
    Collection,
    Hud
}

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

    [Header("Cuadro de resultado")]
    [Tooltip("Collection: se revela y brilla en cuanto la sinergia está desbloqueada (menú principal, Game Over). Hud: el panel miniatura del HUD decide cuándo revelarla (al aterrizar la animación de activación) y la muestra atenuada si solo se conoce de partidas anteriores.")]
    [SerializeField] private SynergyResultDisplayMode resultDisplay = SynergyResultDisplayMode.Collection;
    [Tooltip("Tinte del icono de resultado en modo Hud cuando la sinergia se conoce de otra partida pero todavía no está activa en esta.")]
    [SerializeField] private Color knownInactiveTint = new Color(1f, 1f, 1f, 0.45f);

    private bool isUnlocked;
    private bool hudKnown;
    private bool hudActive;
    private Canvas canvas;

    public SynergyData Synergy => ResolveSynergy();
    public RectTransform ResultSlot => iconResult != null ? iconResult.transform.parent as RectTransform : null;
    public RectTransform ResultIconRect => iconResult != null ? iconResult.rectTransform : null;

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

    public void SetHudState(bool known, bool active)
    {
        hudKnown = known;
        hudActive = active;
        Refresh();
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

        if (resultDisplay == SynergyResultDisplayMode.Hud)
        {
            isUnlocked = hudKnown || hudActive;

            bool revealResult = ApplySlot(iconResult, backdropResult, unknownTextResult, null, data.icon, isUnlocked, 0, 0);

            if (iconResult != null)
                iconResult.color = hudActive ? Color.white : knownInactiveTint;

            if (resultVisuals != null)
                resultVisuals.SetPremium(revealResult && hudActive);
        }
        else
        {
            isUnlocked = SynergyDiscovery.IsSynergyUnlocked(data)
                || (currentA >= data.requiredLevelA && currentB >= data.requiredLevelB);

            bool revealResult = ApplySlot(iconResult, backdropResult, unknownTextResult, null, data.icon, isUnlocked, 0, 0);

            if (resultVisuals != null)
                resultVisuals.SetPremium(revealResult);
        }
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
