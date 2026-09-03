using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SynergyManager : MonoBehaviour
{
    private static SynergyManager instance;
    private static bool isQuitting;

    public static SynergyManager Instance => instance;

    public event Action<SynergyData> OnSynergyActivated;
    public event Action<SynergyData> OnSynergyDeactivated;

    private readonly Dictionary<SynergyData, GameObject> activeEffects = new Dictionary<SynergyData, GameObject>();
    private bool synergiesEnabled = true;
    private Transform player;

    public bool SynergiesEnabled => synergiesEnabled;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        isQuitting = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        EnsureExists();
    }

    public static void EnsureExists()
    {
        if (isQuitting || instance != null) return;

        GameObject go = new GameObject("SynergyManager");
        instance = go.AddComponent<SynergyManager>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            instance = null;
        }
    }

    private void OnApplicationQuit()
    {
        isQuitting = true;
    }

    private void OnEnable()
    {
        SubscribeToPlayerStats();
    }

    private void OnDisable()
    {
        if (PlayerStatsManager.Instance != null)
            PlayerStatsManager.Instance.OnUpgradeApplied -= HandleUpgradeApplied;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ClearActiveEffects();
        SynergyDiscovery.BeginRun();
        player = null;
        SubscribeToPlayerStats();
    }

    private void SubscribeToPlayerStats()
    {
        if (PlayerStatsManager.Instance == null) return;

        PlayerStatsManager.Instance.OnUpgradeApplied -= HandleUpgradeApplied;
        PlayerStatsManager.Instance.OnUpgradeApplied += HandleUpgradeApplied;
    }

    public void SetEnabled(bool value)
    {
        synergiesEnabled = value;

        if (!synergiesEnabled)
        {
            ClearActiveEffects();
            Debug.Log("[SYNERGY] Sinergias desactivadas.");
        }
        else
        {
            Debug.Log("[SYNERGY] Sinergias activadas.");
            CheckAllSynergies();
        }
    }

    public bool IsSynergyActive(SynergyData synergy)
    {
        return synergy != null && activeEffects.ContainsKey(synergy);
    }

    public void ForceActivate(SynergyData synergy)
    {
        if (synergy == null || activeEffects.ContainsKey(synergy)) return;

        Transform playerTransform = GetPlayer();
        if (playerTransform == null)
        {
            Debug.LogWarning($"[SYNERGY] No se pudo forzar '{synergy.synergyName}': no se encontró al jugador.");
            return;
        }

        Activate(synergy, playerTransform);
    }

    private void HandleUpgradeApplied(UpgradeType type, int level)
    {
        SynergyDiscovery.RecordUpgradeLevel(type, level);
        CheckAllSynergies();
    }

    private void CheckAllSynergies()
    {
        if (!synergiesEnabled) return;

        SynergyDatabase database = SynergyDatabase.Instance;
        if (database == null || database.allSynergies == null) return;

        Transform playerTransform = GetPlayer();
        if (playerTransform == null) return;

        for (int i = 0; i < database.allSynergies.Count; i++)
        {
            SynergyData synergy = database.allSynergies[i];
            if (synergy == null || activeEffects.ContainsKey(synergy)) continue;

            if (RequirementsMet(synergy))
            {
                SynergyDiscovery.RecordSynergyUnlocked(synergy);
                Activate(synergy, playerTransform);
            }
        }
    }

    private bool RequirementsMet(SynergyData synergy)
    {
        if (PlayerStatsManager.Instance == null) return false;

        int levelA = PlayerStatsManager.Instance.GetUpgradeLevel(synergy.requiredUpgradeA);
        int levelB = PlayerStatsManager.Instance.GetUpgradeLevel(synergy.requiredUpgradeB);

        return levelA >= synergy.requiredLevelA && levelB >= synergy.requiredLevelB;
    }

    private void Activate(SynergyData synergy, Transform playerTransform)
    {
        if (synergy.effectPrefab == null)
        {
            Debug.LogWarning($"[SYNERGY] '{synergy.synergyName}' no tiene effectPrefab asignado.");
            return;
        }

        GameObject instance = Instantiate(synergy.effectPrefab);

        if (synergy.effectConfig != null)
            synergy.effectConfig.ApplyTo(instance);
        else
            Debug.LogWarning($"[SYNERGY] '{synergy.synergyName}' no tiene effectConfig asignado; usará valores por defecto.");

        ISynergyEffect effect = instance.GetComponent<ISynergyEffect>();

        if (effect == null)
        {
            Debug.LogWarning($"[SYNERGY] El prefab de '{synergy.synergyName}' no implementa ISynergyEffect.");
            Destroy(instance);
            return;
        }

        effect.Activate(playerTransform);
        activeEffects[synergy] = instance;

        Debug.Log($"[SYNERGY] Desbloqueada: {synergy.synergyName}");
        OnSynergyActivated?.Invoke(synergy);
    }

    private void ClearActiveEffects()
    {
        foreach (KeyValuePair<SynergyData, GameObject> pair in activeEffects)
        {
            if (pair.Value != null)
                Destroy(pair.Value);

            OnSynergyDeactivated?.Invoke(pair.Key);
        }

        activeEffects.Clear();
    }

    private Transform GetPlayer()
    {
        if (player != null) return player;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        player = playerObject != null ? playerObject.transform : null;
        return player;
    }
}
