using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-50)]
public sealed class EnemyFlockManager : MonoBehaviour
{
    private sealed class ObstacleEntry
    {
        public Collider Collider;
        public Bounds RawBounds;
        public Bounds Bounds;
        public int LastQueryId;
    }

    private sealed class SteeringMemory
    {
        public int ObstacleSide;
        public float ObstacleSideUntil;
    }

    private static EnemyFlockManager instance;
    private static bool isQuitting;

    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private string targetTag = "Player";

    [Header("Flocking")]
    [SerializeField] private float cellSize = 3f;
    [SerializeField] private float neighborRadius = 3f;
    [SerializeField] private float separationRadius = 1.4f;
    [SerializeField] private int maxNeighbors = 16;
    [SerializeField] private float seekWeight = 1.35f;
    [SerializeField] private float separationWeight = 2f;
    [SerializeField] private float alignmentWeight = 0f;
    [SerializeField] private float cohesionWeight = 0f;

    [Header("Obstacle Avoidance")]
    [SerializeField] private bool autoRegisterSceneObstacles = true;
    [SerializeField] private float obstacleCellSize = 8f;
    [SerializeField] private float obstacleLookAhead = 1.8f;
    [SerializeField] private float obstaclePadding = 0.45f;
    [SerializeField] private float obstacleWeight = 1.85f;
    [SerializeField] private float tangentWeight = 0.2f;
    [SerializeField] private float obstacleSideMemory = 0.7f;

    [Header("Physics Layers")]
    [Tooltip("Activa colision de Enemy contra Buildings/Store en runtime. Enemy contra Enemy queda en soft collision por flocking.")]
    [SerializeField] private bool syncPhysicsLayerCollision = true;
    [SerializeField] private bool enableEnemyBuildingPhysicsCollision = true;
    [SerializeField] private bool enableEnemyEnemyPhysicsCollision = false;

    private readonly List<EnemyController> agents = new List<EnemyController>(512);
    private readonly HashSet<EnemyController> agentSet = new HashSet<EnemyController>();
    private readonly Dictionary<Vector2Int, List<EnemyController>> agentBuckets = new Dictionary<Vector2Int, List<EnemyController>>(512);

    private readonly List<FlockingObstacle> obstacles = new List<FlockingObstacle>(256);
    private readonly HashSet<FlockingObstacle> obstacleSet = new HashSet<FlockingObstacle>();
    private readonly List<ObstacleEntry> obstacleEntries = new List<ObstacleEntry>(512);
    private readonly HashSet<Collider> obstacleColliderSet = new HashSet<Collider>();
    private readonly Dictionary<Vector2Int, List<ObstacleEntry>> obstacleBuckets = new Dictionary<Vector2Int, List<ObstacleEntry>>(512);
    private readonly Dictionary<EnemyController, SteeringMemory> steeringMemoryByAgent = new Dictionary<EnemyController, SteeringMemory>(512);

    private int agentGridFrame = -1;
    private int obstacleQueryId;
    private bool obstacleGridDirty = true;

    public static bool HasInstance => instance != null;

