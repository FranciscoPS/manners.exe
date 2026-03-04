using UnityEngine;

public class SpawnFactory : MonoBehaviour, ISpawnFactory
{
    private static SpawnFactory instance;
    public static SpawnFactory Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("[SpawnFactory]");
                instance = go.AddComponent<SpawnFactory>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    private PoolManager poolManager;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        poolManager = PoolManager.Instance;
    }

    public GameObject CreateEnemy(Vector3 position, EnemyConfiguration config)
    {
        if (config == null)
        {
            return null;
        }

        if (poolManager == null)
        {
            return null;
        }

        return poolManager.SpawnEnemy(position, config);
    }

    public Projectile CreateProjectile(Vector3 position, Quaternion rotation, ProjectileConfiguration config)
    {
        if (config == null)
        {
            return null;
        }

        if (poolManager == null)
        {
            return null;
        }

        return poolManager.SpawnProjectile(position, rotation, config);
    }

    public ExperienceOrb CreateExperienceOrb(Vector3 position, OrbConfiguration config)
    {
        if (config == null)
        {
            return null;
        }

        if (poolManager == null)
        {
            return null;
        }

        return poolManager.SpawnOrb(position, config);
    }

    public Collectible CreateCollectible(Vector3 position, Collectible.CollectibleType type, int value)
    {
        if (poolManager == null)
        {
            return null;
        }

        return poolManager.SpawnCollectible(position, type, value);
    }

    public void DestroyObject(GameObject obj)
    {
        if (obj == null) return;

        if (poolManager == null)
        {
            Destroy(obj);
            return;
        }

        poolManager.Despawn(obj);
    }

    public void PrewarmPools(int enemyCount, int projectileCount, int orbCount, int collectibleCount)
    {
        if (poolManager == null)
        {
            return;
        }

        if (enemyCount > 0)
        {
            poolManager.PrewarmPool(PoolManager.PoolType.BasicEnemy, enemyCount / 2);
            poolManager.PrewarmPool(PoolManager.PoolType.FastEnemy, enemyCount / 2);
        }

        if (projectileCount > 0)
        {
            poolManager.PrewarmPool(PoolManager.PoolType.Projectile, projectileCount);
        }

        if (orbCount > 0)
        {
            poolManager.PrewarmPool(PoolManager.PoolType.ExperienceOrb, orbCount);
        }

        if (collectibleCount > 0)
        {
            poolManager.PrewarmPool(PoolManager.PoolType.Coin, collectibleCount / 2);
            poolManager.PrewarmPool(PoolManager.PoolType.Diamond, collectibleCount / 2);
        }
    }
}
