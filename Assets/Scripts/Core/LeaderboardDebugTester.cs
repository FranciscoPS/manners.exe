using UnityEngine;

public class LeaderboardDebugTester : MonoBehaviour
{
    [SerializeField] private string testInitials = "TST";
    [SerializeField] private float minTestSurvivalTime = 30f;
    [SerializeField] private float maxTestSurvivalTime = 600f;

    private void Start()
    {
        var entry = new LeaderboardEntry
        {
            Initials = testInitials,
            SurvivalTime = Random.Range(minTestSurvivalTime, maxTestSurvivalTime),
            Level = Random.Range(1, 20),
            Kills = Random.Range(0, 200),
            Coins = Random.Range(0, 500),
            Gems = Random.Range(0, 50)
        };

        Debug.Log($"[LeaderboardDebugTester] Enviando entrada de prueba: {entry.Initials} - {LeaderboardEntry.FormatTime(entry.SurvivalTime)}");

        GlobalLeaderboardService.Instance.SubmitAndRefreshTop(
            entry,
            (list, rank) =>
            {
                Debug.Log($"[LeaderboardDebugTester] Envío OK, quedó en el puesto {rank + 1} de {list.Count}.");
                FindFirstObjectByType<LeaderboardUI>()?.Refresh();
            },
            () => Debug.LogError("[LeaderboardDebugTester] El envío falló. Revisá los logs de [Leaderboard] arriba para el detalle.")
        );
    }
}
