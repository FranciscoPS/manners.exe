using UnityEngine;
using UnityEngine.Pool;
using System.Collections.Generic;
using System;

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; private set; }

    public enum PoolType
    {
        Projectile,
        ExperienceOrb,
        Enemy
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
    private Dictionary<GameObject, PoolType> activeObjects = new Dictionary<GameObject, PoolType>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePools();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializePools()
    {
        foreach (var config in poolConfigs)
        {
            if (config.prefab == null)
            {
                Debug.LogWarning($"Pool '{config.poolType}' has no prefab assigned!");
                continue;
            }

            poolPrefabs[config.poolType] = config.prefab;

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
            obj.SetActive(true);
        }
    }

    private void OnReturnToPool(GameObject obj)
    {
        if (obj != null)
        {
            obj.SetActive(false);
        }
    }

    private void OnDestroyPoolObject(GameObject obj)
    {
        Destroy(obj);
    }

    public GameObject Spawn(PoolType poolType, Vector3 position, Quaternion rotation)
    {
        if (!pools.ContainsKey(poolType))
        {
            Debug.LogError($"Pool '{poolType}' does not exist!");
            return null;
        }

        GameObject obj = pools[poolType].Get();
        
        if (obj == null)
        {
            Debug.LogError($"Pool '{poolType}' returned null object!");
            return null;
        }
        
        obj.transform.position = position;
        obj.transform.rotation = rotation;

        activeObjects[obj] = poolType;

        IPoolable poolable = obj.GetComponent<IPoolable>();
        if (poolable != null)
        {
            poolable.OnSpawn();
        }

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
        GameObject obj = Spawn(PoolType.ExperienceOrb, position, Quaternion.identity);
        if (obj == null) return null;

        ExperienceOrb orb = obj.GetComponent<ExperienceOrb>();
        if (orb != null && config != null)
        {
            config.ApplyToOrb(orb);
        }

        return orb;
    }

    public GameObject SpawnEnemy(Vector3 position, EnemyConfiguration config = null)
    {
        GameObject obj = Spawn(PoolType.Enemy, position, Quaternion.identity);
        if (obj != null && config != null)
        {
            config.ApplyToEnemy(obj);
        }

        return obj;
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
        {
            Debug.LogError($"Pool '{poolType}' does not exist!");
            return;
        }

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
