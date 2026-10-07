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
    private const float Outline = 4f;
    private const float PlateOutline = 5f;

    private static readonly string[] Obsolete = { "bolt", "spark", "dot" };
    private static readonly Color CharlieGray = new Color32(0x8E, 0x97, 0xAC, 0xFF);
    private static readonly Color FaceBlack = new Color32(0x08, 0x09, 0x14, 0xFF);

    private sealed class Painter
    {
        public readonly int width;
        public readonly int height;
        private readonly float[] r;
        private readonly float[] g;
        private readonly float[] b;
        private readonly float[] a;

        public Painter(float widthUnits, float heightUnits)
        {
            width = Mathf.RoundToInt(widthUnits * S);
            height = Mathf.RoundToInt(heightUnits * S);
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
                    float coverage = Mathf.Clamp01(0.5f - distance((x + 0.5f) / S, (y + 0.5f) / S) * S) * color.a;
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

        public Texture2D ToTexture(Color bleed)
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

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }
    }

    public static void BuildAll(UIStyle style, StringBuilder report)
    {
        EditorAssetUtility.EnsureFolder(Folder);

        foreach (string name in Obsolete)
        {
            string path = $"{Folder}/{name}.png";
            if (File.Exists(path)) AssetDatabase.DeleteAsset(path);
        }

        style.plate = Save("plate", BuildPlate(style), Vector4.one * 16f);
        style.plateChamfer = Save("plate_chamfer", BuildTile(style), Vector4.one * 20f);
        style.capsule = Save("capsule", BuildCapsule(style), Vector4.one * 22f);
        style.panelDark = Save("panel_dark", BuildScreen(style, 176f, 22f, 30f, Outline, 6f, 2f, false), Vector4.one * 46f);
        style.panelSmall = Save("panel_small", BuildScreen(style, 72f, 12f, 14f, 3.5f, 4f, 0f, false), Vector4.one * 26f);
        style.screenFrame = Save("screen_frame", BuildScreen(style, 176f, 22f, 30f, Outline, 6f, 2f, true), Vector4.one * 46f);
        style.panelPaper = Save("panel_paper", BuildCharliePanel(style), Vector4.one * 46f);
        style.fill = Save("fill", BuildFill(), new Vector4(5f, 6f, 5f, 6f));
        style.frame = Save("frame", BuildFrame(style), Vector4.one * 14f);
        style.burst = Save("burst", BuildBanner(style), Vector4.zero);
        style.ring = Save("ring", BuildRing(style), Vector4.zero);
        style.triangle = Save("triangle", BuildTriangle(style), Vector4.zero);
        style.cursor = Save("cursor", BuildCursor(), Vector4.zero);
        style.arrowDown = Save("arrow_down", BuildArrowDown(style), Vector4.zero);
        style.chevron = Save("chevron", BuildChevron(style), Vector4.zero);
        style.iconClock = Save("icon_clock", BuildClock(style), Vector4.zero);
        style.iconCoin = Save("icon_coin", BuildCoin(style), Vector4.zero);
        style.iconSpeaker = Save("icon_speaker", BuildSpeaker(style), Vector4.zero);
        style.faceScreen = Save("face_screen", BuildFaceScreen(style), Vector4.zero);
        style.faceCalm = Save("face_calm", BuildFace(0), Vector4.zero);
        style.faceHappy = Save("face_happy", BuildFace(1), Vector4.zero);
        style.faceHurt = Save("face_hurt", BuildFace(2), Vector4.zero);
        style.faceAngry = Save("face_angry", BuildFace(3), Vector4.zero);
        style.faceDead = Save("face_dead", BuildFace(4), Vector4.zero);

        EditorUtility.SetDirty(style);
        report.AppendLine($"SPRITES: 24 sprites del estilo generados en {Folder}.");
    }

    private static Sprite Save(string name, Texture2D texture, Vector4 borderUnits)
    {
        string path = $"{Folder}/{name}.png";
        File.WriteAllBytes(path, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.spriteBorder = borderUnits * S;
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

    private static float RoundRect(float px, float py, float x0, float y0, float x1, float y1, float topLeft, float topRight, float bottomRight, float bottomLeft)
    {
        float qx = px - (x0 + x1) * 0.5f;
        float qy = py - (y0 + y1) * 0.5f;
        float radius = qx >= 0f ? (qy >= 0f ? topRight : bottomRight) : (qy >= 0f ? topLeft : bottomLeft);
        float dx = Mathf.Abs(qx) - ((x1 - x0) * 0.5f - radius);
        float dy = Mathf.Abs(qy) - ((y1 - y0) * 0.5f - radius);
        float outside = Mathf.Sqrt(Mathf.Max(dx, 0f) * Mathf.Max(dx, 0f) + Mathf.Max(dy, 0f) * Mathf.Max(dy, 0f));
        return Mathf.Min(Mathf.Max(dx, dy), 0f) + outside - radius;
    }

    private static float CutTopRight(float px, float py, float x1, float y1, float cut)
    {
        return ((px - x1) + (py - y1) + cut) * 0.7071f;
    }

    private static Texture2D BuildPlate(UIStyle style)
    {
        var painter = new Painter(56f, 56f);
        Func<float, float, float> shape = (x, y) => RoundRect(x, y, 3f, 3f, 53f, 53f, 1.5f, 1.5f, 1.5f, 1.5f);

        painter.Paint(shape, style.ink);
        painter.Paint((x, y) => shape(x, y) + PlateOutline, Color.white);
        painter.Paint((x, y) => Mathf.Max(shape(x, y) + PlateOutline, y - 14f), new Color(0.86f, 0.84f, 0.92f, 1f));
        return painter.ToTexture(style.ink);
    }

    private static Texture2D BuildTile(UIStyle style)
    {
        var painter = new Painter(64f, 64f);
        Func<float, float, float> shape = (x, y) => Mathf.Max(RoundRect(x, y, 2f, 2f, 62f, 62f, 10f, 2f, 10f, 10f), CutTopRight(x, y, 62f, 62f, 14f));

        painter.Paint(shape, style.ink);
        painter.Paint((x, y) => shape(x, y) + Outline, Color.white);
        return painter.ToTexture(style.ink);
    }

    private static Texture2D BuildCapsule(UIStyle style)
    {
        var painter = new Painter(64f, 44f);
        Func<float, float, float> shape = (x, y) => RoundRect(x, y, 2f, 2f, 62f, 42f, 20f, 20f, 20f, 20f);

        painter.Paint(shape, style.ink);
        painter.Paint((x, y) => shape(x, y) + Outline, Color.white);
        return painter.ToTexture(style.ink);
    }

    private static Texture2D BuildScreen(UIStyle style, float size, float radius, float cut, float ink, float bezel, float innerLine, bool frameOnly)
    {
        var painter = new Painter(size, size);
        float x1 = size - 3f;
        Func<float, float, float> shape = (x, y) => Mathf.Max(RoundRect(x, y, 3f, 3f, x1, x1, radius, 2f, radius, radius), CutTopRight(x, y, x1, x1, cut));
        Func<float, float, float> band = (x, y) => Mathf.Max(shape(x, y) + ink, -(shape(x, y) + ink + bezel));

        if (frameOnly)
        {
            painter.Paint(band, Color.white);
            return painter.ToTexture(Color.white);
        }

        painter.Paint(shape, style.ink);
        painter.Paint((x, y) => shape(x, y) + ink, style.secondary);
        painter.Paint((x, y) => Mathf.Max(band(x, y), -(CutTopRight(x, y, x1, x1, cut) + ink + bezel + 3f)), style.danger);
        painter.Paint((x, y) => shape(x, y) + ink + bezel, style.ink);
        painter.Paint((x, y) => shape(x, y) + ink + bezel + innerLine, style.panel);
        return painter.ToTexture(style.ink);
    }

    private static Texture2D BuildCharliePanel(UIStyle style)
    {
        var painter = new Painter(176f, 176f);
        Func<float, float, float> shape = (x, y) => RoundRect(x, y, 3f, 3f, 173f, 173f, 26f, 26f, 26f, 26f);

        painter.Paint(shape, style.ink);
        painter.Paint((x, y) => shape(x, y) + Outline, CharlieGray);
        painter.Paint((x, y) => shape(x, y) + Outline + 6f, style.ink);
        painter.Paint((x, y) => shape(x, y) + Outline + 8f, style.cream);
        return painter.ToTexture(style.ink);
    }

    private static Texture2D BuildFill()
    {
        var painter = new Painter(16f, 16f);
        painter.Paint((x, y) => RoundRect(x, y, 1f, 1f, 15f, 15f, 4f, 4f, 4f, 4f), Color.white);
        return painter.ToTexture(Color.white);
    }

    private static Texture2D BuildFrame(UIStyle style)
    {
        var painter = new Painter(48f, 48f);
        Func<float, float, float> shape = (x, y) => RoundRect(x, y, 2f, 2f, 46f, 46f, 8f, 8f, 8f, 8f);

        painter.Paint((x, y) => Mathf.Max(shape(x, y), -(shape(x, y) + 7f)), style.textDim);
        painter.Paint((x, y) => Mathf.Max(shape(x, y), -(shape(x, y) + 5.5f)), style.ink);
        return painter.ToTexture(style.ink);
    }

    private static Texture2D BuildBanner(UIStyle style)
    {
        const float width = 448f;
        const float height = 112f;
        const float point = 52f;
        var painter = new Painter(width, height);
        var shape = new[]
        {
            new Vector2(4f, height * 0.5f), new Vector2(point, height - 4f), new Vector2(width - point, height - 4f),
            new Vector2(width - 4f, height * 0.5f), new Vector2(width - point, 4f), new Vector2(point, 4f)
        };
        Color line = style.ink;
        line.a = 0.5f;

        painter.Paint((x, y) => Polygon(shape, x, y), style.ink);
        painter.Paint((x, y) => Polygon(shape, x, y) + 5f, Color.white);
        painter.Paint((x, y) => Mathf.Abs(Polygon(shape, x, y) + 12f) - 0.9f, line);
        return painter.ToTexture(style.ink);
    }

    private static Texture2D BuildRing(UIStyle style)
    {
        const float size = 256f;
        const float outer = 110f;
        const float center = size * 0.5f;
        var painter = new Painter(size, size);
        var north = new[]
        {
            new Vector2(center - 13f, center + outer + 1f), new Vector2(center + 13f, center + outer + 1f), new Vector2(center, center + outer - 21f)
        };

        painter.Paint((x, y) => Mathf.Max(Circle(x, y, center, center - 5f, outer), -Circle(x, y, center, center, outer - 1f)), style.ink);
        painter.Paint((x, y) => Mathf.Max(Circle(x, y, center, center, outer), -Circle(x, y, center, center, outer - 7.5f)), style.textDim);
        painter.Paint((x, y) => Mathf.Max(Circle(x, y, center, center, outer), -Circle(x, y, center, center, outer - 6f)), style.ink);

        for (int i = 1; i < 12; i++)
        {
            float angle = i / 12f * Mathf.PI * 2f + Mathf.PI * 0.5f;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);
            float inner = i % 3 == 0 ? outer - 15f : outer - 11f;
            painter.Paint((x, y) => Segment(x, y, center + cos * (outer - 6f), center + sin * (outer - 6f), center + cos * inner, center + sin * inner, 1.1f), style.textDim);
        }

        painter.Paint((x, y) => Polygon(north, x, y) - 2.5f, style.ink);
        painter.Paint((x, y) => Polygon(north, x, y), style.danger);
        return painter.ToTexture(style.ink);
    }

    private static Texture2D BuildTriangle(UIStyle style)
    {
        var painter = new Painter(48f, 48f);
        var shape = new[] { new Vector2(24f, 43f), new Vector2(4f, 7f), new Vector2(44f, 7f) };

        painter.Paint((x, y) => Polygon(shape, x, y), style.ink);
        painter.Paint((x, y) => Polygon(shape, x, y) + 4.5f, Color.white);
        painter.Paint((x, y) => Segment(x, y, 24f, 30f, 24f, 22f, 1.9f), style.ink);
        painter.Paint((x, y) => Circle(x, y, 24f, 15.5f, 2.1f), style.ink);
        return painter.ToTexture(style.ink);
    }

    private static Texture2D BuildCursor()
    {
        var painter = new Painter(16f, 28f);
        painter.Paint((x, y) => RoundRect(x, y, 0.5f, 0.5f, 15.5f, 27.5f, 1.5f, 1.5f, 1.5f, 1.5f), Color.white);
        return painter.ToTexture(Color.white);
    }

    private static Texture2D BuildArrowDown(UIStyle style)
    {
        var painter = new Painter(96f, 96f);
        var shape = new[]
        {
            new Vector2(31f, 88f), new Vector2(31f, 50f), new Vector2(13f, 50f), new Vector2(48f, 8f),
            new Vector2(83f, 50f), new Vector2(65f, 50f), new Vector2(65f, 88f)
        };

        painter.Paint((x, y) => Polygon(shape, x, y), style.ink);
        painter.Paint((x, y) => Polygon(shape, x, y) + 5f, Color.white);
        return painter.ToTexture(style.ink);
    }

    private static Texture2D BuildChevron(UIStyle style)
    {
        var painter = new Painter(48f, 48f);
        var shape = new[]
        {
            new Vector2(8f, 42f), new Vector2(24f, 24f), new Vector2(8f, 6f), new Vector2(24f, 6f),
            new Vector2(42f, 24f), new Vector2(24f, 42f)
        };

        painter.Paint((x, y) => Polygon(shape, x, y), style.ink);
        painter.Paint((x, y) => Polygon(shape, x, y) + 4f, Color.white);
        return painter.ToTexture(style.ink);
    }

    private static Texture2D BuildClock(UIStyle style)
    {
        var painter = new Painter(64f, 64f);
        const float c = 32f;

        painter.Paint((x, y) => Circle(x, y, c, c, 28f), style.ink);
        painter.Paint((x, y) => Circle(x, y, c, c, 23f), Color.white);
        painter.Paint((x, y) => Segment(x, y, c, c, c, c + 13f, 2.6f), style.ink);
        painter.Paint((x, y) => Segment(x, y, c, c, c + 10f, c - 5f, 2.6f), style.ink);
        painter.Paint((x, y) => Circle(x, y, c, c, 4f), style.ink);
        return painter.ToTexture(style.ink);
    }

    private static Texture2D BuildCoin(UIStyle style)
    {
        var painter = new Painter(64f, 64f);
        const float c = 32f;
        Color detail = new Color(0.52f, 0.5f, 0.42f, 1f);

        painter.Paint((x, y) => Circle(x, y, c, c, 28f), style.ink);
        painter.Paint((x, y) => Circle(x, y, c, c, 23.5f), Color.white);
        painter.Paint((x, y) => Mathf.Abs(Circle(x, y, c, c, 16f)) - 1.6f, detail);
        painter.Paint((x, y) => Segment(x, y, c, c - 6f, c, c + 6f, 2.4f), detail);
        return painter.ToTexture(style.ink);
    }

    private static Texture2D BuildSpeaker(UIStyle style)
    {
        var painter = new Painter(64f, 64f);
        var body = new[]
        {
            new Vector2(8f, 22f), new Vector2(20f, 22f), new Vector2(36f, 8f), new Vector2(36f, 56f),
            new Vector2(20f, 42f), new Vector2(8f, 42f)
        };
        const float cx = 36f;
        const float cy = 32f;

        Func<float, float, float, float, float> arc = (x, y, radius, half) =>
        {
            float wedge = (Mathf.Abs(y - cy) - (x - cx)) * 0.7071f;
            return Mathf.Max(Mathf.Abs(Circle(x, y, cx, cy, radius)) - half, wedge);
        };

        painter.Paint((x, y) => arc(x, y, 13f, 4.6f), style.ink);
        painter.Paint((x, y) => arc(x, y, 23f, 4.6f), style.ink);
        painter.Paint((x, y) => Polygon(body, x, y) - 4f, style.ink);
        painter.Paint((x, y) => Polygon(body, x, y), Color.white);
        painter.Paint((x, y) => arc(x, y, 13f, 1.9f), Color.white);
        painter.Paint((x, y) => arc(x, y, 23f, 1.9f), Color.white);
        return painter.ToTexture(style.ink);
    }

    private static Texture2D BuildFaceScreen(UIStyle style)
    {
        var painter = new Painter(96f, 96f);
        const float c = 48f;

        painter.Paint((x, y) => Circle(x, y, c, c, 46f), style.ink);
        painter.Paint((x, y) => Circle(x, y, c, c, 42f), style.secondary);
        painter.Paint((x, y) => Circle(x, y, c, c, 36f), style.ink);
        painter.Paint((x, y) => Circle(x, y, c, c, 34f), FaceBlack);
        return painter.ToTexture(style.ink);
    }

    private static Texture2D BuildFace(int expression)
    {
        var painter = new Painter(96f, 96f);
        const float left = 34f;
        const float right = 62f;
        const float eye = 50f;

        for (int side = 0; side < 2; side++)
        {
            float cx = side == 0 ? left : right;
            float inward = side == 0 ? 1f : -1f;

            switch (expression)
            {
                case 0:
                    painter.Paint((x, y) => Segment(x, y, cx, eye - 6f, cx, eye + 6f, 6.2f), Color.white);
                    break;
                case 1:
                    painter.Paint((x, y) => Mathf.Max(Mathf.Abs(Circle(x, y, cx, eye - 5f, 9f)) - 3.3f, (eye - 3f) - y), Color.white);
                    break;
                case 2:
                    painter.Paint((x, y) => Segment(x, y, cx - 7f * inward, eye + 8f, cx + 6f * inward, eye, 3.2f), Color.white);
                    painter.Paint((x, y) => Segment(x, y, cx + 6f * inward, eye, cx - 7f * inward, eye - 8f, 3.2f), Color.white);
                    break;
                case 3:
                    var wedge = new[]
                    {
                        new Vector2(cx - 10f * inward, eye + 9f), new Vector2(cx + 9f * inward, eye - 1f),
                        new Vector2(cx + 9f * inward, eye - 9f), new Vector2(cx - 10f * inward, eye - 9f)
                    };
                    painter.Paint((x, y) => Polygon(wedge, x, y) - 1f, Color.white);
                    break;
                default:
                    painter.Paint((x, y) => Segment(x, y, cx - 7f, eye + 7f, cx + 7f, eye - 7f, 3f), Color.white);
                    painter.Paint((x, y) => Segment(x, y, cx - 7f, eye - 7f, cx + 7f, eye + 7f, 3f), Color.white);
                    break;
            }
        }

        return painter.ToTexture(Color.white);
    }
}
