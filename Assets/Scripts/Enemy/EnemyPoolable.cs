using UnityEngine;

public class EnemyPoolable : MonoBehaviour, IPoolable
{
    private EnemyController enemyController;
    private EnemyHealth enemyHealth;

    private void Awake()
    {
        enemyController = GetComponent<EnemyController>();
        enemyHealth = GetComponent<EnemyHealth>();
    }

    public void OnSpawn()
    {
        if (enemyHealth != null)
        {
            enemyHealth.ResetHealth();
        }
    }

    public void OnDespawn()
    {
        
    }
}
