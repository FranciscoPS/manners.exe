using UnityEngine;

public class PlayerExperience : MonoBehaviour
{
    [Header("Experience Settings")]
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private int currentExperience = 0;

    private int experienceRequiredForNextLevel;

    private PlayerHealth playerHealth;

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
    }
    
    private void Start()
    {
        if (ExperienceManager.Instance != null)
        {
            experienceRequiredForNextLevel = ExperienceManager.Instance.CalculateExperienceForLevel(currentLevel);
            ExperienceManager.Instance.NotifyExperienceChanged(currentExperience, experienceRequiredForNextLevel);
        }
        else
        {
            Debug.LogError("[PlayerExperience] ExperienceManager.Instance is null in Start()!");
        }
    }

    public void AddExperience(int amount)
    {
        if (playerHealth == null)
            return;

        if (playerHealth.IsDead)
            return;

        currentExperience += amount;

        if (ExperienceManager.Instance != null)
        {
            ExperienceManager.Instance.NotifyExperienceChanged(currentExperience, experienceRequiredForNextLevel);
        }

        CheckLevelUp();
    }

    private void CheckLevelUp()
    {
        if (playerHealth != null && playerHealth.IsDead)
            return;

        while (currentExperience >= experienceRequiredForNextLevel)
        {
            currentExperience -= experienceRequiredForNextLevel;
            currentLevel++;
            
            // Registrar nivel en estadísticas
            if (GameSessionStats.Instance != null)
            {
                GameSessionStats.Instance.UpdateMaxLevel(currentLevel);
            }
            
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
