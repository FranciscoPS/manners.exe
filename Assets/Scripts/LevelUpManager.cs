using TMPro;
using UnityEngine;
using System.Collections;

public class LevelUpManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject levelUpPanel;
    [SerializeField] private TextMeshProUGUI levelUpText;

    [Header("Rainbow Text Settings")]
    [SerializeField] private float colorSpeed = 1f;

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

    private void Update()
    {
        if (levelUpActive && levelUpText != null)
        {
            float hue = Mathf.PingPong(Time.unscaledTime * colorSpeed, 1f);
            levelUpText.color = Color.HSVToRGB(hue, 1f, 1f);
        }
    }

    private void HandleLevelUp(int newLevel)
    {
        if (levelUpActive)
            return;

        levelUpActive = true;

        Time.timeScale = 0f;

        if (levelUpText != null)
            levelUpText.text = "LEVEL UP!";

        if (levelUpPanel != null)
            levelUpPanel.SetActive(true);
    }

    public void CloseLevelUp()
    {
        levelUpActive = false;

        if (levelUpPanel != null)
            levelUpPanel.SetActive(false);

        Time.timeScale = 1f;
    }
}

