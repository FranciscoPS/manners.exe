using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class LeaderboardUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI leaderboardText;

    private void Start()
    {
        _started = true;
        Refresh();
    }

    private void OnEnable()
    {
        // Solo refresca si ya pasó Start (panel activado en runtime)
        if (_started) Refresh();
    }

    private bool _started;

    public void Refresh()
    {
        if (leaderboardText == null) return;

        List<LeaderboardEntry> entries = LeaderboardManager.Instance != null
            ? LeaderboardManager.Instance.LoadEntries()
            : new List<LeaderboardEntry>();

        var sb = new StringBuilder();

        for (int i = 0; i < 5; i++)
        {
            if (i < entries.Count)
                sb.AppendLine(FormatEntry(i + 1, entries[i]));
            else
                sb.AppendLine($"#{i + 1}  ---");
        }

        leaderboardText.text = sb.ToString().TrimEnd();
    }

    private static string FormatEntry(int rank, LeaderboardEntry e)
    {
        return $"#{rank}  T: {LeaderboardManager.FormatTime(e.SurvivalTime)}  |  N: {e.Level:0000}  |  K: {e.Kills:0000}  |  M: {e.Coins:0000}";
    }
}
