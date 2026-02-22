using UnityEngine;

/// <summary>
/// Maneja el tiempo total de la partida actual
/// </summary>
public class GameTimeManager : MonoBehaviour
{
    private static GameTimeManager instance;
    public static GameTimeManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("GameTimeManager");
                instance = go.AddComponent<GameTimeManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    private float gameStartTime;
    private bool isGameActive = false;

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
            return;
        }
    }

    private void Start()
    {
        StartGame();
    }

    /// <summary>
    /// Inicia el contador de tiempo de la partida
    /// </summary>
    public void StartGame()
    {
        gameStartTime = Time.time;
        isGameActive = true;
    }

    /// <summary>
    /// Detiene el contador de tiempo
    /// </summary>
    public void StopGame()
    {
        isGameActive = false;
    }

    /// <summary>
    /// Reinicia el contador de tiempo
    /// </summary>
    public void ResetGame()
    {
        gameStartTime = Time.time;
        isGameActive = true;
    }

    /// <summary>
    /// Obtiene el tiempo transcurrido en la partida actual en segundos
    /// </summary>
    public float GetGameTime()
    {
        if (!isGameActive)
            return 0f;
        
        return Time.time - gameStartTime;
    }

    /// <summary>
    /// Obtiene el tiempo de partida formateado como MM:SS
    /// </summary>
    public string GetFormattedTime()
    {
        float gameTime = GetGameTime();
        int minutes = Mathf.FloorToInt(gameTime / 60f);
        int seconds = Mathf.FloorToInt(gameTime % 60f);
        return $"{minutes:00}:{seconds:00}";
    }

    /// <summary>
    /// Obtiene el tiempo de partida formateado como HH:MM:SS para partidas largas
    /// </summary>
    public string GetFormattedTimeLong()
    {
        float gameTime = GetGameTime();
        int hours = Mathf.FloorToInt(gameTime / 3600f);
        int minutes = Mathf.FloorToInt((gameTime % 3600f) / 60f);
        int seconds = Mathf.FloorToInt(gameTime % 60f);
        
        if (hours > 0)
            return $"{hours:00}:{minutes:00}:{seconds:00}";
        else
            return $"{minutes:00}:{seconds:00}";
    }

    public bool IsGameActive => isGameActive;
}
