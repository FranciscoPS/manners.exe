using UnityEngine;
using System;

public class ExperienceManager : MonoBehaviour
{
    public static ExperienceManager Instance { get; private set; }

    public event Action<int> OnLevelUp;
    public event Action<int, int> OnExperienceChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public int CalculateExperienceForLevel(int level)
    {
        if (GameBalanceConfig.Instance != null)
        {
            return GameBalanceConfig.Instance.CalculateExperienceForLevel(level);
        }
        return Mathf.RoundToInt(100 * Mathf.Pow(1.5f, level - 1));
    }

    public void NotifyLevelUp(int newLevel)
    {
        OnLevelUp?.Invoke(newLevel);
    }

    public void NotifyExperienceChanged(int currentExp, int requiredExp)
    {
        OnExperienceChanged?.Invoke(currentExp, requiredExp);
    }
}
