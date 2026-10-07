using System;
using UnityEngine;

[Serializable]
public class LeaderboardEntry
{
    public string Initials;
    public float SurvivalTime;
    public int Level;
    public int Kills;
    public int Coins;
    public int Gems;

    public static string FormatTime(float seconds)
    {
        int m = Mathf.FloorToInt(seconds / 60f);
        int s = Mathf.FloorToInt(seconds % 60f);
        return $"{m:00}:{s:00}";
    }

    public static string RankSuffix(int rank)
    {
        switch (rank)
        {
            case 1: return "ST";
            case 2: return "ND";
            case 3: return "RD";
            default: return "TH";
        }
    }
}
