using UnityEngine;
using System.Collections.Generic;

public class GameSessionStats : MonoBehaviour
{
    private static GameSessionStats instance;
    private static bool isQuitting = false;

    public static GameSessionStats Instance
    {
        get
        {
            if (instance == null && !isQuitting)
            {
                GameObject go = new GameObject("GameSessionStats");
                instance = go.AddComponent<GameSessionStats>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    private int enemiesKilled = 0;
    private int buildingsDestroyed = 0;
    private int coinsCollectedThisSession = 0;
    private int diamondsCollectedThisSession = 0;
    private int maxLevelReached = 1;
    private float survivalTime = 0f;
    private bool isSessionActive = false;

    public int EnemiesKilled => enemiesKilled;
    public int BuildingsDestroyed => buildingsDestroyed;
    public int CoinsCollected => coinsCollectedThisSession;
    public int DiamondsCollected => diamondsCollectedThisSession;
    public int MaxLevelReached => maxLevelReached;
    public float SurvivalTime => survivalTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        isQuitting = false;
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void OnApplicationQuit()
    {
        isQuitting = true;
    }

    private float GetCurrentSurvivalTime()
    {
        if (isSessionActive && GameTimeManager.Instance != null)
        {
            return GameTimeManager.Instance.GetGameTime();
        }
        return survivalTime;
    }

    public float SurvivalTimeUpdated => GetCurrentSurvivalTime();

    public void StartSession()
    {
        ResetStats();
        isSessionActive = true;
    }

    public void EndSession()
    {

        if (isSessionActive && GameTimeManager.Instance != null)
        {
            survivalTime = GameTimeManager.Instance.GetGameTime();
        }
        isSessionActive = false;
    }

    public void ResetStats()
    {
        enemiesKilled = 0;
        buildingsDestroyed = 0;
        coinsCollectedThisSession = 0;
        diamondsCollectedThisSession = 0;
        maxLevelReached = 1;
        survivalTime = 0f;
    }

    public void RegisterEnemyKill()
    {
        enemiesKilled++;
    }

    public void RegisterBuildingDestroyed()
    {
        buildingsDestroyed++;
    }

    public void RegisterCoinsCollected(int amount)
    {
        coinsCollectedThisSession += amount;
    }

    public void RegisterDiamondsCollected(int amount)
    {
        diamondsCollectedThisSession += amount;
    }

    public void UpdateMaxLevel(int level)
    {
        if (level > maxLevelReached)
        {
            maxLevelReached = level;
        }
    }

    public string GetFormattedSurvivalTime()
    {

        float currentTime = GetCurrentSurvivalTime();
        int minutes = Mathf.FloorToInt(currentTime / 60f);
        int seconds = Mathf.FloorToInt(currentTime % 60f);
        return $"{minutes:00}:{seconds:00}";
    }

    public Dictionary<UpgradeType, int> GetUpgradeLevels()
    {
        if (PlayerStatsManager.Instance != null)
        {
            return PlayerStatsManager.Instance.GetAllUpgradeLevels();
        }
        return new Dictionary<UpgradeType, int>();
    }

    public string GetUpgradeDisplayName(UpgradeType upgradeType)
    {
        switch (upgradeType)
        {
            case UpgradeType.Damage:
                return "Damage";
            case UpgradeType.AttackSpeed:
                return "Attack Speed";
            case UpgradeType.AttackRange:
                return "Attack Range";
            case UpgradeType.MoveSpeed:
                return "Move Speed";
            case UpgradeType.MagnetRange:
                return "Magnet Range";
            case UpgradeType.MultiShot:
                return "Multi Shot";
            case UpgradeType.ExplosiveShot:
                return "Explosive Shot";
            case UpgradeType.Knockback:
                return "Knockback";
            default:
                return upgradeType.ToString();
        }
    }
}
