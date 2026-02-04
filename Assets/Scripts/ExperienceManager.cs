using UnityEngine;
using System;

public class ExperienceManager : MonoBehaviour
{
    public static ExperienceManager Instance { get; private set; }

    [Header("Level Settings")]
    [SerializeField] private int baseExperienceRequired = 100;
    [SerializeField] private float experienceMultiplier = 1.5f;

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
        return Mathf.RoundToInt(baseExperienceRequired * Mathf.Pow(experienceMultiplier, level - 1));
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
