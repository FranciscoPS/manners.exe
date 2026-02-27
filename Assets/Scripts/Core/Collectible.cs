using UnityEngine;

/// <summary>
/// Collectible (Coins y Diamonds) - Refactorizado para usar BaseCollectible
/// Elimina código duplicado y usa UpdateManager para mejor rendimiento
/// </summary>
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
        
        // Actualizar configuración basada en el tipo
        UpdateConfiguration();
    }

    public void SetValue(int amount)
    {
        value = amount;
    }

    protected override void UpdateConfiguration()
    {
        // Actualizar rangos y tiempos de vida basados en el tipo
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
        
        // Actualizar rango magnético del jugador si está disponible
        if (PlayerStatsManager.Instance != null)
        {
            attractionRange = PlayerStatsManager.Instance.GetModifiedMagnetRange();
        }
    }

    protected override void OnCollected(GameObject playerObject)
    {
        // Dar monedas o diamantes al jugador según el tipo
        if (type == CollectibleType.Coin)
        {
            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.AddCoins(value);
            }
            
            // Mostrar floating text para monedas
            if (FloatingTextManager.Instance != null)
            {
                Vector3 textPosition = transform.position + Vector3.up * 0.5f;
                FloatingTextManager.Instance.ShowCoins(value, textPosition);
            }
            
            // Reproducir sonido de moneda
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
            
            // Mostrar floating text para diamantes
            if (FloatingTextManager.Instance != null)
            {
                Vector3 textPosition = transform.position + Vector3.up * 0.5f;
                FloatingTextManager.Instance.ShowDiamonds(value, textPosition);
            }
            
            // Reproducir sonido de diamante
            if (PickupAudioManager.Instance != null)
            {
                PickupAudioManager.Instance.PlayDiamondSound();
            }
        }
    }
}
