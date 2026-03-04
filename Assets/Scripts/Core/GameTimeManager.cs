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

    private float gameStartTime;
    private bool isGameActive = false;
    private int lastSecond = -1;
    private bool showHours = false;

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
            string formattedTime = showHours ? GetFormattedTimeLong() : GetFormattedTime();
            GameEvents.TriggerGameTimeUpdated(formattedTime);
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
