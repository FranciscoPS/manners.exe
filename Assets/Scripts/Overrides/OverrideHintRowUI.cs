using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using TMPro;

public enum OverrideResultDisplayMode
{
    Collection,
    Hud
}

public class OverrideHintRowUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private const string UndiscoveredTooltipTitle = "??? Sobrecarga oculta";
    private const string UndiscoveredTooltipBody = "Sube al nivel requerido las dos mejoras de esta fila para revelar qué hace.";

    [Header("Sobrecarga representada por esta fila")]
    [SerializeField, FormerlySerializedAs("synergy")] private OverrideData overrideData;

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
    [Tooltip("Efecto premium (foil holográfico + pulso) que se enciende cuando la sobrecarga está desbloqueada.")]
    [SerializeField] private PremiumUpgradeVisuals resultVisuals;

    [Header("Cuadro de resultado")]
    [Tooltip("Collection: progreso entre partidas; los niveles muestran el máximo alcanzado y el resultado se revela y brilla en cuanto la sobrecarga está desbloqueada (menú principal, Game Over). Hud: guía de la partida actual; los niveles empiezan en 0 y, si la sobrecarga ya se descubrió, sus iconos se ven siempre y el resultado queda atenuado hasta activarse (al aterrizar la animación de activación).")]
    [SerializeField] private OverrideResultDisplayMode resultDisplay = OverrideResultDisplayMode.Collection;
    [Tooltip("Tinte del icono de resultado en modo Hud cuando la sobrecarga se conoce de otra partida pero todavía no está activa en esta.")]
    [SerializeField] private Color knownInactiveTint = new Color(1f, 1f, 1f, 0.45f);

    private bool isUnlocked;
    private bool hudKnown;
    private bool hudActive;
    private Canvas canvas;

    public OverrideData OverrideData => ResolveOverride();
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
        OverrideData data = ResolveOverride();
        if (data == null) return;

        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        if (isUnlocked)
            HoverTooltipUI.Show(canvas, data.overrideName, data.description);
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
        OverrideData data = ResolveOverride();
        if (data == null) return;

        UpgradeData upgradeA = UpgradeDatabase.Instance != null ? UpgradeDatabase.Instance.GetUpgradeData(data.requiredUpgradeA) : null;
        UpgradeData upgradeB = UpgradeDatabase.Instance != null ? UpgradeDatabase.Instance.GetUpgradeData(data.requiredUpgradeB) : null;

        int currentA = CurrentLevel(data.requiredUpgradeA);
        int currentB = CurrentLevel(data.requiredUpgradeB);

        int reachedA = Mathf.Max(currentA, OverrideDiscovery.GetMaxUpgradeLevel(data.requiredUpgradeA));
        int reachedB = Mathf.Max(currentB, OverrideDiscovery.GetMaxUpgradeLevel(data.requiredUpgradeB));

        bool hud = resultDisplay == OverrideResultDisplayMode.Hud;
        bool hudGuide = hud && (hudKnown || hudActive);

        ApplySlot(iconA, backdropA, unknownTextA, levelTextA, upgradeA != null ? upgradeA.icon : null, hudGuide || reachedA > 0, hud ? currentA : reachedA, data.requiredLevelA);
        ApplySlot(iconB, backdropB, unknownTextB, levelTextB, upgradeB != null ? upgradeB.icon : null, hudGuide || reachedB > 0, hud ? currentB : reachedB, data.requiredLevelB);

        if (hud)
        {
            isUnlocked = hudGuide;

            bool revealResult = ApplySlot(iconResult, backdropResult, unknownTextResult, null, data.icon, isUnlocked, 0, 0);

            if (iconResult != null)
                iconResult.color = hudActive ? Color.white : knownInactiveTint;

            if (resultVisuals != null)
                resultVisuals.SetPremium(revealResult && hudActive);
        }
        else
        {
            isUnlocked = OverrideDiscovery.IsOverrideUnlocked(data)
                || (currentA >= data.requiredLevelA && currentB >= data.requiredLevelB);

            bool revealResult = ApplySlot(iconResult, backdropResult, unknownTextResult, null, data.icon, isUnlocked, 0, 0);

            if (resultVisuals != null)
                resultVisuals.SetPremium(revealResult);
        }
    }

    private OverrideData ResolveOverride()
    {
        if (overrideData == null) return null;

        OverrideDatabase database = OverrideDatabase.Instance;
        if (database == null || database.allOverrides == null) return overrideData;

        for (int i = 0; i < database.allOverrides.Count; i++)
        {
            OverrideData candidate = database.allOverrides[i];
            if (candidate != null && candidate.overrideName == overrideData.overrideName)
                return candidate;
        }

        return overrideData;
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
