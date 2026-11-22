using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class AccessibilitySettingsMenu : MonoBehaviour, IUpdateable
{
    public const int PresetRow = 0;
    public const int PansRow = 1;
    public const int SwayRow = 2;
    public const int ZoomRow = 3;
    public const int ShakeRow = 4;
    public const int LockRow = 5;
    public const int DeadZoneRow = 6;
    public const int FollowSpeedRow = 7;
    public const int DistanceRow = 8;
    public const int TvFilterRow = 9;
    public const int RowCount = 10;

    [Serializable]
    private sealed class SettingRow
    {
        public TextMeshProUGUI value;
        public Button previous;
        public Button next;
        [Tooltip("Explicación que aparece abajo al elegir esta opción.")]
        public LocalizedString hint;
        public Func<string> read;
        public Func<bool> available;
    }

    [Header("Textos editables")]
    [SerializeField] private LocalizedString enabledValue = new LocalizedString("On", "Activado");
    [SerializeField] private LocalizedString disabledValue = new LocalizedString("Off", "Desactivado");
    [SerializeField] private LocalizedString presetHelicopter = new LocalizedString("Helicopter", "Helicóptero");
    [SerializeField] private LocalizedString presetGentle = new LocalizedString("Gentle", "Suave");
    [SerializeField] private LocalizedString presetFixed = new LocalizedString("Fixed", "Fija");
    [SerializeField] private LocalizedString presetCustom = new LocalizedString("Custom", "Personalizado");
    [SerializeField] private LocalizedString speedSlow = new LocalizedString("Slow", "Lenta");
    [SerializeField] private LocalizedString speedNormal = new LocalizedString("Normal", "Normal");
    [SerializeField] private LocalizedString speedFast = new LocalizedString("Fast", "Rápida");
    [SerializeField] private LocalizedString distanceVeryClose = new LocalizedString("Very close", "Muy cerca");
    [SerializeField] private LocalizedString distanceClose = new LocalizedString("Close", "Cerca");
    [SerializeField] private LocalizedString distanceNormal = new LocalizedString("Normal", "Normal");
    [SerializeField] private LocalizedString distanceFar = new LocalizedString("Far", "Lejos");
    [SerializeField] private LocalizedString distanceVeryFar = new LocalizedString("Very far", "Muy lejos");
    [SerializeField] private LocalizedString savedInstantly = new LocalizedString("Changes are saved instantly.", "Los cambios se guardan al momento.");
    [SerializeField] private LocalizedString resetDone = new LocalizedString("Recommended settings restored.", "Valores recomendados restablecidos.");

    [Header("Referencias del prefab")]
    [SerializeField] private List<SettingRow> rows = new List<SettingRow>();
    [SerializeField] private Button entryButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button backButton;
    [SerializeField] private TextMeshProUGUI hintText;
    [SerializeField] private TextMeshProUGUI statusText;

    private readonly List<SettingRow> activeRows = new List<SettingRow>(RowCount);
    private GameAccessibilitySettings.SettingsData draft;
    private GameObject lastSelected;
    private int hintRow = PresetRow;
    private bool showingReset;
    private static int lastBackFrame = -1;

    public bool IsOpen => gameObject.activeInHierarchy;

    public bool IsActive => isActiveAndEnabled;

    public static bool ConsumedBackThisFrame => lastBackFrame == Time.frameCount;

    public event Action Closed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        lastBackFrame = -1;
    }

    private void Awake()
    {
        if (rows.Count < RowCount) return;

        Bind(PresetRow, ReadPreset, StepPreset);
        Bind(PansRow, () => OnOff(draft.cameraPans), step => draft.cameraPans = !draft.cameraPans);
        Bind(SwayRow, () => OnOff(draft.cameraSway), step => draft.cameraSway = !draft.cameraSway);
        Bind(ZoomRow, () => OnOff(draft.autoZoom), step => draft.autoZoom = !draft.autoZoom);
        Bind(ShakeRow, () => OnOff(draft.impactShake), step => draft.impactShake = !draft.impactShake);
        Bind(LockRow, () => OnOff(draft.lockOnRobot), step => draft.lockOnRobot = !draft.lockOnRobot);
        Bind(DeadZoneRow, () => OnOff(draft.deadZone), step => draft.deadZone = !draft.deadZone, Unlocked);
        Bind(FollowSpeedRow, ReadFollowSpeed, step => draft.followSpeed = Mathf.Clamp(draft.followSpeed + step, 0, GameAccessibilitySettings.FollowSpeedSteps - 1), Unlocked);
        Bind(DistanceRow, ReadDistance, step => draft.cameraDistance = Mathf.Clamp(draft.cameraDistance + step, 0, GameAccessibilitySettings.DistanceSteps - 1));
        Bind(TvFilterRow, () => OnOff(draft.tvFilter), step => draft.tvFilter = !draft.tvFilter);
        if (resetButton != null) resetButton.onClick.AddListener(ResetSettings);
        if (backButton != null) backButton.onClick.AddListener(Close);
    }

    private void OnEnable()
    {
        if (rows.Count < RowCount) return;

        GameAccessibilitySettings.Changed += RefreshFromService;
        GameLocalization.LanguageChanged += RefreshLanguage;
        UpdateManager.Instance?.Register(this);
        showingReset = false;
        hintRow = PresetRow;
        RefreshFromService();
        Focus(rows[PresetRow].next);
    }

    private void OnDisable()
    {
        GameAccessibilitySettings.Changed -= RefreshFromService;
        GameLocalization.LanguageChanged -= RefreshLanguage;
        UpdateManager.Instance?.Unregister(this);
    }

    public void OnUpdate(float deltaTime)
    {
        GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        if (selected != lastSelected)
        {
            lastSelected = selected;
            int row = RowOf(selected);
            if (row >= 0) ShowHint(row);
        }

        bool back = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            || (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);
        if (!back) return;

        lastBackFrame = Time.frameCount;
        Close();
    }

    private void Bind(int index, Func<string> read, Action<int> edit, Func<bool> available = null)
    {
        SettingRow row = rows[index];
        row.read = read;
        row.available = available;
        if (row.previous != null) row.previous.onClick.AddListener(() => Edit(index, edit, -1));
        if (row.next != null) row.next.onClick.AddListener(() => Edit(index, edit, 1));
    }

    private void Edit(int index, Action<int> edit, int step)
    {
        edit(step);
        showingReset = false;
        GameAccessibilitySettings.Apply(draft);
        ShowHint(index);
    }

    private void StepPreset(int step)
    {
        GameAccessibilitySettings.CameraPreset preset = GameAccessibilitySettings.PresetOf(draft);
        int count = (int)GameAccessibilitySettings.CameraPreset.Custom;
        int next = preset == GameAccessibilitySettings.CameraPreset.Custom
            ? (step > 0 ? 0 : count - 1)
            : ((int)preset + step % count + count) % count;
        draft = GameAccessibilitySettings.WithPreset(draft, (GameAccessibilitySettings.CameraPreset)next);
    }

    private void ResetSettings()
    {
        GameAccessibilitySettings.ResetToDefaults();
        showingReset = true;
        RefreshStatus();
    }

    private void RefreshFromService()
    {
        draft = GameAccessibilitySettings.Current;
        RefreshRows();
        RefreshStatus();
    }

    private void RefreshLanguage()
    {
        RefreshRows();
        RefreshStatus();
        ShowHint(hintRow);
    }

    private void RefreshRows()
    {
        foreach (SettingRow row in rows)
        {
            if (row.read == null || row.value == null) continue;
            row.value.text = row.read();
            bool enabled = row.available == null || row.available();
            if (row.previous != null) row.previous.interactable = enabled;
            if (row.next != null) row.next.interactable = enabled;
            row.value.alpha = enabled ? 1f : 0.45f;
        }
        ConfigureNavigation();
        ShowHint(hintRow);
    }

    private void RefreshStatus()
    {
        if (statusText != null) statusText.text = showingReset ? resetDone.Value : savedInstantly.Value;
    }

    private void ShowHint(int index)
    {
        if (index < 0 || index >= rows.Count) return;
        hintRow = index;
        if (hintText != null) hintText.text = rows[index].hint.Value;
    }

    private int RowOf(GameObject selected)
    {
        if (selected == null) return -1;
        for (int i = 0; i < rows.Count; i++)
        {
            SettingRow row = rows[i];
            if ((row.previous != null && row.previous.gameObject == selected) || (row.next != null && row.next.gameObject == selected)) return i;
        }
        return -1;
    }

    private void Close()
    {
        lastBackFrame = Time.frameCount;
        gameObject.SetActive(false);
        Closed?.Invoke();
        Focus(entryButton);
    }

    private void ConfigureNavigation()
    {
        activeRows.Clear();
        foreach (SettingRow row in rows)
            if (row.previous != null && row.previous.interactable) activeRows.Add(row);
        for (int i = 0; i < activeRows.Count; i++)
        {
            SettingRow row = activeRows[i];
            Selectable upLeft = i == 0 ? backButton : activeRows[i - 1].previous;
            Selectable upRight = i == 0 ? resetButton : activeRows[i - 1].next;
            Selectable downLeft = i == activeRows.Count - 1 ? backButton : activeRows[i + 1].previous;
            Selectable downRight = i == activeRows.Count - 1 ? resetButton : activeRows[i + 1].next;
            SetNavigation(row.previous, row.next, row.next, upLeft, downLeft);
            SetNavigation(row.next, row.previous, row.previous, upRight, downRight);
        }
        if (activeRows.Count == 0) return;
        SetNavigation(backButton, resetButton, resetButton, activeRows[activeRows.Count - 1].previous, activeRows[0].previous);
        SetNavigation(resetButton, backButton, backButton, activeRows[activeRows.Count - 1].next, activeRows[0].next);
    }

    private static void SetNavigation(Button button, Selectable left, Selectable right, Selectable up, Selectable down)
    {
        if (button == null) return;
        button.navigation = new Navigation
        {
            mode = Navigation.Mode.Explicit,
            selectOnLeft = left, selectOnRight = right, selectOnUp = up, selectOnDown = down
        };
    }

    private static void Focus(Button button)
    {
        if (button != null && button.gameObject.activeInHierarchy && button.IsInteractable() && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(button.gameObject);
    }

    private bool Unlocked() => !draft.lockOnRobot;

    private string OnOff(bool value) => value ? enabledValue.Value : disabledValue.Value;

    private string ReadPreset()
    {
        switch (GameAccessibilitySettings.PresetOf(draft))
        {
            case GameAccessibilitySettings.CameraPreset.Helicopter: return presetHelicopter.Value;
            case GameAccessibilitySettings.CameraPreset.Gentle: return presetGentle.Value;
            case GameAccessibilitySettings.CameraPreset.Fixed: return presetFixed.Value;
            default: return presetCustom.Value;
        }
    }

    private string ReadFollowSpeed()
    {
        if (draft.followSpeed < GameAccessibilitySettings.NormalFollowSpeed) return speedSlow.Value;
        return draft.followSpeed > GameAccessibilitySettings.NormalFollowSpeed ? speedFast.Value : speedNormal.Value;
    }

    private string ReadDistance()
    {
        switch (draft.cameraDistance)
        {
            case 0: return distanceVeryClose.Value;
            case 1: return distanceClose.Value;
            case 3: return distanceFar.Value;
            case 4: return distanceVeryFar.Value;
            default: return distanceNormal.Value;
        }
    }
}
