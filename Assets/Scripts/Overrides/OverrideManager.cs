using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class OverrideManager : MonoBehaviour
{
    private static OverrideManager instance;
    private static bool isQuitting;

    public static OverrideManager Instance => instance;

    public event Action<OverrideData> OnOverrideActivated;
    public event Action<OverrideData> OnOverrideDeactivated;

    private readonly Dictionary<OverrideData, GameObject> activeEffects = new Dictionary<OverrideData, GameObject>();
    private bool overridesEnabled = true;
    private Transform player;

    public bool OverridesEnabled => overridesEnabled;

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

        GameObject go = new GameObject("OverrideManager");
        instance = go.AddComponent<OverrideManager>();
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
        OverrideDiscovery.BeginRun();
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
        overridesEnabled = value;

        if (!overridesEnabled)
        {
            ClearActiveEffects();
            Debug.Log("[OVERRIDE] Sobrecargas desactivadas.");
        }
        else
        {
            Debug.Log("[OVERRIDE] Sobrecargas activadas.");
            CheckAllOverrides();
        }
    }

    public bool IsOverrideActive(OverrideData overrideData)
    {
        return overrideData != null && activeEffects.ContainsKey(overrideData);
    }

    // Only running effects contribute to combinations; database entries alone
    // must never grant an override that the player has not unlocked.
    public T GetActiveConfig<T>() where T : OverrideEffectConfig
    {
        if (!overridesEnabled) return null;

        foreach (KeyValuePair<OverrideData, GameObject> pair in activeEffects)
        {
            if (pair.Value == null || !pair.Value.activeInHierarchy || !(pair.Key.effectConfig is T effectConfig))
                continue;
            Behaviour effect = pair.Value.GetComponent<IOverrideEffect>() as Behaviour;
            if (effect == null || effect.isActiveAndEnabled) return effectConfig;
        }

        return null;
    }

    public void ForceActivate(OverrideData overrideData)
    {
        if (overrideData == null || activeEffects.ContainsKey(overrideData)) return;

        Transform playerTransform = GetPlayer();
        if (playerTransform == null)
        {
            Debug.LogWarning($"[OVERRIDE] No se pudo forzar '{overrideData.overrideName}': no se encontró al jugador.");
            return;
        }

        Activate(overrideData, playerTransform);
    }

    private void HandleUpgradeApplied(UpgradeType type, int level)
    {
        OverrideDiscovery.RecordUpgradeLevel(type, level);
        CheckAllOverrides();
    }

    private void CheckAllOverrides()
    {
        if (!overridesEnabled) return;

        OverrideDatabase database = OverrideDatabase.Instance;
        if (database == null || database.allOverrides == null) return;

        Transform playerTransform = GetPlayer();
        if (playerTransform == null) return;

        for (int i = 0; i < database.allOverrides.Count; i++)
        {
            OverrideData overrideData = database.allOverrides[i];
            if (overrideData == null || activeEffects.ContainsKey(overrideData)) continue;

            if (RequirementsMet(overrideData))
            {
                OverrideDiscovery.RecordOverrideUnlocked(overrideData);
                Activate(overrideData, playerTransform);
            }
        }
    }

    private bool RequirementsMet(OverrideData overrideData)
    {
        if (PlayerStatsManager.Instance == null) return false;

        int levelA = PlayerStatsManager.Instance.GetUpgradeLevel(overrideData.requiredUpgradeA);
        int levelB = PlayerStatsManager.Instance.GetUpgradeLevel(overrideData.requiredUpgradeB);

        return levelA >= overrideData.requiredLevelA && levelB >= overrideData.requiredLevelB;
    }

    private void Activate(OverrideData overrideData, Transform playerTransform)
    {
        if (overrideData.effectPrefab == null)
        {
            Debug.LogWarning($"[OVERRIDE] '{overrideData.overrideName}' no tiene effectPrefab asignado.");
            return;
        }

        GameObject instance = Instantiate(overrideData.effectPrefab);

        if (overrideData.effectConfig != null)
            overrideData.effectConfig.ApplyTo(instance);
        else
            Debug.LogWarning($"[OVERRIDE] '{overrideData.overrideName}' no tiene effectConfig asignado; usará valores por defecto.");

        IOverrideEffect effect = instance.GetComponent<IOverrideEffect>();

        if (effect == null)
        {
            Debug.LogWarning($"[OVERRIDE] El prefab de '{overrideData.overrideName}' no implementa IOverrideEffect.");
            Destroy(instance);
            return;
        }

        effect.Activate(playerTransform);
        activeEffects[overrideData] = instance;

        Debug.Log($"[OVERRIDE] Desbloqueada: {overrideData.overrideName}");
        OnOverrideActivated?.Invoke(overrideData);
    }

    private void ClearActiveEffects()
    {
        OverrideProcVisual.ClearAll();
        foreach (KeyValuePair<OverrideData, GameObject> pair in activeEffects)
        {
            if (pair.Value != null)
                Destroy(pair.Value);

            OnOverrideDeactivated?.Invoke(pair.Key);
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
