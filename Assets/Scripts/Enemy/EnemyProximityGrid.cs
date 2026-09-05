using System.Collections.Generic;
using UnityEngine;

public class EnemyProximityGrid
{
    private readonly Dictionary<long, List<int>> cells = new Dictionary<long, List<int>>(256);
    private readonly Stack<List<int>> listPool = new Stack<List<int>>(64);
    private readonly List<EnemyHealth> entries = new List<EnemyHealth>(256);
    private readonly List<Vector3> positions = new List<Vector3>(256);

    private float invCellSize = 1f;

    public void Build(List<EnemyHealth> enemies, float cellSize)
    {
        invCellSize = 1f / Mathf.Max(0.1f, cellSize);
        Clear();

        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyHealth enemy = enemies[i];
            if (enemy == null) continue;

            Vector3 position = enemy.transform.position;
            int index = entries.Count;
            entries.Add(enemy);
            positions.Add(position);

            long key = CellKey(Mathf.FloorToInt(position.x * invCellSize), Mathf.FloorToInt(position.z * invCellSize));
            if (!cells.TryGetValue(key, out List<int> bucket))
            {
                bucket = RentList();
                cells[key] = bucket;
            }

            bucket.Add(index);
        }
    }

    public void CollectWithin(Vector3 center, float radius, List<EnemyHealth> results)
    {
        float radiusSqr = radius * radius;
        int reach = Mathf.CeilToInt(radius * invCellSize);
        int centerX = Mathf.FloorToInt(center.x * invCellSize);
        int centerZ = Mathf.FloorToInt(center.z * invCellSize);

        for (int offsetX = -reach; offsetX <= reach; offsetX++)
        {
            for (int offsetZ = -reach; offsetZ <= reach; offsetZ++)
            {
                if (!cells.TryGetValue(CellKey(centerX + offsetX, centerZ + offsetZ), out List<int> bucket)) continue;

                for (int b = 0; b < bucket.Count; b++)
                {
                    int index = bucket[b];
                    if ((positions[index] - center).sqrMagnitude <= radiusSqr)
                        results.Add(entries[index]);
                }
            }
        }
    }

    private void Clear()
    {
        foreach (KeyValuePair<long, List<int>> kvp in cells)
        {
            kvp.Value.Clear();
            listPool.Push(kvp.Value);
        }

        cells.Clear();
        entries.Clear();
        positions.Clear();
    }

    private List<int> RentList()
    {
        return listPool.Count > 0 ? listPool.Pop() : new List<int>(8);
    }

    private static long CellKey(int x, int z)
    {
        return ((long)x << 32) ^ (uint)z;
    }
}
