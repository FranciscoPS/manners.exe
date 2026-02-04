using UnityEngine;

public class PlayerExperience : MonoBehaviour
{
    [Header("Experience Settings")]
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private int currentExperience = 0;

    private int experienceRequiredForNextLevel;

    private void Start()
    {
        if (ExperienceManager.Instance != null)
        {
            experienceRequiredForNextLevel = ExperienceManager.Instance.CalculateExperienceForLevel(currentLevel);
            ExperienceManager.Instance.NotifyExperienceChanged(currentExperience, experienceRequiredForNextLevel);
        }
    }

    public void AddExperience(int amount)
    {
        currentExperience += amount;

        if (ExperienceManager.Instance != null)
        {
            ExperienceManager.Instance.NotifyExperienceChanged(currentExperience, experienceRequiredForNextLevel);
        }

        CheckLevelUp();
    }

    private void CheckLevelUp()
    {
        while (currentExperience >= experienceRequiredForNextLevel)
        {
            currentExperience -= experienceRequiredForNextLevel;
            currentLevel++;
            
            if (ExperienceManager.Instance != null)
            {
                experienceRequiredForNextLevel = ExperienceManager.Instance.CalculateExperienceForLevel(currentLevel);
                ExperienceManager.Instance.NotifyLevelUp(currentLevel);
                ExperienceManager.Instance.NotifyExperienceChanged(currentExperience, experienceRequiredForNextLevel);
            }
        }
    }

    public int GetCurrentLevel()
    {
        return currentLevel;
    }

    public int GetCurrentExperience()
    {
        return currentExperience;
    }

    public int GetExperienceRequiredForNextLevel()
    {
        return experienceRequiredForNextLevel;
    }
}