    public static EnemyFlockManager Instance
    {
        get
        {
            if (instance == null && !isQuitting)
            {
                instance = FindFirstObjectByType<EnemyFlockManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("[EnemyFlockManager]");
                    instance = go.AddComponent<EnemyFlockManager>();
                }
            }

            return instance;
        }
    }

    public Transform Target => target;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        isQuitting = false;
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        ResolveTarget();
        ApplyPhysicsLayerSettings();
    }

    private void Start()
    {
        if (autoRegisterSceneObstacles)
        {
            RegisterExistingSceneObstacles();
        }
    }

    private void OnApplicationQuit()
    {
        isQuitting = true;
    }

    public void Register(EnemyController agent)
    {
        if (agent == null || !agentSet.Add(agent))
        {
            return;
        }

        agents.Add(agent);
        if (!steeringMemoryByAgent.ContainsKey(agent))
        {
            steeringMemoryByAgent[agent] = new SteeringMemory();
        }
        agentGridFrame = -1;
    }

    public void Unregister(EnemyController agent)
    {
        if (agent == null || !agentSet.Remove(agent))
        {
            return;
        }

        agents.Remove(agent);
        steeringMemoryByAgent.Remove(agent);
        agentGridFrame = -1;
    }

    public void RegisterObstacle(FlockingObstacle obstacle)
    {
        if (obstacle == null || !obstacleSet.Add(obstacle))
        {
            return;
        }

        obstacles.Add(obstacle);
        obstacleGridDirty = true;
    }

    public void UnregisterObstacle(FlockingObstacle obstacle)
    {
        if (obstacle == null || !obstacleSet.Remove(obstacle))
        {
            return;
        }

        obstacles.Remove(obstacle);
        obstacleGridDirty = true;
    }

    public void MarkObstacleGridDirty()
    {
        obstacleGridDirty = true;
    }

    public Vector3 GetDesiredDirection(EnemyController agent, Vector3 targetPosition)
    {
        if (agent == null)
        {
            return Vector3.zero;
        }

        if (target == null)
        {
            ResolveTarget();
        }

        if (target != null)
        {
            targetPosition = target.position;
        }

        RebuildAgentGridIfNeeded();
        RebuildObstacleGridIfNeeded();

        Vector3 position = agent.transform.position;
        Vector3 seek = targetPosition - position;
        seek.y = 0f;
        seek = seek.sqrMagnitude > 0.001f ? seek.normalized : Vector3.zero;

        Vector3 separation = Vector3.zero;
        Vector3 alignment = Vector3.zero;
        Vector3 cohesion = Vector3.zero;
        int neighborCount = 0;

        int searchRange = Mathf.CeilToInt(neighborRadius / Mathf.Max(0.1f, cellSize));
        Vector2Int centerCell = GetCell(position, cellSize);

        for (int x = centerCell.x - searchRange; x <= centerCell.x + searchRange; x++)
        {
            for (int z = centerCell.y - searchRange; z <= centerCell.y + searchRange; z++)
            {
                if (!agentBuckets.TryGetValue(new Vector2Int(x, z), out List<EnemyController> bucket))
                {
                    continue;
                }

                for (int i = 0; i < bucket.Count; i++)
                {
                    EnemyController other = bucket[i];
                    if (other == null || other == agent || !other.IsActive)
                    {
                        continue;
                    }

                    Vector3 offset = position - other.transform.position;
                    offset.y = 0f;
                    float sqrDistance = offset.sqrMagnitude;
                    if (sqrDistance <= 0.0001f || sqrDistance > neighborRadius * neighborRadius)
                    {
                        continue;
                    }

                    neighborCount++;

                    Vector3 otherVelocity = other.PlanarVelocity;
                    otherVelocity.y = 0f;
                    if (otherVelocity.sqrMagnitude > 0.01f)
                    {
                        alignment += otherVelocity.normalized;
                    }

                    cohesion += other.transform.position;

                    float personalSpace = Mathf.Max(separationRadius, agent.AgentRadius + other.AgentRadius);
                    if (sqrDistance < personalSpace * personalSpace)
                    {
                        float distance = Mathf.Sqrt(sqrDistance);
                        float pressure = Mathf.Clamp01((personalSpace - distance) / personalSpace);
                        separation += (offset / distance) * (pressure + 0.15f);
                    }

                    if (neighborCount >= maxNeighbors)
                    {
                        break;
                    }
                }

                if (neighborCount >= maxNeighbors)
                {
                    break;
                }
            }

            if (neighborCount >= maxNeighbors)
            {
                break;
            }
        }

        if (neighborCount > 0)
        {
            cohesion = ((cohesion / neighborCount) - position);
            cohesion.y = 0f;
            cohesion = cohesion.sqrMagnitude > 0.001f ? cohesion.normalized : Vector3.zero;
            alignment = alignment.sqrMagnitude > 0.001f ? alignment.normalized : Vector3.zero;
        }

        Vector3 obstacleAvoidance = CalculateObstacleAvoidance(agent, position, seek, agent.AgentRadius);

        Vector3 desired =
            seek * seekWeight +
            separation * separationWeight +
            alignment * alignmentWeight +
            cohesion * cohesionWeight +
            obstacleAvoidance * obstacleWeight;

        if (desired.sqrMagnitude <= 0.001f)
        {
            return seek;
        }

        return desired.normalized;
    }

    public Vector3 ResolveObstacleVelocity(EnemyController agent, Vector3 velocity, float fixedDeltaTime)
    {
        velocity.y = 0f;
        if (agent == null || fixedDeltaTime <= 0f || velocity.sqrMagnitude <= 0.0001f)
        {
            return velocity;
        }

        RebuildObstacleGridIfNeeded();
        if (obstacleEntries.Count == 0)
        {
            return velocity;
        }

        Vector3 resolved = velocity;
        Vector3 position = agent.transform.position;
        position.y = 0f;

        float radius = agent.AgentRadius + 0.04f;
        float maxEscapeSpeed = Mathf.Max(1f, velocity.magnitude);

        for (int i = 0; i < 3; i++)
        {
            Vector3 futurePosition = position + resolved * fixedDeltaTime;
            if (!TryGetBlockingNormal(futurePosition, resolved, radius, out Vector3 normal, out float penetration))
            {
                break;
            }

            float speedIntoObstacle = Vector3.Dot(resolved, normal);
            if (speedIntoObstacle < 0f)
            {
                resolved -= normal * speedIntoObstacle;
            }

            if (penetration > 0.01f)
            {
                float escapeSpeed = Mathf.Min(maxEscapeSpeed, penetration / Mathf.Max(0.001f, fixedDeltaTime));
                resolved += normal * escapeSpeed * 0.35f;
            }

            resolved.y = 0f;
            if (resolved.sqrMagnitude <= 0.0001f)
            {
                return Vector3.zero;
            }
        }

        return resolved;
    }

    private void ResolveTarget()
    {
        if (target != null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(targetTag))
        {
            target = GameObject.FindGameObjectWithTag(targetTag)?.transform;
        }
    }

    private void RegisterExistingSceneObstacles()
    {
        BuildingsScript[] buildings = FindObjectsByType<BuildingsScript>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < buildings.Length; i++)
        {
            if (buildings[i] == null)
            {
                continue;
            }

            if (!buildings[i].TryGetComponent(out FlockingObstacle obstacle))
            {
                obstacle = buildings[i].gameObject.AddComponent<FlockingObstacle>();
            }

            obstacle.RefreshColliders();
            RegisterObstacle(obstacle);
        }

        RegisterLayerObstacleColliders();
    }

    private void RegisterLayerObstacleColliders()
    {
        int buildingsLayer = LayerMask.NameToLayer("Buildings");
        int storeLayer = LayerMask.NameToLayer("Store");
        if (buildingsLayer < 0 && storeLayer < 0)
        {
            return;
        }

        Collider[] sceneColliders = FindObjectsByType<Collider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < sceneColliders.Length; i++)
        {
            Collider sceneCollider = sceneColliders[i];
            if (sceneCollider == null || !sceneCollider.enabled || !sceneCollider.gameObject.activeInHierarchy)
            {
                continue;
            }

            int layer = sceneCollider.gameObject.layer;
            bool isBuildingsLayer = layer == buildingsLayer;
            bool isStoreLayer = layer == storeLayer;
            if (!isBuildingsLayer && !isStoreLayer)
            {
                continue;
            }

            if (sceneCollider.isTrigger && !isBuildingsLayer)
            {
                continue;
            }

            if (sceneCollider.GetComponentInParent<FlockingObstacle>() != null)
            {
                continue;
            }

            FlockingObstacle obstacle = sceneCollider.gameObject.AddComponent<FlockingObstacle>();
            obstacle.RefreshColliders();
            RegisterObstacle(obstacle);
        }
    }

    private void ApplyPhysicsLayerSettings()
    {
        if (!syncPhysicsLayerCollision)
        {
            return;
        }

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        int buildingsLayer = LayerMask.NameToLayer("Buildings");
        int storeLayer = LayerMask.NameToLayer("Store");

        if (enemyLayer >= 0)
        {
            Physics.IgnoreLayerCollision(enemyLayer, enemyLayer, !enableEnemyEnemyPhysicsCollision);
        }

        if (enemyLayer >= 0 && buildingsLayer >= 0)
        {
            Physics.IgnoreLayerCollision(enemyLayer, buildingsLayer, !enableEnemyBuildingPhysicsCollision);
        }

        if (enemyLayer >= 0 && storeLayer >= 0)
        {
            Physics.IgnoreLayerCollision(enemyLayer, storeLayer, !enableEnemyBuildingPhysicsCollision);
        }
    }

    private void RebuildAgentGridIfNeeded()
    {
        if (agentGridFrame == Time.frameCount)
        {
            return;
        }

        agentGridFrame = Time.frameCount;
        agentBuckets.Clear();

        for (int i = agents.Count - 1; i >= 0; i--)
        {
            EnemyController agent = agents[i];
            if (agent == null)
            {
                agents.RemoveAt(i);
                continue;
            }

            if (!agent.IsActive)
            {
                continue;
            }

            Vector2Int cell = GetCell(agent.transform.position, cellSize);
            if (!agentBuckets.TryGetValue(cell, out List<EnemyController> bucket))
            {
                bucket = new List<EnemyController>(8);
                agentBuckets[cell] = bucket;
            }

            bucket.Add(agent);
        }
    }

    private void RebuildObstacleGridIfNeeded()
    {
        if (!obstacleGridDirty)
        {
            return;
        }

        obstacleGridDirty = false;
        obstacleEntries.Clear();
        obstacleColliderSet.Clear();
        obstacleBuckets.Clear();

        for (int i = obstacles.Count - 1; i >= 0; i--)
        {
            FlockingObstacle obstacle = obstacles[i];
            if (obstacle == null)
            {
                obstacles.RemoveAt(i);
                continue;
            }

            if (!obstacle.isActiveAndEnabled)
            {
                continue;
            }

            IReadOnlyList<Collider> colliders = obstacle.Colliders;
            for (int c = 0; c < colliders.Count; c++)
            {
                Collider collider = colliders[c];
                if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (!obstacleColliderSet.Add(collider))
                {
                    continue;
                }

                Bounds rawBounds = collider.bounds;
                ObstacleEntry entry = new ObstacleEntry
                {
                    Collider = collider,
                    RawBounds = rawBounds,
                    Bounds = rawBounds,
                    LastQueryId = -1
                };

                entry.Bounds.Expand(obstaclePadding * 2f);
                obstacleEntries.Add(entry);
                AddObstacleToBuckets(entry);
            }
        }
    }

    private void AddObstacleToBuckets(ObstacleEntry entry)
    {
        Vector2Int min = GetCell(entry.Bounds.min, obstacleCellSize);
        Vector2Int max = GetCell(entry.Bounds.max, obstacleCellSize);

        for (int x = min.x; x <= max.x; x++)
        {
            for (int z = min.y; z <= max.y; z++)
            {
                Vector2Int cell = new Vector2Int(x, z);
                if (!obstacleBuckets.TryGetValue(cell, out List<ObstacleEntry> bucket))
                {
                    bucket = new List<ObstacleEntry>(4);
                    obstacleBuckets[cell] = bucket;
                }

                bucket.Add(entry);
            }
        }
    }

    private Vector3 CalculateObstacleAvoidance(EnemyController agent, Vector3 position, Vector3 seekDirection, float agentRadius)
    {
        if (obstacleEntries.Count == 0 || seekDirection.sqrMagnitude <= 0.001f)
        {
            return Vector3.zero;
        }

        obstacleQueryId++;

        seekDirection.Normalize();
        float pathRadius = agentRadius + obstaclePadding;
        float queryRadius = obstacleLookAhead + pathRadius;
        int searchRange = Mathf.CeilToInt(queryRadius / Mathf.Max(0.1f, obstacleCellSize));
        Vector2Int centerCell = GetCell(position, obstacleCellSize);

        Vector3 awayAvoidance = Vector3.zero;
        Vector3 tangentAvoidance = Vector3.zero;
        int blockerCount = 0;
        SteeringMemory memory = GetSteeringMemory(agent);

        for (int x = centerCell.x - searchRange; x <= centerCell.x + searchRange; x++)
        {
            for (int z = centerCell.y - searchRange; z <= centerCell.y + searchRange; z++)
            {
                if (!obstacleBuckets.TryGetValue(new Vector2Int(x, z), out List<ObstacleEntry> bucket))
                {
                    continue;
                }

                for (int i = 0; i < bucket.Count; i++)
                {
                    ObstacleEntry entry = bucket[i];
                    if (entry == null || entry.LastQueryId == obstacleQueryId)
                    {
                        continue;
                    }

                    entry.LastQueryId = obstacleQueryId;
                    Bounds bounds = entry.Bounds;

                    if (!TryGetObstacleInPath(bounds, position, seekDirection, obstacleLookAhead, pathRadius, out Vector3 closestOnPath, out Vector3 closestOnBounds))
                    {
                        continue;
                    }

                    Vector3 away = closestOnPath - closestOnBounds;
                    away.y = 0f;

                    if (away.sqrMagnitude <= 0.001f)
                    {
                        away = position - bounds.center;
                        away.y = 0f;
                    }

                    if (away.sqrMagnitude <= 0.001f)
                    {
                        continue;
                    }

                    away.Normalize();

                    Vector3 tangent = GetStableTangent(agent, memory, away, seekDirection);

                    float pathDistance = Vector3.Distance(closestOnPath, closestOnBounds);
                    float pressure = Mathf.Clamp01((pathRadius - pathDistance) / Mathf.Max(0.1f, pathRadius));
                    float tangentPressure = pressure * Mathf.Clamp01(pathDistance / Mathf.Max(0.1f, pathRadius));

                    if (Vector3.Dot(away, seekDirection) < -0.7f)
                    {
                        away *= 0.35f;
                        tangentPressure = Mathf.Max(tangentPressure, pressure * 0.25f);
                    }

                    blockerCount++;
                    awayAvoidance += away * (pressure + 0.25f);
                    tangentAvoidance += tangent * tangentWeight * tangentPressure;
                }
            }
        }

        if (blockerCount == 0)
        {
            return Vector3.zero;
        }

        float tangentScale = blockerCount > 1 ? 0.2f : 1f;
        Vector3 avoidance = awayAvoidance + tangentAvoidance * tangentScale;

        if (avoidance.sqrMagnitude > 0.001f && Vector3.Dot(avoidance.normalized, seekDirection) < -0.25f)
        {
            avoidance += seekDirection * 0.5f;
        }

        return avoidance.sqrMagnitude > 0.001f ? Vector3.ClampMagnitude(avoidance, 1f) : Vector3.zero;
    }

    private bool TryGetBlockingNormal(
        Vector3 point,
        Vector3 fallbackVelocity,
        float radius,
        out Vector3 normal,
        out float penetration)
    {
        normal = Vector3.zero;
        penetration = 0f;

        if (obstacleEntries.Count == 0)
        {
            return false;
        }

        obstacleQueryId++;

        float radiusSqr = radius * radius;
        int searchRange = Mathf.CeilToInt(radius / Mathf.Max(0.1f, obstacleCellSize)) + 1;
        Vector2Int centerCell = GetCell(point, obstacleCellSize);

        for (int x = centerCell.x - searchRange; x <= centerCell.x + searchRange; x++)
        {
            for (int z = centerCell.y - searchRange; z <= centerCell.y + searchRange; z++)
            {
                if (!obstacleBuckets.TryGetValue(new Vector2Int(x, z), out List<ObstacleEntry> bucket))
                {
                    continue;
                }

                for (int i = 0; i < bucket.Count; i++)
                {
                    ObstacleEntry entry = bucket[i];
                    if (entry == null || entry.LastQueryId == obstacleQueryId)
                    {
                        continue;
                    }

                    entry.LastQueryId = obstacleQueryId;
                    if (entry.Collider == null || !entry.Collider.enabled || !entry.Collider.gameObject.activeInHierarchy)
                    {
                        continue;
                    }

                    Bounds bounds = entry.RawBounds;
                    if (!TryGetCircleBoundsPenetration(point, fallbackVelocity, bounds, radius, radiusSqr, out Vector3 candidateNormal, out float candidatePenetration))
                    {
                        continue;
                    }

                    if (candidatePenetration > penetration)
                    {
                        penetration = candidatePenetration;
                        normal = candidateNormal;
                    }
                }
            }
        }

        return penetration > 0f && normal.sqrMagnitude > 0.001f;
    }

    private static bool TryGetCircleBoundsPenetration(
        Vector3 point,
        Vector3 fallbackVelocity,
        Bounds bounds,
        float radius,
        float radiusSqr,
        out Vector3 normal,
        out float penetration)
    {
        normal = Vector3.zero;
        penetration = 0f;

        bool insideX = point.x >= bounds.min.x && point.x <= bounds.max.x;
        bool insideZ = point.z >= bounds.min.z && point.z <= bounds.max.z;
        if (insideX && insideZ)
        {
            normal = GetClosestExitNormal(point, bounds, out float distanceToExit);
            penetration = radius + distanceToExit;
            return normal.sqrMagnitude > 0.001f;
        }

        Vector3 closest = bounds.ClosestPoint(point);
        closest.y = point.y;

        Vector3 delta = point - closest;
        delta.y = 0f;

        float sqrDistance = delta.sqrMagnitude;
        if (sqrDistance >= radiusSqr)
        {
            return false;
        }

        float distance = Mathf.Sqrt(sqrDistance);
        if (distance > 0.0001f)
        {
            normal = delta / distance;
        }
        else
        {
            normal = fallbackVelocity.sqrMagnitude > 0.0001f ? -fallbackVelocity.normalized : Vector3.back;
        }

        penetration = radius - distance;
        return true;
    }

    private static Vector3 GetClosestExitNormal(Vector3 point, Bounds bounds, out float distanceToExit)
    {
        float left = Mathf.Abs(point.x - bounds.min.x);
        float right = Mathf.Abs(bounds.max.x - point.x);
        float back = Mathf.Abs(point.z - bounds.min.z);
        float forward = Mathf.Abs(bounds.max.z - point.z);

        distanceToExit = left;
        Vector3 normal = Vector3.left;

        if (right < distanceToExit)
        {
            distanceToExit = right;
            normal = Vector3.right;
        }

        if (back < distanceToExit)
        {
            distanceToExit = back;
            normal = Vector3.back;
        }

        if (forward < distanceToExit)
        {
            distanceToExit = forward;
            normal = Vector3.forward;
        }

        return normal;
    }

    private SteeringMemory GetSteeringMemory(EnemyController agent)
    {
        if (agent == null)
        {
            return null;
        }

        if (!steeringMemoryByAgent.TryGetValue(agent, out SteeringMemory memory))
        {
            memory = new SteeringMemory();
            steeringMemoryByAgent[agent] = memory;
        }

        return memory;
    }

    private Vector3 GetStableTangent(EnemyController agent, SteeringMemory memory, Vector3 away, Vector3 seekDirection)
    {
        Vector3 tangent = Vector3.Cross(Vector3.up, away).normalized;
        int side = memory != null && memory.ObstacleSide != 0 && Time.time < memory.ObstacleSideUntil
            ? memory.ObstacleSide
            : ChooseObstacleSide(agent, tangent, seekDirection);

        if (memory != null)
        {
            memory.ObstacleSide = side;
            memory.ObstacleSideUntil = Time.time + obstacleSideMemory;
        }

        return side >= 0 ? tangent : -tangent;
    }

    private static int ChooseObstacleSide(EnemyController agent, Vector3 tangent, Vector3 seekDirection)
    {
        Vector3 currentDirection = agent != null ? agent.PlanarVelocity : Vector3.zero;
        currentDirection.y = 0f;
        if (currentDirection.sqrMagnitude <= 0.01f)
        {
            currentDirection = seekDirection;
        }
        else
        {
            currentDirection.Normalize();
        }

        float positiveScore = Vector3.Dot(tangent, seekDirection) + Vector3.Dot(tangent, currentDirection) * 0.35f;
        float negativeScore = Vector3.Dot(-tangent, seekDirection) + Vector3.Dot(-tangent, currentDirection) * 0.35f;

        if (Mathf.Abs(positiveScore - negativeScore) < 0.05f && agent != null)
        {
            return agent.GetInstanceID() % 2 == 0 ? 1 : -1;
        }

        return positiveScore >= negativeScore ? 1 : -1;
    }

    private static bool TryGetObstacleInPath(
        Bounds bounds,
        Vector3 position,
        Vector3 seekDirection,
        float lookAhead,
        float pathRadius,
        out Vector3 closestOnPath,
        out Vector3 closestOnBounds)
    {
        Vector3 center = bounds.center;
        center.y = position.y;

        float projectedDistance = Vector3.Dot(center - position, seekDirection);
        projectedDistance = Mathf.Clamp(projectedDistance, 0f, lookAhead);

        closestOnPath = position + seekDirection * projectedDistance;
        closestOnBounds = bounds.ClosestPoint(closestOnPath);

        Vector3 delta = closestOnPath - closestOnBounds;
        delta.y = 0f;

        return delta.sqrMagnitude <= pathRadius * pathRadius;
    }

    private static Vector2Int GetCell(Vector3 position, float size)
    {
        float safeSize = Mathf.Max(0.1f, size);
        return new Vector2Int(
            Mathf.FloorToInt(position.x / safeSize),
            Mathf.FloorToInt(position.z / safeSize)
        );
    }
}
