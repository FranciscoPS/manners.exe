using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class GlobalLeaderboardService : MonoBehaviour
{
    private static GlobalLeaderboardService _instance;
    public static GlobalLeaderboardService Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("GlobalLeaderboardService");
                _instance = go.AddComponent<GlobalLeaderboardService>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private const string CacheKeyPrefix = "GLB_LB_";
    private const string CacheCountKey = "GLB_LB_Count";
    private const int MaxSlots = 10;
    private const int MaxSubmitAttempts = 3;

    [Serializable]
    private class ArrayWrapper
    {
        public LeaderboardEntry[] items;
    }

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void FetchTop(Action<List<LeaderboardEntry>> onComplete, Action onError)
    {
        StartCoroutine(FetchTopRoutine(onComplete, onError));
    }

    public void SubmitAndRefreshTop(LeaderboardEntry newEntry, Action<List<LeaderboardEntry>, int> onComplete, Action onError)
    {
        StartCoroutine(SubmitRoutine(newEntry, onComplete, onError));
    }

    public List<LeaderboardEntry> GetCachedTop()
    {
        var result = new List<LeaderboardEntry>();
        int count = PlayerPrefs.GetInt(CacheCountKey, 0);
        for (int i = 0; i < count; i++)
        {
            result.Add(new LeaderboardEntry
            {
                Initials = PlayerPrefs.GetString(CacheKeyPrefix + i + "_I", "---"),
                SurvivalTime = PlayerPrefs.GetFloat(CacheKeyPrefix + i + "_T", 0f),
                Level = PlayerPrefs.GetInt(CacheKeyPrefix + i + "_L", 0),
                Kills = PlayerPrefs.GetInt(CacheKeyPrefix + i + "_K", 0),
                Coins = PlayerPrefs.GetInt(CacheKeyPrefix + i + "_C", 0),
                Gems = PlayerPrefs.GetInt(CacheKeyPrefix + i + "_G", 0)
            });
        }
        return result;
    }

    private IEnumerator FetchTopRoutine(Action<List<LeaderboardEntry>> onComplete, Action onError)
    {
        LeaderboardConfig config = LeaderboardConfig.Instance;
        if (config == null || config.DatabaseUrl == null)
        {
            Debug.LogWarning("[Leaderboard] FetchTop: falta LeaderboardConfig o DatabaseUrl.");
            onError?.Invoke();
            yield break;
        }

        string url = $"{config.DatabaseUrl}/leaderboard.json";
        Debug.Log($"[Leaderboard] FetchTop GET {url}");

        using UnityWebRequest req = UnityWebRequest.Get(url);
        req.timeout = Mathf.CeilToInt(config.RequestTimeoutSeconds);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[Leaderboard] FetchTop FALLÓ: {req.result} ({req.responseCode}) {req.error}");
            onError?.Invoke();
            yield break;
        }

        Debug.Log($"[Leaderboard] FetchTop respuesta cruda: {req.downloadHandler.text}");

        List<LeaderboardEntry> entries = ParseSnapshot(req.downloadHandler.text);
        entries.Sort((a, b) => b.SurvivalTime.CompareTo(a.SurvivalTime));
        CacheLocally(entries);
        Debug.Log($"[Leaderboard] FetchTop OK: {entries.Count} entradas.");
        onComplete?.Invoke(entries);
    }

    private IEnumerator SubmitRoutine(LeaderboardEntry newEntry, Action<List<LeaderboardEntry>, int> onComplete, Action onError)
    {
        LeaderboardConfig config = LeaderboardConfig.Instance;
        if (config == null || config.DatabaseUrl == null)
        {
            Debug.LogWarning("[Leaderboard] SubmitAndRefreshTop: falta LeaderboardConfig o DatabaseUrl.");
            onError?.Invoke();
            yield break;
        }

        Debug.Log($"[Leaderboard] SubmitAndRefreshTop: iniciales={newEntry.Initials} tiempo={newEntry.SurvivalTime}");

        for (int attempt = 0; attempt < MaxSubmitAttempts; attempt++)
        {
            Debug.Log($"[Leaderboard] Submit intento {attempt + 1}/{MaxSubmitAttempts}: GET con ETag");

            string etag;
            List<LeaderboardEntry> current;

            using (UnityWebRequest getReq = UnityWebRequest.Get($"{config.DatabaseUrl}/leaderboard.json"))
            {
                getReq.SetRequestHeader("X-Firebase-ETag", "true");
                getReq.timeout = Mathf.CeilToInt(config.RequestTimeoutSeconds);
                yield return getReq.SendWebRequest();

                if (getReq.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"[Leaderboard] Submit GET FALLÓ: {getReq.result} ({getReq.responseCode}) {getReq.error}");
                    onError?.Invoke();
                    yield break;
                }

                etag = getReq.GetResponseHeader("ETag");
                current = ParseSnapshot(getReq.downloadHandler.text);
                Debug.Log($"[Leaderboard] Submit GET OK: {current.Count} entradas actuales, ETag={etag}");
            }

            current.Add(newEntry);
            current.Sort((a, b) => b.SurvivalTime.CompareTo(a.SurvivalTime));
            if (current.Count > config.MaxEntries)
                current.RemoveRange(config.MaxEntries, current.Count - config.MaxEntries);

            int rank = current.IndexOf(newEntry);
            string body = BuildSnapshotJson(current);
            Debug.Log($"[Leaderboard] Submit PUT body: {body}");

            using UnityWebRequest putReq = new UnityWebRequest($"{config.DatabaseUrl}/leaderboard.json", "PUT");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(body);
            putReq.uploadHandler = new UploadHandlerRaw(bodyRaw);
            putReq.downloadHandler = new DownloadHandlerBuffer();
            putReq.SetRequestHeader("Content-Type", "application/json");
            if (!string.IsNullOrEmpty(etag))
                putReq.SetRequestHeader("if-match", etag);
            putReq.timeout = Mathf.CeilToInt(config.RequestTimeoutSeconds);
            yield return putReq.SendWebRequest();

            if (putReq.result == UnityWebRequest.Result.Success)
            {
                CacheLocally(current);
                Debug.Log($"[Leaderboard] Submit OK: entrada quedó en rank {rank + 1}.");
                onComplete?.Invoke(current, rank);
                yield break;
            }

            Debug.LogWarning($"[Leaderboard] Submit PUT FALLÓ: {putReq.result} ({putReq.responseCode}) {putReq.error} | respuesta: {putReq.downloadHandler.text}");

            if (putReq.responseCode != 412)
            {
                onError?.Invoke();
                yield break;
            }

            Debug.Log("[Leaderboard] 412 Precondition Failed, reintentando...");
        }

        Debug.LogWarning("[Leaderboard] Submit agotó los reintentos.");
        onError?.Invoke();
    }

    private static List<LeaderboardEntry> ParseSnapshot(string raw)
    {
        var result = new List<LeaderboardEntry>();
        if (string.IsNullOrEmpty(raw) || raw == "null")
            return result;

        ArrayWrapper wrapper = JsonUtility.FromJson<ArrayWrapper>("{\"items\":" + raw + "}");
        if (wrapper?.items == null)
            return result;

        result.AddRange(wrapper.items);
        return result;
    }

    private static string BuildSnapshotJson(List<LeaderboardEntry> entries)
    {
        var sb = new StringBuilder();
        sb.Append('[');
        int count = Mathf.Min(entries.Count, MaxSlots);
        for (int i = 0; i < count; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append(JsonUtility.ToJson(entries[i]));
        }
        sb.Append(']');
        return sb.ToString();
    }

    private static void CacheLocally(List<LeaderboardEntry> entries)
    {
        PlayerPrefs.SetInt(CacheCountKey, entries.Count);
        for (int i = 0; i < entries.Count; i++)
        {
            PlayerPrefs.SetString(CacheKeyPrefix + i + "_I", entries[i].Initials ?? "");
            PlayerPrefs.SetFloat(CacheKeyPrefix + i + "_T", entries[i].SurvivalTime);
            PlayerPrefs.SetInt(CacheKeyPrefix + i + "_L", entries[i].Level);
            PlayerPrefs.SetInt(CacheKeyPrefix + i + "_K", entries[i].Kills);
            PlayerPrefs.SetInt(CacheKeyPrefix + i + "_C", entries[i].Coins);
            PlayerPrefs.SetInt(CacheKeyPrefix + i + "_G", entries[i].Gems);
        }
        PlayerPrefs.Save();
    }
}
