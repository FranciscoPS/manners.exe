using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class UIStyleAudit
{
    private const float OverlapMinPixels = 4f;
    private const float OverlapMinShare = 0.1f;
    private const float EscapeTolerance = 5f;

    private struct Entry
    {
        public TMP_Text text;
        public Rect rect;
        public string path;
    }

    private static readonly Vector3[] Corners = new Vector3[4];

    public static int Run(Camera camera, int width, int height, string label, StringBuilder report)
    {
        var entries = new List<Entry>();
        int issues = 0;

        foreach (TMP_Text text in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (!text.isActiveAndEnabled || string.IsNullOrWhiteSpace(text.text)) continue;
            if (text.canvas == null || text.canvas.rootCanvas.worldCamera != camera) continue;
            if (EffectiveAlpha(text) < 0.08f) continue;

            text.ForceMeshUpdate(true, true);
            Bounds bounds = text.textBounds;
            if (bounds.size.x <= 0.5f || bounds.size.y <= 0.5f) continue;

            Rect rect = ScreenRect(text.transform, bounds.min, bounds.max, camera);
            string path = Path(text.transform);
            entries.Add(new Entry { text = text, rect = rect, path = path });

            if (text.isTextOverflowing && text.overflowMode != TextOverflowModes.Overflow)
                issues += Line(report, label, "NO CABE", $"{path} \"{Shorten(text.text)}\"");

            if (text.enableAutoSizing && text.fontSizeMax > 0f && text.fontSize < text.fontSizeMax * 0.62f)
                issues += Line(report, label, "MUY REDUCIDO", $"{path} \"{Shorten(text.text)}\" ({text.fontSize:0.#} de {text.fontSizeMax:0.#})");

            if (rect.xMin < -3f || rect.yMin < -3f || rect.xMax > width + 3f || rect.yMax > height + 3f)
                issues += Line(report, label, "FUERA DE PANTALLA", $"{path} \"{Shorten(text.text)}\"");

            Image background = Background(text);
            if (background != null)
            {
                Rect plate = ScreenRect(background.rectTransform, background.rectTransform.rect.min, background.rectTransform.rect.max, camera);
                Rect box = ScreenRect(text.rectTransform, text.rectTransform.rect.min, text.rectTransform.rect.max, camera);
                bool designedInside = box.xMin >= plate.xMin - 2f && box.xMax <= plate.xMax + 2f && box.yMin >= plate.yMin - 2f && box.yMax <= plate.yMax + 2f;
                float escape = Mathf.Max(Mathf.Max(plate.xMin - rect.xMin, rect.xMax - plate.xMax), Mathf.Max(plate.yMin - rect.yMin, rect.yMax - plate.yMax));
                if (designedInside && escape > EscapeTolerance)
                    issues += Line(report, label, "SE SALE DE SU FONDO", $"{path} \"{Shorten(text.text)}\" ({escape:0} px fuera de {background.name})");
            }
        }

        for (int i = 0; i < entries.Count; i++)
        {
            for (int j = i + 1; j < entries.Count; j++)
            {
                Rect a = entries[i].rect;
                Rect b = entries[j].rect;
                float w = Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin);
                float h = Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin);
                if (w < OverlapMinPixels || h < OverlapMinPixels) continue;

                float smaller = Mathf.Min(a.width * a.height, b.width * b.height);
                if (w * h < smaller * OverlapMinShare) continue;

                issues += Line(report, label, "SE ENCIMAN", $"{entries[i].path} \"{Shorten(entries[i].text.text)}\" <-> {entries[j].path} \"{Shorten(entries[j].text.text)}\" ({w:0}x{h:0} px)");
            }
        }

        report.AppendLine($"AUDITORIA {label}: {entries.Count} textos visibles, {issues} avisos.");
        return issues;
    }

    private static int Line(StringBuilder report, string label, string kind, string detail)
    {
        report.AppendLine($"  [{label}] {kind}: {detail}");
        return 1;
    }

    private static float EffectiveAlpha(TMP_Text text)
    {
        float alpha = text.color.a * text.alpha;
        Transform current = text.transform;
        while (current != null)
        {
            CanvasGroup group = current.GetComponent<CanvasGroup>();
            if (group != null)
            {
                alpha *= group.alpha;
                if (group.ignoreParentGroups) break;
            }
            current = current.parent;
        }
        return alpha;
    }

    private static Image Background(TMP_Text text)
    {
        Transform current = text.transform.parent;
        int depth = 0;
        while (current != null && depth < 3)
        {
            Image image = current.GetComponent<Image>();
            if (image != null && image.enabled && image.color.a > 0.2f && current.GetComponent<Canvas>() == null && !IsFullScreen(image.rectTransform))
                return image;

            foreach (Transform sibling in current)
            {
                if (!sibling.name.StartsWith("Style") || !sibling.gameObject.activeInHierarchy) continue;
                Image plate = sibling.GetComponent<Image>();
                if (plate != null && plate.enabled && plate.color.a > 0.2f && sibling.name != "StyleGlyph" && sibling.name != "StyleFrame")
                    return plate;
            }

            current = current.parent;
            depth++;
        }
        return null;
    }

    private static bool IsFullScreen(RectTransform rect)
    {
        return rect.anchorMin.sqrMagnitude < 0.0001f && (rect.anchorMax - Vector2.one).sqrMagnitude < 0.0001f && rect.sizeDelta.sqrMagnitude < 4f;
    }

    private static Rect ScreenRect(Transform transform, Vector3 min, Vector3 max, Camera camera)
    {
        Corners[0] = new Vector3(min.x, min.y, 0f);
        Corners[1] = new Vector3(min.x, max.y, 0f);
        Corners[2] = new Vector3(max.x, max.y, 0f);
        Corners[3] = new Vector3(max.x, min.y, 0f);

        Vector2 low = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 high = new Vector2(float.MinValue, float.MinValue);
        for (int i = 0; i < 4; i++)
        {
            Vector2 point = RectTransformUtility.WorldToScreenPoint(camera, transform.TransformPoint(Corners[i]));
            low = Vector2.Min(low, point);
            high = Vector2.Max(high, point);
        }
        return Rect.MinMaxRect(low.x, low.y, high.x, high.y);
    }

    private static string Shorten(string value)
    {
        value = value.Replace("\n", " ").Replace("\t", " ").Trim();
        return value.Length <= 28 ? value : value.Substring(0, 28) + "...";
    }

    private static string Path(Transform transform)
    {
        var parts = new List<string>();
        Transform current = transform;
        while (current != null && parts.Count < 4)
        {
            parts.Add(current.name);
            current = current.parent;
        }
        parts.Reverse();
        return string.Join("/", parts);
    }
}
