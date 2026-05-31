using UnityEngine;
using UnityEngine.AI;

public sealed class FlockingEnemySpawner : MonoBehaviour, IUpdateable
{
    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("Spawn")]
    [SerializeField] private EnemyConfiguration[] enemyConfigurations;
    [SerializeField] private float spawnInterval = 1f;
    [SerializeField] private int enemiesPerBurst = 1;
    [SerializeField] private int maxActiveEnemies = 10;
    [SerializeField] private float minSpawnRadius = 14f;
    [SerializeField] private float maxSpawnRadius = 18f;
    [SerializeField] private bool sampleNavMesh = true;
    [SerializeField] private int spawnAttempts = 8;

    private float timer;

    public bool IsActive => isActiveAndEnabled;

    private void OnEnable()
    {
        ResolvePlayer();
        timer = spawnInterval;

        if (UpdateManager.Instance != null)
        {
            UpdateManager.Instance.Register(this);
        }
    }

    private void OnDisable()
    {
        if (UpdateManager.Instance != null)
        {
            UpdateManager.Instance.Unregister(this);
        }
    }

    public void OnUpdate(float deltaTime)
    {
        if (player == null)
        {
            ResolvePlayer();
            if (player == null)
            {
                return;
            }
        }

        timer -= deltaTime;
        if (timer > 0f)
        {
            return;
        }

        timer = spawnInterval;
        SpawnBurst();
    }

    private void SpawnBurst()
    {
        if (enemyConfigurations == null || enemyConfigurations.Length == 0)
        {
            return;
        }

        if (EnemyHealth.ActiveEnemyCount >= maxActiveEnemies)
        {
            return;
        }

        int availableSlots = maxActiveEnemies - EnemyHealth.ActiveEnemyCount;
        int count = Mathf.Min(enemiesPerBurst, availableSlots);

        for (int i = 0; i < count; i++)
        {
            EnemyConfiguration config = enemyConfigurations[Random.Range(0, enemyConfigurations.Length)];
            if (config == null)
            {
                continue;
            }

            Vector3 position = GetSpawnPosition();
            SpawnFactory.Instance.CreateEnemy(position, config);
        }
    }

    private Vector3 GetSpawnPosition()
    {
        Vector3 fallback = player.position + Vector3.forward * maxSpawnRadius;

        for (int i = 0; i < spawnAttempts; i++)
        {
            Vector2 direction = Random.insideUnitCircle;
            if (direction.sqrMagnitude <= 0.001f)
            {
                direction = Vector2.up;
            }

            direction.Normalize();
            float distance = Random.Range(minSpawnRadius, maxSpawnRadius);
            Vector3 candidate = player.position + new Vector3(direction.x, 0f, direction.y) * distance;
            candidate.y = player.position.y;

            if (!sampleNavMesh)
            {
                return candidate;
            }

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 8f, NavMesh.AllAreas))
            {
                return hit.position;
            }

            fallback = candidate;
        }

        return fallback;
    }

    private void ResolvePlayer()
    {
        if (player != null)
        {
            return;
        }

        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }
}
