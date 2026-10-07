using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PerformanceAssetAudit
{
    public static void Run()
    {
        var report = new StringBuilder();
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Characters" }))
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            if (!prefab.GetComponent<EnemyController>()) continue;
            long triangles = 0;
            foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
                if (filter.sharedMesh) for (int i = 0; i < filter.sharedMesh.subMeshCount; i++) triangles += filter.sharedMesh.GetIndexCount(i) / 3;
            report.AppendLine($"ENEMY {prefab.name}: triangles={triangles}, renderers={prefab.GetComponentsInChildren<Renderer>(true).Length}, materials={prefab.GetComponentsInChildren<Renderer>(true).Sum(r => r.sharedMaterials.Length)}, colliders={prefab.GetComponentsInChildren<Collider>(true).Length}");
        }
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Final Levels/LEVEL 1/LEVEL 1.unity");
        var roots = scene.GetRootGameObjects();
        var filters = roots.SelectMany(r => r.GetComponentsInChildren<MeshFilter>(true)).Where(f => f.sharedMesh).ToArray();
        foreach (var group in filters.GroupBy(f => f.sharedMesh).OrderByDescending(g => (long)g.Key.vertexCount * g.Count()).Take(15))
            report.AppendLine($"MESH {group.Key.name}: vertices={group.Key.vertexCount} copies={group.Count()} asset={AssetDatabase.GetAssetPath(group.Key)}");
        var renderers = roots.SelectMany(r => r.GetComponentsInChildren<Renderer>(true)).ToArray();
        var colliders = roots.SelectMany(r => r.GetComponentsInChildren<Collider>(true)).ToArray();
        report.AppendLine($"SCENE renderers={renderers.Length}, materials={renderers.Sum(r => r.sharedMaterials.Length)}, lights={roots.Sum(r => r.GetComponentsInChildren<Light>(true).Length)}, buildings={roots.Sum(r => r.GetComponentsInChildren<BuildingsScript>(true).Length)}");
        foreach (var group in colliders.GroupBy(c => c.GetType().Name)) report.AppendLine($"COLLIDERS {group.Key}={group.Count()}");
        foreach (var camera in roots.SelectMany(r => r.GetComponentsInChildren<Camera>(true))) report.AppendLine($"CAMERA {camera.name}: {camera.pixelWidth}x{camera.pixelHeight}, mask={camera.cullingMask}");
        foreach (var terrain in roots.SelectMany(r => r.GetComponentsInChildren<Terrain>(true))) report.AppendLine($"TERRAIN {terrain.name}: heightmap={terrain.terrainData.heightmapResolution}, detail={terrain.terrainData.detailResolution}, trees={terrain.terrainData.treeInstanceCount}, pixelError={terrain.heightmapPixelError}");
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/performance-asset-audit.txt", report.ToString());
    }
}
