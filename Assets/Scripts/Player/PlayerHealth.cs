using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    
    private float currentHealth;
    private DamageTween damageTween;

    private void Start()
    {
        currentHealth = maxHealth;
        damageTween = GetComponent<DamageTween>();
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        
        if (CameraShakeManager.Instance != null)
        {
            CameraShakeManager.Instance.ShakeMedium();
        }
        
        if (damageTween != null)
        {
            damageTween.TweenFx();
        }
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("Player Died");
        Time.timeScale = 0f;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            EnemyController enemy = collision.gameObject.GetComponent<EnemyController>();
            if (enemy != null)
            {
                TakeDamage(enemy.ContactDamage);
            }
        }
    }
}
