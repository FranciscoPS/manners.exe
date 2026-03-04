using UnityEngine;

public class EnemyPoolable : MonoBehaviour, IPoolable
{
    private EnemyController enemyController;
    private EnemyHealth enemyHealth;
    private DamageTween damageTween;

    private void Awake()
    {
        enemyController = GetComponent<EnemyController>();
        enemyHealth = GetComponent<EnemyHealth>();
        damageTween = GetComponentInChildren<DamageTween>();
    }

    public void OnSpawn()
    {
        if (enemyHealth != null)
        {
            enemyHealth.ResetHealth();
        }

        if (damageTween != null)
        {
            damageTween.InitializeMaterial();
        }
    }

    public void OnDespawn()
    {

    }
}
