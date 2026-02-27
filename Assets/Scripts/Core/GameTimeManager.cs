using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Maneja el tiempo total de la partida actual
/// Refactorizado para usar UpdateManager y eventos
/// </summary>
public class GameTimeManager : MonoBehaviour, IUpdateable
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
    private int lastSecond = -1; // Para detectar cambios de segundo
    private bool showHours = false;

    // IUpdateable implementation
    public bool IsActive => isGameActive && this != null && enabled;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
            
            // Registrar con UpdateManager
            if (UpdateManager.Instance != null)
            {
                UpdateManager.Instance.Register(this);
            }
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            
            // Unregister del UpdateManager
            if (UpdateManager.Instance != null)
            {
                UpdateManager.Instance.Unregister(this);
            }
        }
    }

    // IUpdateable implementation
    public void OnUpdate(float deltaTime)
    {
        if (!isGameActive) return;
        
        // Solo disparar evento cuando cambia el segundo (reduce de 60 FPS a 1 Hz)
        int currentSecond = Mathf.FloorToInt(GetGameTime());
        if (currentSecond != lastSecond)
        {
            lastSecond = currentSecond;
            string formattedTime = showHours ? GetFormattedTimeLong() : GetFormattedTime();
            GameEvents.TriggerGameTimeUpdated(formattedTime);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Si cargamos una escena que NO es el menú principal, iniciar música y timer
        if (!scene.name.Contains("Menu") && !scene.name.Contains("MainMenu"))
        {
            StartGame();
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
        
        // Iniciar sesión de estadísticas
        if (GameSessionStats.Instance != null)
        {
            GameSessionStats.Instance.StartSession();
        }
        
        // Iniciar la música del juego
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.PlayMusic();
        }
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
        
        // Reiniciar sesión de estadísticas
        if (GameSessionStats.Instance != null)
        {
            GameSessionStats.Instance.StartSession();
        }
        
        // Reiniciar la música del juego
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.PlayMusic();
        }
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
