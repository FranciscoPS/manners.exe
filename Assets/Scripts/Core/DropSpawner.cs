using System.Collections.Generic;
using UnityEngine;

public class DropSpawner : MonoBehaviour
{
    public static DropSpawner Instance { get; private set; }

    [Tooltip("Cuántos drops se instancian por frame como máximo.")]
    [SerializeField] private int dropsPerFrame = 5;

    private enum DropType { Orb, Coin, Diamond }

    private struct PendingDrop
    {
        public Vector3           center;
        public float             radius;
        public DropType          type;
        public OrbConfiguration  orbConfig;
        public int               orbValue;
    }

    private readonly Queue<PendingDrop> queue = new Queue<PendingDrop>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Instance = null;

    public static DropSpawner GetOrCreate()
    {
        if (Instance != null) return Instance;
        var go = new GameObject("DropSpawner");
        DontDestroyOnLoad(go);
        return go.AddComponent<DropSpawner>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Update()
    {
        int toProcess = Mathf.Min(dropsPerFrame, queue.Count);
        for (int i = 0; i < toProcess; i++)
            SpawnNext(queue.Dequeue());
    }

    public void EnqueueOrb(Vector3 center, float radius, OrbConfiguration orbConfig, int orbValue)
    {
        queue.Enqueue(new PendingDrop
        {
            center    = center,
            radius    = radius,
            type      = DropType.Orb,
            orbConfig = orbConfig,
            orbValue  = orbValue
        });
    }

    public void EnqueueCoin(Vector3 center, float radius)
    {
        queue.Enqueue(new PendingDrop { center = center, radius = radius, type = DropType.Coin });
    }

    public void EnqueueDiamond(Vector3 center, float radius)
    {
        queue.Enqueue(new PendingDrop { center = center, radius = radius, type = DropType.Diamond });
    }

    private void SpawnNext(PendingDrop drop)
    {
        if (SpawnFactory.Instance == null) return;

        Vector2 rnd       = Random.insideUnitCircle * drop.radius;
        Vector3 candidate = drop.center + new Vector3(rnd.x, 0f, rnd.y);

        UnityEngine.AI.NavMeshHit hit;
        Vector3 pos = UnityEngine.AI.NavMesh.SamplePosition(candidate, out hit, 3f, UnityEngine.AI.NavMesh.AllAreas)
            ? hit.position + Vector3.up * 0.1f
            : new Vector3(candidate.x, drop.center.y + 0.1f, candidate.z);

        switch (drop.type)
        {
            case DropType.Orb:
                ExperienceOrb orb = SpawnFactory.Instance.CreateExperienceOrb(pos, drop.orbConfig);
                if (orb != null && drop.orbConfig == null)
                    orb.SetExperienceValue(drop.orbValue);
                break;
            case DropType.Coin:
                SpawnFactory.Instance.CreateCollectible(pos, Collectible.CollectibleType.Coin, 1);
                break;
            case DropType.Diamond:
                SpawnFactory.Instance.CreateCollectible(pos, Collectible.CollectibleType.Diamond, 1);
                break;
        }
    }
}
