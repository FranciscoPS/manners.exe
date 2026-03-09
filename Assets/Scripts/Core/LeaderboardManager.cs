using System.Collections.Generic;
using UnityEngine;

public class LeaderboardEntry
{
    public float SurvivalTime;
    public int Level;
    public int Kills;
    public int Coins;
    public int Gems;
}

public class LeaderboardManager : MonoBehaviour
{
    private static LeaderboardManager _instance;
    public static LeaderboardManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("LeaderboardManager");
                _instance = go.AddComponent<LeaderboardManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private const int MaxEntries = 5;
    private const string KeyPrefix = "LB_";
    private const string CountKey = "LB_Count";

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SubmitEntry(float survivalTime, int level, int kills, int coins, int gems)
    {
        List<LeaderboardEntry> entries = LoadEntries();

        entries.Add(new LeaderboardEntry
        {
            SurvivalTime = survivalTime,
            Level        = level,
            Kills        = kills,
            Coins        = coins,
            Gems         = gems
        });

        // Sort by survival time descending (longer = better)
        entries.Sort((a, b) => b.SurvivalTime.CompareTo(a.SurvivalTime));

        if (entries.Count > MaxEntries)
            entries.RemoveRange(MaxEntries, entries.Count - MaxEntries);

        SaveEntries(entries);
    }

    public List<LeaderboardEntry> LoadEntries()
    {
        var entries = new List<LeaderboardEntry>();
        int count = PlayerPrefs.GetInt(CountKey, 0);
        for (int i = 0; i < count; i++)
        {
            entries.Add(new LeaderboardEntry
            {
                SurvivalTime = PlayerPrefs.GetFloat(KeyPrefix + i + "_T", 0f),
                Level        = PlayerPrefs.GetInt(KeyPrefix + i + "_N", 0),
                Kills        = PlayerPrefs.GetInt(KeyPrefix + i + "_K", 0),
                Coins        = PlayerPrefs.GetInt(KeyPrefix + i + "_M", 0),
                Gems         = PlayerPrefs.GetInt(KeyPrefix + i + "_G", 0)
            });
        }
        return entries;
    }

    private void SaveEntries(List<LeaderboardEntry> entries)
    {
        PlayerPrefs.SetInt(CountKey, entries.Count);
        for (int i = 0; i < entries.Count; i++)
        {
            PlayerPrefs.SetFloat(KeyPrefix + i + "_T", entries[i].SurvivalTime);
            PlayerPrefs.SetInt(KeyPrefix + i + "_N", entries[i].Level);
            PlayerPrefs.SetInt(KeyPrefix + i + "_K", entries[i].Kills);
            PlayerPrefs.SetInt(KeyPrefix + i + "_M", entries[i].Coins);
            PlayerPrefs.SetInt(KeyPrefix + i + "_G", entries[i].Gems);
        }
        PlayerPrefs.Save();
    }

    public static string FormatTime(float seconds)
    {
        int m = Mathf.FloorToInt(seconds / 60f);
        int s = Mathf.FloorToInt(seconds % 60f);
        return $"{m:00}:{s:00}";
    }

    [ContextMenu("Clear Leaderboard")]
    public void ClearLeaderboard()
    {
        int count = PlayerPrefs.GetInt(CountKey, 0);
        for (int i = 0; i < count; i++)
        {
            PlayerPrefs.DeleteKey(KeyPrefix + i + "_T");
            PlayerPrefs.DeleteKey(KeyPrefix + i + "_N");
            PlayerPrefs.DeleteKey(KeyPrefix + i + "_K");
            PlayerPrefs.DeleteKey(KeyPrefix + i + "_M");
            PlayerPrefs.DeleteKey(KeyPrefix + i + "_G");
        }
        PlayerPrefs.DeleteKey(CountKey);
        PlayerPrefs.Save();
    }
}
