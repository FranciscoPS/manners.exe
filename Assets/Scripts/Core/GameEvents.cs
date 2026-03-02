using System;
using System.Collections.Generic;

/// <summary>
/// Sistema centralizado de eventos del juego (Observer Pattern)
/// Elimina dependencias directas y reduce coupling
/// </summary>
public static class GameEvents
{
    // === PLAYER EVENTS ===
    public static event Action<float> OnPlayerHealthChanged;
    public static event Action OnPlayerDied;
    public static event Action<float> OnPlayerDamaged;
    
    // === EXPERIENCE EVENTS ===
    public static event Action<int> OnExperienceGained;
    public static event Action<int, int> OnExperienceChanged; // current, required
    public static event Action<int> OnLevelUp;
    
    // === CURRENCY EVENTS ===
    public static event Action<int> OnCoinsChanged;
    public static event Action<int> OnDiamondsChanged;
    public static event Action<int> OnCoinsGained;
    public static event Action<int> OnDiamondsGained;
    
    // === COMBAT EVENTS ===
    public static event Action<float> OnEnemyDamaged;
    public static event Action OnEnemyKilled;
    public static event Action<int> OnBuildingDestroyed;
    
    // === UPGRADE EVENTS ===
    public static event Action<UpgradeType, int> OnUpgradeApplied;
    public static event Action<UpgradeData> OnUpgradePurchased;
    
    // === GAME STATE EVENTS ===
    public static event Action OnGameStarted;
    public static event Action OnGamePaused;
    public static event Action OnGameResumed;
    public static event Action OnGameOver;
    
    // === WAVE EVENTS ===
    public static event Action<int> OnWaveStarted;
    public static event Action<int> OnWaveCompleted;
    
    // === TIME EVENTS ===
    public static event Action<string> OnGameTimeUpdated; // Formatted time string
    
    // === SHOP EVENTS ===
    public static event Action<int> OnShopLocationChanged;
    /// <summary>Se dispara cada vez que la tienda se abre.</summary>
    public static event Action OnShopOpened;
    /// <summary>Se dispara cuando la tienda se cierra automáticamente después de una compra.</summary>
    public static event Action OnShopAutoClosed;
    
    // === TUTORIAL EVENTS ===
    /// <summary>Se dispara cada vez que se muestra un paso del tutorial. Parámetro: id del paso.</summary>
    public static event Action<string> OnTutorialStepShown;
    /// <summary>Se dispara cuando el jugador completa el tutorial por completo.</summary>
    public static event Action OnTutorialCompleted;
    
    // ===== TRIGGER METHODS =====
    
    // Player
    public static void TriggerPlayerHealthChanged(float health) => OnPlayerHealthChanged?.Invoke(health);
    public static void TriggerPlayerDied() => OnPlayerDied?.Invoke();
    public static void TriggerPlayerDamaged(float damage) => OnPlayerDamaged?.Invoke(damage);
    
    // Experience
    public static void TriggerExperienceGained(int amount) => OnExperienceGained?.Invoke(amount);
    public static void TriggerExperienceChanged(int current, int required) => OnExperienceChanged?.Invoke(current, required);
    public static void TriggerLevelUp(int level) => OnLevelUp?.Invoke(level);
    
    // Currency
    public static void TriggerCoinsChanged(int amount) => OnCoinsChanged?.Invoke(amount);
    public static void TriggerDiamondsChanged(int amount) => OnDiamondsChanged?.Invoke(amount);
    public static void TriggerCoinsGained(int amount) => OnCoinsGained?.Invoke(amount);
    public static void TriggerDiamondsGained(int amount) => OnDiamondsGained?.Invoke(amount);
    
    // Combat
    public static void TriggerEnemyDamaged(float damage) => OnEnemyDamaged?.Invoke(damage);
    public static void TriggerEnemyKilled() => OnEnemyKilled?.Invoke();
    public static void TriggerBuildingDestroyed(int count) => OnBuildingDestroyed?.Invoke(count);
    
    // Upgrades
    public static void TriggerUpgradeApplied(UpgradeType type, int level) => OnUpgradeApplied?.Invoke(type, level);
    public static void TriggerUpgradePurchased(UpgradeData upgrade) => OnUpgradePurchased?.Invoke(upgrade);
    
    // Game State
    public static void TriggerGameStarted() => OnGameStarted?.Invoke();
    public static void TriggerGamePaused() => OnGamePaused?.Invoke();
    public static void TriggerGameResumed() => OnGameResumed?.Invoke();
    public static void TriggerGameOver() => OnGameOver?.Invoke();
    
    // Wave
    public static void TriggerWaveStarted(int waveIndex) => OnWaveStarted?.Invoke(waveIndex);
    public static void TriggerWaveCompleted(int waveIndex) => OnWaveCompleted?.Invoke(waveIndex);
    
    // Time
    public static void TriggerGameTimeUpdated(string formattedTime) => OnGameTimeUpdated?.Invoke(formattedTime);
    
    // Shop
    public static void TriggerShopLocationChanged(int newShopIndex) => OnShopLocationChanged?.Invoke(newShopIndex);
    public static void TriggerShopOpened()      => OnShopOpened?.Invoke();
    public static void TriggerShopAutoClosed()  => OnShopAutoClosed?.Invoke();
    
    // Tutorial
    public static void TriggerTutorialStepShown(string stepId) => OnTutorialStepShown?.Invoke(stepId);
    public static void TriggerTutorialCompleted()              => OnTutorialCompleted?.Invoke();
    
    /// <summary>
    /// Limpia todos los subscribers (útil para scene transitions)
    /// </summary>
    public static void ClearAllEvents()
    {
        OnPlayerHealthChanged = null;
        OnPlayerDied = null;
        OnPlayerDamaged = null;
        OnExperienceGained = null;
        OnExperienceChanged = null;
        OnLevelUp = null;
        OnCoinsChanged = null;
        OnDiamondsChanged = null;
        OnCoinsGained = null;
        OnDiamondsGained = null;
        OnEnemyDamaged = null;
        OnEnemyKilled = null;
        OnBuildingDestroyed = null;
        OnUpgradeApplied = null;
        OnUpgradePurchased = null;
        OnGameStarted = null;
        OnGamePaused = null;
        OnGameResumed = null;
        OnGameOver = null;
        OnShopLocationChanged = null;
        OnShopOpened = null;
        OnShopAutoClosed = null;
        OnWaveStarted = null;
        OnWaveCompleted = null;
        OnGameTimeUpdated = null;
        OnTutorialStepShown = null;
        OnTutorialCompleted = null;
    }
}
