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
        Transform expBarPanel = transform.Find("ExpBarPanel");
        if (expBarPanel == null) return;
        
        Image[] allImages = expBarPanel.GetComponentsInChildren<Image>(true);
        foreach (var img in allImages)
        {
            if (img.gameObject.name.Contains("ExpBarFill") || img.gameObject.name.Contains("Fill"))
            {
                expBarFill = img;
                break;
            }
        }

        TextMeshProUGUI[] allTexts = expBarPanel.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var txt in allTexts)
        {
            string txtName = txt.gameObject.name.ToLower();
            if (txtName.Contains("level") || txtName.Contains("lvl") || txtName.Contains("nivel"))
            {
                levelText = txt;
                break;
            }
        }

        foreach (var txt in allTexts)
        {
            string txtName = txt.gameObject.name.ToLower();
            if ((txtName.Contains("exp") || txtName.Contains("xp")) && !txtName.Contains("level") && !txtName.Contains("nivel"))
            {
                expText = txt;
                break;
            }
        }
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
            float lerpSpeed = Time.timeScale > 0 ? fillSpeed * Time.deltaTime : fillSpeed * Time.unscaledDeltaTime;
            currentFillAmount = Mathf.Lerp(currentFillAmount, targetFillAmount, lerpSpeed);
            
            RectTransform rt = expBarFill.rectTransform;
            rt.anchorMax = new Vector2(currentFillAmount, 1f);
        }
    }

    private void UpdateExperienceBar(int currentExp, int requiredExp)
    {
        targetFillAmount = requiredExp > 0 ? (float)currentExp / requiredExp : 0f;

        if (levelText != null && playerExperience != null)
        {
            int level = playerExperience.GetCurrentLevel();
            levelText.text = "NIVEL " + level;
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
