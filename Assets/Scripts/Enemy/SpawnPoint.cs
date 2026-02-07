using UnityEngine;
using UnityEngine.AI;

public class SpawnPoint : MonoBehaviour
{
    public enum SpawnSector
    {
        North,
        South,
        East,
        West,
        NorthEast,
        SouthEast,
        SouthWest,
        NorthWest
    }

    [Header("Spawn Settings")]
    [SerializeField] private SpawnSector sector;
    [SerializeField] private int maxEnemiesPerSpawn = 4;
    [SerializeField] private float spawnCooldown = 2f;
    [SerializeField] private float spawnRadius = 3f;
    [SerializeField] private bool useNavMesh = true;

    [Header("Enemy Configuration")]
    [SerializeField] private EnemyConfiguration[] allowedEnemyTypes;

    private float cooldownTimer = 0f;

    public SpawnSector Sector => sector;
    public int MaxEnemiesPerSpawn => maxEnemiesPerSpawn;
    public bool IsReady => cooldownTimer <= 0f;

    private void Update()
    {
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }
    }

    public void SpawnEnemies(int count, EnemyConfiguration config)
    {
        if (config == null || count <= 0) return;

        int enemiesToSpawn = Mathf.Min(count, maxEnemiesPerSpawn);

        for (int i = 0; i < enemiesToSpawn; i++)
        {
            Vector3 spawnPosition = GetSpawnPosition();
            SpawnSingleEnemy(spawnPosition, config);
        }

        cooldownTimer = spawnCooldown;
    }

    private Vector3 GetSpawnPosition()
    {
        Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;
        Vector3 basePosition = transform.position;
        Vector3 attemptPosition = basePosition + new Vector3(randomOffset.x, 0f, randomOffset.y);

        if (useNavMesh)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(attemptPosition, out hit, 10f, NavMesh.AllAreas))
            {
                return hit.position;
            }
        }

        return attemptPosition;
    }

    private void SpawnSingleEnemy(Vector3 position, EnemyConfiguration config)
    {
        if (PoolManager.Instance != null)
        {
            PoolManager.Instance.SpawnEnemy(position, config);
        }
    }

    public bool CanSpawnEnemyType(EnemyConfiguration config)
    {
        if (allowedEnemyTypes == null || allowedEnemyTypes.Length == 0)
            return true;

        foreach (var allowed in allowedEnemyTypes)
        {
            if (allowed == config)
                return true;
        }

        return false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = GetSectorColor();
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 2f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = GetSectorColor();
        Gizmos.DrawSphere(transform.position, 0.5f);
    }

    private Color GetSectorColor()
    {
        switch (sector)
        {
            case SpawnSector.North: return Color.red;
            case SpawnSector.South: return Color.blue;
            case SpawnSector.East: return Color.green;
            case SpawnSector.West: return Color.yellow;
            case SpawnSector.NorthEast: return new Color(1f, 0.5f, 0f);
            case SpawnSector.SouthEast: return Color.cyan;
            case SpawnSector.SouthWest: return Color.magenta;
            case SpawnSector.NorthWest: return new Color(0.5f, 0f, 1f);
            default: return Color.white;
        }
    }
}
