using UnityEngine;

[CreateAssetMenu(fileName = "LeaderboardConfig", menuName = "Game/Leaderboard Configuration")]
public class LeaderboardConfig : ScriptableObject
{
    private static LeaderboardConfig instance;
    public static LeaderboardConfig Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<LeaderboardConfig>("LeaderboardConfig");
            }
            return instance;
        }
    }

    [SerializeField] private string databaseUrl;
    [SerializeField] private int maxEntries = 5;
    [SerializeField] private float requestTimeoutSeconds = 8f;

    public string DatabaseUrl => string.IsNullOrEmpty(databaseUrl) ? null : databaseUrl.TrimEnd('/');
    public int MaxEntries => maxEntries;
    public float RequestTimeoutSeconds => requestTimeoutSeconds;
}
