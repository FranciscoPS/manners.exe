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
    public const int RowCount = 9;

    [Serializable]
    private sealed class SettingRow
    {
        public TextMeshProUGUI value;
        public Button previous;
        public Button next;
        public Slider slider;
        [Tooltip("Explicación que aparece abajo al elegir esta opción.")]
        public LocalizedString hint;
        public Func<string> read;
        public Func<float> sliderValue;
        public Func<bool> available;

        public Selectable Left => previous != null ? previous : (Selectable)slider;

        public Selectable Right => next != null ? next : (Selectable)slider;
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
    [Tooltip("Lectura del slider de distancia; {0} es la posición de 0 a 100.")]
    [SerializeField] private LocalizedString distanceValue = new LocalizedString("{0}%", "{0}%");
    [SerializeField] private LocalizedString applyHint = new LocalizedString("Changes are saved when you press Apply.", "Los cambios se guardan al pulsar Aplicar.");
    [SerializeField] private LocalizedString pendingChanges = new LocalizedString("Unsaved changes. Press Apply to save.", "Cambios pendientes. Pulsa Aplicar para guardarlos.");
    [SerializeField] private LocalizedString applied = new LocalizedString("Settings applied and saved.", "Configuración aplicada y guardada.");
    [SerializeField] private LocalizedString resetDone = new LocalizedString("Recommended settings restored.", "Valores recomendados restablecidos.");

    [Header("Referencias del prefab")]
    [SerializeField] private List<SettingRow> rows = new List<SettingRow>();
    [SerializeField] private Button entryButton;
    [SerializeField] private Button applyButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button backButton;
    [SerializeField] private TextMeshProUGUI hintText;
    [SerializeField] private TextMeshProUGUI statusText;

    private readonly List<SettingRow> activeRows = new List<SettingRow>(RowCount);
    private GameAccessibilitySettings.SettingsData draft;
    private GameObject lastSelected;
    private int hintRow = PresetRow;
    private LocalizedString status;
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
        BindSlider(DistanceRow, ReadDistance, () => draft.cameraDistance, value => draft.cameraDistance = value);
        if (applyButton != null) applyButton.onClick.AddListener(ApplySettings);
        if (resetButton != null) resetButton.onClick.AddListener(ResetSettings);
        if (backButton != null) backButton.onClick.AddListener(Close);
    }

    private void OnEnable()
    {
        if (rows.Count < RowCount) return;

        GameAccessibilitySettings.Changed += RefreshFromService;
        GameLocalization.LanguageChanged += RefreshLanguage;
        UpdateManager.Instance?.Register(this);
        hintRow = PresetRow;
        RefreshFromService();
        SetStatus(applyHint);
        Focus(rows[PresetRow].Right);
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

    private void BindSlider(int index, Func<string> read, Func<float> current, Action<float> edit)
    {
        SettingRow row = rows[index];
        row.read = read;
        row.sliderValue = current;
        if (row.slider == null) return;

        row.slider.onValueChanged.AddListener(value =>
        {
            edit(value);
            RefreshRows();
            MarkChanged(index);
        });
    }

    private void Edit(int index, Action<int> edit, int step)
    {
        edit(step);
        RefreshRows();
        MarkChanged(index);
    }

    private void MarkChanged(int index)
    {
        ShowHint(index);
        SetStatus(GameAccessibilitySettings.SameSettings(draft, GameAccessibilitySettings.Current) ? applyHint : pendingChanges);
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

    private void ApplySettings()
    {
        GameAccessibilitySettings.Apply(draft);
        SetStatus(applied);
    }

    private void ResetSettings()
    {
        GameAccessibilitySettings.ResetToDefaults();
        SetStatus(resetDone);
    }

    private void RefreshFromService()
    {
        draft = GameAccessibilitySettings.Current;
        RefreshRows();
    }

    private void RefreshLanguage()
    {
        RefreshRows();
        SetStatus(status);
    }

    private void RefreshRows()
    {
        foreach (SettingRow row in rows)
        {
            if (row.read == null) continue;
            bool enabled = row.available == null || row.available();
            if (row.value != null)
            {
                row.value.text = row.read();
                row.value.alpha = enabled ? 1f : 0.45f;
            }
            if (row.previous != null) row.previous.interactable = enabled;
            if (row.next != null) row.next.interactable = enabled;
            if (row.slider != null)
            {
                row.slider.interactable = enabled;
                if (row.sliderValue != null) row.slider.SetValueWithoutNotify(row.sliderValue());
            }
        }
        ConfigureNavigation();
        ShowHint(hintRow);
    }

    private void SetStatus(LocalizedString message)
    {
        status = message;
        if (statusText != null) statusText.text = message.Value;
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
            if (IsObject(row.previous, selected) || IsObject(row.next, selected) || IsObject(row.slider, selected)) return i;
        }
        return -1;
    }

    private static bool IsObject(Component component, GameObject target) => component != null && component.gameObject == target;

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
        {
            Selectable left = row.Left;
            if (left != null && left.interactable) activeRows.Add(row);
        }
        for (int i = 0; i < activeRows.Count; i++)
        {
            SettingRow row = activeRows[i];
            Selectable upLeft = i == 0 ? backButton : activeRows[i - 1].Left;
            Selectable upRight = i == 0 ? applyButton : activeRows[i - 1].Right;
            Selectable downLeft = i == activeRows.Count - 1 ? backButton : activeRows[i + 1].Left;
            Selectable downRight = i == activeRows.Count - 1 ? applyButton : activeRows[i + 1].Right;
            if (row.previous == null && row.next == null)
            {
                SetNavigation(row.slider, null, null, upLeft, downLeft);
                continue;
            }
            SetNavigation(row.previous, row.next, row.next, upLeft, downLeft);
            SetNavigation(row.next, row.previous, row.previous, upRight, downRight);
        }
        if (activeRows.Count == 0) return;

        Selectable firstLeft = activeRows[0].Left;
        Selectable firstRight = activeRows[0].Right;
        Selectable lastLeft = activeRows[activeRows.Count - 1].Left;
        Selectable lastRight = activeRows[activeRows.Count - 1].Right;
        SetNavigation(backButton, applyButton, resetButton, lastLeft, firstLeft);
        SetNavigation(resetButton, backButton, applyButton, lastLeft, firstLeft);
        SetNavigation(applyButton, resetButton, backButton, lastRight, firstRight);
    }

    private static void SetNavigation(Selectable target, Selectable left, Selectable right, Selectable up, Selectable down)
    {
        if (target == null) return;
        target.navigation = new Navigation
        {
            mode = Navigation.Mode.Explicit,
            selectOnLeft = left, selectOnRight = right, selectOnUp = up, selectOnDown = down
        };
    }

    private static void Focus(Selectable target)
    {
        if (target != null && target.gameObject.activeInHierarchy && target.IsInteractable() && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(target.gameObject);
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

    private string ReadDistance() => distanceValue.Format(Mathf.RoundToInt(draft.cameraDistance * 100f));
}
