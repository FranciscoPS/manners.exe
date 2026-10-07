using System.Collections.Generic;
using UnityEngine;

public class EnemySeparationManager : MonoBehaviour, IUpdateable
{
    public static EnemySeparationManager Instance { get; private set; }

    [Header("Separación")]
    [Tooltip("Radio en el que un enemigo 'siente' a sus vecinos y se aparta. Súbelo para que mantengan más espacio.")]
    [SerializeField] private float separationRadius = 1.1f;

    [Tooltip("Velocidad máxima (u/seg) que puede aportar la separación. Limita cuán fuerte se empujan.")]
    [SerializeField] private float maxSeparationSpeed = 2.5f;

    [Tooltip("Intensidad de la repulsión. Más alto = se separan más rápido.")]
    [SerializeField] private float pushStrength = 3.5f;

    [Tooltip("Cada cuántos segundos se recalcula la separación (no por frame).")]
    [SerializeField] private float recalcInterval = 0.12f;

    [Tooltip("Máximo de vecinos a considerar por enemigo (corta casos extremos de aglomeración).")]
    [SerializeField] private int maxNeighbors = 12;

    private static readonly List<EnemyController> agents = new List<EnemyController>(256);

    private float timer;
    private readonly List<Vector3> positions = new List<Vector3>(256);
    private readonly Dictionary<long, List<int>> grid = new Dictionary<long, List<int>>(256);
    private readonly Stack<List<int>> listPool = new Stack<List<int>>(64);

    public bool IsActive => isActiveAndEnabled;

    public void Configure(float radius, float maxSpeed, float push, float interval, int neighbors)
    {
        separationRadius = radius;
        maxSeparationSpeed = maxSpeed;
        pushStrength = push;
        recalcInterval = interval;
        maxNeighbors = neighbors;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        agents.Clear();
        Instance = null;
    }

    public static void Register(EnemyController agent)
    {
        if (agent == null) return;
        if (!agents.Contains(agent)) agents.Add(agent);
        EnsureExists();
    }

    public static void Unregister(EnemyController agent)
    {
        agents.Remove(agent);
    }

    private static void EnsureExists()
    {
        if (Instance != null) return;
        var go = new GameObject("[EnemySeparationManager]");
        Instance = go.AddComponent<EnemySeparationManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        if (UpdateManager.Instance != null)
            UpdateManager.Instance.Register(this as IUpdateable);
    }

    private void OnDisable()
    {
        if (UpdateManager.Instance != null)
            UpdateManager.Instance.Unregister(this as IUpdateable);
    }

    public void OnUpdate(float deltaTime)
    {
        timer += deltaTime;
        if (timer < recalcInterval) return;
        timer = 0f;
        Recalculate();
    }

    private void Recalculate()
    {
        int count = agents.Count;
        if (count == 0) return;

        float cellSize = Mathf.Max(0.1f, separationRadius);
        float invCell = 1f / cellSize;
        float radiusSqr = separationRadius * separationRadius;

        ClearGrid();
        positions.Clear();

        for (int i = 0; i < count; i++)
        {
            var a = agents[i];
            Vector3 p = (a != null) ? a.transform.position : Vector3.zero;
            positions.Add(p);

            if (a == null || !a.WantsSeparation) continue;

            long key = CellKey(Mathf.FloorToInt(p.x * invCell), Mathf.FloorToInt(p.z * invCell));
            if (!grid.TryGetValue(key, out var bucket))
            {
                bucket = RentList();
                grid[key] = bucket;
            }
            bucket.Add(i);
        }

        for (int i = 0; i < count; i++)
        {
            var self = agents[i];
            if (self == null || !self.WantsSeparation)
            {
                self?.SetSeparation(Vector3.zero);
                continue;
            }

            Vector3 selfPos = positions[i];
            int cx = Mathf.FloorToInt(selfPos.x * invCell);
            int cz = Mathf.FloorToInt(selfPos.z * invCell);

            Vector3 sum = Vector3.zero;
            int neighbors = 0;

            for (int ox = -1; ox <= 1 && neighbors < maxNeighbors; ox++)
            {
                for (int oz = -1; oz <= 1 && neighbors < maxNeighbors; oz++)
                {
                    if (!grid.TryGetValue(CellKey(cx + ox, cz + oz), out var bucket)) continue;

                    for (int b = 0; b < bucket.Count; b++)
                    {
                        int j = bucket[b];
                        if (j == i) continue;

                        Vector3 away = selfPos - positions[j];
                        away.y = 0f;
                        float dSqr = away.sqrMagnitude;
                        if (dSqr > radiusSqr || dSqr < 1e-8f)
                        {
                            if (dSqr < 1e-8f)
                            {

                                float ang = (i * 137) % 360 * Mathf.Deg2Rad;
                                sum += new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
                                if (++neighbors >= maxNeighbors) break;
                            }
                            continue;
                        }

                        float d = Mathf.Sqrt(dSqr);

                        sum += (away / d) * (1f - d / separationRadius);

                        if (++neighbors >= maxNeighbors) break;
                    }
                }
            }

            Vector3 sep = sum * pushStrength;
            float mSqr = sep.sqrMagnitude;
            if (mSqr > maxSeparationSpeed * maxSeparationSpeed)
                sep = sep / Mathf.Sqrt(mSqr) * maxSeparationSpeed;

            self.SetSeparation(sep);
        }
    }

    private static long CellKey(int x, int z)
    {
        return ((long)x << 32) ^ (uint)z;
    }

    private void ClearGrid()
    {
        foreach (var kvp in grid)
        {
            kvp.Value.Clear();
            listPool.Push(kvp.Value);
        }
        grid.Clear();
    }

    private List<int> RentList()
    {
        return listPool.Count > 0 ? listPool.Pop() : new List<int>(8);
    }
}
