using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SandboxEnvironmentBuilder
{
    public static IEnumerator Build(SandboxConfig config, Transform root)
    {
        switch (config.Environment)
        {
            case SandboxConfig.EnvironmentMode.ProceduralArena:
                BuildProceduralArena(config, root);
                break;

            case SandboxConfig.EnvironmentMode.EnvironmentPrefab:
                BuildFromPrefab(config, root);
                break;

            case SandboxConfig.EnvironmentMode.AdditiveScene:
                yield return BuildFromAdditiveScene(config);
                break;
        }
    }

    private static void BuildProceduralArena(SandboxConfig config, Transform root)
    {
        float size = Mathf.Max(10f, config.ArenaSize);

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.SetParent(root, false);
        ground.transform.localScale = new Vector3(size / 10f, 1f, size / 10f);

        if (config.GroundMaterial != null)
            ground.GetComponent<Renderer>().sharedMaterial = config.GroundMaterial;

        if (config.CreateInvisibleWalls)
            BuildWalls(root, size, config.WallHeight);

        int obstacleCount = BuildObstacles(config, root, size);

        SandboxLog.Ok($"Entorno: arena procedural {size:F0}x{size:F0}m, muros={config.CreateInvisibleWalls}, obstáculos={obstacleCount}");
    }

    private static void BuildWalls(Transform root, float size, float height)
    {
        GameObject walls = new GameObject("InvisibleWalls");
        walls.transform.SetParent(root, false);

        float half = size * 0.5f;
        float thickness = 2f;

        CreateWall(walls.transform, "IWall_North", new Vector3(0f, height * 0.5f, half), new Vector3(size, height, thickness));
        CreateWall(walls.transform, "IWall_South", new Vector3(0f, height * 0.5f, -half), new Vector3(size, height, thickness));
        CreateWall(walls.transform, "IWall_East", new Vector3(half, height * 0.5f, 0f), new Vector3(thickness, height, size));
        CreateWall(walls.transform, "IWall_West", new Vector3(-half, height * 0.5f, 0f), new Vector3(thickness, height, size));
    }

    private static void CreateWall(Transform parent, string name, Vector3 position, Vector3 size)
    {
        GameObject wall = new GameObject(name);
        wall.transform.SetParent(parent, false);
        wall.transform.localPosition = position;

        BoxCollider collider = wall.AddComponent<BoxCollider>();
        collider.size = size;
    }

    private static int BuildObstacles(SandboxConfig config, Transform root, float arenaSize)
    {
        SandboxConfig.ObstacleRing ring = config.Obstacles;

        if (ring == null || ring.prefabs == null || ring.prefabs.Length == 0 || ring.count <= 0)
            return 0;

        GameObject container = new GameObject("Obstacles");
        container.transform.SetParent(root, false);

        Random.State previousState = Random.state;
        Random.InitState(config.ArenaSeed);

        float limit = arenaSize * 0.5f - 4f;
        float inner = Mathf.Max(0f, ring.innerRadius);
        float outer = Mathf.Clamp(ring.outerRadius, inner + 1f, limit);

        int placed = 0;
        for (int i = 0; i < ring.count; i++)
        {
            GameObject prefab = ring.prefabs[Random.Range(0, ring.prefabs.Length)];
            if (prefab == null) continue;

            float angle = (i / (float)ring.count) * Mathf.PI * 2f + Random.Range(-0.12f, 0.12f);
            float radius = Random.Range(inner, outer);
            Vector3 position = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

            GameObject instance = Object.Instantiate(prefab, container.transform);
            instance.transform.localPosition = position;
            instance.transform.localRotation = ring.randomRotation
                ? Quaternion.Euler(0f, Random.Range(0f, 360f), 0f)
                : prefab.transform.rotation;
            instance.transform.localScale = Vector3.one * Mathf.Max(0.01f, ring.uniformScale);
            placed++;
        }

        Random.state = previousState;
        return placed;
    }

    private static void BuildFromPrefab(SandboxConfig config, Transform root)
    {
        if (config.EnvironmentPrefab == null)
        {
            SandboxLog.Warn("Modo EnvironmentPrefab sin prefab asignado. El sandbox se quedará sin suelo.");
            return;
        }

        GameObject instance = Object.Instantiate(config.EnvironmentPrefab, root);
        instance.transform.localPosition = Vector3.zero;
        instance.name = config.EnvironmentPrefab.name;

        SandboxLog.Ok($"Entorno: prefab '{config.EnvironmentPrefab.name}' instanciado.");
    }

    private static IEnumerator BuildFromAdditiveScene(SandboxConfig config)
    {
        string sceneName = config.AdditiveSceneName;

        if (string.IsNullOrEmpty(sceneName))
        {
            SandboxLog.Warn("Modo AdditiveScene sin nombre de escena. El sandbox se quedará sin suelo.");
            yield break;
        }

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

        if (operation == null)
        {
            SandboxLog.Error($"No se pudo cargar '{sceneName}'. Añádela a Build Settings (File > Build Profiles > Scene List).");
            yield break;
        }

        while (!operation.isDone)
            yield return null;

        Scene loaded = SceneManager.GetSceneByName(sceneName);
        if (!loaded.IsValid())
        {
            SandboxLog.Error($"La escena '{sceneName}' se cargó pero no es válida.");
            yield break;
        }

        HashSet<string> keep = new HashSet<string>(config.AdditiveKeepRoots ?? new string[0]);
        List<string> disabled = new List<string>();

        GameObject[] roots = loaded.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (keep.Contains(roots[i].name)) continue;

            disabled.Add(roots[i].name);
            Object.Destroy(roots[i]);
        }

        SandboxLog.Ok($"Entorno: escena '{sceneName}' cargada de forma aditiva. Conservados: [{string.Join(", ", keep)}]. Eliminados: [{string.Join(", ", disabled)}]");
    }

    public static int PurgeImportedManagers()
    {
        int destroyed = 0;

        destroyed += DestroyAll<PlayerController>();
        destroyed += DestroyAll<EnemySpawnManager>();
        destroyed += DestroyAll<PoolManager>();
        destroyed += DestroyAll<CurrencyManager>();
        destroyed += DestroyAll<ExperienceManager>();
        destroyed += DestroyAll<PlayerStatsManager>();
        destroyed += DestroyAll<PerformanceMonitor>();
        destroyed += DestroyAll<PickupAudioManager>();
        destroyed += DestroyAll<FloatingTextManager>();
        destroyed += DestroyAll<TutorialManager>();
        destroyed += DestroyAll<ShopManager>();
        destroyed += DestroyAll<CameraShakeManager>();

        return destroyed;
    }

    private static int DestroyAll<T>() where T : Component
    {
        T[] found = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < found.Length; i++)
        {
            if (found[i] != null)
                Object.Destroy(found[i].gameObject);
        }

        return found.Length;
    }
}
