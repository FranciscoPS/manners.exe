using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System;

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; private set; }

    public enum PoolType
    {
        Projectile,
        ExperienceOrb,
        Enemy,          // Pool genérico (deprecated)
        BasicEnemy,     // Enemy básico
        FastEnemy,      // Enemy rápido
        L2BasicEnemy,
        L2FastEnemy,
        Coin,
        Diamond
    }

    [System.Serializable]
    public class PoolConfig
    {
        public PoolType poolType;

        [Header("Prefabs")]
        [Tooltip("Si quieres variantes (por ejemplo 3 variantes para enemigos) añádelas aquí.")]
        public List<GameObject> prefabs = new List<GameObject>();

        [Tooltip("Campo legacy: si usas este campo se convertirá en 1 elemento de 'prefabs' automáticamente.")]
        public GameObject prefab;

        [Header("Capacity")]
        public int defaultCapacity = 20;
        public int maxSize = 100;
    }

    [Header("Pool Configurations")]
    [SerializeField] private List<PoolConfig> poolConfigs = new List<PoolConfig>();

    private Dictionary<PoolType, ObjectPool<GameObject>> pools = new Dictionary<PoolType, ObjectPool<GameObject>>();

    // Ahora almacenamos arrays de prefabs y rotaciones por PoolType
    private Dictionary<PoolType, GameObject[]> poolPrefabs = new Dictionary<PoolType, GameObject[]>();
    private Dictionary<PoolType, Quaternion[]> poolPrefabRotations = new Dictionary<PoolType, Quaternion[]>();
    private Dictionary<PoolType, PoolConfig> poolConfigMap = new Dictionary<PoolType, PoolConfig>();

    private Dictionary<GameObject, PoolType> activeObjects = new Dictionary<GameObject, PoolType>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            // Construye la configuración desde el inspector (migra campo legacy prefab -> prefabs)
            BuildConfigMapFromInspector(this.poolConfigs, replace: true);

            InitializePools();
        }
        else
        {
            // Si ya existe singleton, intentamos aplicar la configuración de la escena actual
            // al singleton (override) para que cada escena pueda definir sus variantes.
            // Si prefieres que el primer PoolManager (persistente) sea la única fuente,
            // cambia `replace: true` a `replace: false` en la siguiente línea.
            if (Instance != this)
            {
                Instance.BuildConfigMapFromInspector(this.poolConfigs, replace: true);
                Instance.ResetAllPools();
                Destroy(gameObject);
            }
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Al cargar una escena, reconstruimos pools según la configuración actual
        // (que viene del singleton poolConfigMap / poolPrefabs).
        ResetAllPools();
    }

    private void InitializePools()
    {
        // Crear pools sólo para las entradas que todavía no existan.
        foreach (var kvp in poolConfigMap)
        {
            PoolType poolType = kvp.Key;
            PoolConfig config = kvp.Value;

            if (pools.ContainsKey(poolType))
                continue;

            GameObject[] prefabsForType = poolPrefabs.ContainsKey(poolType) ? poolPrefabs[poolType] : null;
            if ((config == null || (config.prefab == null && (config.prefabs == null || config.prefabs.Count == 0))) 
                && (prefabsForType == null || prefabsForType.Length == 0))
            {
                Debug.LogWarning($"[PoolManager] PoolConfig for {poolType} has no prefab(s) assigned!");
                continue;
            }

            int defaultCapacity = (config != null) ? Mathf.Max(1, config.defaultCapacity) : 20;
            int maxSize = (config != null) ? Mathf.Max(1, config.maxSize) : 100;

            var pool = new ObjectPool<GameObject>(
                () => CreatePooledObject(poolType, prefabsForType),
                OnGetFromPool,
                OnReturnToPool,
                OnDestroyPoolObject,
                true,
                defaultCapacity,
                maxSize
            );

            pools[poolType] = pool;
        }
    }

    private GameObject CreatePooledObject(PoolType poolType, GameObject[] prefabsForType)
    {
        GameObject prefab = GetRandomPrefabForType(poolType, prefabsForType);
        if (prefab == null)
        {
            Debug.LogError($"[PoolManager] No prefab available when creating pooled object for {poolType}");
            GameObject fallback = new GameObject($"MissingPrefab_{poolType}");
            fallback.SetActive(false);
            return fallback;
        }

        GameObject obj = Instantiate(prefab);
        obj.name = $"{prefab.name}_{poolType}";
        obj.SetActive(false);
        return obj;
    }

    private void OnGetFromPool(GameObject obj)
    {
        // Hook para cuando un objeto se obtiene del pool (vacío por ahora)
        if (obj != null)
        {
        }
    }

    private void OnReturnToPool(GameObject obj)
    {
        if (obj != null)
        {
            IPoolable poolable = obj.GetComponent<IPoolable>();
            if (poolable != null)
            {
                poolable.OnDespawn();
            }
            obj.SetActive(false);
        }
    }

    private void OnDestroyPoolObject(GameObject obj)
    {
        Destroy(obj);
    }

    private GameObject GetFromPool(PoolType poolType, Vector3 position, Quaternion rotation)
    {
        if (!pools.ContainsKey(poolType))
            return null;

        GameObject obj = pools[poolType].Get();

        if (obj == null)
            return null;

        obj.transform.position = position;
        obj.transform.rotation = rotation;

        return obj;
    }

    public GameObject Spawn(PoolType poolType, Vector3 position, Quaternion rotation)
    {
        GameObject obj = GetFromPool(poolType, position, rotation);
        if (obj == null) return null;

        IPoolable poolable = obj.GetComponent<IPoolable>();
        if (poolable != null)
        {
            poolable.OnSpawn();
        }

        obj.SetActive(true);

        activeObjects[obj] = poolType;

        return obj;
    }

    public T Spawn<T>(PoolType poolType, Vector3 position, Quaternion rotation, Action<T> onConfigure = null) where T : Component
    {
        GameObject obj = Spawn(poolType, position, rotation);
        if (obj == null) return null;

        T component = obj.GetComponent<T>();
        if (component != null && onConfigure != null)
        {
            onConfigure(component);
        }

        return component;
    }

    public Projectile SpawnProjectile(Vector3 position, Quaternion rotation, ProjectileConfiguration config = null)
    {
        GameObject obj = Spawn(PoolType.Projectile, position, rotation);
        if (obj == null) return null;

        Projectile projectile = obj.GetComponent<Projectile>();
        if (projectile != null && config != null)
        {
            config.ApplyToProjectile(projectile);
        }

        return projectile;
    }

    public ExperienceOrb SpawnOrb(Vector3 position, OrbConfiguration config = null)
    {
        GameObject obj = GetFromPool(PoolType.ExperienceOrb, position, Quaternion.identity);
        if (obj == null) return null;

        ExperienceOrb orb = obj.GetComponent<ExperienceOrb>();
        if (orb != null)
        {
            if (config != null)
            {
                config.ApplyToOrb(orb);
            }

            IPoolable poolable = orb as IPoolable;
            if (poolable != null)
            {
                poolable.OnSpawn();
            }
        }

        obj.SetActive(true);
        activeObjects[obj] = PoolType.ExperienceOrb;

        return orb;
    }

    public GameObject SpawnEnemy(Vector3 position, EnemyConfiguration config = null)
    {
        PoolType poolType = config != null && config.enemyPoolType != PoolType.Enemy
            ? config.enemyPoolType
            : PoolType.Enemy;

        // Usamos la API existente: el pool ya contiene instancias creadas con variantes aleatorias.
        GameObject obj = GetFromPool(poolType, position, Quaternion.identity);
        if (obj != null)
        {
            if (config != null)
            {
                config.ApplyToEnemy(obj);
            }

            IPoolable poolable = obj.GetComponent<IPoolable>();
            if (poolable != null)
            {
                poolable.OnSpawn();
            }

            obj.SetActive(true);
            activeObjects[obj] = poolType;
        }

        return obj;
    }

    public Collectible SpawnCollectible(Vector3 position, Collectible.CollectibleType type, int value)
    {
        PoolType poolType = type == Collectible.CollectibleType.Coin ? PoolType.Coin : PoolType.Diamond;
        Quaternion rotation = GetDefaultRotationForPool(poolType);
        GameObject obj = GetFromPool(poolType, position, rotation);

        if (obj != null)
        {
            Collectible collectible = obj.GetComponent<Collectible>();
            if (collectible != null)
            {
                collectible.SetType(type);
                collectible.SetValue(value);

                IPoolable poolable = collectible as IPoolable;
                if (poolable != null)
                {
                    poolable.OnSpawn();
                }
            }

            obj.SetActive(true);
            activeObjects[obj] = poolType;

            return collectible;
        }

        return null;
    }

    public void Despawn(GameObject obj)
    {
        if (obj == null) return;

        if (!activeObjects.ContainsKey(obj))
        {
            obj.SetActive(false);
            return;
        }

        IPoolable poolable = obj.GetComponent<IPoolable>();
        if (poolable != null)
        {
            poolable.OnDespawn();
        }

        PoolType poolType = activeObjects[obj];
        activeObjects.Remove(obj);

        if (pools.ContainsKey(poolType))
        {
            pools[poolType].Release(obj);
        }
    }

    public void PrewarmPool(PoolType poolType, int count)
    {
        if (!pools.ContainsKey(poolType))
            return;

        List<GameObject> temp = new List<GameObject>();
        for (int i = 0; i < count; i++)
        {
            temp.Add(pools[poolType].Get());
        }

        foreach (var obj in temp)
        {
            pools[poolType].Release(obj);
        }
    }

    public void ClearPool(PoolType poolType)
    {
        if (!pools.ContainsKey(poolType)) return;

        pools[poolType].Clear();
    }

    public void ClearAllPools()
    {
        activeObjects.Clear();

        foreach (var pool in pools.Values)
        {
            pool.Clear();
        }
    }

    private void ResetAllPools()
    {
        // Limpiar referencias de objetos activos
        activeObjects.Clear();

        // Limpiar y recrear pools usando la configuración persistente en poolConfigMap y poolPrefabs
        foreach (var pool in pools.Values)
        {
            pool.Clear();
        }

        pools.Clear();

        // Recrear pools basados en la configuración que guardamos en poolConfigMap
        foreach (var kvp in poolConfigMap)
        {
            PoolType poolType = kvp.Key;
            PoolConfig config = kvp.Value;

            GameObject[] prefabsForType = poolPrefabs.ContainsKey(poolType) ? poolPrefabs[poolType] : null;
            if (prefabsForType == null || prefabsForType.Length == 0)
                continue;

            int defaultCapacity = (config != null) ? Mathf.Max(1, config.defaultCapacity) : 20;
            int maxSize = (config != null) ? Mathf.Max(1, config.maxSize) : 100;

            var pool = new ObjectPool<GameObject>(
                () => CreatePooledObject(poolType, prefabsForType),
                OnGetFromPool,
                OnReturnToPool,
                OnDestroyPoolObject,
                true,
                defaultCapacity,
                maxSize
            );

            pools[poolType] = pool;
        }
    }

    public void CleanupDestroyedObjects()
    {
        List<GameObject> toRemove = new List<GameObject>();

        foreach (var kvp in activeObjects)
        {
            if (kvp.Key == null)
            {
                toRemove.Add(kvp.Key);
            }
        }

        foreach (var obj in toRemove)
        {
            activeObjects.Remove(obj);
        }
    }

    // Construye el mapa de configuración desde la lista serializada en el inspector
    // Si replace == true, REEMPLAZA la configuración existente del singleton.
    private void BuildConfigMapFromInspector(List<PoolConfig> configs, bool replace = false)
    {
        if (configs == null) return;

        if (replace)
        {
            poolConfigMap.Clear();
            poolPrefabs.Clear();
            poolPrefabRotations.Clear();
        }

        foreach (var config in configs)
        {
            if (config == null) continue;

            poolConfigMap[config.poolType] = config;

            // Construir array de prefabs: preferimos la lista `prefabs` si tiene elementos,
            // si no usamos el campo legacy `prefab`.
            List<GameObject> finalList = new List<GameObject>();
            if (config.prefabs != null && config.prefabs.Count > 0)
            {
                for (int i = 0; i < config.prefabs.Count; i++)
                {
                    if (config.prefabs[i] != null)
                        finalList.Add(config.prefabs[i]);
                }
            }
            else if (config.prefab != null)
            {
                finalList.Add(config.prefab);
            }

            if (finalList.Count > 0)
            {
                poolPrefabs[config.poolType] = finalList.ToArray();

                Quaternion[] rotations = new Quaternion[finalList.Count];
                for (int i = 0; i < finalList.Count; i++)
                    rotations[i] = finalList[i] != null ? finalList[i].transform.rotation : Quaternion.identity;
                poolPrefabRotations[config.poolType] = rotations;
            }
        }
    }

    // Obtiene un prefab aleatorio (uniforme) para el poolType.
    private GameObject GetRandomPrefabForType(PoolType poolType, GameObject[] fallback = null)
    {
        GameObject[] arr = null;
        if (poolPrefabs.ContainsKey(poolType))
            arr = poolPrefabs[poolType];
        else
            arr = fallback;

        if (arr == null || arr.Length == 0) return null;
        int idx = UnityEngine.Random.Range(0, arr.Length);
        return arr[idx];
    }

    // Obtiene rotación por defecto (primera) para un poolType si existe
    private Quaternion GetDefaultRotationForPool(PoolType poolType)
    {
        if (poolPrefabRotations.ContainsKey(poolType) && poolPrefabRotations[poolType].Length > 0)
            return poolPrefabRotations[poolType][0];
        return Quaternion.identity;
    }
}
