using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class GraphicsSettingsMenu : MonoBehaviour, IUpdateable
{
    [Serializable]
    private sealed class SettingRow
    {
        public TextMeshProUGUI value;
        public Button previous;
        public Button next;
        public Func<string> read;
        public Func<bool> available;
    }

    [Header("Textos editables")]
    [SerializeField] private LocalizedString nativeResolution = new LocalizedString("Native", "Nativa");
    [SerializeField] private LocalizedString fullScreen = new LocalizedString("Fullscreen", "Pantalla completa");
    [SerializeField] private LocalizedString window = new LocalizedString("Windowed", "Ventana");
    [SerializeField] private LocalizedString textureFull = new LocalizedString("Full", "Completo");
    [SerializeField] private LocalizedString textureHalf = new LocalizedString("Half", "Mitad");
    [SerializeField] private LocalizedString textureQuarter = new LocalizedString("Quarter", "Cuarto");
    [SerializeField] private LocalizedString unavailable = new LocalizedString("Unavailable", "No disponible");
    [SerializeField] private LocalizedString buildOnly = new LocalizedString("In build only", "Solo en la build");
    [SerializeField] private LocalizedString systemManaged = new LocalizedString("System managed", "Gestionado por sistema");
    [SerializeField] private LocalizedString enabledValue = new LocalizedString("On", "Activado");
    [SerializeField] private LocalizedString disabledValue = new LocalizedString("Off", "Desactivado");
    [SerializeField] private LocalizedString eco = new LocalizedString("Eco", "Eco");
    [SerializeField] private LocalizedString balanced = new LocalizedString("Balanced", "Equilibrado");
    [SerializeField] private LocalizedString quality = new LocalizedString("Quality", "Calidad");
    [SerializeField] private LocalizedString custom = new LocalizedString("Custom", "Personalizado");
    [SerializeField] private LocalizedString displaySaved = new LocalizedString("Display settings saved.", "Configuración de pantalla guardada.");
    [SerializeField] private LocalizedString displayRestored = new LocalizedString("Previous display restored.", "Pantalla anterior restaurada.");
    [SerializeField] private LocalizedString pendingChanges = new LocalizedString("Unsaved changes. Press Apply to save.", "Cambios pendientes. Pulsa Aplicar para guardarlos.");
    [SerializeField] private LocalizedString applyHint = new LocalizedString("Changes are saved when you press Apply.", "Los cambios se guardan al pulsar Aplicar.");
    [SerializeField] private LocalizedString confirmHint = new LocalizedString("Confirm the display change to keep it.", "Confirma el cambio de pantalla para conservarlo.");
    [SerializeField] private LocalizedString applied = new LocalizedString("Settings applied and saved.", "Configuración aplicada y guardada.");
    [SerializeField] private LocalizedString resetDone = new LocalizedString("Recommended settings restored.", "Valores recomendados restablecidos.");
    [SerializeField] private LocalizedString saved = new LocalizedString("Current settings saved.", "Configuración actual guardada.");
    [SerializeField] private LocalizedString confirmationQuestion = new LocalizedString("Keep these display settings?\n\nReverting automatically in {0} s.", "¿Mantener esta configuración de pantalla?\n\nSe revierte automáticamente en {0} s.");
    [SerializeField] private LocalizedString vSyncHint = new LocalizedString("VSync respects the FPS limit; some displays may use a lower rate to synchronize.\n3D scaling keeps the UI sharp. Menus and pause: up to 30 FPS.", "VSync respeta el límite elegido; algunas pantallas usarán una frecuencia menor para sincronizar.\nLa escala 3D conserva la nitidez de la interfaz. Menús y pausa: máximo 30 FPS.");
    [SerializeField] private LocalizedString fpsHint = new LocalizedString("The FPS limit avoids unnecessary frames. Start with 30–60 FPS on laptops.\n3D scaling keeps the UI sharp. Menus and pause: up to 30 FPS.", "El límite de FPS evita dibujar fotogramas innecesarios. 30–60 FPS es un buen inicio en portátiles.\nLa escala 3D conserva la nitidez de la interfaz. Menús y pausa: máximo 30 FPS.");

    private static readonly int[] FrameRates = { 30, 45, 60, 90, 120 };
    private static readonly int[] ResolutionHeights = { 720, 900, 1080, 1440, 2160, 0 };
    private static readonly float[] RenderScales = { .5f, .6f, .67f, .75f, .85f, 1f };
    [SerializeField] private List<SettingRow> rows = new List<SettingRow>();
    private readonly List<SettingRow> activeRows = new List<SettingRow>(11);
    private GameGraphicsSettings.SettingsData draft;
    [SerializeField] private Button entryButton;
    [SerializeField] private Button applyButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Button keepButton;
    [SerializeField] private Button revertButton;
    [SerializeField] private CanvasGroup controls;
    [SerializeField] private GameObject confirmation;
    [SerializeField] private TextMeshProUGUI confirmationText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI syncHint;
    private bool wasDisplayPending;
    private int lastCountdown = -1;
    private static int lastBackFrame = -1;

    public bool IsOpen => gameObject.activeInHierarchy;
    public static bool ConsumedBackThisFrame => lastBackFrame == Time.frameCount;

    public event Action Closed;

    private void Awake()
    {
        Bind(0, ReadPresetName, step =>
        {
            int height = draft.resolutionHeight;
            bool fullscreen = draft.fullScreen;
            draft = GameGraphicsSettings.CreatePreset((GameGraphicsSettings.GraphicsPreset)Wrap((int)draft.preset + step, 3));
            draft.resolutionHeight = height;
            draft.fullScreen = fullscreen;
        });
        Bind(1, () => draft.frameRate + " FPS", step => draft.frameRate = Cycle(FrameRates, draft.frameRate, step));
        Bind(2, () => OnOff(draft.vSync), step => draft.vSync = !draft.vSync);
        Bind(3, () => GameGraphicsSettings.SupportsDisplayChanges ? (draft.resolutionHeight == 0 ? nativeResolution.Value : draft.resolutionHeight + "p") : UnavailableDisplayLabel,
            step => draft.resolutionHeight = Cycle(ResolutionHeights, draft.resolutionHeight, step), () => GameGraphicsSettings.SupportsDisplayChanges);
        Bind(4, () => GameGraphicsSettings.SupportsDisplayChanges ? (draft.fullScreen ? fullScreen.Value : window.Value) : UnavailableDisplayLabel,
            step => draft.fullScreen = !draft.fullScreen, () => GameGraphicsSettings.SupportsDisplayChanges);
        Bind(5, () => Mathf.RoundToInt(draft.renderScale * 100) + "%", step =>
        {
            int closest = 0;
            for (int i = 1; i < RenderScales.Length; i++)
                if (Mathf.Abs(RenderScales[i] - draft.renderScale) < Mathf.Abs(RenderScales[closest] - draft.renderScale)) closest = i;
            draft.renderScale = RenderScales[Wrap(closest + step, RenderScales.Length)];
        });
        Bind(6, () => draft.textureMipmapLimit == 0 ? textureFull.Value : draft.textureMipmapLimit == 1 ? textureHalf.Value : textureQuarter.Value,
            step => draft.textureMipmapLimit = Wrap(draft.textureMipmapLimit + step, 3));
        Bind(7, () => OnOff(draft.shadows), step => draft.shadows = !draft.shadows);
        Bind(8, () => GameGraphicsSettings.SupportsAmbientOcclusion ? OnOff(draft.ambientOcclusion) : unavailable.Value,
            step => draft.ambientOcclusion = !draft.ambientOcclusion, () => GameGraphicsSettings.SupportsAmbientOcclusion);
        Bind(9, () => OnOff(draft.postProcessing), step => draft.postProcessing = !draft.postProcessing);
        Bind(10, () => OnOff(draft.outlines), step => draft.outlines = !draft.outlines);
        backButton.onClick.AddListener(Close);
        resetButton.onClick.AddListener(ResetSettings);
        applyButton.onClick.AddListener(ApplySettings);
        keepButton.onClick.AddListener(KeepDisplay);
        revertButton.onClick.AddListener(RevertDisplay);
    }

    private void Bind(int index, Func<string> read, Action<int> edit, Func<bool> available = null)
    {
        SettingRow row = rows[index];
        row.read = read;
        row.available = available;
        row.previous.onClick.AddListener(() => Edit(edit, -1));
        row.next.onClick.AddListener(() => Edit(edit, 1));
    }

    private void KeepDisplay()
    {
        GameGraphicsSettings.ConfirmDisplayChange();
        RefreshFromService();
        statusText.text = displaySaved.Value;
    }

    private void RevertDisplay()
    {
        GameGraphicsSettings.RevertDisplayChange();
        RefreshFromService();
        statusText.text = displayRestored.Value;
    }

    private void OnEnable()
    {
        if (controls == null) return;
        GameGraphicsSettings.Changed += RefreshFromService;
        GameLocalization.LanguageChanged += RefreshLanguage;
        UpdateManager.Instance?.Register(this);
        RefreshFromService();
        statusText.text = applyHint.Value;
        Focus(GameGraphicsSettings.IsDisplayChangePending ? keepButton : rows[0].next);
    }

    private void OnDisable()
    {
        GameGraphicsSettings.Changed -= RefreshFromService;
        GameLocalization.LanguageChanged -= RefreshLanguage;
        UpdateManager.Instance?.Unregister(this);
        if (GameGraphicsSettings.IsDisplayChangePending) GameGraphicsSettings.RevertDisplayChange();
    }

    public bool IsActive => isActiveAndEnabled;
    public void OnUpdate(float deltaTime)
    {
        bool pending = GameGraphicsSettings.IsDisplayChangePending;
        if (pending != wasDisplayPending) RefreshFromService();
        if (pending)
        {
            int seconds = Mathf.CeilToInt(GameGraphicsSettings.DisplayConfirmationSecondsRemaining);
            if (seconds != lastCountdown)
            {
                lastCountdown = seconds;
                confirmationText.text = confirmationQuestion.Format(seconds);
            }
        }
        bool back = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            || (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);
        if (!back) return;
        lastBackFrame = Time.frameCount;
        if (pending)
        {
            GameGraphicsSettings.RevertDisplayChange();
            RefreshFromService();
            statusText.text = displayRestored.Value;
        }
        else Close();
    }

    private void RefreshLanguage()
    {
        lastCountdown = -1;
        RefreshRows();
        statusText.text = pendingChanges.Value;
    }

    private void Edit(Action<int> edit, int step)
    {
        if (GameGraphicsSettings.IsDisplayChangePending) return;
        edit(step);
        RefreshRows();
        statusText.text = pendingChanges.Value;
    }

    private void ApplySettings()
    {
        GameGraphicsSettings.Apply(draft);
        RefreshFromService();
        statusText.text = GameGraphicsSettings.IsDisplayChangePending
            ? confirmHint.Value
            : applied.Value;
    }

    private void ResetSettings()
    {
        GameGraphicsSettings.ResetToDefaults();
        RefreshFromService();
        statusText.text = resetDone.Value;
    }

    private void RefreshFromService()
    {
        if (controls == null) return;
        draft = GameGraphicsSettings.Current;
        RefreshRows();
        bool pending = GameGraphicsSettings.IsDisplayChangePending;
        controls.interactable = !pending;
        confirmation.SetActive(pending);
        if (pending && !wasDisplayPending)
        {
            lastCountdown = -1;
            Focus(keepButton);
        }
        else if (!pending && wasDisplayPending)
        {
            statusText.text = saved.Value;
            Focus(applyButton);
        }
        wasDisplayPending = pending;
    }

    private void RefreshRows()
    {
        foreach (SettingRow row in rows)
        {
            row.value.text = row.read();
            bool enabled = row.available == null || row.available();
            row.previous.interactable = row.next.interactable = enabled;
            row.value.alpha = enabled ? 1 : .45f;
        }
        syncHint.text = draft.vSync
            ? vSyncHint.Value
            : fpsHint.Value;
        ConfigureNavigation();
    }

    private void Close()
    {
        lastBackFrame = Time.frameCount;
        if (GameGraphicsSettings.IsDisplayChangePending) GameGraphicsSettings.RevertDisplayChange();
        gameObject.SetActive(false);
        Closed?.Invoke();
        Focus(entryButton);
    }

    private void ConfigureNavigation()
    {
        activeRows.Clear();
        foreach (SettingRow row in rows)
            if (row.previous.interactable) activeRows.Add(row);
        for (int i = 0; i < activeRows.Count; i++)
        {
            SettingRow row = activeRows[i];
            Button upLeft = i == 0 ? backButton : activeRows[i - 1].previous;
            Button upRight = i == 0 ? applyButton : activeRows[i - 1].next;
            Button downLeft = i == activeRows.Count - 1 ? backButton : activeRows[i + 1].previous;
            Button downRight = i == activeRows.Count - 1 ? applyButton : activeRows[i + 1].next;
            SetNavigation(row.previous, row.next, row.next, upLeft, downLeft);
            SetNavigation(row.next, row.previous, row.previous, upRight, downRight);
        }
        if (activeRows.Count == 0) return;
        SetNavigation(backButton, applyButton, resetButton, activeRows[activeRows.Count - 1].previous, activeRows[0].previous);
        SetNavigation(resetButton, backButton, applyButton, activeRows[activeRows.Count - 1].previous, activeRows[0].previous);
        SetNavigation(applyButton, resetButton, backButton, activeRows[activeRows.Count - 1].next, activeRows[0].next);
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

    private string ReadPresetName()
    {
        GameGraphicsSettings.SettingsData preset = GameGraphicsSettings.CreatePreset(draft.preset);
        bool matches = draft.frameRate == preset.frameRate && draft.vSync == preset.vSync
            && Mathf.Approximately(draft.renderScale, preset.renderScale)
            && draft.textureMipmapLimit == preset.textureMipmapLimit && draft.shadows == preset.shadows
            && draft.ambientOcclusion == preset.ambientOcclusion && draft.postProcessing == preset.postProcessing
            && draft.outlines == preset.outlines;
        return matches ? PresetName(draft.preset) : custom.Value;
    }

    private string UnavailableDisplayLabel => Application.isEditor ? buildOnly.Value : systemManaged.Value;

    private static int Cycle(int[] choices, int current, int step)
    {
        int index = Array.IndexOf(choices, current);
        return choices[Wrap((index < 0 ? 0 : index) + step, choices.Length)];
    }

    private static int Wrap(int value, int count) => (value % count + count) % count;
    private string OnOff(bool value) => value ? enabledValue.Value : disabledValue.Value;
    private string PresetName(GameGraphicsSettings.GraphicsPreset preset) => preset == GameGraphicsSettings.GraphicsPreset.Eco ? eco.Value : preset == GameGraphicsSettings.GraphicsPreset.Balanced ? balanced.Value : quality.Value;
}
