using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class SynergyHudPanel : MonoBehaviour
{
    public static SynergyHudPanel Instance { get; private set; }

    [Header("Filas")]
    [Tooltip("Filas del panel (una por sinergia). Si se deja vacío se toman todas las SynergyHintRowUI hijas.")]
    [SerializeField] private SynergyHintRowUI[] rows;

    [Header("Pop del cuadro de resultado al aterrizar el icono")]
    [SerializeField] private float landPopScale = 1.3f;
    [SerializeField] private float landPopDuration = 0.35f;

    private readonly HashSet<SynergyData> landed = new HashSet<SynergyData>();
    private readonly Dictionary<SynergyHintRowUI, bool> knownBeforeRun = new Dictionary<SynergyHintRowUI, bool>();

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
        SyncActiveSynergies();
        RefreshAll();
    }

    private void OnDisable()
    {
        Unsubscribe();

        if (Instance == this)
            Instance = null;
    }

    public RectTransform GetResultSlot(SynergyData synergy)
    {
        SynergyHintRowUI row = FindRow(synergy);
        return row != null ? row.ResultSlot : null;
    }

    public RectTransform GetResultIconRect(SynergyData synergy)
    {
        SynergyHintRowUI row = FindRow(synergy);
        return row != null ? row.ResultIconRect : null;
    }

    public void Land(SynergyData synergy)
    {
        SynergyHintRowUI row = FindRow(synergy);
        if (row == null) return;

        landed.Add(row.Synergy);
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
            rows = GetComponentsInChildren<SynergyHintRowUI>(true);
    }

    private void SnapshotDiscovery()
    {
        knownBeforeRun.Clear();

        for (int i = 0; i < rows.Length; i++)
        {
            SynergyHintRowUI row = rows[i];
            if (row == null) continue;

            knownBeforeRun[row] = SynergyDiscovery.IsSynergyUnlocked(row.Synergy);
        }
    }

    private void Subscribe()
    {
        SynergyManager.EnsureExists();

        if (PlayerStatsManager.Instance != null)
        {
            PlayerStatsManager.Instance.OnUpgradeApplied -= HandleUpgradeApplied;
            PlayerStatsManager.Instance.OnUpgradeApplied += HandleUpgradeApplied;
        }

        if (SynergyManager.Instance != null)
        {
            SynergyManager.Instance.OnSynergyActivated -= HandleSynergyActivated;
            SynergyManager.Instance.OnSynergyActivated += HandleSynergyActivated;
            SynergyManager.Instance.OnSynergyDeactivated -= HandleSynergyDeactivated;
            SynergyManager.Instance.OnSynergyDeactivated += HandleSynergyDeactivated;
        }
    }

    private void Unsubscribe()
    {
        if (PlayerStatsManager.Instance != null)
            PlayerStatsManager.Instance.OnUpgradeApplied -= HandleUpgradeApplied;

        if (SynergyManager.Instance != null)
        {
            SynergyManager.Instance.OnSynergyActivated -= HandleSynergyActivated;
            SynergyManager.Instance.OnSynergyDeactivated -= HandleSynergyDeactivated;
        }
    }

    private void SyncActiveSynergies()
    {
        if (SynergyManager.Instance == null) return;

        for (int i = 0; i < rows.Length; i++)
        {
            SynergyHintRowUI row = rows[i];
            if (row == null) continue;

            SynergyData synergy = row.Synergy;
            if (synergy == null || landed.Contains(synergy)) continue;

            if (SynergyManager.Instance.IsSynergyActive(synergy) && !SynergyActivationHUD.WillAnnounce(synergy))
                landed.Add(synergy);
        }
    }

    private void HandleUpgradeApplied(UpgradeType type, int level)
    {
        RefreshAll();
    }

    private void HandleSynergyActivated(SynergyData synergy)
    {
        if (SynergyActivationHUD.Exists)
            RefreshAll();
        else
            Land(synergy);
    }

    private void HandleSynergyDeactivated(SynergyData synergy)
    {
        SynergyHintRowUI row = FindRow(synergy);
        if (row != null)
            landed.Remove(row.Synergy);

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

    private void RefreshRow(SynergyHintRowUI row)
    {
        SynergyData synergy = row.Synergy;
        bool known = knownBeforeRun.TryGetValue(row, out bool wasKnown) && wasKnown;
        bool active = synergy != null && landed.Contains(synergy);

        row.SetHudState(known, active);
    }

    private SynergyHintRowUI FindRow(SynergyData synergy)
    {
        if (synergy == null) return null;

        for (int i = 0; i < rows.Length; i++)
        {
            SynergyHintRowUI row = rows[i];
            if (row == null) continue;

            SynergyData candidate = row.Synergy;
            if (candidate == null) continue;

            if (candidate == synergy || candidate.synergyName == synergy.synergyName)
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
