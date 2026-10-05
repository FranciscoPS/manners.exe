using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CurrencyUI : MonoBehaviour
{
    [Header("Coin UI")]
    [SerializeField] private TextMeshProUGUI coinText;

    [Header("Diamond UI")]
    [SerializeField] private TextMeshProUGUI diamondText;

    [Header("Juice")]
    [Tooltip("Cuánto se agranda el texto al cambiar la cantidad de monedas/gemas.")]
    [SerializeField] private float punchScale = 1.2f;
    [Tooltip("Duración total del rebote del texto al cambiar de valor.")]
    [SerializeField] private float punchDuration = 0.3f;
    [Tooltip("Activo: la placa entera (fondo, icono y número) da el golpe. Desactivado: solo rebota el texto.")]
    [SerializeField] private bool punchWholePanel = true;

    private bool coinInitialized;
    private bool diamondInitialized;
    private RectTransform panelRect;

    private void Awake()
    {
        panelRect = transform as RectTransform;
    }

    private void Start()
    {
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCoinsChanged += UpdateCoinDisplay;
            CurrencyManager.Instance.OnDiamondsChanged += UpdateDiamondDisplay;

            UpdateCoinDisplay(CurrencyManager.Instance.CurrentCoins);
            UpdateDiamondDisplay(CurrencyManager.Instance.CurrentDiamonds);
        }
    }

    private void OnDestroy()
    {
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCoinsChanged -= UpdateCoinDisplay;
            CurrencyManager.Instance.OnDiamondsChanged -= UpdateDiamondDisplay;
        }
    }

    private void UpdateCoinDisplay(int amount)
    {
        if (coinText == null) return;

        coinText.text = amount.ToString();

        if (coinInitialized)
            PlayPunch(coinText.rectTransform);

        coinInitialized = true;
    }

    private void UpdateDiamondDisplay(int amount)
    {
        if (diamondText == null) return;

        diamondText.text = $"Gemas: {amount}";

        if (diamondInitialized)
            PlayPunch(diamondText.rectTransform);

        diamondInitialized = true;
    }

    private void PlayPunch(RectTransform textRect)
    {
        if (punchWholePanel && panelRect != null)
            panelRect.Punch();
        else
            textRect.PunchScale(punchScale, punchDuration);
    }
}
