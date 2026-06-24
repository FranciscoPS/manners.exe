using UnityEditor;
using UnityEngine;

public static class AddBuildingFaderTool
{
    private const string BuildingsFolder = "Assets/Prefabs/Buildings";

    [MenuItem("Tools/Buildings/Add BuildingFader To Prefabs")]
    public static void AddToAllBuildingPrefabs()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { BuildingsFolder });
        int added = 0;
        int skipped = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (root == null) continue;

            bool changed = false;
            if (root.GetComponent<BuildingFader>() == null)
            {
                root.AddComponent<BuildingFader>();
                changed = true;
                added++;
            }
            else
            {
                skipped++;
            }

            if (changed)
                PrefabUtility.SaveAsPrefabAsset(root, path);

            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[AddBuildingFaderTool] BuildingFader añadido a {added} prefab(s), {skipped} ya lo tenían.");
    }
}
