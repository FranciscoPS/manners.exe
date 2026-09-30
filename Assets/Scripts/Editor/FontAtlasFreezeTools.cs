using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;

public static class FontAtlasFreezeTools
{
    private const string LogPrefix = "[FontAtlasFreeze]";
    private const string SpanishCharacters = " ¡¿ÁÉÍÓÚÜÑáéíóúüñ°×–—‘’“”•…€";
    private const string SymbolCharacters = "←↑→↓↔─│═►◄▲▼●○■□≈";

    private static readonly FrozenFont[] FrozenFonts =
    {
        new FrozenFont("04f713734c4b9c74f84e76d6d49dc08c", PrintableAscii() + SpanishCharacters),
        new FrozenFont("2e498d1c8094910479dc3e1b768306a4", SymbolCharacters)
    };

    private readonly struct FrozenFont
    {
        public readonly string Guid;
        public readonly string Characters;

        public FrozenFont(string guid, string characters)
        {
            Guid = guid;
            Characters = characters;
        }
    }

    [MenuItem("Tools/Manners/Fuentes/Congelar atlas de fuentes TMP", false, 70)]
    public static void FreezeFontAtlases()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError($"{LogPrefix} Sal de Play Mode antes de congelar las fuentes.");
            return;
        }

        foreach (FrozenFont frozenFont in FrozenFonts)
            Freeze(frozenFont);

        AssetDatabase.SaveAssets();
        WarnAboutDynamicFonts();
    }

    private static void Freeze(FrozenFont frozenFont)
    {
        string path = AssetDatabase.GUIDToAssetPath(frozenFont.Guid);
        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if (fontAsset == null)
        {
            Debug.LogError($"{LogPrefix} No se encontró la fuente con GUID {frozenFont.Guid}.");
            return;
        }

        Font sourceFont = LoadSourceFont(fontAsset);
        if (sourceFont == null)
        {
            Debug.LogError($"{LogPrefix} '{fontAsset.name}' no tiene su archivo de fuente original (.ttf/.otf); no se puede regenerar el atlas.", fontAsset);
            return;
        }

        bool multiAtlas = fontAsset.isMultiAtlasTexturesEnabled;
        EnableDynamicPopulation(fontAsset, sourceFont);
        fontAsset.isMultiAtlasTexturesEnabled = true;
        fontAsset.ClearFontAssetData(false);
        fontAsset.TryAddCharacters(frozenFont.Characters, out string missingCharacters, true);
        ClearKerningLookupFlags(fontAsset);
        fontAsset.isMultiAtlasTexturesEnabled = multiAtlas || fontAsset.atlasTextures.Length > 1;
        fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;
        Save(fontAsset);

        foreach (Texture2D atlas in fontAsset.atlasTextures)
            SetReadable(atlas, false);
        Save(fontAsset);
        TMPro_EventManager.ON_FONT_PROPERTY_CHANGED(true, fontAsset);

        string missing = string.IsNullOrEmpty(missingCharacters)
            ? string.Empty
            : $" La fuente no trae '{missingCharacters}': TMP los toma de sus fuentes de respaldo.";
        Debug.Log($"{LogPrefix} '{fontAsset.name}' congelada en Static con {fontAsset.characterTable.Count} caracteres en {fontAsset.atlasTextures.Length} atlas de {fontAsset.atlasWidth}x{fontAsset.atlasHeight}. Ya no cambia al usarla ni al hacer build.{missing}", fontAsset);
    }

    private static Font LoadSourceFont(TMP_FontAsset fontAsset)
    {
        if (fontAsset.sourceFontFile != null) return fontAsset.sourceFontFile;

        string guid = new SerializedObject(fontAsset).FindProperty("m_SourceFontFileGUID")?.stringValue;
        return string.IsNullOrEmpty(guid) ? null : AssetDatabase.LoadAssetAtPath<Font>(AssetDatabase.GUIDToAssetPath(guid));
    }

    private static void EnableDynamicPopulation(TMP_FontAsset fontAsset, Font sourceFont)
    {
        var serializedFont = new SerializedObject(fontAsset);
        serializedFont.FindProperty("m_AtlasPopulationMode").intValue = (int)AtlasPopulationMode.Dynamic;
        serializedFont.FindProperty("m_SourceFontFile").objectReferenceValue = sourceFont;
        serializedFont.ApplyModifiedPropertiesWithoutUndo();
        fontAsset.ReadFontAssetDefinition();
    }

    private static void ClearKerningLookupFlags(TMP_FontAsset fontAsset)
    {
        var serializedFont = new SerializedObject(fontAsset);
        SerializedProperty pairs = serializedFont.FindProperty("m_FontFeatureTable.m_GlyphPairAdjustmentRecords");
        if (pairs == null) return;

        for (int i = 0; i < pairs.arraySize; i++)
        {
            SerializedProperty flags = pairs.GetArrayElementAtIndex(i).FindPropertyRelative("m_FeatureLookupFlags");
            if (flags != null) flags.intValue = 0;
        }

        serializedFont.ApplyModifiedPropertiesWithoutUndo();
        fontAsset.ReadFontAssetDefinition();
    }

    private static void SetReadable(Texture2D atlas, bool readable)
    {
        if (atlas == null) return;

        var serializedAtlas = new SerializedObject(atlas);
        SerializedProperty isReadable = serializedAtlas.FindProperty("m_IsReadable");
        if (isReadable == null || isReadable.boolValue == readable) return;

        isReadable.boolValue = readable;
        serializedAtlas.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Save(TMP_FontAsset fontAsset)
    {
        EditorUtility.SetDirty(fontAsset);
        if (fontAsset.material != null) EditorUtility.SetDirty(fontAsset.material);
        foreach (Texture2D atlas in fontAsset.atlasTextures)
            if (atlas != null) EditorUtility.SetDirty(atlas);
        AssetDatabase.SaveAssetIfDirty(fontAsset);
    }

    private static void WarnAboutDynamicFonts()
    {
        var dynamicFonts = new List<string>();
        foreach (string guid in AssetDatabase.FindAssets("t:TMP_FontAsset", new[] { "Assets" }))
        {
            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guid));
            if (fontAsset != null && fontAsset.atlasPopulationMode != AtlasPopulationMode.Static)
                dynamicFonts.Add(fontAsset.name);
        }

        if (dynamicFonts.Count > 0)
            Debug.LogWarning($"{LogPrefix} Siguen en modo dinámico y cambiarán en git cada vez que se usen: {string.Join(", ", dynamicFonts)}.");
    }

    private static string PrintableAscii()
    {
        var characters = new StringBuilder();
        for (char c = ' '; c <= '~'; c++)
            characters.Append(c);
        return characters.ToString();
    }
}
