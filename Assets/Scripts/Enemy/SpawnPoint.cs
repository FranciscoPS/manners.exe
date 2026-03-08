using UnityEngine;
using UnityEngine.AI;
using System.Collections;

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

    [Header("Warning Settings")]
    [SerializeField] private bool showSpawnWarning = true;
    [SerializeField] private float warningDuration = 1f;

    private float lastSpawnTime = -999f;
    private SpawnWarningIndicator warningIndicator;

    public SpawnSector Sector => sector;
    public int MaxEnemiesPerSpawn => maxEnemiesPerSpawn;
    public bool IsReady => Time.time >= lastSpawnTime + spawnCooldown;

    private void Awake()
    {
        if (showSpawnWarning)
        {
            CreateWarningIndicator();
        }
    }

    private void CreateWarningIndicator()
    {
        GameObject warningObj = new GameObject("SpawnWarningIndicator");
        warningObj.transform.SetParent(transform);
        warningObj.transform.localPosition = Vector3.zero;
        warningIndicator = warningObj.AddComponent<SpawnWarningIndicator>();
    }

    public void SpawnEnemies(int count, EnemyConfiguration config)
    {
        if (config == null || count <= 0) return;
        if (!IsReady) return;

        lastSpawnTime = Time.time; // inmediato: bloquea re-entrada antes de que termine la coroutine

        if (showSpawnWarning && warningIndicator != null)
        {
            StartCoroutine(SpawnWithWarning(count, config));
        }
        else
        {
            SpawnEnemiesImmediate(count, config);
        }
    }

    private IEnumerator SpawnWithWarning(int count, EnemyConfiguration config)
    {
        warningIndicator.ShowWarning(transform.position, warningDuration, spawnRadius);
        yield return new WaitForSeconds(warningDuration);
        SpawnEnemiesImmediate(count, config);
    }

    /// <summary>
    /// Shows the red warning circle, waits warningDuration, then warps the enemy here.
    /// Call this instead of EnemyController.WarpTo() when you want the player to see the indicator.
    /// </summary>
    public void WarnThenWarp(EnemyController ctrl)
    {
        StartCoroutine(WarnThenWarpRoutine(ctrl));
    }

    private IEnumerator WarnThenWarpRoutine(EnemyController ctrl)
    {
        if (showSpawnWarning && warningIndicator != null)
        {
            warningIndicator.ShowWarning(transform.position, warningDuration, spawnRadius);
            yield return new WaitForSeconds(warningDuration);
        }
        if (ctrl != null)
            ctrl.WarpTo(transform.position);
    }

    private void SpawnEnemiesImmediate(int count, EnemyConfiguration config)
    {
        int enemiesToSpawn = Mathf.Min(count, maxEnemiesPerSpawn);

        for (int i = 0; i < enemiesToSpawn; i++)
        {
            Vector3 spawnPosition = GetSpawnPosition();
            SpawnSingleEnemy(spawnPosition, config);
        }
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
        if (SpawnFactory.Instance != null)
        {
            SpawnFactory.Instance.CreateEnemy(position, config);
        }
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
