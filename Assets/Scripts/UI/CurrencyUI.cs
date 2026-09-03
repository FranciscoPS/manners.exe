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

    private bool coinInitialized;
    private bool diamondInitialized;

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

        coinText.text = $"Monedas: {amount}";

        if (coinInitialized)
            coinText.rectTransform.PunchScale(punchScale, punchDuration);

        coinInitialized = true;
    }

    private void UpdateDiamondDisplay(int amount)
    {
        if (diamondText == null) return;

        diamondText.text = $"Gemas: {amount}";

        if (diamondInitialized)
            diamondText.rectTransform.PunchScale(punchScale, punchDuration);

        diamondInitialized = true;
    }
}
