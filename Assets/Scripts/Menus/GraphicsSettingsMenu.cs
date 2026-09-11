using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Shared, scene-independent graphics UI. Built once beside each existing Audio entry,
/// so all gameplay scenes get the same controls without duplicating serialized panels.
/// </summary>
public sealed class GraphicsSettingsMenu : MonoBehaviour
{
    private sealed class SettingRow
    {
        public TextMeshProUGUI value;
        public Button previous;
        public Button next;
        public Func<string> read;
        public Func<bool> available;
    }

    private static readonly int[] FrameRates = { 30, 45, 60, 90, 120 };
    private static readonly int[] ResolutionHeights = { 720, 900, 1080, 1440, 2160, 0 };
    private static readonly float[] RenderScales = { .5f, .6f, .67f, .75f, .85f, 1f };
    private static readonly Color Accent = new Color(.16f, .86f, .82f);
    private readonly List<SettingRow> rows = new List<SettingRow>();
    private GameGraphicsSettings.SettingsData draft;
    private TMP_FontAsset font;
    private Action onClose;
    private Button entryButton;
    private Button applyButton;
    private Button resetButton;
    private Button backButton;
    private Button keepButton;
    private Button revertButton;
    private CanvasGroup controls;
    private GameObject confirmation;
    private TextMeshProUGUI confirmationText;
    private TextMeshProUGUI statusText;
    private TextMeshProUGUI syncHint;
    private bool wasDisplayPending;
    private int lastCountdown = -1;
    private static int lastBackFrame = -1;

    public bool IsOpen => gameObject.activeInHierarchy;
    public static bool ConsumedBackThisFrame => lastBackFrame == Time.frameCount;

    public static GraphicsSettingsMenu Install(GameObject ownerPanel, Action open, Action close)
    {
        if (ownerPanel == null || ownerPanel.transform.parent == null) return null;

        Button audio = FindAudioButton(ownerPanel);
        Button style = audio;
        if (style == null || style.GetComponentInChildren<TMP_Text>(true) == null)
        {
            foreach (Button candidate in ownerPanel.GetComponentsInChildren<Button>(true))
            {
                if (candidate.GetComponentInChildren<TMP_Text>(true) == null) continue;
                style = candidate;
                break;
            }
        }

        RectTransform root = CreateRect("GraphicsSettingsPanel", ownerPanel.transform.parent);
        root.gameObject.SetActive(false);
        Stretch(root, 0, 0, 1, 1);
        GraphicsSettingsMenu menu = root.gameObject.AddComponent<GraphicsSettingsMenu>();
        menu.onClose = close;
        TMP_Text sample = style != null ? style.GetComponentInChildren<TMP_Text>(true) : null;
        menu.font = sample != null ? sample.font : TMP_Settings.defaultFontAsset;
        menu.Build();
        menu.entryButton = menu.CreateEntry(ownerPanel, audio, style, open);
        return menu;
    }

    private static Button FindAudioButton(GameObject ownerPanel)
    {
        foreach (Button button in ownerPanel.GetComponentsInChildren<Button>(true))
        {
            if (string.Equals(button.name, "Audio", StringComparison.OrdinalIgnoreCase)) return button;
            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                if (button.onClick.GetPersistentMethodName(i) == "OnAudioButtonPressed") return button;
        }
        return null;
    }

    private Button CreateEntry(GameObject ownerPanel, Button audio, Button style, Action open)
    {
        // Prefer joining the real menu button list (e.g. Reanudar/Reiniciar/Menu Principal,
        // or Audio/Controles/Ayuda/Volver) instead of anchoring beside whatever button happens
        // to trigger audio settings - that button can be a decorative, rotated icon tab that
        // makes a poor layout template (see the pause menu's rotated AudioPanel flap).
        Transform list = FindButtonListContainer(ownerPanel);
        if (list != null) return CreateListEntry(list, open);

        Transform parent = audio != null ? audio.transform.parent : ownerPanel.transform;
        Button button = CreateButton("GraphicsButton", parent, "Gráficos", open);
        RectTransform rect = (RectTransform)button.transform;
        if (audio != null)
        {
            RectTransform source = (RectTransform)audio.transform;
            bool icon = audio.GetComponentInChildren<TMP_Text>(true) == null;
            rect.anchorMin = source.anchorMin;
            rect.anchorMax = source.anchorMax;
            rect.pivot = source.pivot;
            rect.sizeDelta = icon ? new Vector2(190, 72) : source.sizeDelta;
            rect.anchoredPosition = source.anchoredPosition + new Vector2(icon ? 160 : source.rect.width + 28, 0);
            rect.localScale = source.localScale;
        }
        else
        {
            rect.anchorMin = rect.anchorMax = new Vector2(.8f, .5f);
            rect.sizeDelta = new Vector2(260, 80);
        }
        ApplyStyle(button, style);
        return button;
    }

