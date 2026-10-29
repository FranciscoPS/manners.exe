using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class ExperienceUI : MonoBehaviour, IUpdateable
{
    [Header("Textos editables por idioma")]
    [SerializeField] private LocalizedString levelLabel = new LocalizedString("Level ", "Nivel ");

    [Header("Animation Settings")]
    [SerializeField] private float fillSpeed = 5f;

    [SerializeField] private Image expBarFill;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI expText;
    [SerializeField] private RectTransform barRect;

    private PlayerExperience playerExperience;

    private float targetFillAmount = 0f;
    private float currentFillAmount = 0f;

    public bool IsActive => gameObject.activeInHierarchy && enabled;

    private string cachedExpText = "";
    private int lastCurrentExp = -1;
    private int lastRequiredExp = -1;

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

        if (UpdateManager.Instance != null)
        {
            UpdateManager.Instance.Register(this);
        }
    }

    private void OnDestroy()
    {
        if (ExperienceManager.Instance != null)
        {
            ExperienceManager.Instance.OnExperienceChanged -= UpdateExperienceBar;
            ExperienceManager.Instance.OnLevelUp -= HandleLevelUp;
        }

        if (UpdateManager.Instance != null)
        {
            UpdateManager.Instance.Unregister(this);
        }
    }

    public void OnUpdate(float deltaTime)
    {
        if (expBarFill != null)
        {
            float lerpSpeed = Time.timeScale > 0 ? fillSpeed * deltaTime : fillSpeed * Time.unscaledDeltaTime;
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
            levelText.text = levelLabel.Value + level;
        }

        if (expText != null && (currentExp != lastCurrentExp || requiredExp != lastRequiredExp))
        {
            cachedExpText = currentExp + " / " + requiredExp;
            expText.text = cachedExpText;
            lastCurrentExp = currentExp;
            lastRequiredExp = requiredExp;
        }
    }

    private void HandleLevelUp(int newLevel)
    {
        currentFillAmount = 0f;
        targetFillAmount = 0f;

        if (levelText != null)
        {
            levelText.text = levelLabel.Value + newLevel;
        }

        if (barRect != null)
        {
            barRect.DOKill();
            barRect.localScale = Vector3.one;
            barRect.DOPunchScale(new Vector3(0.012f, 0.35f, 0f), 0.4f, 6, 0.6f).SetUpdate(true);
        }
    }
    private void OnEnable() => GameLocalization.LanguageChanged += RefreshLanguage;
    private void OnDisable() => GameLocalization.LanguageChanged -= RefreshLanguage;
    private void RefreshLanguage()
    {
        if (levelText != null && playerExperience != null) levelText.text = levelLabel.Value + playerExperience.GetCurrentLevel();
    }
}
