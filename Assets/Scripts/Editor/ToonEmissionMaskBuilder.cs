using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ToonEmissionMaskBuilder
{
    public enum Source { Automatico, SuavidadDelMaskMap, ColorClaveDelBaseMap }

    public sealed class Result
    {
        public Texture2D mask;
        public float coverage;
        public string method;
        public string warning;
    }

    public const float MinAutoCoverage = 0.02f;
    public const float MaxAutoCoverage = 0.45f;
    public const int DefaultMaskSize = 1024;

    public static Texture2D LoadReadable(Texture texture)
    {
        if (texture == null) return null;
        string path = AssetDatabase.GetAssetPath(texture);
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
        string extension = Path.GetExtension(path).ToLowerInvariant();
        if (extension != ".png" && extension != ".jpg" && extension != ".jpeg") return null;
        var readable = new Texture2D(2, 2, TextureFormat.RGBA32, false, true) { hideFlags = HideFlags.HideAndDontSave };
        if (!readable.LoadImage(File.ReadAllBytes(path)))
        {
            UnityEngine.Object.DestroyImmediate(readable);
            return null;
        }
        return readable;
    }

    public static Result Automatic(Texture baseMap, Texture maskMap, int size = DefaultMaskSize)
    {
        Texture2D readableMask = LoadReadable(maskMap);
        try
        {
            if (readableMask != null)
            {
                Result fromSmoothness = FromSmoothness(readableMask, 0.75f, size);
                if (fromSmoothness.mask != null && fromSmoothness.coverage >= MinAutoCoverage && fromSmoothness.coverage <= MaxAutoCoverage)
                    return fromSmoothness;
                if (fromSmoothness.mask != null) UnityEngine.Object.DestroyImmediate(fromSmoothness.mask);
            }
        }
        finally
        {
            if (readableMask != null) UnityEngine.Object.DestroyImmediate(readableMask);
        }

        Texture2D readableBase = LoadReadable(baseMap);
        if (readableBase == null)
            return new Result { warning = "No se pudo leer la textura base como PNG/JPG; genera la máscara a mano." };
        try
        {
            Result fromBlue = FromDarkBlue(readableBase, size);
            if (fromBlue.mask != null && fromBlue.coverage >= MinAutoCoverage && fromBlue.coverage <= MaxAutoCoverage)
                return fromBlue;
            if (fromBlue.mask != null) UnityEngine.Object.DestroyImmediate(fromBlue.mask);
            return new Result { warning = $"Ninguna heurística dio una cobertura razonable (azul oscuro: {fromBlue.coverage:P1}); genera la máscara con color clave." };
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(readableBase);
        }
    }

    public static Result FromSmoothness(Texture2D readableMask, float threshold, int size)
    {
        Color32[] pixels = readableMask.GetPixels32();
        var values = new float[pixels.Length];
        float limit = Mathf.Clamp01(threshold) * 255f;
        for (int i = 0; i < pixels.Length; i++) values[i] = pixels[i].a >= limit ? 1f : 0f;
        return Build(values, readableMask.width, readableMask.height, size, "suavidad (alfa) del MaskMap");
    }

    public static Result FromColorKey(Texture2D readableBase, Color key, float tolerance, int size)
    {
        Color32[] pixels = readableBase.GetPixels32();
        var values = new float[pixels.Length];
        float limit = Mathf.Max(0.001f, tolerance);
        for (int i = 0; i < pixels.Length; i++)
        {
            Color32 c = pixels[i];
            float dr = c.r / 255f - key.r;
            float dg = c.g / 255f - key.g;
            float db = c.b / 255f - key.b;
            float distance = Mathf.Sqrt(dr * dr + dg * dg + db * db);
            values[i] = distance <= limit ? 1f : 0f;
        }
        return Build(values, readableBase.width, readableBase.height, size, "color clave del BaseMap");
    }

    public static Result FromDarkBlue(Texture2D readableBase, int size)
    {
        Color32[] pixels = readableBase.GetPixels32();
        var values = new float[pixels.Length];
        for (int i = 0; i < pixels.Length; i++)
        {
            Color32 c = pixels[i];
            Color.RGBToHSV(new Color(c.r / 255f, c.g / 255f, c.b / 255f), out float h, out float s, out float v);
            bool isGlass = h >= 195f / 360f && h <= 235f / 360f && s >= 0.32f && s <= 0.85f && v >= 0.18f && v <= 0.56f;
            values[i] = isGlass ? 1f : 0f;
        }
        return Build(values, readableBase.width, readableBase.height, size, "azul oscuro del BaseMap (heurística)");
    }

    private static Result Build(float[] values, int width, int height, int size, string method)
    {
        int target = Mathf.Clamp(Mathf.Min(size, Mathf.Max(width, height)), 64, 4096);
        var mask = new Texture2D(target, target, TextureFormat.RGB24, false, true) { hideFlags = HideFlags.HideAndDontSave };
        var output = new Color32[target * target];
        double covered = 0;
        for (int y = 0; y < target; y++)
        {
            int y0 = y * height / target;
            int y1 = Mathf.Max(y0 + 1, (y + 1) * height / target);
            for (int x = 0; x < target; x++)
            {
                int x0 = x * width / target;
                int x1 = Mathf.Max(x0 + 1, (x + 1) * width / target);
                float sum = 0f;
                int count = 0;
                for (int sy = y0; sy < y1; sy++)
                    for (int sx = x0; sx < x1; sx++)
                    {
                        sum += values[sy * width + sx];
                        count++;
                    }
                float value = count > 0 ? sum / count : 0f;
                covered += value;
                byte b = (byte)Mathf.RoundToInt(Mathf.Clamp01(value) * 255f);
                output[y * target + x] = new Color32(b, b, b, 255);
            }
        }
        mask.SetPixels32(output);
        mask.Apply(false, false);
        return new Result { mask = mask, coverage = (float)(covered / output.Length), method = method };
    }

    public static Texture2D SaveMaskAsset(Texture2D mask, Texture baseMap, string suffix = "_Emission")
    {
        string basePath = AssetDatabase.GetAssetPath(baseMap);
        if (string.IsNullOrEmpty(basePath)) return null;
        string directory = Path.GetDirectoryName(basePath).Replace('\\', '/');
        string fileName = Path.GetFileNameWithoutExtension(basePath) + suffix + ".png";
        string path = directory + "/" + fileName;
        File.WriteAllBytes(path, mask.EncodeToPNG());
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = false;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = Mathf.Max(mask.width, mask.height);
            importer.textureCompression = TextureImporterCompression.Compressed;
            var baseImporter = AssetImporter.GetAtPath(basePath) as TextureImporter;
            if (baseImporter != null) importer.wrapMode = baseImporter.wrapMode;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
}