    // Finds the parent of the largest group of sibling, unrotated Buttons in the panel -
    // i.e. the real vertical menu list, as opposed to a single decorative icon button.
    private static Transform FindButtonListContainer(GameObject ownerPanel)
    {
        Transform best = null;
        int bestCount = 1;
        foreach (Button candidate in ownerPanel.GetComponentsInChildren<Button>(true))
        {
            Transform parent = candidate.transform.parent;
            if (parent == null || parent == best) continue;
            int count = 0;
            bool anyRotated = false;
            foreach (Transform child in parent)
            {
                if (child.GetComponent<Button>() == null) continue;
                count++;
                if (Quaternion.Angle(child.localRotation, Quaternion.identity) > 1f) anyRotated = true;
            }
            if (anyRotated || count <= bestCount) continue;
            bestCount = count;
            best = parent;
        }
        return best;
    }

    // Inserts the new entry second-to-last, so whatever button is currently last
    // (Volver, Menu Principal, ...) stays the final option in the list.
    private Button CreateListEntry(Transform list, Action open)
    {
        int originalCount = list.childCount;
        RectTransform lastRect = (RectTransform)list.GetChild(originalCount - 1);
        RectTransform templateRect = (RectTransform)list.GetChild(originalCount - 2);
        Button templateButton = templateRect.GetComponent<Button>();
        Vector2 step = lastRect.anchoredPosition - templateRect.anchoredPosition;

        Button button = CreateButton("GraphicsButton", list, "Gráficos", open);
        RectTransform rect = (RectTransform)button.transform;
        rect.anchorMin = templateRect.anchorMin;
        rect.anchorMax = templateRect.anchorMax;
        rect.pivot = templateRect.pivot;
        rect.sizeDelta = templateRect.sizeDelta;
        rect.localRotation = templateRect.localRotation;
        rect.localScale = templateRect.localScale;
        rect.anchoredPosition = lastRect.anchoredPosition;
        lastRect.anchoredPosition += step;

        ApplyStyle(button, templateButton);
        rect.SetSiblingIndex(originalCount - 1);

        if (list.GetComponent<LayoutGroup>() != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)list);

