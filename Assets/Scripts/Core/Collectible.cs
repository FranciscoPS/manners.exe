using UnityEngine;

public class Collectible : BaseCollectible
{
    public enum CollectibleType
    {
        Coin,
        Diamond
    }

    private CollectibleType type;
    private int value = 1;

    public void SetType(CollectibleType collectibleType)
    {
        type = collectibleType;

        UpdateConfiguration();
    }

    public void SetValue(int amount)
    {
        value = amount;
    }

    protected override void UpdateConfiguration()
    {

        if (GameBalanceConfig.Instance != null)
        {
            attractionRange = type == CollectibleType.Coin ?
                GameBalanceConfig.Instance.CoinAttractionRange :
                GameBalanceConfig.Instance.DiamondAttractionRange;

            lifeTime = type == CollectibleType.Coin ?
                GameBalanceConfig.Instance.CoinLifetime :
                GameBalanceConfig.Instance.DiamondLifetime;
        }
        else
        {
            attractionRange = 5f;
            lifeTime = 30f;
        }

        if (PlayerStatsManager.Instance != null)
        {
            attractionRange = PlayerStatsManager.Instance.GetModifiedMagnetRange();
        }
    }

    protected override void OnCollected(GameObject playerObject)
    {

        if (type == CollectibleType.Coin)
        {
            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.AddCoins(value);
            }

            if (FloatingTextManager.Instance != null)
            {
                Vector3 textPosition = transform.position + Vector3.up * 0.5f;
                FloatingTextManager.Instance.ShowCoins(value, textPosition);
            }

            if (PickupAudioManager.Instance != null)
            {
                PickupAudioManager.Instance.PlayCoinSound();
            }
        }
        else if (type == CollectibleType.Diamond)
        {
            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.AddDiamonds(value);
            }

            if (FloatingTextManager.Instance != null)
            {
                Vector3 textPosition = transform.position + Vector3.up * 0.5f;
                FloatingTextManager.Instance.ShowDiamonds(value, textPosition);
            }

            if (PickupAudioManager.Instance != null)
            {
                PickupAudioManager.Instance.PlayDiamondSound();
            }
        }
    }
}
