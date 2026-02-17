using TMPro;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LevelUpManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject levelUpPanel;
    [SerializeField] private TextMeshProUGUI levelUpText;
    
    [Header("Upgrade Buttons")]
    [SerializeField] private UpgradeButton upgradeButton1;
    [SerializeField] private UpgradeButton upgradeButton2;
    [SerializeField] private UpgradeButton upgradeButton3;

    [Header("Rainbow Text Settings")]
    [SerializeField] private float colorSpeed = 1f;

    private bool levelUpActive = false;
    private int currentPlayerLevel = 1;

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
        currentPlayerLevel = newLevel;

        Time.timeScale = 0f;

        if (levelUpText != null)
            levelUpText.text = $"LEVEL {newLevel}!";

        // Generar opciones de upgrade aleatorias
        GenerateUpgradeOptions();

        if (levelUpPanel != null)
            levelUpPanel.SetActive(true);
    }
    
    private void GenerateUpgradeOptions()
    {
        if (UpgradeDatabase.Instance == null)
        {
            Debug.LogError("UpgradeDatabase not found! Cannot generate upgrade options.");
            return;
        }
        
        if (PlayerStatsManager.Instance == null)
        {
            Debug.LogError("PlayerStatsManager not found! Cannot generate upgrade options.");
            return;
        }
        
        // Obtener niveles actuales de upgrades
        Dictionary<UpgradeType, int> currentLevels = PlayerStatsManager.Instance.GetAllUpgradeLevels();
        
        // Obtener 3 upgrades aleatorios
        List<UpgradeData> selectedUpgrades = UpgradeDatabase.Instance.GetRandomUpgrades(currentLevels, currentPlayerLevel);
        
        // Configurar botones
        if (upgradeButton1 != null)
        {
            if (selectedUpgrades.Count > 0)
            {
                int currentLevel = currentLevels.ContainsKey(selectedUpgrades[0].upgradeType) 
                    ? currentLevels[selectedUpgrades[0].upgradeType] 
                    : 0;
                upgradeButton1.Setup(selectedUpgrades[0], currentLevel);
            }
            else
            {
                upgradeButton1.gameObject.SetActive(false);
            }
        }
        
        if (upgradeButton2 != null)
        {
            if (selectedUpgrades.Count > 1)
            {
                int currentLevel = currentLevels.ContainsKey(selectedUpgrades[1].upgradeType) 
                    ? currentLevels[selectedUpgrades[1].upgradeType] 
                    : 0;
                upgradeButton2.Setup(selectedUpgrades[1], currentLevel);
            }
            else
            {
                upgradeButton2.gameObject.SetActive(false);
            }
        }
        
        if (upgradeButton3 != null)
        {
            if (selectedUpgrades.Count > 2)
            {
                int currentLevel = currentLevels.ContainsKey(selectedUpgrades[2].upgradeType) 
                    ? currentLevels[selectedUpgrades[2].upgradeType] 
                    : 0;
                upgradeButton3.Setup(selectedUpgrades[2], currentLevel);
            }
            else
            {
                upgradeButton3.gameObject.SetActive(false);
            }
        }
    }
    
    public void OnUpgradeChosen()
    {
        CloseLevelUp();
    }

    public void CloseLevelUp()
    {
        levelUpActive = false;

        if (levelUpPanel != null)
            levelUpPanel.SetActive(false);

        Time.timeScale = 1f;
    }
}

