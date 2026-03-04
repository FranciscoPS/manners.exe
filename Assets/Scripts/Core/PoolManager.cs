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
        Enemy,
        BasicEnemy,
        FastEnemy,
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
    private Dictionary<PoolType, GameObject> poolPrefabs = new Dictionary<PoolType, GameObject>();
    private Dictionary<PoolType, Quaternion> poolPrefabRotations = new Dictionary<PoolType, Quaternion>();
    private Dictionary<GameObject, PoolType> activeObjects = new Dictionary<GameObject, PoolType>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            InitializePools();
        }
        else
        {
            Destroy(gameObject);
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

        ResetAllPools();
    }

    private void InitializePools()
    {
        foreach (var config in poolConfigs)
        {
            if (config.prefab == null)
            {
                continue;
            }

            poolPrefabs[config.poolType] = config.prefab;
            poolPrefabRotations[config.poolType] = config.prefab.transform.rotation;

            var pool = new ObjectPool<GameObject>(
                () => CreatePooledObject(config.poolType, config.prefab),
                OnGetFromPool,
                OnReturnToPool,
                OnDestroyPoolObject,
                true,
                config.defaultCapacity,
                config.maxSize
            );

            pools[config.poolType] = pool;
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

        activeObjects.Clear();

        foreach (var poolType in pools.Keys)
        {
            pools[poolType].Clear();
        }

        pools.Clear();
        InitializePools();
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
}
