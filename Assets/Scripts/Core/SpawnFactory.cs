using UnityEngine;

/// <summary>
/// Concrete Factory Pattern - Implementación que usa Object Pooling
/// Basado en: https://refactoring.guru/es/design-patterns/abstract-factory
/// 
/// Esta implementación combina Abstract Factory con Object Pool Pattern para:
/// - Reutilizar objetos en vez de Instantiate/Destroy constante (performance)
/// - Proveer una API limpia y semántica para crear objetos
/// - Centralizar toda la lógica de spawning en un solo lugar
/// 
/// Ventajas:
/// - El código cliente no necesita conocer PoolManager
/// - Fácil de testear con mocks/stubs
/// - Fácil cambiar implementación (ej: sin pooling para debug)
/// - Reduce coupling entre sistemas
/// </summary>
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
        
        // Obtener referencia al PoolManager
        poolManager = PoolManager.Instance;
    }
    
    /// <summary>
    /// Crea un enemigo usando el pool apropiado según su configuración
    /// </summary>
    public GameObject CreateEnemy(Vector3 position, EnemyConfiguration config)
    {
        if (config == null)
        {
            Debug.LogWarning("[SpawnFactory] CreateEnemy called with null config!");
            return null;
        }
        
        if (poolManager == null)
        {
            Debug.LogError("[SpawnFactory] PoolManager is null!");
            return null;
        }
        
        return poolManager.SpawnEnemy(position, config);
    }
    
    /// <summary>
    /// Crea un proyectil con la configuración especificada
    /// </summary>
    public Projectile CreateProjectile(Vector3 position, Quaternion rotation, ProjectileConfiguration config)
    {
        if (config == null)
        {
            Debug.LogWarning("[SpawnFactory] CreateProjectile called with null config!");
            return null;
        }
        
        if (poolManager == null)
        {
            Debug.LogError("[SpawnFactory] PoolManager is null!");
            return null;
        }
        
        return poolManager.SpawnProjectile(position, rotation, config);
    }
    
    /// <summary>
    /// Crea un orbe de experiencia con la configuración especificada
    /// </summary>
    public ExperienceOrb CreateExperienceOrb(Vector3 position, OrbConfiguration config)
    {
        if (config == null)
        {
            Debug.LogWarning("[SpawnFactory] CreateExperienceOrb called with null config!");
            return null;
        }
        
        if (poolManager == null)
        {
            Debug.LogError("[SpawnFactory] PoolManager is null!");
            return null;
        }
        
        return poolManager.SpawnOrb(position, config);
    }
    
    /// <summary>
    /// Crea un coleccionable (moneda o diamante)
    /// </summary>
    public Collectible CreateCollectible(Vector3 position, Collectible.CollectibleType type, int value)
    {
        if (poolManager == null)
        {
            Debug.LogError("[SpawnFactory] PoolManager is null!");
            return null;
        }
        
        return poolManager.SpawnCollectible(position, type, value);
    }
    
    /// <summary>
    /// Destruye un objeto (lo devuelve al pool)
    /// </summary>
    public void DestroyObject(GameObject obj)
    {
        if (obj == null) return;
        
        if (poolManager == null)
        {
            Debug.LogError("[SpawnFactory] PoolManager is null!");
            Destroy(obj);
            return;
        }
        
        poolManager.Despawn(obj);
    }
    
    /// <summary>
    /// Pre-calienta los pools para reducir lag durante el gameplay
    /// Útil llamar esto durante una pantalla de carga
    /// </summary>
    public void PrewarmPools(int enemyCount, int projectileCount, int orbCount, int collectibleCount)
    {
        if (poolManager == null)
        {
            Debug.LogError("[SpawnFactory] PoolManager is null!");
            return;
        }
        
        // Precalentar pools de enemigos
        if (enemyCount > 0)
        {
            poolManager.PrewarmPool(PoolManager.PoolType.BasicEnemy, enemyCount / 2);
            poolManager.PrewarmPool(PoolManager.PoolType.FastEnemy, enemyCount / 2);
        }
        
        // Precalentar proyectiles
        if (projectileCount > 0)
        {
            poolManager.PrewarmPool(PoolManager.PoolType.Projectile, projectileCount);
        }
        
        // Precalentar orbes de experiencia
        if (orbCount > 0)
        {
            poolManager.PrewarmPool(PoolManager.PoolType.ExperienceOrb, orbCount);
        }
        
        // Precalentar coleccionables
        if (collectibleCount > 0)
        {
            poolManager.PrewarmPool(PoolManager.PoolType.Coin, collectibleCount / 2);
            poolManager.PrewarmPool(PoolManager.PoolType.Diamond, collectibleCount / 2);
        }
    }
}
