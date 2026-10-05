using System;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

public static class UIStyleFontBuilder
{
    public const string FontAssetPath = "Assets/Fonts/Orbitron-ExtraBold Display SDF.asset";

    private const string SourceFontPath = "Assets/Fonts/Orbitron-ExtraBold.ttf";
    private const string ExtraCharacters = " ¡¿ÁÉÍÓÚÜÑáéíóúüñ°×–—‘’“”•…€";
    private const int SamplingSize = 80;
    private const int Padding = 18;
    private const int AtlasWidth = 2048;
    private const int AtlasHeight = 1024;

    public static void Build(UIStyle style, StringBuilder report)
    {
        TMP_FontAsset asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (asset == null || asset.atlasPadding != Padding || asset.faceInfo.pointSize != SamplingSize)
        {
            if (asset != null) AssetDatabase.DeleteAsset(FontAssetPath);
            asset = Create(report);
        }

        if (asset == null) return;

        byte italic = (byte)Mathf.Clamp(Mathf.RoundToInt(style.skew * 100f), 0, 60);
        if (asset.italicStyle != italic || !Mathf.Approximately(asset.boldStyle, 0f))
        {
            asset.italicStyle = italic;
            asset.boldStyle = 0f;
            asset.boldSpacing = 0f;
            EditorUtility.SetDirty(asset);
        }

        asset.material.SetFloat("_WeightBold", 0f);
        EditorUtility.SetDirty(asset.material);

        style.font = asset;
        style.textInk = Preset(asset, "Ink", material =>
        {
            material.SetFloat("_FaceDilate", 0.27f);
            material.SetFloat("_OutlineWidth", 0.27f);
            material.SetColor("_OutlineColor", style.ink);
            material.EnableKeyword("OUTLINE_ON");
            material.EnableKeyword("UNDERLAY_ON");
            material.SetColor("_UnderlayColor", style.ink);
            material.SetFloat("_UnderlayOffsetX", 0.32f);
            material.SetFloat("_UnderlayOffsetY", -0.32f);
            material.SetFloat("_UnderlayDilate", 0.4f);
            material.SetFloat("_UnderlaySoftness", 0f);
        });
        style.textInkThin = Preset(asset, "Ink Thin", material =>
        {
            material.SetFloat("_FaceDilate", 0.1f);
            material.SetFloat("_OutlineWidth", 0.16f);
            material.SetColor("_OutlineColor", style.ink);
            material.EnableKeyword("OUTLINE_ON");
            material.DisableKeyword("UNDERLAY_ON");
        });
        style.textPlain = Preset(asset, "Plain", material =>
        {
            material.SetFloat("_FaceDilate", 0f);
            material.SetFloat("_OutlineWidth", 0f);
            material.DisableKeyword("OUTLINE_ON");
            material.DisableKeyword("UNDERLAY_ON");
        });

        EditorUtility.SetDirty(style);
        report.AppendLine($"FUENTE: '{asset.name}' ({asset.characterTable.Count} caracteres, atlas {asset.atlasWidth}x{asset.atlasHeight}, padding {asset.atlasPadding}) y 3 materiales de texto.");
    }

    private static TMP_FontAsset Create(StringBuilder report)
    {
        Font source = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
        if (source == null)
        {
            report.AppendLine($"FUENTE: no se encontró {SourceFontPath}.");
            return null;
        }

        TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(source, SamplingSize, Padding, GlyphRenderMode.SDFAA, AtlasWidth, AtlasHeight, AtlasPopulationMode.Dynamic, false);
        asset.name = Path.GetFileNameWithoutExtension(FontAssetPath);
        AssetDatabase.CreateAsset(asset, FontAssetPath);

        asset.material.name = asset.name + " Atlas Material";
        asset.atlasTexture.name = asset.name + " Atlas";
        AssetDatabase.AddObjectToAsset(asset.material, asset);
        AssetDatabase.AddObjectToAsset(asset.atlasTexture, asset);

        var characters = new StringBuilder();
        for (char c = ' '; c <= '~'; c++) characters.Append(c);
        characters.Append(ExtraCharacters);

        asset.TryAddCharacters(characters.ToString(), out string missing, false);
        asset.atlasPopulationMode = AtlasPopulationMode.Static;

        foreach (Texture2D atlas in asset.atlasTextures)
        {
            if (atlas == null) continue;
            var serializedAtlas = new SerializedObject(atlas);
            SerializedProperty readable = serializedAtlas.FindProperty("m_IsReadable");
            if (readable != null)
            {
                readable.boolValue = false;
                serializedAtlas.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorUtility.SetDirty(atlas);
        }

        EditorUtility.SetDirty(asset);
        EditorUtility.SetDirty(asset.material);
        AssetDatabase.SaveAssetIfDirty(asset);
        TMPro_EventManager.ON_FONT_PROPERTY_CHANGED(true, asset);

        if (!string.IsNullOrEmpty(missing))
            report.AppendLine($"FUENTE: Orbitron no trae '{missing}'; TMP los toma de la fuente de respaldo.");

        return asset;
    }

    private static Material Preset(TMP_FontAsset asset, string suffix, Action<Material> configure)
    {
        string path = $"Assets/Fonts/{asset.name} - {suffix}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(asset.material);
            AssetDatabase.CreateAsset(material, path);
        }
        else
        {
            material.shader = asset.material.shader;
            material.CopyPropertiesFromMaterial(asset.material);
        }

        material.SetTexture("_MainTex", asset.atlasTexture);
        material.SetFloat("_WeightBold", 0f);
        configure(material);
        ShaderUtilities.UpdateShaderRatios(material);
        EditorUtility.SetDirty(material);
        return material;
    }
}
