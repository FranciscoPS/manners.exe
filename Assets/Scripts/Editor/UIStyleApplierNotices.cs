using System;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static partial class UIStyleApplier
{
    public const string OvertimeAlertPrefabPath = "Assets/Prefabs/UI/Notifications/OvertimeAlert.prefab";
    public const string ChestSequencePrefabPath = "Assets/Prefabs/UI/Chest/ChestOpeningSequence.prefab";
    public const string NoticesReportPath = "Logs/ui-notices-report.txt";

    private const string ChestHintName = "SkipHint";
    private const float OvertimeFontSize = 45f;
    private const float ChestPromptAnchorY = 0.165f;
    private const float ChestHintAnchorY = 0.0965f;
    private const float ChestHintFontMax = 30f;
    private const float ChestHintFontMin = 18f;
    private static readonly Vector2 OvertimeBannerSize = new Vector2(600f, 100f);
    private static readonly Vector2 OvertimeBurstSize = new Vector2(500f, 108f);
    private static readonly Vector2 ChestHintSize = new Vector2(780f, 68f);

    [MenuItem("Tools/Manners/UI/Avisos: tiempo extra y cofre", false, 116)]
    public static void ApplyNoticesFromMenu()
    {
        var noticesReport = new StringBuilder();
        ApplyNotices(UIStyleTools.GetOrCreateStyle(), noticesReport);
        Directory.CreateDirectory("Logs");
        File.WriteAllText(NoticesReportPath, noticesReport.ToString());
        Debug.Log(noticesReport.ToString());
    }

    public static void ApplyNoticesBatch()
    {
        var noticesReport = new StringBuilder();
        bool passed = true;
        try
        {
            ApplyNotices(UIStyleTools.GetOrCreateStyle(), noticesReport);
        }
        catch (Exception exception)
        {
            noticesReport.AppendLine("EXCEPTION: " + exception);
            passed = false;
        }

        Directory.CreateDirectory("Logs");
        File.WriteAllText(NoticesReportPath, noticesReport.ToString());
        if (passed) Debug.Log(noticesReport.ToString());
        else Debug.LogError(noticesReport.ToString());
        if (Application.isBatchMode) EditorApplication.Exit(passed ? 0 : 1);
    }

    public static void ApplyNotices(UIStyle targetStyle, StringBuilder targetReport)
    {
        style = targetStyle;
        report = targetReport;
        EditNoticePrefab(OvertimeAlertPrefabPath, StyleOvertimeAlert);
        EditNoticePrefab(ChestSequencePrefabPath, StyleChestHint);
        AssetDatabase.SaveAssets();
    }

    private static void EditNoticePrefab(string path, Action<Transform> edit)
    {
        if (!File.Exists(path))
        {
            report.AppendLine("AVISOS: falta " + path + "; no se tocó.");
            return;
        }

        ResetCounters();
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            edit(root.transform);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
        report.AppendLine($"AVISOS {path}: listo ({createdCount} objetos nuevos).");
    }

    private static void StyleOvertimeAlert(Transform root)
    {
        Transform banner = FindDeep(root, "OvertimeBanner");
        if (banner == null)
        {
            report.AppendLine("AVISOS: OvertimeAlert no tiene OvertimeBanner.");
            return;
        }

        ((RectTransform)banner).sizeDelta = OvertimeBannerSize;
        Touch(banner);

        Transform burst = DirectChild(banner, "Burst");
        if (burst != null)
        {
            ((RectTransform)burst).sizeDelta = OvertimeBurstSize;
            Touch(burst);
        }

        Transform textTransform = DirectChild(banner, "OvertimeText");
        TMP_Text text = textTransform != null ? textTransform.GetComponent<TMP_Text>() : null;
        if (text == null) return;

        text.fontSize = OvertimeFontSize;
        text.fontSizeMax = OvertimeFontSize;
        text.fontSizeMin = Mathf.Min(text.fontSizeMin, OvertimeFontSize);
        Finish(text);
    }

    private static void StyleChestHint(Transform root)
    {
        ChestOpeningSequence sequence = root.GetComponent<ChestOpeningSequence>();
        Transform text = FindDeep(root, "SkipHintText");
        if (sequence == null || text == null)
        {
            report.AppendLine("AVISOS: ChestOpeningSequence sin SkipHintText; no se tocó.");
            return;
        }

        Transform container = text.parent != null && text.parent.name == ChestHintName ? text.parent : null;
        if (container == null)
        {
            var holder = new GameObject(ChestHintName, typeof(RectTransform));
            holder.layer = text.gameObject.layer;
            container = holder.transform;
            container.SetParent(text.parent, false);
            container.SetSiblingIndex(text.GetSiblingIndex());
            text.SetParent(container, false);
            createdCount++;
        }

        var rect = (RectTransform)container;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, ChestHintAnchorY);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = ChestHintSize;
        rect.localScale = Vector3.one;
        CanvasGroup group = GetOrAdd<CanvasGroup>(container.gameObject);
        group.blocksRaycasts = false;
        group.interactable = false;
        Touch(group);
        Touch(rect);

        RectTransform plate = GetOrCreateChild(rect, StylePrefix + "HintPlate", 0);
        Stretch(plate, 0f, 0f, 0f, 0f);
        Image plateImage = GetOrAdd<Image>(plate.gameObject);
        plateImage.raycastTarget = false;
        HudPlate(plateImage, style.cyan);
        Touch(plate);

        var textRect = (RectTransform)text;
        textRect.localScale = Vector3.one;
        Stretch(textRect, 40f, 6f, -40f, -6f);
        text.SetAsLastSibling();
        Touch(textRect);

        TMP_Text hint = text.GetComponent<TMP_Text>();
        Apply(hint, UITextRole.Label, style.paper, true);
        hint.fontSizeMax = ChestHintFontMax;
        hint.fontSizeMin = ChestHintFontMin;
        hint.alignment = TextAlignmentOptions.Center;
        hint.textWrappingMode = TextWrappingModes.NoWrap;
        hint.richText = true;
        Finish(hint);

        Transform prompt = FindDeep(root, "PromptText");
        if (prompt is RectTransform promptRect)
        {
            promptRect.anchorMin = new Vector2(promptRect.anchorMin.x, ChestPromptAnchorY);
            promptRect.anchorMax = new Vector2(promptRect.anchorMax.x, ChestPromptAnchorY);
            Touch(promptRect);
        }

        var serialized = new SerializedObject(sequence);
        SetReference(serialized, "skipHintGroup", group);
        SerializedProperty skipHint = serialized.FindProperty("skipHint");
        skipHint.FindPropertyRelative("english").stringValue = "Press {0} to close";
        skipHint.FindPropertyRelative("spanish").stringValue = "Pulsa {0} para cerrar";
        serialized.ApplyModifiedPropertiesWithoutUndo();
        Touch(sequence);
    }
}
