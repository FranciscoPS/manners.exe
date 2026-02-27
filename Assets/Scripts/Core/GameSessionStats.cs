using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Rastrea las estadísticas de la sesión actual de juego
/// </summary>
public class GameSessionStats : MonoBehaviour
{
    private static GameSessionStats instance;
    public static GameSessionStats Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("GameSessionStats");
                instance = go.AddComponent<GameSessionStats>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    // Estadísticas de la sesión
    private int enemiesKilled = 0;
    private int buildingsDestroyed = 0;
    private int coinsCollectedThisSession = 0;
    private int diamondsCollectedThisSession = 0;
    private int maxLevelReached = 1;
    private float survivalTime = 0f;
    private bool isSessionActive = false;

    // Propiedades públicas para acceso de lectura
    public int EnemiesKilled => enemiesKilled;
    public int BuildingsDestroyed => buildingsDestroyed;
    public int CoinsCollected => coinsCollectedThisSession;
    public int DiamondsCollected => diamondsCollectedThisSession;
    public int MaxLevelReached => maxLevelReached;
    public float SurvivalTime => survivalTime;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Obtiene el tiempo de supervivencia actualizado (calculado on-demand, no cada frame)
    /// </summary>
    private float GetCurrentSurvivalTime()
    {
        if (isSessionActive && GameTimeManager.Instance != null)
        {
            return GameTimeManager.Instance.GetGameTime();
        }
        return survivalTime;
    }
    
    // Propiedad pública actualizada para obtener el tiempo on-demand
    public float SurvivalTimeUpdated => GetCurrentSurvivalTime();

    /// <summary>
    /// Inicia una nueva sesión de estadísticas
    /// </summary>
    public void StartSession()
    {
        ResetStats();
        isSessionActive = true;
    }

    /// <summary>
    /// Detiene la sesión actual y captura el tiempo final de supervivencia
    /// </summary>
    public void EndSession()
    {
        // Capturar el tiempo final antes de detener la sesión
        if (isSessionActive && GameTimeManager.Instance != null)
        {
            survivalTime = GameTimeManager.Instance.GetGameTime();
        }
        isSessionActive = false;
    }

    /// <summary>
    /// Reinicia todas las estadísticas
    /// </summary>
    public void ResetStats()
    {
        enemiesKilled = 0;
        buildingsDestroyed = 0;
        coinsCollectedThisSession = 0;
        diamondsCollectedThisSession = 0;
        maxLevelReached = 1;
        survivalTime = 0f;
    }

    /// <summary>
    /// Incrementa el contador de enemigos eliminados
    /// </summary>
    public void RegisterEnemyKill()
    {
        enemiesKilled++;
    }

    /// <summary>
    /// Incrementa el contador de edificios destruidos
    /// </summary>
    public void RegisterBuildingDestroyed()
    {
        buildingsDestroyed++;
    }

    /// <summary>
    /// Registra monedas recolectadas
    /// </summary>
    public void RegisterCoinsCollected(int amount)
    {
        coinsCollectedThisSession += amount;
    }

    /// <summary>
    /// Registra diamantes recolectados
    /// </summary>
    public void RegisterDiamondsCollected(int amount)
    {
        diamondsCollectedThisSession += amount;
    }

    /// <summary>
    /// Actualiza el nivel máximo alcanzado
    /// </summary>
    public void UpdateMaxLevel(int level)
    {
        if (level > maxLevelReached)
        {
            maxLevelReached = level;
        }
    }

    /// <summary>
    /// Obtiene el tiempo de supervivencia formateado
    /// </summary>
    public string GetFormattedSurvivalTime()
    {
        // Obtener el tiempo actual (no el cacheado)
        float currentTime = GetCurrentSurvivalTime();
        int minutes = Mathf.FloorToInt(currentTime / 60f);
        int seconds = Mathf.FloorToInt(currentTime % 60f);
        return $"{minutes:00}:{seconds:00}";
    }

    /// <summary>
    /// Obtiene todos los niveles de mejoras del jugador
    /// </summary>
    public Dictionary<UpgradeType, int> GetUpgradeLevels()
    {
        if (PlayerStatsManager.Instance != null)
        {
            return PlayerStatsManager.Instance.GetAllUpgradeLevels();
        }
        return new Dictionary<UpgradeType, int>();
    }

    /// <summary>
    /// Obtiene el nombre legible de un tipo de mejora
    /// </summary>
    public string GetUpgradeDisplayName(UpgradeType upgradeType)
    {
        switch (upgradeType)
        {
            case UpgradeType.Damage:
                return "Damage";
            case UpgradeType.AttackSpeed:
                return "Attack Speed";
            case UpgradeType.AttackRange:
                return "Attack Range";
            case UpgradeType.MoveSpeed:
                return "Move Speed";
            case UpgradeType.MagnetRange:
                return "Magnet Range";
            case UpgradeType.MultiShot:
                return "Multi Shot";
            case UpgradeType.ExplosiveShot:
                return "Explosive Shot";
            case UpgradeType.Knockback:
                return "Knockback";
            default:
                return upgradeType.ToString();
        }
    }
}