        return button;
    }

    private static void ApplyStyle(Button button, Button style)
    {
        if (style == null) return;
        Image original = style.targetGraphic as Image;
        Image target = button.targetGraphic as Image;
        if (original != null && target != null)
        {
            target.sprite = original.sprite;
            target.type = original.type;
            target.color = original.color;
        }
        button.colors = style.colors;
        TMP_Text sample = style.GetComponentInChildren<TMP_Text>(true);
        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
        if (sample != null)
        {
            label.fontSize = Mathf.Min(sample.fontSize, 32);
            label.color = sample.color;
        }
    }

    private void Build()
    {
        Image scrim = gameObject.AddComponent<Image>();
        scrim.color = new Color(.018f, .025f, .05f, .97f);

        RectTransform panel = CreateRect("GraphicsCard", transform);
        Stretch(panel, .08f, .06f, .92f, .94f);
        Image background = panel.gameObject.AddComponent<Image>();
        background.color = new Color(.035f, .055f, .09f, 1);
        Outline border = panel.gameObject.AddComponent<Outline>();
        border.effectColor = Accent;
        border.effectDistance = new Vector2(2, -2);

        RectTransform content = CreateRect("Controls", panel);
        Stretch(content, .035f, .035f, .965f, .97f);
        controls = content.gameObject.AddComponent<CanvasGroup>();
        TextMeshProUGUI title = CreateText("Title", content, "Gráficos", 50, TextAlignmentOptions.Left);
        Stretch(title.rectTransform, 0, .895f, 1, 1);
        TextMeshProUGUI intro = CreateText("Introduction", content,
            "Empieza con Equilibrado. Para reducir consumo y calor, prueba Eco, 30 FPS o menor escala 3D.",
            24, TextAlignmentOptions.Left);
        Stretch(intro.rectTransform, 0, .79f, 1, .91f);

        AddRow(content, 0, 0, "Perfil", ReadPresetName, step =>
        {
            int height = draft.resolutionHeight;
            bool fullscreen = draft.fullScreen;
            int next = Wrap((int)draft.preset + step, 3);
            draft = GameGraphicsSettings.CreatePreset((GameGraphicsSettings.GraphicsPreset)next);
            draft.resolutionHeight = height;
            draft.fullScreen = fullscreen;
        });
        AddRow(content, 0, 1, "Límite de FPS", () => draft.frameRate + " FPS", step =>
            draft.frameRate = Cycle(FrameRates, draft.frameRate, step));
        AddRow(content, 0, 2, "Sincronización vertical", () => OnOff(draft.vSync), step => draft.vSync = !draft.vSync);
        AddRow(content, 0, 3, "Resolución de pantalla", () =>
            GameGraphicsSettings.SupportsDisplayChanges ? (draft.resolutionHeight == 0 ? "Nativa" : draft.resolutionHeight + "p") : UnavailableDisplayLabel,
            step => draft.resolutionHeight = Cycle(ResolutionHeights, draft.resolutionHeight, step),
            () => GameGraphicsSettings.SupportsDisplayChanges);
        AddRow(content, 0, 4, "Modo de pantalla", () =>
            GameGraphicsSettings.SupportsDisplayChanges ? (draft.fullScreen ? "Pantalla completa" : "Ventana") : UnavailableDisplayLabel,
            step => draft.fullScreen = !draft.fullScreen, () => GameGraphicsSettings.SupportsDisplayChanges);
        AddRow(content, 1, 0, "Escala de renderizado 3D", () => Mathf.RoundToInt(draft.renderScale * 100) + "%", step =>
        {
            int closest = 0;
            for (int i = 1; i < RenderScales.Length; i++)
                if (Mathf.Abs(RenderScales[i] - draft.renderScale) < Mathf.Abs(RenderScales[closest] - draft.renderScale)) closest = i;
            draft.renderScale = RenderScales[Wrap(closest + step, RenderScales.Length)];
        });
        AddRow(content, 1, 1, "Detalle de texturas", () =>
            draft.textureMipmapLimit == 0 ? "Completo" : draft.textureMipmapLimit == 1 ? "Mitad" : "Cuarto",
            step => draft.textureMipmapLimit = Wrap(draft.textureMipmapLimit + step, 3));
        AddRow(content, 1, 2, "Sombras en tiempo real", () => OnOff(draft.shadows), step => draft.shadows = !draft.shadows);
        AddRow(content, 1, 3, "Oclusión ambiental (SSAO)", () =>
            GameGraphicsSettings.SupportsAmbientOcclusion ? OnOff(draft.ambientOcclusion) : "No disponible",
            step => draft.ambientOcclusion = !draft.ambientOcclusion, () => GameGraphicsSettings.SupportsAmbientOcclusion);
        AddRow(content, 1, 4, "Postprocesado", () => OnOff(draft.postProcessing), step => draft.postProcessing = !draft.postProcessing);

        syncHint = CreateText("SyncHint", content, "", 21, TextAlignmentOptions.Left);
        Stretch(syncHint.rectTransform, 0, .115f, 1, .215f);
        syncHint.color = new Color(.67f, .8f, .84f);
        statusText = CreateText("Status", content, "", 20, TextAlignmentOptions.Left);
        Stretch(statusText.rectTransform, 0, .068f, 1, .12f);
        statusText.color = Accent;

        backButton = CreateButton("Back", content, "Volver", Close);
        Stretch((RectTransform)backButton.transform, 0, 0, .25f, .065f);
        resetButton = CreateButton("Reset", content, "Restablecer", ResetSettings);
        Stretch((RectTransform)resetButton.transform, .375f, 0, .66f, .065f);
        applyButton = CreateButton("Apply", content, "Aplicar", ApplySettings);
        Stretch((RectTransform)applyButton.transform, .715f, 0, 1, .065f);

        BuildConfirmation(panel);
        ConfigureNavigation();
    }

    private void AddRow(Transform parent, int column, int row, string label, Func<string> read, Action<int> edit, Func<bool> available = null)
    {
        RectTransform holder = CreateRect(label, parent);
        float left = column == 0 ? 0 : .53f;
        float top = .79f - row * .112f;
        Stretch(holder, left, top - .1f, left + .47f, top);
        TextMeshProUGUI caption = CreateText("Label", holder, label, 23, TextAlignmentOptions.Left);
        Stretch(caption.rectTransform, 0, .58f, 1, 1);
        SettingRow setting = new SettingRow { read = read, available = available };
        setting.previous = CreateButton("Previous", holder, "<", () => Edit(edit, -1));
        Stretch((RectTransform)setting.previous.transform, 0, 0, .125f, .57f);
        setting.next = CreateButton("Next", holder, ">", () => Edit(edit, 1));
        Stretch((RectTransform)setting.next.transform, .875f, 0, 1, .57f);
        setting.value = CreateText("Value", holder, "", 23, TextAlignmentOptions.Center);
        Stretch(setting.value.rectTransform, .135f, 0, .865f, .57f);
        setting.value.color = Accent;
        rows.Add(setting);
    }

    private void BuildConfirmation(Transform parent)
    {
        RectTransform overlay = CreateRect("DisplayConfirmation", parent);
        Stretch(overlay, 0, 0, 1, 1);
        confirmation = overlay.gameObject;
        overlay.gameObject.AddComponent<Image>().color = new Color(.02f, .035f, .065f, .99f);
        confirmationText = CreateText("Question", overlay, "", 35, TextAlignmentOptions.Center);
        Stretch(confirmationText.rectTransform, .1f, .46f, .9f, .78f);
        keepButton = CreateButton("Keep", overlay, "Mantener", () =>
        {
            GameGraphicsSettings.ConfirmDisplayChange();
            RefreshFromService();
            statusText.text = "Configuración de pantalla guardada.";
        });
        Stretch((RectTransform)keepButton.transform, .15f, .28f, .47f, .39f);
        revertButton = CreateButton("Revert", overlay, "Revertir", () =>
        {
            GameGraphicsSettings.RevertDisplayChange();
            RefreshFromService();
            statusText.text = "Pantalla anterior restaurada.";
        });
        Stretch((RectTransform)revertButton.transform, .53f, .28f, .85f, .39f);
        SetNavigation(keepButton, revertButton, revertButton, revertButton, revertButton);
        SetNavigation(revertButton, keepButton, keepButton, keepButton, keepButton);
        confirmation.SetActive(false);
    }

    private void OnEnable()
    {
        if (controls == null) return;
        GameGraphicsSettings.Changed += RefreshFromService;
        RefreshFromService();
        statusText.text = "Los cambios se guardan al pulsar Aplicar.";
        Focus(GameGraphicsSettings.IsDisplayChangePending ? keepButton : rows[0].next);
    }

    private void OnDisable()
    {
        GameGraphicsSettings.Changed -= RefreshFromService;
        if (GameGraphicsSettings.IsDisplayChangePending) GameGraphicsSettings.RevertDisplayChange();
    }

    private void Update()
    {
        bool pending = GameGraphicsSettings.IsDisplayChangePending;
        if (pending != wasDisplayPending) RefreshFromService();
        if (pending)
        {
            int seconds = Mathf.CeilToInt(GameGraphicsSettings.DisplayConfirmationSecondsRemaining);
            if (seconds != lastCountdown)
            {
                lastCountdown = seconds;
                confirmationText.text = "¿Mantener esta configuración de pantalla?\n\nSe revierte automáticamente en " + seconds + " s.";
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
            statusText.text = "Pantalla anterior restaurada.";
        }
        else Close();
    }

    private void Edit(Action<int> edit, int step)
    {
        if (GameGraphicsSettings.IsDisplayChangePending) return;
        edit(step);
        RefreshRows();
        statusText.text = "Cambios pendientes. Pulsa Aplicar para guardarlos.";
    }

    private void ApplySettings()
    {
        GameGraphicsSettings.Apply(draft);
        RefreshFromService();
        statusText.text = GameGraphicsSettings.IsDisplayChangePending
            ? "Confirma el cambio de pantalla para conservarlo."
            : "Configuración aplicada y guardada.";
    }

    private void ResetSettings()
    {
        GameGraphicsSettings.ResetToDefaults();
        RefreshFromService();
        statusText.text = "Valores recomendados restablecidos.";
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
            statusText.text = "Configuración actual guardada.";
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
            ? "VSync respeta el límite elegido; algunas pantallas usarán una frecuencia menor para sincronizar.\nLa escala 3D conserva la nitidez de la interfaz. Menús y pausa: máximo 30 FPS."
            : "El límite de FPS evita dibujar fotogramas innecesarios. 30–60 FPS es un buen inicio en portátiles.\nLa escala 3D conserva la nitidez de la interfaz. Menús y pausa: máximo 30 FPS.";
        ConfigureNavigation();
    }

    private void Close()
    {
        lastBackFrame = Time.frameCount;
        if (GameGraphicsSettings.IsDisplayChangePending) GameGraphicsSettings.RevertDisplayChange();
        gameObject.SetActive(false);
        onClose?.Invoke();
        Focus(entryButton);
    }

    // Explicit navigation keeps focus inside the settings panel and skips unavailable options.
    private void ConfigureNavigation()
    {
        var activeRows = new List<SettingRow>();
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

    private Button CreateButton(string name, Transform parent, string caption, Action clicked)
    {
        RectTransform rect = CreateRect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = Color.white;
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(.07f, .22f, .25f);
        colors.highlightedColor = new Color(.14f, .45f, .45f);
        colors.selectedColor = new Color(.14f, .45f, .45f);
        colors.pressedColor = new Color(.12f, .55f, .4f);
        colors.disabledColor = new Color(.08f, .12f, .15f, .5f);
        colors.fadeDuration = .1f;
        button.colors = colors;
        button.onClick.AddListener(() =>
        {
            MusicManager.Instance?.PlayUISound(MusicManager.Instance.clickSFX);
            clicked?.Invoke();
        });
        TextMeshProUGUI text = CreateText("Text", rect, caption, 26, TextAlignmentOptions.Center);
        Stretch(text.rectTransform, .04f, .04f, .96f, .96f);
        return button;
    }

    private TextMeshProUGUI CreateText(string name, Transform parent, string text, float size, TextAlignmentOptions alignment)
    {
        RectTransform rect = CreateRect(name, parent);
        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) label.font = font;
        label.text = text;
        label.fontSize = size;
        label.enableAutoSizing = true;
        label.fontSizeMin = size * .75f;
        label.fontSizeMax = size;
        label.alignment = alignment;
        label.color = Color.white;
        label.raycastTarget = false;
        label.overflowMode = TextOverflowModes.Ellipsis;
        return label;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static void Stretch(RectTransform rect, float minX, float minY, float maxX, float maxY)
    {
        rect.anchorMin = new Vector2(minX, minY);
        rect.anchorMax = new Vector2(maxX, maxY);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private string ReadPresetName()
    {
        GameGraphicsSettings.SettingsData preset = GameGraphicsSettings.CreatePreset(draft.preset);
        bool matches = draft.frameRate == preset.frameRate && draft.vSync == preset.vSync
            && Mathf.Approximately(draft.renderScale, preset.renderScale)
            && draft.textureMipmapLimit == preset.textureMipmapLimit && draft.shadows == preset.shadows
            && draft.ambientOcclusion == preset.ambientOcclusion && draft.postProcessing == preset.postProcessing;
        return matches ? PresetName(draft.preset) : "Personalizado";
    }

    private static string UnavailableDisplayLabel => Application.isEditor ? "Solo en la build" : "Gestionado por sistema";

    private static int Cycle(int[] choices, int current, int step)
    {
        int index = Array.IndexOf(choices, current);
        return choices[Wrap((index < 0 ? 0 : index) + step, choices.Length)];
    }

    private static int Wrap(int value, int count) => (value % count + count) % count;
    private static string OnOff(bool value) => value ? "Activado" : "Desactivado";
    private static string PresetName(GameGraphicsSettings.GraphicsPreset preset) =>
        preset == GameGraphicsSettings.GraphicsPreset.Eco ? "Eco" :
        preset == GameGraphicsSettings.GraphicsPreset.High ? "Alto" : "Equilibrado";
}
