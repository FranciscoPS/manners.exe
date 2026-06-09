using UnityEngine;
using UnityEngine.SceneManagement;

public class GameTimeManager : MonoBehaviour, IUpdateable
{
    private static GameTimeManager instance;
    private static bool isQuitting = false;

    public static GameTimeManager Instance
    {
        get
        {
            if (instance == null && !isQuitting)
            {
                GameObject go = new GameObject("GameTimeManager");
                instance = go.AddComponent<GameTimeManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    [Header("Match Duration")]
    [Tooltip("Duracion de la partida en minutos. El cronometro cuenta hacia atras desde aqui; al llegar a 0 se lanza la oleada final imposible.")]
    [SerializeField] private float matchDurationMinutes = 15f;

    private float gameStartTime;
    private bool isGameActive = false;
    private int lastSecond = -1;
    private bool matchTimeExpired = false;

    private float MatchDuration => matchDurationMinutes * 60f;

    public bool IsActive => isGameActive && this != null && enabled;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        isQuitting = false;
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;

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

            if (UpdateManager.Instance != null)
            {
                UpdateManager.Instance.Unregister(this);
            }

            instance = null;
        }
    }

    private void OnApplicationQuit()
    {
        isQuitting = true;
    }

    public void OnUpdate(float deltaTime)
    {
        if (!isGameActive) return;

        int currentSecond = Mathf.FloorToInt(GetGameTime());
        if (currentSecond != lastSecond)
        {
            lastSecond = currentSecond;
            string formattedTime = GetFormattedCountdown();
            GameEvents.TriggerGameTimeUpdated(formattedTime);
        }

        // Al agotarse el tiempo de partida, lanza la oleada final imposible (una sola vez).
        if (!matchTimeExpired && GetRemainingTime() <= 0f)
        {
            matchTimeExpired = true;
            GameEvents.TriggerMatchTimeExpired();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {

        if (!scene.name.Contains("Menu") && !scene.name.Contains("MainMenu"))
        {
            StartGame();
        }
    }

    private void Start()
    {
        StartGame();
    }

    public void StartGame()
    {
        gameStartTime = Time.time;
        isGameActive = true;
        matchTimeExpired = false;
        lastSecond = -1;

        if (GameSessionStats.Instance != null)
        {
            GameSessionStats.Instance.StartSession();
        }

        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.PlayMusic();
        }
    }

    public void StopGame()
    {
        isGameActive = false;
    }

    public void ResetGame()
    {
        gameStartTime = Time.time;
        isGameActive = true;
        matchTimeExpired = false;
        lastSecond = -1;

        if (GameSessionStats.Instance != null)
        {
            GameSessionStats.Instance.StartSession();
        }

        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.PlayMusic();
        }
    }

    public float GetGameTime()
    {
        if (!isGameActive)
            return 0f;

        return Time.time - gameStartTime;
    }

    /// <summary>Tiempo restante de partida en segundos (cuenta regresiva, nunca negativo).</summary>
    public float GetRemainingTime()
    {
        if (!isGameActive)
            return MatchDuration;

        return Mathf.Max(0f, MatchDuration - GetGameTime());
    }

    /// <summary>Tiempo restante formateado MM:SS para el cronometro de cuenta regresiva.</summary>
    public string GetFormattedCountdown()
    {
        float remaining = GetRemainingTime();
        int minutes = Mathf.FloorToInt(remaining / 60f);
        int seconds = Mathf.FloorToInt(remaining % 60f);
        return $"{minutes:00}:{seconds:00}";
    }

    public string GetFormattedTime()
    {
        float gameTime = GetGameTime();
        int minutes = Mathf.FloorToInt(gameTime / 60f);
        int seconds = Mathf.FloorToInt(gameTime % 60f);
        return $"{minutes:00}:{seconds:00}";
    }

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
