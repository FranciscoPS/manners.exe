#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FlockingSandboxSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/FlockingSandbox.unity";

    [MenuItem("Tools/Manners/Create Flocking Sandbox Scene")]
    public static void CreateFlockingSandboxScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject systems = new GameObject("[Systems]");

        CreateLighting();
        GameObject ground = CreateGround();
        GameObject player = CreatePlayer();
        CreateCamera(player.transform);
        CreatePoolManager(systems.transform);
        CreateExperienceManager(systems.transform);
        CreateFlockingSystems(systems.transform, player.transform);
        CreateObstacleCourse();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();

        Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        Debug.Log($"Flocking sandbox scene created at {ScenePath}. Ground: {ground.name}");
    }

    public static void CreateSceneFromCommandLine()
    {
        CreateFlockingSandboxScene();
    }

    private static void CreateLighting()
    {
        GameObject lightObject = new GameObject("Directional Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        lightObject.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
    }

    private static GameObject CreateGround()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(12f, 1f, 12f);
        return ground;
    }

    private static GameObject CreatePlayer()
    {
        GameObject prefab = LoadAsset<GameObject>("Assets/Prefabs/Characters/Player.prefab");
        GameObject player = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        player.name = "Player";
        player.transform.position = Vector3.zero;
        player.transform.rotation = Quaternion.identity;
        return player;
    }

    private static void CreateCamera(Transform target)
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 55f;
        camera.nearClipPlane = 0.3f;
        camera.farClipPlane = 250f;

        CameraFollow follow = cameraObject.AddComponent<CameraFollow>();
        SerializedObject serialized = new SerializedObject(follow);
        serialized.FindProperty("target").objectReferenceValue = target;
        serialized.FindProperty("offset").vector3Value = new Vector3(0f, 24f, -11f);
        serialized.FindProperty("smoothSpeed").floatValue = 7f;
        serialized.FindProperty("lookDownAngle").floatValue = 64f;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        cameraObject.transform.position = target.position + new Vector3(0f, 24f, -11f);
        cameraObject.transform.rotation = Quaternion.Euler(64f, 0f, 0f);
    }

    private static void CreatePoolManager(Transform parent)
    {
        GameObject poolObject = new GameObject("PoolManager");
        poolObject.transform.SetParent(parent);
        PoolManager poolManager = poolObject.AddComponent<PoolManager>();

        SerializedObject serialized = new SerializedObject(poolManager);
        SerializedProperty configs = serialized.FindProperty("poolConfigs");
        configs.arraySize = 6;

        ConfigurePool(
            configs.GetArrayElementAtIndex(0),
            PoolManager.PoolType.Projectile,
            new[] { LoadAsset<GameObject>("Assets/Prefabs/Resources/Projectile.prefab") },
            32,
            96);

        ConfigurePool(
            configs.GetArrayElementAtIndex(1),
            PoolManager.PoolType.ExperienceOrb,
            new[] { LoadAsset<GameObject>("Assets/Prefabs/Resources/ExperienceOrb.prefab") },
            80,
            240);

        ConfigurePool(
            configs.GetArrayElementAtIndex(2),
            PoolManager.PoolType.Coin,
            new[] { LoadAsset<GameObject>("Assets/Prefabs/Resources/Coin.prefab") },
            32,
            120);

        ConfigurePool(
            configs.GetArrayElementAtIndex(3),
            PoolManager.PoolType.Diamond,
            new[] { LoadAsset<GameObject>("Assets/Prefabs/Resources/Diamond.prefab") },
            16,
            64);

        ConfigurePool(
            configs.GetArrayElementAtIndex(4),
            PoolManager.PoolType.BasicEnemy,
            new[]
            {
                LoadAsset<GameObject>("Assets/Prefabs/Characters/Basic Enemy.prefab"),
                LoadAsset<GameObject>("Assets/Prefabs/Characters/BEnemy2.prefab"),
                LoadAsset<GameObject>("Assets/Prefabs/Characters/BEnemy3.prefab")
            },
            80,
            180);

        ConfigurePool(
            configs.GetArrayElementAtIndex(5),
            PoolManager.PoolType.FastEnemy,
            new[]
            {
                LoadAsset<GameObject>("Assets/Prefabs/Characters/Fast Enemy.prefab"),
                LoadAsset<GameObject>("Assets/Prefabs/Characters/FEnemy2.prefab"),
                LoadAsset<GameObject>("Assets/Prefabs/Characters/FEnemy3.prefab")
            },
            60,
            140);

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(poolManager);
    }

    private static void CreateExperienceManager(Transform parent)
    {
        GameObject experienceObject = new GameObject("ExperienceManager");
        experienceObject.transform.SetParent(parent);
        experienceObject.AddComponent<ExperienceManager>();
    }

    private static void ConfigurePool(SerializedProperty element, PoolManager.PoolType type, GameObject[] prefabs, int defaultCapacity, int maxSize)
    {
        element.FindPropertyRelative("poolType").enumValueIndex = (int)type;

        SerializedProperty prefabList = element.FindPropertyRelative("prefabs");
        prefabList.arraySize = prefabs.Length;
        for (int i = 0; i < prefabs.Length; i++)
        {
            prefabList.GetArrayElementAtIndex(i).objectReferenceValue = prefabs[i];
        }

        element.FindPropertyRelative("prefab").objectReferenceValue = prefabs.Length > 0 ? prefabs[0] : null;
        element.FindPropertyRelative("defaultCapacity").intValue = defaultCapacity;
        element.FindPropertyRelative("maxSize").intValue = maxSize;
    }

    private static void CreateFlockingSystems(Transform parent, Transform player)
    {
        GameObject flockObject = new GameObject("EnemyFlockManager");
        flockObject.transform.SetParent(parent);
        EnemyFlockManager flockManager = flockObject.AddComponent<EnemyFlockManager>();

        SerializedObject flockSerialized = new SerializedObject(flockManager);
        flockSerialized.FindProperty("target").objectReferenceValue = player;
        flockSerialized.FindProperty("neighborRadius").floatValue = 2.6f;
        flockSerialized.FindProperty("separationRadius").floatValue = 1.25f;
        flockSerialized.FindProperty("maxNeighbors").intValue = 14;
        flockSerialized.FindProperty("seekWeight").floatValue = 1.25f;
        flockSerialized.FindProperty("separationWeight").floatValue = 2.15f;
        flockSerialized.FindProperty("alignmentWeight").floatValue = 0f;
        flockSerialized.FindProperty("cohesionWeight").floatValue = 0f;
        flockSerialized.FindProperty("crowdRadius").floatValue = 4.2f;
        flockSerialized.FindProperty("crowdSoftLimit").intValue = 5;
        flockSerialized.FindProperty("crowdPressureWeight").floatValue = 0.95f;
        flockSerialized.FindProperty("engagementRadius").floatValue = 1.85f;
        flockSerialized.FindProperty("engagementSpreadDistance").floatValue = 6f;
        flockSerialized.FindProperty("engagementSpreadWeight").floatValue = 0.55f;
        flockSerialized.FindProperty("closeSeekScale").floatValue = 0.4f;
        flockSerialized.FindProperty("obstacleLookAhead").floatValue = 1.8f;
        flockSerialized.FindProperty("obstaclePadding").floatValue = 0.45f;
        flockSerialized.FindProperty("obstacleWeight").floatValue = 1.85f;
        flockSerialized.FindProperty("tangentWeight").floatValue = 0.2f;
        flockSerialized.FindProperty("obstacleSideMemory").floatValue = 0.7f;
        flockSerialized.FindProperty("enableEnemyBuildingPhysicsCollision").boolValue = true;
        flockSerialized.FindProperty("enableEnemyEnemyPhysicsCollision").boolValue = false;
        flockSerialized.ApplyModifiedPropertiesWithoutUndo();

        GameObject spawnerObject = new GameObject("FlockingEnemySpawner");
        spawnerObject.transform.SetParent(parent);
        FlockingEnemySpawner spawner = spawnerObject.AddComponent<FlockingEnemySpawner>();

        SerializedObject spawnerSerialized = new SerializedObject(spawner);
        spawnerSerialized.FindProperty("player").objectReferenceValue = player;

        SerializedProperty configs = spawnerSerialized.FindProperty("enemyConfigurations");
        configs.arraySize = 2;
        configs.GetArrayElementAtIndex(0).objectReferenceValue =
            LoadAsset<EnemyConfiguration>("Assets/Configurations/Enemies Configurations/BasicEnemy.asset");
        configs.GetArrayElementAtIndex(1).objectReferenceValue =
            LoadAsset<EnemyConfiguration>("Assets/Configurations/Enemies Configurations/FastEnemy.asset");

        spawnerSerialized.FindProperty("spawnInterval").floatValue = 1f;
        spawnerSerialized.FindProperty("enemiesPerBurst").intValue = 1;
        spawnerSerialized.FindProperty("maxActiveEnemies").intValue = 10;
        spawnerSerialized.FindProperty("minSpawnRadius").floatValue = 14f;
        spawnerSerialized.FindProperty("maxSpawnRadius").floatValue = 18f;
        spawnerSerialized.FindProperty("sampleNavMesh").boolValue = false;
        spawnerSerialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateObstacleCourse()
    {
        GameObject root = new GameObject("Flocking Obstacles");
        GameObject[] prefabs =
        {
            LoadAsset<GameObject>("Assets/Prefabs/Buildings/Bulding4.prefab"),
            LoadAsset<GameObject>("Assets/Prefabs/Buildings/Building.prefab"),
            LoadAsset<GameObject>("Assets/Prefabs/Buildings/Building2.prefab"),
            LoadAsset<GameObject>("Assets/Prefabs/Buildings/Building3.prefab")
        };

        int index = 0;
        for (int x = -28; x <= 28; x += 8)
        {
            if (Mathf.Abs(x) < 4)
            {
                continue;
            }

            PlaceBuilding(prefabs[index++ % prefabs.Length], root.transform, new Vector3(x, 0f, 9f), Quaternion.Euler(0f, 18f * index, 0f));
            PlaceBuilding(prefabs[index++ % prefabs.Length], root.transform, new Vector3(x, 0f, -9f), Quaternion.Euler(0f, -22f * index, 0f));
        }

        for (int z = -24; z <= 24; z += 12)
        {
            if (Mathf.Abs(z) < 6)
            {
                continue;
            }

            PlaceBuilding(prefabs[index++ % prefabs.Length], root.transform, new Vector3(-14f, 0f, z), Quaternion.Euler(0f, 90f, 0f));
            PlaceBuilding(prefabs[index++ % prefabs.Length], root.transform, new Vector3(14f, 0f, z), Quaternion.Euler(0f, -90f, 0f));
        }
    }

    private static void PlaceBuilding(GameObject prefab, Transform parent, Vector3 position, Quaternion rotation)
    {
        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        instance.transform.SetParent(parent);
        instance.transform.position = position;
        instance.transform.rotation = rotation;
        instance.transform.localScale = Vector3.one * 2f;

        if (!instance.TryGetComponent(out FlockingObstacle obstacle))
        {
            obstacle = instance.AddComponent<FlockingObstacle>();
        }

        obstacle.RefreshColliders();
    }

    private static T LoadAsset<T>(string path) where T : Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            throw new System.IO.FileNotFoundException($"Missing asset at {path}", path);
        }

        return asset;
    }
}
#endif
