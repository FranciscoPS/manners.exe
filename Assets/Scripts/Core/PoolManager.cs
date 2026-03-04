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
        public GameObject prefab;
        public int defaultCapacity = 20;
        public int maxSize = 100;
    }

    [Header("Pool Configurations")]
    [SerializeField] private List<PoolConfig> poolConfigs = new List<PoolConfig>();

    private Dictionary<PoolType, ObjectPool<GameObject>> pools = new Dictionary<PoolType, ObjectPool<GameObject>>();

    // Persistimos las referencias a prefabs y rotaciones independientemente de recargas de escena
    // para que la configuración asignada en el inspector se mantenga siempre en el singleton.
    private Dictionary<PoolType, GameObject> poolPrefabs = new Dictionary<PoolType, GameObject>();
    private Dictionary<PoolType, Quaternion> poolPrefabRotations = new Dictionary<PoolType, Quaternion>();
    private Dictionary<PoolType, PoolConfig> poolConfigMap = new Dictionary<PoolType, PoolConfig>();

    private Dictionary<GameObject, PoolType> activeObjects = new Dictionary<GameObject, PoolType>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            // Construir mapa de configuración desde el inspector y asegurarnos de
            // almacenar los prefabs para uso persistente.
            BuildConfigMapFromInspector(this.poolConfigs, replace: true);

            InitializePools();
        }
        else
        {
            // Si ya existe un singleton, aplicar la configuración de la instancia
            // que acaba de crearse (por ejemplo, escena recién cargada) PARA QUE
            // el PoolManager refleje siempre la configuración del inspector de la
            // escena actual. Esto reemplaza la configuración persistente y recrea pools.
            if (Instance != this)
            {
                // Reemplazar configuración del singleton con la de la nueva instancia
                Instance.BuildConfigMapFromInspector(this.poolConfigs, replace: true);

                // Forzar recreación de pools basados en la nueva configuración
                Instance.ResetAllPools();

                // Destruir la instancia duplicada en la escena actual (el singleton
                // ya contiene la configuración).
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
        // Cuando se carga una nueva escena, queremos que el singleton utilice la
        // configuración que esté definida en la escena si existe un PoolManager en ella.
        // Para eso, si hay un PoolManager en la escena (no-singleson), su Awake habrá
        // intentado reemplazar la configuración del singleton ya. Aquí sólo limpiamos pools.
        ResetAllPools();
    }

    private void InitializePools()
    {
        // Crear pools sólo para las entradas que todavía no existan.
        foreach (var kvp in poolConfigMap)
        {
            PoolType poolType = kvp.Key;
            PoolConfig config = kvp.Value;

            // Si ya tenemos un pool creado para este tipo no lo recreamos
            if (pools.ContainsKey(poolType))
                continue;

            if (config == null || config.prefab == null)
            {
                // Intentar usar prefab almacenado previamente si existe
                if (!poolPrefabs.ContainsKey(poolType) || poolPrefabs[poolType] == null)
                {
                    Debug.LogWarning($"[PoolManager] PoolConfig for {poolType} has no prefab assigned!");
                    continue;
                }
            }

            GameObject prefab = (config != null && config.prefab != null) ? config.prefab : poolPrefabs[poolType];
            int defaultCapacity = (config != null) ? config.defaultCapacity : 20;
            int maxSize = (config != null) ? config.maxSize : 100;

            // Guardar prefab/rotation si no están guardados
            if (!poolPrefabs.ContainsKey(poolType) || poolPrefabs[poolType] == null)
            {
                poolPrefabs[poolType] = prefab;
                poolPrefabRotations[poolType] = prefab != null ? prefab.transform.rotation : Quaternion.identity;
            }

            var pool = new ObjectPool<GameObject>(
                () => CreatePooledObject(poolType, prefab),
                OnGetFromPool,
                OnReturnToPool,
                OnDestroyPoolObject,
                true,
                Mathf.Max(1, defaultCapacity),
                Mathf.Max(1, maxSize)
            );

            pools[poolType] = pool;
        }
    }

    private GameObject CreatePooledObject(PoolType poolType, GameObject prefab)
    {
        GameObject obj = Instantiate(prefab);
        obj.name = $"{prefab.name}_{poolType}";
        obj.SetActive(false);
        return obj;
    }

    private void OnGetFromPool(GameObject obj)
    {
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
        else if (config == null)
        {
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
        Quaternion rotation = poolPrefabRotations.ContainsKey(poolType) ? poolPrefabRotations[poolType] : Quaternion.identity;
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

            GameObject prefab = (config != null && config.prefab != null) ? config.prefab :
                                (poolPrefabs.ContainsKey(poolType) ? poolPrefabs[poolType] : null);

            if (prefab == null)
                continue;

            int defaultCapacity = (config != null) ? config.defaultCapacity : 20;
            int maxSize = (config != null) ? config.maxSize : 100;

            // Ensure we have rotation cached
            if (!poolPrefabRotations.ContainsKey(poolType))
                poolPrefabRotations[poolType] = prefab.transform.rotation;

            var pool = new ObjectPool<GameObject>(
                () => CreatePooledObject(poolType, prefab),
                OnGetFromPool,
                OnReturnToPool,
                OnDestroyPoolObject,
                true,
                Mathf.Max(1, defaultCapacity),
                Mathf.Max(1, maxSize)
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
        }

        foreach (var config in configs)
        {
            if (config == null) continue;

            // Si replace==true siempre sobrescribimos; si false sólo añadimos faltantes
            poolConfigMap[config.poolType] = config;

            // Guardar prefab/rotation sólo si no existe aún o si replace==true
            if (config.prefab != null)
            {
                if (replace || !poolPrefabs.ContainsKey(config.poolType) || poolPrefabs[config.poolType] == null)
                {
                    poolPrefabs[config.poolType] = config.prefab;
                    poolPrefabRotations[config.poolType] = config.prefab.transform.rotation;
                }
            }
        }
    }
}
