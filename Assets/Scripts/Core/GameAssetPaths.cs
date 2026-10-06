using System;
using System.IO;

public static class GameAssetPaths
{
    public const string Production = "Assets/Configurations/Production";
    public const string Sandbox = "Assets/Configurations/Sandbox";
    public const string ProductionResources = Production + "/Resources";
    public const string UIResources = ProductionResources + "/UI";

    public static string BaseName(string assetName)
    {
        if (assetName.EndsWith("_Production", StringComparison.Ordinal)) return assetName.Substring(0, assetName.Length - 11);
        if (assetName.EndsWith("_Sandbox", StringComparison.Ordinal)) return assetName.Substring(0, assetName.Length - 8);
        return assetName;
    }
    public static string SandboxFileName(string sourcePath) => BaseName(Path.GetFileNameWithoutExtension(sourcePath)) + "_Sandbox" + Path.GetExtension(sourcePath);
}
