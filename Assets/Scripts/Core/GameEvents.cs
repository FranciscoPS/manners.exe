using System;
using System.Collections.Generic;

public static class GameEvents
{

    public static event Action<float> OnPlayerHealthChanged;
    public static event Action OnPlayerDied;
    public static event Action<float> OnPlayerDamaged;

    public static event Action<int> OnExperienceGained;
    public static event Action<int, int> OnExperienceChanged;
    public static event Action<int> OnLevelUp;

    public static event Action<int> OnCoinsChanged;
    public static event Action<int> OnDiamondsChanged;
    public static event Action<int> OnCoinsGained;
    public static event Action<int> OnDiamondsGained;

    public static event Action<float> OnEnemyDamaged;
    public static event Action OnEnemyKilled;
    public static event Action<int> OnBuildingDestroyed;

    public static event Action<UpgradeType, int> OnUpgradeApplied;
    public static event Action<UpgradeData> OnUpgradePurchased;

    public static event Action OnGameStarted;
    public static event Action OnChestSpawned;
    public static event Action OnGamePaused;
    public static event Action OnGameResumed;
    public static event Action OnGameOver;

    public static event Action<int> OnWaveStarted;
    public static event Action<int> OnWaveCompleted;

    public static event Action<string> OnGameTimeUpdated;

    public static event Action OnMatchTimeExpired;

    public static event Action<int> OnShopLocationChanged;

    public static event Action OnShopOpened;

    public static event Action OnShopAutoClosed;

    public static event Action<string> OnTutorialStepShown;

    public static event Action OnTutorialCompleted;

    public static void TriggerPlayerHealthChanged(float health) => OnPlayerHealthChanged?.Invoke(health);
    public static void TriggerPlayerDied() => OnPlayerDied?.Invoke();
    public static void TriggerPlayerDamaged(float damage) => OnPlayerDamaged?.Invoke(damage);

    public static void TriggerExperienceGained(int amount) => OnExperienceGained?.Invoke(amount);
    public static void TriggerExperienceChanged(int current, int required) => OnExperienceChanged?.Invoke(current, required);
    public static void TriggerLevelUp(int level) => OnLevelUp?.Invoke(level);

    public static void TriggerCoinsChanged(int amount) => OnCoinsChanged?.Invoke(amount);
    public static void TriggerDiamondsChanged(int amount) => OnDiamondsChanged?.Invoke(amount);
    public static void TriggerCoinsGained(int amount) => OnCoinsGained?.Invoke(amount);
    public static void TriggerDiamondsGained(int amount) => OnDiamondsGained?.Invoke(amount);

    public static void TriggerEnemyDamaged(float damage) => OnEnemyDamaged?.Invoke(damage);
    public static void TriggerEnemyKilled() => OnEnemyKilled?.Invoke();
    public static void TriggerBuildingDestroyed(int count) => OnBuildingDestroyed?.Invoke(count);

    public static void TriggerUpgradeApplied(UpgradeType type, int level) => OnUpgradeApplied?.Invoke(type, level);
    public static void TriggerUpgradePurchased(UpgradeData upgrade) => OnUpgradePurchased?.Invoke(upgrade);

    public static void TriggerGameStarted() => OnGameStarted?.Invoke();
    public static void TriggerChestSpawned() => OnChestSpawned?.Invoke();
    public static void TriggerGamePaused() => OnGamePaused?.Invoke();
    public static void TriggerGameResumed() => OnGameResumed?.Invoke();
    public static void TriggerGameOver() => OnGameOver?.Invoke();

    public static void TriggerWaveStarted(int waveIndex) => OnWaveStarted?.Invoke(waveIndex);
    public static void TriggerWaveCompleted(int waveIndex) => OnWaveCompleted?.Invoke(waveIndex);

    public static void TriggerGameTimeUpdated(string formattedTime) => OnGameTimeUpdated?.Invoke(formattedTime);

    public static void TriggerMatchTimeExpired() => OnMatchTimeExpired?.Invoke();

    public static void TriggerShopLocationChanged(int newShopIndex) => OnShopLocationChanged?.Invoke(newShopIndex);
    public static void TriggerShopOpened() => OnShopOpened?.Invoke();
    public static void TriggerShopAutoClosed() => OnShopAutoClosed?.Invoke();

    public static void TriggerTutorialStepShown(string stepId) => OnTutorialStepShown?.Invoke(stepId);
    public static void TriggerTutorialCompleted() => OnTutorialCompleted?.Invoke();

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
        OnChestSpawned = null;
        OnGamePaused = null;
        OnGameResumed = null;
        OnGameOver = null;
        OnShopLocationChanged = null;
        OnShopOpened = null;
        OnShopAutoClosed = null;
        OnWaveStarted = null;
        OnWaveCompleted = null;
        OnGameTimeUpdated = null;
        OnMatchTimeExpired = null;
        OnTutorialStepShown = null;
        OnTutorialCompleted = null;
    }
}
