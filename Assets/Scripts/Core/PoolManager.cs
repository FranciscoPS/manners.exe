using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
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

        [Header("Preload")]
        [Tooltip("Cantidad fija que se instancia al cargar. Estos objetos sólo se apagan/encienden; no se crean en mitad del juego. Sube este valor para los tipos que aparecen en masa (orbes, monedas, enemigos).")]
        public int prewarmCount = 20;

        [Tooltip("Si está marcado, el pool NO crecerá aunque se quede sin objetos libres (devolverá null). Déjalo desmarcado para tener una red de seguridad.")]
        public bool preventGrow = false;

        [Header("Legacy (sin uso en el nuevo motor; se mantiene por compatibilidad)")]
        [Tooltip("Si prewarmCount es 0, se usa este valor como cantidad de precarga.")]
        public int defaultCapacity = 20;
        public int maxSize = 100;
    }

    [Header("Pool Configurations")]
    [SerializeField] private List<PoolConfig> poolConfigs = new List<PoolConfig>();

    [Header("Preload Timing")]
    [Tooltip("Reparte la pre-instanciación en varios frames para evitar un tirón al cargar.")]
    [SerializeField] private bool prewarmSpreadOverFrames = true;

    [Tooltip("Objetos a instanciar por frame cuando se reparte la precarga.")]
    [SerializeField] private int prewarmObjectsPerFrame = 25;

    [Header("Debug")]
    [Tooltip("Muestra un aviso cuando un pool tiene que crecer en runtime (útil para ajustar prewarmCount).")]
    [SerializeField] private bool warnOnGrow = false;

    private class Pool
    {
        public PoolType type;
        public GameObject[] prefabs;
        public Quaternion[] rotations;
        public bool allowGrow;
        public int prewarmCount;
        public readonly Stack<GameObject> inactive = new Stack<GameObject>(64);
        public int totalCount;
    }

    private readonly Dictionary<PoolType, Pool> pools = new Dictionary<PoolType, Pool>();
    private readonly Dictionary<PoolType, PoolConfig> poolConfigMap = new Dictionary<PoolType, PoolConfig>();
    private readonly Dictionary<GameObject, PoolType> activeObjects = new Dictionary<GameObject, PoolType>();
    private Transform container;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            container = transform;

            BuildConfigMap(poolConfigs);
            InitializePools();
        }
        else if (Instance != this)
        {

            Instance.BuildConfigMap(poolConfigs);
            Instance.InitializePools();
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
        if (Instance != this) return;

        DespawnAll();
    }

    private void BuildConfigMap(List<PoolConfig> configs)
    {
        if (configs == null) return;

        foreach (var config in configs)
        {
            if (config == null) continue;
            poolConfigMap[config.poolType] = config;
        }
    }

    private void InitializePools()
    {
        List<Pool> newlyCreated = null;

        foreach (var kvp in poolConfigMap)
        {
            PoolType type = kvp.Key;
            PoolConfig config = kvp.Value;

            if (pools.ContainsKey(type)) continue;

            GameObject[] prefabs = BuildPrefabArray(config);
            if (prefabs.Length == 0)
            {
                Debug.LogWarning($"[PoolManager] PoolConfig for {type} has no prefab(s) assigned!");
                continue;
            }

            Quaternion[] rotations = new Quaternion[prefabs.Length];
            for (int i = 0; i < prefabs.Length; i++)
                rotations[i] = prefabs[i] != null ? prefabs[i].transform.rotation : Quaternion.identity;

            int prewarm = config.prewarmCount > 0 ? config.prewarmCount : Mathf.Max(0, config.defaultCapacity);

            Pool pool = new Pool
            {
                type = type,
                prefabs = prefabs,
                rotations = rotations,
                allowGrow = !config.preventGrow,
                prewarmCount = prewarm
            };

            pools[type] = pool;
            (newlyCreated ??= new List<Pool>()).Add(pool);
        }

        if (newlyCreated == null) return;

        if (prewarmSpreadOverFrames && Application.isPlaying)
            StartCoroutine(PrewarmRoutine(newlyCreated));
        else
            foreach (var pool in newlyCreated) PrewarmImmediate(pool, pool.prewarmCount);
    }

    private GameObject[] BuildPrefabArray(PoolConfig config)
    {
        List<GameObject> list = new List<GameObject>();

        if (config.prefabs != null)
        {
            for (int i = 0; i < config.prefabs.Count; i++)
                if (config.prefabs[i] != null) list.Add(config.prefabs[i]);
        }

        if (list.Count == 0 && config.prefab != null)
            list.Add(config.prefab);

        return list.ToArray();
    }

    private IEnumerator PrewarmRoutine(List<Pool> targets)
    {
        int budget = Mathf.Max(1, prewarmObjectsPerFrame);
        int createdThisFrame = 0;

        for (int p = 0; p < targets.Count; p++)
        {
            Pool pool = targets[p];
            while (pool.inactive.Count < pool.prewarmCount)
            {
                GameObject obj = CreateInstance(pool);
                if (obj == null) break;
                pool.inactive.Push(obj);

                if (++createdThisFrame >= budget)
                {
                    createdThisFrame = 0;
                    yield return null;
                }
            }
        }
    }

    private void PrewarmImmediate(Pool pool, int count)
    {
        while (pool.inactive.Count < count)
        {
            GameObject obj = CreateInstance(pool);
            if (obj == null) break;
            pool.inactive.Push(obj);
        }
    }

    private GameObject CreateInstance(Pool pool)
    {
        GameObject prefab = GetRandomPrefab(pool);
        if (prefab == null)
        {
            Debug.LogError($"[PoolManager] No prefab available when creating pooled object for {pool.type}");
            return null;
        }

        GameObject obj = Instantiate(prefab);
        obj.name = $"{prefab.name}_{pool.type}";
        obj.transform.SetParent(container, false);
        obj.SetActive(false);
        pool.totalCount++;
        return obj;
    }

    private GameObject GetRandomPrefab(Pool pool)
    {
        if (pool.prefabs == null || pool.prefabs.Length == 0) return null;
        if (pool.prefabs.Length == 1) return pool.prefabs[0];
        return pool.prefabs[UnityEngine.Random.Range(0, pool.prefabs.Length)];
    }

    private GameObject GetFromPool(PoolType poolType, Vector3 position, Quaternion rotation)
    {
        if (!pools.TryGetValue(poolType, out Pool pool))
            return null;

        GameObject obj = null;

        while (pool.inactive.Count > 0 && obj == null)
            obj = pool.inactive.Pop();

        if (obj == null)
        {
            if (!pool.allowGrow)
                return null;

            if (warnOnGrow)
                Debug.LogWarning($"[PoolManager] Pool '{poolType}' agotado (instancias={pool.totalCount}). Creciendo en runtime; considera subir prewarmCount.");

            obj = CreateInstance(pool);
            if (obj == null) return null;
        }

        obj.transform.SetPositionAndRotation(position, rotation);
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

        if (!activeObjects.TryGetValue(obj, out PoolType poolType))
        {

            obj.SetActive(false);
            return;
        }

        IPoolable poolable = obj.GetComponent<IPoolable>();
        if (poolable != null)
        {
            poolable.OnDespawn();
        }

        activeObjects.Remove(obj);
        obj.SetActive(false);

        if (pools.TryGetValue(poolType, out Pool pool))
        {
            pool.inactive.Push(obj);
        }
    }

    public void PrewarmPool(PoolType poolType, int count)
    {
        if (!pools.TryGetValue(poolType, out Pool pool))
            return;

        PrewarmImmediate(pool, count);
    }

    public void ClearPool(PoolType poolType)
    {
        if (!pools.TryGetValue(poolType, out Pool pool)) return;

        while (pool.inactive.Count > 0)
        {
            GameObject obj = pool.inactive.Pop();
            if (obj != null) Destroy(obj);
        }
        pool.totalCount = 0;
    }

    public void ClearAllPools()
    {
        activeObjects.Clear();

        foreach (var pool in pools.Values)
        {
            while (pool.inactive.Count > 0)
            {
                GameObject obj = pool.inactive.Pop();
                if (obj != null) Destroy(obj);
            }
            pool.totalCount = 0;
        }
    }

    public void DespawnAll()
    {
        if (activeObjects.Count == 0) return;

        var snapshot = new List<GameObject>(activeObjects.Keys);
        for (int i = 0; i < snapshot.Count; i++)
            Despawn(snapshot[i]);
    }

    public void CleanupDestroyedObjects()
    {
        List<GameObject> toRemove = null;

        foreach (var kvp in activeObjects)
        {
            if (kvp.Key == null)
                (toRemove ??= new List<GameObject>()).Add(kvp.Key);
        }

        if (toRemove != null)
        {
            for (int i = 0; i < toRemove.Count; i++)
                activeObjects.Remove(toRemove[i]);
        }
    }

    private Quaternion GetDefaultRotationForPool(PoolType poolType)
    {
        if (pools.TryGetValue(poolType, out Pool pool) && pool.rotations != null && pool.rotations.Length > 0)
            return pool.rotations[0];
        return Quaternion.identity;
    }
}
