using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class FlockingObstacle : MonoBehaviour
{
    [SerializeField] private bool includeChildColliders = true;
    [SerializeField] private Collider[] explicitColliders;

    private Collider[] cachedColliders;

    public IReadOnlyList<Collider> Colliders
    {
        get
        {
            if (cachedColliders == null)
            {
                RefreshColliders();
            }

            return cachedColliders;
        }
    }

    private void Awake()
    {
        RefreshColliders();
    }

    private void OnEnable()
    {
        RefreshColliders();
        EnemyFlockManager.Instance.RegisterObstacle(this);
    }

    private void OnDisable()
    {
        if (EnemyFlockManager.HasInstance)
        {
            EnemyFlockManager.Instance.UnregisterObstacle(this);
        }
    }

    public void RefreshColliders()
    {
        if (explicitColliders != null && explicitColliders.Length > 0)
        {
            cachedColliders = explicitColliders;
        }
        else
        {
            cachedColliders = includeChildColliders
                ? GetComponentsInChildren<Collider>(false)
                : GetComponents<Collider>();
        }

        if (EnemyFlockManager.HasInstance)
        {
            EnemyFlockManager.Instance.MarkObstacleGridDirty();
        }
    }
}
