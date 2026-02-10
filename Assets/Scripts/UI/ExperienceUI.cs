using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ExperienceUI : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float fillSpeed = 5f;

    private Image expBarFill;
    private TextMeshProUGUI levelText;
    private TextMeshProUGUI expText;
    
    private PlayerExperience playerExperience;

    private float targetFillAmount = 0f;
    private float currentFillAmount = 0f;

    private void Awake()
    {
        Transform expBarFillTransform = transform.Find("ExpBarPanel/ExpBarBackground/ExpBarFill");
        if (expBarFillTransform != null)
            expBarFill = expBarFillTransform.GetComponent<Image>();

        Transform levelTextTransform = transform.Find("ExpBarPanel/LevelText");
        if (levelTextTransform != null)
            levelText = levelTextTransform.GetComponent<TextMeshProUGUI>();

        Transform expTextTransform = transform.Find("ExpBarPanel/ExpText");
        if (expTextTransform != null)
            expText = expTextTransform.GetComponent<TextMeshProUGUI>();
    }

    private void Start()
    {
        playerExperience = FindFirstObjectByType<PlayerExperience>();

        if (ExperienceManager.Instance != null)
        {
            ExperienceManager.Instance.OnExperienceChanged += UpdateExperienceBar;
            ExperienceManager.Instance.OnLevelUp += HandleLevelUp;
        }

        if (playerExperience != null)
        {
            int currentExp = playerExperience.GetCurrentExperience();
            int requiredExp = playerExperience.GetExperienceRequiredForNextLevel();
            UpdateExperienceBar(currentExp, requiredExp);
        }
    }

    private void OnDestroy()
    {
        if (ExperienceManager.Instance != null)
        {
            ExperienceManager.Instance.OnExperienceChanged -= UpdateExperienceBar;
            ExperienceManager.Instance.OnLevelUp -= HandleLevelUp;
        }
    }

    private void Update()
    {
        if (expBarFill != null)
        {
            currentFillAmount = Mathf.Lerp(currentFillAmount, targetFillAmount, fillSpeed * Time.unscaledDeltaTime);
            
            RectTransform rt = expBarFill.rectTransform;
            rt.anchorMax = new Vector2(currentFillAmount, 1f);
        }
    }

    private void UpdateExperienceBar(int currentExp, int requiredExp)
    {
        targetFillAmount = requiredExp > 0 ? (float)currentExp / requiredExp : 0f;

        if (levelText != null && playerExperience != null)
        {
            levelText.text = "NIVEL " + playerExperience.GetCurrentLevel();
        }

        if (expText != null)
        {
            expText.text = currentExp + " / " + requiredExp;
        }
    }

    private void HandleLevelUp(int newLevel)
    {
        currentFillAmount = 0f;
        targetFillAmount = 0f;

        if (levelText != null)
        {
            levelText.text = "NIVEL " + newLevel;
        }

    }
}
