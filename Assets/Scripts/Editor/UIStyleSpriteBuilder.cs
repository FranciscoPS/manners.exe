using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class UIStyleSpriteBuilder
{
    public const string Folder = "Assets/Sprites/UI/Style";

    private const int PixelsPerUnit = 200;
    private const float S = 2f;
    private const float Outline = 5f;

    private sealed class Painter
    {
        public readonly int width;
        public readonly int height;
        private readonly float[] r;
        private readonly float[] g;
        private readonly float[] b;
        private readonly float[] a;

        public Painter(int width, int height)
        {
            this.width = width;
            this.height = height;
            int count = width * height;
            r = new float[count];
            g = new float[count];
            b = new float[count];
            a = new float[count];
        }

        public void Paint(Func<float, float, float> distance, Color color)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float coverage = Mathf.Clamp01(0.5f - distance(x + 0.5f, y + 0.5f)) * color.a;
                    if (coverage <= 0f) continue;

                    int i = y * width + x;
                    float keep = 1f - coverage;
                    r[i] = color.r * coverage + r[i] * keep;
                    g[i] = color.g * coverage + g[i] * keep;
                    b[i] = color.b * coverage + b[i] * keep;
                    a[i] = coverage + a[i] * keep;
                }
            }
        }

        public Color32[] ToPixels(Color bleed)
        {
            var pixels = new Color32[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                float alpha = a[i];
                if (alpha > 0.0005f)
                    pixels[i] = new Color(r[i] / alpha, g[i] / alpha, b[i] / alpha, alpha);
                else
                    pixels[i] = new Color(bleed.r, bleed.g, bleed.b, 0f);
            }
            return pixels;
        }
    }

    public static void BuildAll(UIStyle style, StringBuilder report)
    {
        EditorAssetUtility.EnsureFolder(Folder);

        style.plate = Save("plate", BuildPlate(style), Border(16f));
        style.plateChamfer = Save("plate_chamfer", BuildPlateChamfer(style), Border(20f));
        style.panelDark = Save("panel_dark", BuildPanel(style, style.panel, style.panelAlt, true), Border(80f));
        style.panelSmall = Save("panel_small", BuildPanelSmall(style), Border(26f));
        style.panelPaper = Save("panel_paper", BuildPanel(style, style.paper, Color.Lerp(style.paper, style.secondary, 0.3f), false), Border(80f));
        style.fill = Save("fill", BuildFill(), new Vector4(5f, 6f, 5f, 6f) * S);
        style.frame = Save("frame", BuildFrame(style), Border(14f));
        style.burst = Save("burst", BuildBurst(style), Vector4.zero);
        style.bolt = Save("bolt", BuildBolt(style), Vector4.zero);
        style.spark = Save("spark", BuildSpark(style), Vector4.zero);
        style.ring = Save("ring", BuildRing(style), Vector4.zero);
        style.arrowDown = Save("arrow_down", BuildArrowDown(style), Vector4.zero);
        style.chevron = Save("chevron", BuildChevron(style), Vector4.zero);
        style.iconClock = Save("icon_clock", BuildClock(style), Vector4.zero);
        style.iconCoin = Save("icon_coin", BuildCoin(style), Vector4.zero);
        style.iconSpeaker = Save("icon_speaker", BuildSpeaker(style), Vector4.zero);

        EditorUtility.SetDirty(style);
        report.AppendLine($"SPRITES: 16 sprites del estilo generados en {Folder}.");
    }

    private static Vector4 Border(float units)
    {
        return Vector4.one * (units * S);
    }

    private static Texture2D ToTexture(Painter painter, Color bleed)
    {
        var texture = new Texture2D(painter.width, painter.height, TextureFormat.RGBA32, false);
        texture.SetPixels32(painter.ToPixels(bleed));
        texture.Apply();
        return texture;
    }

    private static Sprite Save(string name, Texture2D texture, Vector4 border)
    {
        string path = $"{Folder}/{name}.png";
        File.WriteAllBytes(path, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.spriteBorder = border;
        importer.mipmapEnabled = true;
        importer.filterMode = FilterMode.Trilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.alphaIsTransparency = true;
        importer.sRGBTexture = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 1024;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteGenerateFallbackPhysicsShape = false;
        settings.spriteExtrude = 0;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static float Polygon(Vector2[] v, float px, float py)
    {
        float dx = px - v[0].x;
        float dy = py - v[0].y;
        float d = dx * dx + dy * dy;
        float sign = 1f;
        int n = v.Length;

        for (int i = 0, j = n - 1; i < n; j = i, i++)
        {
            float ex = v[j].x - v[i].x;
            float ey = v[j].y - v[i].y;
            float wx = px - v[i].x;
            float wy = py - v[i].y;
            float t = Mathf.Clamp01((wx * ex + wy * ey) / (ex * ex + ey * ey));
            float bx = wx - ex * t;
            float by = wy - ey * t;
            d = Mathf.Min(d, bx * bx + by * by);

            bool c1 = py >= v[i].y;
            bool c2 = py < v[j].y;
            bool c3 = ex * wy > ey * wx;
            if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) sign = -sign;
        }

        return sign * Mathf.Sqrt(d);
    }

    private static float Circle(float px, float py, float cx, float cy, float radius)
    {
        float dx = px - cx;
        float dy = py - cy;
        return Mathf.Sqrt(dx * dx + dy * dy) - radius;
    }

    private static float Segment(float px, float py, float ax, float ay, float bx, float by, float radius)
    {
        float pax = px - ax;
        float pay = py - ay;
        float bax = bx - ax;
        float bay = by - ay;
        float h = Mathf.Clamp01((pax * bax + pay * bay) / (bax * bax + bay * bay));
        float dx = pax - bax * h;
        float dy = pay - bay * h;
        return Mathf.Sqrt(dx * dx + dy * dy) - radius;
    }

    private static Vector2[] Scale(Vector2[] points)
    {
        var scaled = new Vector2[points.Length];
        for (int i = 0; i < points.Length; i++) scaled[i] = points[i] * S;
        return scaled;
    }

    private static Vector2[] Rect(float x0, float y0, float x1, float y1)
    {
        return Scale(new[] { new Vector2(x0, y0), new Vector2(x1, y0), new Vector2(x1, y1), new Vector2(x0, y1) });
    }

    private static Vector2[] Chamfered(float x0, float y0, float x1, float y1, float cut)
    {
        return Scale(new[]
        {
            new Vector2(x0, y0), new Vector2(x1 - cut, y0), new Vector2(x1, y0 + cut),
            new Vector2(x1, y1), new Vector2(x0 + cut, y1), new Vector2(x0, y1 - cut)
        });
    }

    private static Color Shade()
    {
        return new Color(0.86f, 0.83f, 0.93f, 1f);
    }

    private static Texture2D BuildPlate(UIStyle style)
    {
        const float size = 56f;
        const float margin = 4f;
        var painter = new Painter((int)(size * S), (int)(size * S));
        Vector2[] shape = Rect(margin, margin, size - margin, size - margin);
        float bandTop = (margin + Outline + 5f) * S;

        painter.Paint((x, y) => Polygon(shape, x, y), style.ink);
        painter.Paint((x, y) => Polygon(shape, x, y) + Outline * S, Color.white);
        painter.Paint((x, y) => Mathf.Max(Polygon(shape, x, y) + Outline * S, y - bandTop), Shade());
        return ToTexture(painter, style.ink);
    }

    private static Texture2D BuildPlateChamfer(UIStyle style)
    {
        const float size = 64f;
        const float margin = 4f;
        var painter = new Painter((int)(size * S), (int)(size * S));
        Vector2[] shape = Chamfered(margin, margin, size - margin, size - margin, 12f);

        painter.Paint((x, y) => Polygon(shape, x, y), style.ink);
        painter.Paint((x, y) => Polygon(shape, x, y) + Outline * S, Color.white);
        return ToTexture(painter, style.ink);
    }

    private static Texture2D BuildPanel(UIStyle style, Color fill, Color dots, bool keyline)
    {
        const float size = 176f;
        const float margin = 4f;
        const float cut = 30f;
        const float ink = 6f;
        float key = keyline ? 2.5f : 0f;
        var painter = new Painter((int)(size * S), (int)(size * S));
        Vector2[] shape = Chamfered(margin, margin, size - margin, size - margin, cut);

        if (keyline) painter.Paint((x, y) => Polygon(shape, x, y), style.paper);
        painter.Paint((x, y) => Polygon(shape, x, y) + key * S, style.ink);
        painter.Paint((x, y) => Polygon(shape, x, y) + (key + ink) * S, fill);

        float inset = (key + ink + 2f) * S;
        var topLeft = new Vector2(margin + cut * 0.5f, size - margin - cut * 0.5f) * S;
        var bottomRight = new Vector2(size - margin - cut * 0.5f, margin + cut * 0.5f) * S;
        painter.Paint((x, y) => Mathf.Max(Polygon(shape, x, y) + inset, Halftone(x, y, topLeft, new Vector2(0.7071f, -0.7071f))), dots);
        painter.Paint((x, y) => Mathf.Max(Polygon(shape, x, y) + inset, Halftone(x, y, bottomRight, new Vector2(-0.7071f, 0.7071f))), dots);

        return ToTexture(painter, keyline ? style.paper : style.ink);
    }

    private static float Halftone(float px, float py, Vector2 origin, Vector2 direction)
    {
        const float spacing = 9f * S;
        const float extent = 46f * S;
        const float maxRadius = 3.3f * S;

        float along = (px - origin.x) * direction.x + (py - origin.y) * direction.y;
        float t = along / extent;
        if (t < -0.15f || t > 1f) return 1000f;

        float u = (px + py) * 0.7071f / spacing;
        float v = (px - py) * 0.7071f / spacing;
        float cu = (Mathf.Round(u) - u) * spacing;
        float cv = (Mathf.Round(v) - v) * spacing;
        float radius = maxRadius * Mathf.Pow(Mathf.Clamp01(1f - t), 0.9f);
        return Mathf.Sqrt(cu * cu + cv * cv) - radius;
    }

    private static Texture2D BuildPanelSmall(UIStyle style)
    {
        const float size = 72f;
        const float margin = 4f;
        const float key = 2f;
        var painter = new Painter((int)(size * S), (int)(size * S));
        Vector2[] shape = Chamfered(margin, margin, size - margin, size - margin, 14f);

        painter.Paint((x, y) => Polygon(shape, x, y), style.paper);
        painter.Paint((x, y) => Polygon(shape, x, y) + key * S, style.ink);
        painter.Paint((x, y) => Polygon(shape, x, y) + (key + Outline) * S, style.panel);
        return ToTexture(painter, style.paper);
    }

    private static Texture2D BuildFill()
    {
        const float size = 16f;
        const float margin = 1f;
        var painter = new Painter((int)(size * S), (int)(size * S));
        Vector2[] shape = Rect(margin, margin, size - margin, size - margin);
        float bandTop = (margin + 4f) * S;

        painter.Paint((x, y) => Polygon(shape, x, y), Color.white);
        painter.Paint((x, y) => Mathf.Max(Polygon(shape, x, y), y - bandTop), Shade());
        return ToTexture(painter, Color.white);
    }

    private static Texture2D BuildFrame(UIStyle style)
    {
        const float size = 48f;
        const float margin = 2f;
        const float key = 2f;
        var painter = new Painter((int)(size * S), (int)(size * S));
        Vector2[] shape = Rect(margin, margin, size - margin, size - margin);

        painter.Paint((x, y) => Mathf.Max(Polygon(shape, x, y), -(Polygon(shape, x, y) + (key + Outline) * S)), style.paper);
        painter.Paint((x, y) => Mathf.Max(Polygon(shape, x, y) + key * S, -(Polygon(shape, x, y) + (key + Outline) * S)), style.ink);
        return ToTexture(painter, style.ink);
    }

    private static Texture2D BuildBurst(UIStyle style)
    {
        const float width = 384f;
        const float height = 144f;
        const int spikes = 18;
        var painter = new Painter((int)(width * S), (int)(height * S));
        var points = new Vector2[spikes * 2];
        var random = new System.Random(7);
        float rx = width * 0.5f - 8f;
        float ry = height * 0.5f - 8f;

        for (int i = 0; i < points.Length; i++)
        {
            float angle = (i + 0.35f) / points.Length * Mathf.PI * 2f;
            bool tip = i % 2 == 0;
            float radius = tip ? 0.86f + (float)random.NextDouble() * 0.14f : 0.6f + (float)random.NextDouble() * 0.1f;
            points[i] = new Vector2(width * 0.5f + Mathf.Cos(angle) * rx * radius, height * 0.5f + Mathf.Sin(angle) * ry * radius);
        }

        Array.Reverse(points);
        Vector2[] shape = Scale(points);
        painter.Paint((x, y) => Polygon(shape, x, y), style.ink);
        painter.Paint((x, y) => Polygon(shape, x, y) + 6f * S, Color.white);
        return ToTexture(painter, style.ink);
    }

    private static Texture2D BuildBolt(UIStyle style)
    {
        var painter = new Painter((int)(96f * S), (int)(160f * S));
        Vector2[] shape = Scale(new[]
        {
            new Vector2(58f, 154f), new Vector2(14f, 74f), new Vector2(44f, 74f), new Vector2(26f, 6f),
            new Vector2(84f, 96f), new Vector2(52f, 96f), new Vector2(78f, 154f)
        });

        painter.Paint((x, y) => Polygon(shape, x, y), style.ink);
        painter.Paint((x, y) => Polygon(shape, x, y) + Outline * S, Color.white);
        return ToTexture(painter, style.ink);
    }

    private static Texture2D BuildSpark(UIStyle style)
    {
        const float size = 64f;
        var painter = new Painter((int)(size * S), (int)(size * S));
        var points = new Vector2[8];
        for (int i = 0; i < 8; i++)
        {
            float angle = i / 8f * Mathf.PI * 2f + Mathf.PI * 0.5f;
            float radius = i % 2 == 0 ? 29f : 9f;
            points[i] = new Vector2(size * 0.5f + Mathf.Cos(angle) * radius, size * 0.5f + Mathf.Sin(angle) * radius);
        }

        Vector2[] shape = Scale(points);
        painter.Paint((x, y) => Polygon(shape, x, y), style.ink);
        painter.Paint((x, y) => Polygon(shape, x, y) + 4f * S, Color.white);
        return ToTexture(painter, style.ink);
    }

    private static Texture2D BuildRing(UIStyle style)
    {
        const int size = 512;
        const float outer = 220f;
        const float unit = outer * 2f / 245f;
        var painter = new Painter(size, size);
        float center = size * 0.5f;
        float inkWidth = 9f * unit;
        float keyWidth = 2.5f * unit;
        float shadow = 9f * unit;

        painter.Paint((x, y) => Mathf.Max(Circle(x, y, center + shadow, center - shadow, outer), -Circle(x, y, center, center, outer - 2f)), style.ink);
        painter.Paint((x, y) => Mathf.Max(Circle(x, y, center, center, outer), -Circle(x, y, center, center, outer - inkWidth - keyWidth)), style.paper);
        painter.Paint((x, y) => Mathf.Max(Circle(x, y, center, center, outer), -Circle(x, y, center, center, outer - inkWidth)), style.ink);
        return ToTexture(painter, style.ink);
    }

    private static Texture2D BuildArrowDown(UIStyle style)
    {
        const float size = 96f;
        var painter = new Painter((int)(size * S), (int)(size * S));
        Vector2[] shape = Scale(new[]
        {
            new Vector2(31f, 88f), new Vector2(31f, 50f), new Vector2(13f, 50f), new Vector2(48f, 8f),
            new Vector2(83f, 50f), new Vector2(65f, 50f), new Vector2(65f, 88f)
        });

        painter.Paint((x, y) => Polygon(shape, x, y), style.ink);
        painter.Paint((x, y) => Polygon(shape, x, y) + Outline * S, Color.white);
        return ToTexture(painter, style.ink);
    }

    private static Texture2D BuildChevron(UIStyle style)
    {
        const float size = 48f;
        var painter = new Painter((int)(size * S), (int)(size * S));
        Vector2[] shape = Scale(new[]
        {
            new Vector2(8f, 42f), new Vector2(24f, 24f), new Vector2(8f, 6f), new Vector2(24f, 6f),
            new Vector2(42f, 24f), new Vector2(24f, 42f)
        });

        painter.Paint((x, y) => Polygon(shape, x, y), style.ink);
        painter.Paint((x, y) => Polygon(shape, x, y) + 4f * S, Color.white);
        return ToTexture(painter, style.ink);
    }

    private static Texture2D BuildClock(UIStyle style)
    {
        const float size = 64f;
        var painter = new Painter((int)(size * S), (int)(size * S));
        float c = size * 0.5f * S;

        painter.Paint((x, y) => Circle(x, y, c, c, 28f * S), style.ink);
        painter.Paint((x, y) => Circle(x, y, c, c, 22f * S), Color.white);
        painter.Paint((x, y) => Segment(x, y, c, c, c, c + 13f * S, 2.6f * S), style.ink);
        painter.Paint((x, y) => Segment(x, y, c, c, c + 10f * S, c - 5f * S, 2.6f * S), style.ink);
        painter.Paint((x, y) => Circle(x, y, c, c, 4f * S), style.ink);
        return ToTexture(painter, style.ink);
    }

    private static Texture2D BuildCoin(UIStyle style)
    {
        const float size = 64f;
        var painter = new Painter((int)(size * S), (int)(size * S));
        float c = size * 0.5f * S;
        Color detail = new Color(0.62f, 0.5f, 0.45f, 1f);

        painter.Paint((x, y) => Circle(x, y, c, c, 28f * S), style.ink);
        painter.Paint((x, y) => Circle(x, y, c, c, 22.5f * S), Color.white);
        painter.Paint((x, y) => Mathf.Abs(Circle(x, y, c, c, 15.5f * S)) - 1.6f * S, detail);
        painter.Paint((x, y) => Segment(x, y, c, c - 6f * S, c, c + 6f * S, 2.4f * S), detail);
        return ToTexture(painter, style.ink);
    }

    private static Texture2D BuildSpeaker(UIStyle style)
    {
        const float size = 64f;
        var painter = new Painter((int)(size * S), (int)(size * S));
        Vector2[] body = Scale(new[]
        {
            new Vector2(8f, 22f), new Vector2(20f, 22f), new Vector2(36f, 8f), new Vector2(36f, 56f),
            new Vector2(20f, 42f), new Vector2(8f, 42f)
        });
        float cx = 36f * S;
        float cy = 32f * S;

        Func<float, float, float, float, float> arc = (x, y, radius, half) =>
        {
            float wedge = (Mathf.Abs(y - cy) - (x - cx)) * 0.7071f;
            return Mathf.Max(Mathf.Abs(Circle(x, y, cx, cy, radius)) - half, wedge);
        };

        painter.Paint((x, y) => arc(x, y, 13f * S, 4.6f * S), style.ink);
        painter.Paint((x, y) => arc(x, y, 23f * S, 4.6f * S), style.ink);
        painter.Paint((x, y) => Polygon(body, x, y) - 4f * S, style.ink);
        painter.Paint((x, y) => Polygon(body, x, y), Color.white);
        painter.Paint((x, y) => arc(x, y, 13f * S, 1.9f * S), Color.white);
        painter.Paint((x, y) => arc(x, y, 23f * S, 1.9f * S), Color.white);
        return ToTexture(painter, style.ink);
    }
}
