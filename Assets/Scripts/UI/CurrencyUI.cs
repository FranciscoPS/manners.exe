using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CurrencyUI : MonoBehaviour
{
    [Header("Coin UI")]
    [SerializeField] private TextMeshProUGUI coinText;
    
    [Header("Diamond UI")]
    [SerializeField] private TextMeshProUGUI diamondText;

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
        if (coinText != null)
        {
            coinText.text = amount.ToString();
        }
    }

    private void UpdateDiamondDisplay(int amount)
    {
        if (diamondText != null)
        {
            diamondText.text = amount.ToString();
        }
    }
}
