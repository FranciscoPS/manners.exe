using UnityEditor;
using UnityEngine;

public static class LevelOptimizationTools
{
    private static readonly string[] StaticGeometryFolders =
    {
        "Assets/Prefabs/Buildings",
        "Assets/Prefabs/Map"
    };

    private const StaticEditorFlags GeometryFlags =
        StaticEditorFlags.BatchingStatic
        | StaticEditorFlags.OccluderStatic
        | StaticEditorFlags.OccludeeStatic
        | StaticEditorFlags.ReflectionProbeStatic;

    [MenuItem("Tools/Manners/Performance/1. Marcar edificios y mapa como estáticos", false, 40)]
    public static void MarkBuildingsAndMapStatic()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", StaticGeometryFolders);
        int prefabsTouched = 0;
        int objectsMarked = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (root == null) continue;

            bool hasRuntimeRendererMutator = root.GetComponentInChildren<BuildingFader>(true) != null
                || root.GetComponentInChildren<BuildingsScript>(true) != null;

            if (hasRuntimeRendererMutator)
            {
                Debug.Log($"[LevelOptimizationTools] {path} tiene BuildingFader/BuildingsScript (modifica materiales o se desactiva en runtime) — se deja sin marcar para no romper static batching.");
                PrefabUtility.UnloadPrefabContents(root);
                continue;
            }

            bool changed = false;
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (GameObjectUtility.GetStaticEditorFlags(t.gameObject) == GeometryFlags) continue;

                GameObjectUtility.SetStaticEditorFlags(t.gameObject, GeometryFlags);
                objectsMarked++;
                changed = true;
            }

            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                prefabsTouched++;
            }

            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[LevelOptimizationTools] {objectsMarked} objeto(s) marcados como estáticos en {prefabsTouched} prefab(s) de {StaticGeometryFolders.Length} carpeta(s). Ahora abrí cada escena de nivel y corré el paso 2 para bakear Occlusion Culling.");
    }

    [MenuItem("Tools/Manners/Performance/2. Bakear Occlusion Culling (escena actual)", false, 41)]
    public static void BakeOcclusionCullingForCurrentScene()
    {
        if (StaticOcclusionCulling.isRunning)
        {
            Debug.LogWarning("[LevelOptimizationTools] Ya hay un bake de Occlusion Culling en curso.");
            return;
        }

        StaticOcclusionCulling.Compute();
        Debug.Log("[LevelOptimizationTools] Bake de Occlusion Culling iniciado para la escena abierta. Repetí esto en cada escena de nivel (LEVEL 1, CityTest, MilitaryBase, Sandbox) — cada una necesita su propio bake, no lo comparten. Progreso visible en Window > Rendering > Occlusion Culling.");
    }

    [MenuItem("Tools/Manners/Performance/3. Activar GPU Instancing en materiales", false, 42)]
    public static void EnableGpuInstancingOnMaterials()
    {
        string[] guids = AssetDatabase.FindAssets("t:Material");
        int enabled = 0;
        int alreadyOn = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            if (mat.enableInstancing)
            {
                alreadyOn++;
                continue;
            }

            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            enabled++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[LevelOptimizationTools] GPU Instancing activado en {enabled} material(es) ({alreadyOn} ya lo tenían).");
    }
}
