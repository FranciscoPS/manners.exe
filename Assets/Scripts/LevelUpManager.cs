using UnityEngine;

public class LevelUpManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject levelUpPanel;

    private bool levelUpActive = false;

    private void Awake()
    {
        if (levelUpPanel != null)
            levelUpPanel.SetActive(false);
    }

    private void Start()
    {
        if (ExperienceManager.Instance != null)
            ExperienceManager.Instance.OnLevelUp += HandleLevelUp;
    }

    private void OnDisable()
    {
        if (ExperienceManager.Instance != null)
            ExperienceManager.Instance.OnLevelUp -= HandleLevelUp;
    }

    private void HandleLevelUp(int newLevel)
    {
        if (levelUpActive)
            return;

        levelUpActive = true;

        Time.timeScale = 0f;

        levelUpPanel.SetActive(true);
    }

    public void CloseLevelUp()
    {
        levelUpActive = false;

        levelUpPanel.SetActive(false);
        Time.timeScale = 1f;
    }
}
