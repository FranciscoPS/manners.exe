using System;
using System.Collections.Generic;

public static class GameEvents
{

    public static event Action<float> OnPlayerDamaged;

    public static event Action<int> OnExperienceGained;
    public static event Action<int> OnLevelUp;

    public static event Action<float> OnEnemyDamaged;

    public static event Action OnGameStarted;
    public static event Action OnChestSpawned;

    public static event Action<string> OnGameTimeUpdated;

    public static event Action OnMatchTimeExpired;

    public static event Action<int> OnShopLocationChanged;

    public static event Action OnShopOpened;

    public static event Action OnShopAutoClosed;

    public static event Action<string> OnTutorialStepShown;

    public static event Action OnTutorialCompleted;

    public static void TriggerPlayerDamaged(float damage) => OnPlayerDamaged?.Invoke(damage);

    public static void TriggerExperienceGained(int amount) => OnExperienceGained?.Invoke(amount);
    public static void TriggerLevelUp(int level) => OnLevelUp?.Invoke(level);

    public static void TriggerEnemyDamaged(float damage) => OnEnemyDamaged?.Invoke(damage);

    public static void TriggerGameStarted() => OnGameStarted?.Invoke();
    public static void TriggerChestSpawned() => OnChestSpawned?.Invoke();

    public static void TriggerGameTimeUpdated(string formattedTime) => OnGameTimeUpdated?.Invoke(formattedTime);

    public static void TriggerMatchTimeExpired() => OnMatchTimeExpired?.Invoke();

    public static void TriggerShopLocationChanged(int newShopIndex) => OnShopLocationChanged?.Invoke(newShopIndex);
    public static void TriggerShopOpened() => OnShopOpened?.Invoke();
    public static void TriggerShopAutoClosed() => OnShopAutoClosed?.Invoke();

    public static void TriggerTutorialStepShown(string stepId) => OnTutorialStepShown?.Invoke(stepId);
    public static void TriggerTutorialCompleted() => OnTutorialCompleted?.Invoke();

    public static void ClearAllEvents()
    {
        OnPlayerDamaged = null;
        OnExperienceGained = null;
        OnLevelUp = null;
        OnEnemyDamaged = null;
        OnGameStarted = null;
        OnChestSpawned = null;
        OnShopLocationChanged = null;
        OnShopOpened = null;
        OnShopAutoClosed = null;
        OnGameTimeUpdated = null;
        OnMatchTimeExpired = null;
        OnTutorialStepShown = null;
        OnTutorialCompleted = null;
    }
}
