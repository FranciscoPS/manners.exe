using UnityEngine;
using TMPro;

public class ShopCooldownUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI shopCooldownText;
    
    [Header("Color Settings")]
    [SerializeField] private Color cooldownColor = Color.red;
    [SerializeField] private Color availableColor1 = Color.green;
    [SerializeField] private Color availableColor2 = Color.white;
    [SerializeField] private float blinkSpeed = 2f;
    
    [Header("Managers")]
    [SerializeField] private LevelUpManager levelUpManager;
    
    private void Start()
    {
        if (levelUpManager == null)
        {
            levelUpManager = FindFirstObjectByType<LevelUpManager>();
        }
        
        if (shopCooldownText == null)
        {
            shopCooldownText = GetComponent<TextMeshProUGUI>();
        }
    }
    
    private void Update()
    {
        if (levelUpManager == null || shopCooldownText == null)
            return;
        
        if (levelUpManager.IsShopAvailable())
        {
            ShowAvailableState();
        }
        else
        {
            ShowCooldownState();
        }
    }
    
    private void ShowCooldownState()
    {
        float remainingTime = levelUpManager.GetShopCooldownRemaining();
        
        int minutes = Mathf.FloorToInt(remainingTime / 60f);
        int seconds = Mathf.FloorToInt(remainingTime % 60f);
        
        shopCooldownText.text = $"Shop: {minutes:00}:{seconds:00}";
        shopCooldownText.color = cooldownColor;
    }
    
    private void ShowAvailableState()
    {
        shopCooldownText.text = "Shop Available!";
        
        float t = (Mathf.Sin(Time.time * blinkSpeed) + 1f) / 2f;
        shopCooldownText.color = Color.Lerp(availableColor1, availableColor2, t);
    }
}
