using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class OverrideHudPanel : MonoBehaviour
{
    public static OverrideHudPanel Instance { get; private set; }

    [Header("Filas")]
    [Tooltip("Filas del panel (una por sobrecarga). Si se deja vacío se toman todas las OverrideHintRowUI hijas.")]
    [SerializeField] private OverrideHintRowUI[] rows;

    [Header("Pop del cuadro de resultado al aterrizar el icono")]
    [SerializeField] private float landPopScale = 1.3f;
    [SerializeField] private float landPopDuration = 0.35f;

    private readonly HashSet<OverrideData> landed = new HashSet<OverrideData>();
    private readonly Dictionary<OverrideHintRowUI, bool> knownBeforeRun = new Dictionary<OverrideHintRowUI, bool>();

    private void OnEnable()
    {
        Instance = this;
        CollectRows();
        SnapshotDiscovery();
        Subscribe();
        RefreshAll();
    }

    private void Start()
    {
        Subscribe();
        SyncActiveOverrides();
        RefreshAll();
    }

    private void OnDisable()
    {
        Unsubscribe();

        if (Instance == this)
            Instance = null;
    }

    public RectTransform GetResultSlot(OverrideData overrideData)
    {
        OverrideHintRowUI row = FindRow(overrideData);
        return row != null ? row.ResultSlot : null;
    }

    public RectTransform GetResultIconRect(OverrideData overrideData)
    {
        OverrideHintRowUI row = FindRow(overrideData);
        return row != null ? row.ResultIconRect : null;
    }

    public void Land(OverrideData overrideData)
    {
        OverrideHintRowUI row = FindRow(overrideData);
        if (row == null) return;

        landed.Add(row.OverrideData);
        RefreshRow(row);
        PlayLandPop(row.ResultSlot);
    }

    public void ResyncDiscovery()
    {
        SnapshotDiscovery();
        RefreshAll();
    }

    private void CollectRows()
    {
        if (rows == null || rows.Length == 0)
            rows = GetComponentsInChildren<OverrideHintRowUI>(true);
    }

    private void SnapshotDiscovery()
    {
        knownBeforeRun.Clear();

        for (int i = 0; i < rows.Length; i++)
        {
            OverrideHintRowUI row = rows[i];
            if (row == null) continue;

            knownBeforeRun[row] = OverrideDiscovery.IsOverrideUnlocked(row.OverrideData);
        }
    }

    private void Subscribe()
    {
        OverrideManager.EnsureExists();

        if (PlayerStatsManager.Instance != null)
        {
            PlayerStatsManager.Instance.OnUpgradeApplied -= HandleUpgradeApplied;
            PlayerStatsManager.Instance.OnUpgradeApplied += HandleUpgradeApplied;
        }

        if (OverrideManager.Instance != null)
        {
            OverrideManager.Instance.OnOverrideActivated -= HandleOverrideActivated;
            OverrideManager.Instance.OnOverrideActivated += HandleOverrideActivated;
            OverrideManager.Instance.OnOverrideDeactivated -= HandleOverrideDeactivated;
            OverrideManager.Instance.OnOverrideDeactivated += HandleOverrideDeactivated;
        }
    }

    private void Unsubscribe()
    {
        if (PlayerStatsManager.Instance != null)
            PlayerStatsManager.Instance.OnUpgradeApplied -= HandleUpgradeApplied;

        if (OverrideManager.Instance != null)
        {
            OverrideManager.Instance.OnOverrideActivated -= HandleOverrideActivated;
            OverrideManager.Instance.OnOverrideDeactivated -= HandleOverrideDeactivated;
        }
    }

    private void SyncActiveOverrides()
    {
        if (OverrideManager.Instance == null) return;

        for (int i = 0; i < rows.Length; i++)
        {
            OverrideHintRowUI row = rows[i];
            if (row == null) continue;

            OverrideData overrideData = row.OverrideData;
            if (overrideData == null || landed.Contains(overrideData)) continue;

            if (OverrideManager.Instance.IsOverrideActive(overrideData) && !OverrideActivationHUD.WillAnnounce(overrideData))
                landed.Add(overrideData);
        }
    }

    private void HandleUpgradeApplied(UpgradeType type, int level)
    {
        RefreshAll();
    }

    private void HandleOverrideActivated(OverrideData overrideData)
    {
        if (OverrideActivationHUD.Exists)
            RefreshAll();
        else
            Land(overrideData);
    }

    private void HandleOverrideDeactivated(OverrideData overrideData)
    {
        OverrideHintRowUI row = FindRow(overrideData);
        if (row != null)
            landed.Remove(row.OverrideData);

        RefreshAll();
    }

    private void RefreshAll()
    {
        for (int i = 0; i < rows.Length; i++)
        {
            if (rows[i] != null)
                RefreshRow(rows[i]);
        }
    }

    private void RefreshRow(OverrideHintRowUI row)
    {
        OverrideData overrideData = row.OverrideData;
        bool known = knownBeforeRun.TryGetValue(row, out bool wasKnown) && wasKnown;
        bool active = overrideData != null && landed.Contains(overrideData);

        row.SetHudState(known, active);
    }

    private OverrideHintRowUI FindRow(OverrideData overrideData)
    {
        if (overrideData == null) return null;

        for (int i = 0; i < rows.Length; i++)
        {
            OverrideHintRowUI row = rows[i];
            if (row == null) continue;

            OverrideData candidate = row.OverrideData;
            if (candidate == null) continue;

            if (candidate == overrideData || candidate.PersistentId == overrideData.PersistentId)
                return row;
        }

        return null;
    }

    private void PlayLandPop(RectTransform slot)
    {
        if (slot == null) return;

        slot.DOKill();
        slot.localScale = Vector3.one;

        Sequence pop = DOTween.Sequence().SetUpdate(true).SetTarget(slot);
        pop.Append(slot.DOScale(landPopScale, landPopDuration * 0.55f).SetEase(Ease.OutBack));
        pop.Append(slot.DOScale(1f, landPopDuration * 0.45f).SetEase(Ease.InOutSine));
    }
}
