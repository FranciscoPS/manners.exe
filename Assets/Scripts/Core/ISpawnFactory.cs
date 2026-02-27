using UnityEngine;

/// <summary>
/// Abstract Factory Pattern - Interface para crear objetos del juego
/// Basado en: https://refactoring.guru/es/design-patterns/abstract-factory
/// 
/// Propósito:
/// - Desacoplar la creación de objetos del código cliente
/// - Encapsular la lógica de spawning y pooling
/// - Facilitar testing mediante mocking de la factory
/// - Proveer una API consistente para crear objetos
/// </summary>
public interface ISpawnFactory
{
    /// <summary>
    /// Crea un enemigo en la posición especificada
    /// </summary>
    GameObject CreateEnemy(Vector3 position, EnemyConfiguration config);
    
    /// <summary>
    /// Crea un proyectil en la posición y rotación especificadas
    /// </summary>
    Projectile CreateProjectile(Vector3 position, Quaternion rotation, ProjectileConfiguration config);
    
    /// <summary>
    /// Crea un orbe de experiencia en la posición especificada
    /// </summary>
    ExperienceOrb CreateExperienceOrb(Vector3 position, OrbConfiguration config);
    
    /// <summary>
    /// Crea un coleccionable (moneda o diamante) en la posición especificada
    /// </summary>
    Collectible CreateCollectible(Vector3 position, Collectible.CollectibleType type, int value);
    
    /// <summary>
    /// Destruye/devuelve un objeto al pool
    /// </summary>
    void DestroyObject(GameObject obj);
    
    /// <summary>
    /// Pre-caliente el pool para reducir lag durante el juego
    /// </summary>
    void PrewarmPools(int enemyCount, int projectileCount, int orbCount, int collectibleCount);
}
