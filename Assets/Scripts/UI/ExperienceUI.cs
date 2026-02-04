using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ExperienceUI : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float fillSpeed = 5f;
    [SerializeField] private float levelUpDisplayTime = 2f;
    
    private Image expBarFill;
    private TextMeshProUGUI levelText;
    private TextMeshProUGUI expText;
    private GameObject levelUpPanel;
    
    private PlayerExperience playerExperience;
    private float targetFillAmount = 0f;
    private float currentFillAmount = 0f;

    private void Awake()
    {
        Transform expBarFillTransform = transform.Find("ExpBarPanel/ExpBarBackground/ExpBarFill");
        if (expBarFillTransform != null)
        {
            expBarFill = expBarFillTransform.GetComponent<Image>();
            Debug.Log("ExpBarFill encontrado");
        }
        else
        {
            Debug.LogError("No se encontró ExpBarFill en la ruta: ExpBarPanel/ExpBarBackground/ExpBarFill");
        }

        Transform levelTextTransform = transform.Find("ExpBarPanel/LevelText");
        if (levelTextTransform != null)
        {
            levelText = levelTextTransform.GetComponent<TextMeshProUGUI>();
            Debug.Log("LevelText encontrado");
        }
        else
        {
            Debug.LogError("No se encontró LevelText en la ruta: ExpBarPanel/LevelText");
        }

        Transform expTextTransform = transform.Find("ExpBarPanel/ExpText");
        if (expTextTransform != null)
            expText = expTextTransform.GetComponent<TextMeshProUGUI>();

        Transform levelUpPanelTransform = transform.Find("LevelUpPanel");
        if (levelUpPanelTransform != null)
            levelUpPanel = levelUpPanelTransform.gameObject;
    }

    private void Start()
    {
        playerExperience = FindFirstObjectByType<PlayerExperience>();
        
        if (playerExperience == null)
        {
            Debug.LogError("No se encontró PlayerExperience en la escena");
        }
        
        if (levelUpPanel != null)
            levelUpPanel.SetActive(false);

        if (ExperienceManager.Instance != null)
        {
            ExperienceManager.Instance.OnExperienceChanged += UpdateExperienceBar;
            ExperienceManager.Instance.OnLevelUp += OnLevelUp;
            Debug.Log("ExperienceManager conectado");
        }
        else
        {
            Debug.LogError("No se encontró ExperienceManager en la escena");
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
            ExperienceManager.Instance.OnLevelUp -= OnLevelUp;
        }
    }

    private void Update()
    {
        if (expBarFill != null)
        {
            currentFillAmount = Mathf.Lerp(currentFillAmount, targetFillAmount, fillSpeed * Time.deltaTime);
            
            RectTransform rt = expBarFill.rectTransform;
            rt.anchorMax = new Vector2(currentFillAmount, 1f);
        }
    }

    private void UpdateExperienceBar(int currentExp, int requiredExp)
    {
        targetFillAmount = requiredExp > 0 ? (float)currentExp / requiredExp : 0f;
        
        Debug.Log($"Exp: {currentExp}/{requiredExp} - Fill: {targetFillAmount}");

        if (levelText != null && playerExperience != null)
        {
            levelText.text = "NIVEL " + playerExperience.GetCurrentLevel();
        }

        if (expText != null)
        {
            expText.text = currentExp + " / " + requiredExp;
        }
    }

    private void OnLevelUp(int newLevel)
    {
        currentFillAmount = 0f;
        
        if (levelUpPanel != null)
        {
            levelUpPanel.SetActive(true);
            TextMeshProUGUI levelUpText = levelUpPanel.GetComponentInChildren<TextMeshProUGUI>();
            if (levelUpText != null)
            {
                levelUpText.text = "NIVEL " + newLevel;
            }
            Invoke(nameof(HideLevelUpPanel), levelUpDisplayTime);
        }
    }

    private void HideLevelUpPanel()
    {
        if (levelUpPanel != null)
            levelUpPanel.SetActive(false);
    }
}
