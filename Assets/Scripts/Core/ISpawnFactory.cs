using UnityEngine;

public interface ISpawnFactory
{

    GameObject CreateEnemy(Vector3 position, EnemyConfiguration config);

    Projectile CreateProjectile(Vector3 position, Quaternion rotation, ProjectileConfiguration config);

    ExperienceOrb CreateExperienceOrb(Vector3 position, OrbConfiguration config);

    Collectible CreateCollectible(Vector3 position, Collectible.CollectibleType type, int value);

    void DestroyObject(GameObject obj);

    void PrewarmPools(int enemyCount, int projectileCount, int orbCount, int collectibleCount);
}
