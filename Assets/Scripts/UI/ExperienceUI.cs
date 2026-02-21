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
        if (expBarPanel == null)
        {
            Debug.LogError("[ExperienceUI] No se encontró ExpBarPanel como hijo de Canvas");
            return;
        }
        
        // Buscar ExpBarFill dentro de ExpBarPanel (debe ser tipo Sliced con nombre ExpBarFill)
        Image[] allImages = expBarPanel.GetComponentsInChildren<Image>(true);
        foreach (var img in allImages)
        {
            if (img.gameObject.name.Contains("ExpBarFill") || img.gameObject.name.Contains("Fill"))
            {
                expBarFill = img;
                break;
            }
        }
        
        if (expBarFill == null)
        {
            Debug.LogError($"[ExperienceUI] No se encontró ExpBarFill.");
        }

        // Buscar LevelText dentro de ExpBarPanel
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
        
        if (levelText == null)
        {
            Debug.LogError($"[ExperienceUI] No se encontró levelText en ExpBarPanel.");
        }

        // Buscar ExpText dentro de ExpBarPanel
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
        else
        {
            Debug.LogError("[ExperienceUI] ExperienceManager.Instance es NULL!");
        }

        if (playerExperience != null)
        {
            int currentExp = playerExperience.GetCurrentExperience();
            int requiredExp = playerExperience.GetExperienceRequiredForNextLevel();
            UpdateExperienceBar(currentExp, requiredExp);
        }
        else
        {
            Debug.LogError("[ExperienceUI] PlayerExperience es NULL!");
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
            // Use deltaTime for normal gameplay (respects Time.timeScale = 0 pause)
            float lerpSpeed = Time.timeScale > 0 ? fillSpeed * Time.deltaTime : fillSpeed * Time.unscaledDeltaTime;
            currentFillAmount = Mathf.Lerp(currentFillAmount, targetFillAmount, lerpSpeed);
            
            // Update anchorMax.x for Sliced type Image (horizontal fill left to right)
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
