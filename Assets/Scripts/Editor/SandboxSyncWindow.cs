using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class SandboxSyncWindow : EditorWindow
{
    private class Group
    {
        public SandboxDiffTool.AssetPair pair;
        public List<SandboxDiffTool.DiffEntry> entries = new List<SandboxDiffTool.DiffEntry>();
        public bool expanded = true;
    }

    private static readonly string[] DirectionLabels = { "Sandbox → Producción", "Producción → Sandbox" };
    private const float ToggleWidth = 22f;
    private const float ValueWidth = 170f;
    private const float ArrowWidth = 24f;

    private SandboxDiffTool.DiffResult result;
    private readonly List<Group> groups = new List<Group>();
    private SandboxDiffTool.SyncDirection direction = SandboxDiffTool.SyncDirection.SandboxToProduction;
    private Vector2 scroll;
    private string filter = "";

    private GUIStyle sourceValueStyle;
    private GUIStyle targetValueStyle;
    private GUIStyle arrowStyle;

    [MenuItem("Tools/Manners/Sandbox/4. Comparar y sincronizar sandbox ↔ producción", false, 13)]
    public static void Open()
    {
        SandboxSyncWindow window = GetWindow<SandboxSyncWindow>("Sandbox ↔ Producción");
        window.minSize = new Vector2(840f, 420f);
        window.Scan();
        window.Focus();
    }

    private void OnEnable()
    {
        if (result == null)
            Scan();
    }

    private void Scan()
    {
        result = SandboxDiffTool.Compare();
        groups.Clear();

        Group current = null;
        for (int i = 0; i < result.entries.Count; i++)
        {
            SandboxDiffTool.DiffEntry entry = result.entries[i];

            if (current == null || current.pair != entry.pair)
            {
                current = new Group { pair = entry.pair };
                groups.Add(current);
            }

            current.entries.Add(entry);
        }

        scroll = Vector2.zero;
        Repaint();
    }

    private void OnGUI()
    {
        EnsureStyles();
        DrawToolbar();

        if (result == null) return;

        if (result.pairs.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "No se encontraron assets del sandbox emparejados con los de producción.\n" +
                "Ejecuta 'Tools > Manners > Sandbox > 1. Crear assets del sandbox' y vuelve a comparar.",
                MessageType.Warning);
            return;
        }

        if (groups.Count == 0)
        {
            EditorGUILayout.HelpBox(
                $"Sin diferencias: los {result.pairs.Count} assets del sandbox coinciden con producción en todos los valores rastreados " +
                "(números, texto, bool, color, enum, vectores y tamaños de listas; las referencias a otros assets no se comparan).",
                MessageType.Info);
            return;
        }

        EditorGUILayout.Space(4f);
        DrawColumnHeaders();

        scroll = EditorGUILayout.BeginScrollView(scroll);
        for (int i = 0; i < groups.Count; i++)
        {
            if (GroupMatchesFilter(groups[i]))
                DrawGroup(groups[i]);
        }
        EditorGUILayout.EndScrollView();

        DrawFooter();
    }

    private void EnsureStyles()
    {
        if (sourceValueStyle != null) return;

        sourceValueStyle = new GUIStyle(EditorStyles.boldLabel) { wordWrap = false, clipping = TextClipping.Clip };

        targetValueStyle = new GUIStyle(EditorStyles.label) { wordWrap = false, clipping = TextClipping.Clip };
        Color dimmed = targetValueStyle.normal.textColor;
        dimmed.a = 0.55f;
        targetValueStyle.normal.textColor = dimmed;

        arrowStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter };
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            if (GUILayout.Button("Volver a comparar", EditorStyles.toolbarButton, GUILayout.Width(130f)))
                Scan();

            GUILayout.Space(10f);
            GUILayout.Label("Copiar:", EditorStyles.miniLabel, GUILayout.Width(44f));
            direction = (SandboxDiffTool.SyncDirection)EditorGUILayout.Popup((int)direction, DirectionLabels, EditorStyles.toolbarPopup, GUILayout.Width(180f));

            GUILayout.Space(10f);
            if (GUILayout.Button("Todo", EditorStyles.toolbarButton, GUILayout.Width(48f)))
                SetAllSelected(true);
            if (GUILayout.Button("Nada", EditorStyles.toolbarButton, GUILayout.Width(48f)))
                SetAllSelected(false);

            GUILayout.FlexibleSpace();

            filter = EditorGUILayout.TextField(filter, EditorStyles.toolbarSearchField, GUILayout.Width(220f));

            if (GUILayout.Button("Reporte a consola", EditorStyles.toolbarButton, GUILayout.Width(120f)) && result != null)
                Debug.Log(SandboxDiffTool.BuildReport(result));
        }
    }

    private void DrawColumnHeaders()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Space(ToggleWidth + 8f);
            GUILayout.Label("Campo", EditorStyles.miniBoldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.Label("Producción", EditorStyles.miniBoldLabel, GUILayout.Width(ValueWidth));
            GUILayout.Space(ArrowWidth);
            GUILayout.Label("Sandbox", EditorStyles.miniBoldLabel, GUILayout.Width(ValueWidth));
            GUILayout.Space(16f);
        }
    }

    private void DrawGroup(Group group)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                bool all = group.entries.TrueForAll(entry => entry.selected);
                bool any = group.entries.Exists(entry => entry.selected);

                EditorGUI.showMixedValue = any && !all;
                bool toggled = EditorGUILayout.Toggle(all, GUILayout.Width(ToggleWidth));
                EditorGUI.showMixedValue = false;

                if (toggled != all)
                {
                    for (int i = 0; i < group.entries.Count; i++)
                        group.entries[i].selected = toggled;
                }

                group.expanded = EditorGUILayout.Foldout(group.expanded, $"{group.pair.label}  ({group.entries.Count})", true, EditorStyles.foldoutHeader);

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Prod", EditorStyles.miniButtonLeft, GUILayout.Width(44f)))
                    EditorGUIUtility.PingObject(group.pair.production);
                if (GUILayout.Button("Sandbox", EditorStyles.miniButtonRight, GUILayout.Width(62f)))
                    EditorGUIUtility.PingObject(group.pair.sandbox);
            }

            if (!group.expanded) return;

            for (int i = 0; i < group.entries.Count; i++)
            {
                if (EntryMatchesFilter(group, group.entries[i]))
                    DrawEntry(group.entries[i]);
            }
        }
    }

    private void DrawEntry(SandboxDiffTool.DiffEntry entry)
    {
        bool toProduction = direction == SandboxDiffTool.SyncDirection.SandboxToProduction;

        using (new EditorGUILayout.HorizontalScope())
        {
            entry.selected = EditorGUILayout.Toggle(entry.selected, GUILayout.Width(ToggleWidth));

            GUILayout.Label(new GUIContent(entry.displayName, entry.propertyPath), GUILayout.MinWidth(120f));
            GUILayout.FlexibleSpace();

            GUILayout.Label(new GUIContent(entry.productionValue, entry.productionValue), toProduction ? targetValueStyle : sourceValueStyle, GUILayout.Width(ValueWidth));
            GUILayout.Label(toProduction ? "◀" : "▶", arrowStyle, GUILayout.Width(ArrowWidth));
            GUILayout.Label(new GUIContent(entry.sandboxValue, entry.sandboxValue), toProduction ? sourceValueStyle : targetValueStyle, GUILayout.Width(ValueWidth));
        }
    }

    private void DrawFooter()
    {
        List<SandboxDiffTool.DiffEntry> selected = CollectSelected();

        using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
        {
            GUILayout.Label($"{result.entries.Count} diferencia(s) en {groups.Count} asset(s) · {selected.Count} seleccionada(s). Los valores en negrita son el origen; los atenuados se sobrescriben.", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();

            using (new EditorGUI.DisabledScope(selected.Count == 0))
            {
                if (GUILayout.Button($"Aplicar {selected.Count} · {DirectionLabels[(int)direction]}", GUILayout.Width(280f), GUILayout.Height(24f)))
                    ConfirmAndApply(selected);
            }
        }
    }

    private void ConfirmAndApply(List<SandboxDiffTool.DiffEntry> selected)
    {
        bool toProduction = direction == SandboxDiffTool.SyncDirection.SandboxToProduction;
        string sourceName = toProduction ? "SANDBOX" : "PRODUCCIÓN";
        string targetName = toProduction ? "PRODUCCIÓN" : "SANDBOX";

        bool confirmed = EditorUtility.DisplayDialog(
            "Sincronizar valores",
            $"Se van a sobrescribir {selected.Count} valor(es) en {targetName} con los de {sourceName}.\n\n" +
            "Los assets se guardan en disco al terminar. Puedes deshacer con Ctrl+Z mientras no cierres Unity.\n\n¿Continuar?",
            "Aplicar", "Cancelar");

        if (!confirmed) return;

        int applied = SandboxDiffTool.Apply(result, selected, direction);
        Debug.Log($"[SandboxSync] {applied} valor(es) copiados de {sourceName} a {targetName}.");
        Scan();
    }

    private List<SandboxDiffTool.DiffEntry> CollectSelected()
    {
        List<SandboxDiffTool.DiffEntry> selected = new List<SandboxDiffTool.DiffEntry>();
        if (result == null) return selected;

        for (int i = 0; i < result.entries.Count; i++)
        {
            if (result.entries[i].selected)
                selected.Add(result.entries[i]);
        }

        return selected;
    }

    private void SetAllSelected(bool value)
    {
        if (result == null) return;

        for (int i = 0; i < result.entries.Count; i++)
            result.entries[i].selected = value;
    }

    private bool GroupMatchesFilter(Group group)
    {
        if (string.IsNullOrWhiteSpace(filter)) return true;
        if (Contains(group.pair.label, filter)) return true;

        for (int i = 0; i < group.entries.Count; i++)
        {
            if (Contains(group.entries[i].displayName, filter)) return true;
        }

        return false;
    }

    private bool EntryMatchesFilter(Group group, SandboxDiffTool.DiffEntry entry)
    {
        if (string.IsNullOrWhiteSpace(filter)) return true;
        if (Contains(group.pair.label, filter)) return true;

        return Contains(entry.displayName, filter);
    }

    private static bool Contains(string text, string needle)
    {
        return text != null && text.IndexOf(needle, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
