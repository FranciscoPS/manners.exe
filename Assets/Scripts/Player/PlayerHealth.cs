using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    private float maxHealth;
    private float currentHealth;
    private DamageTween damageTween;

    public event Action<float, float> OnHealthChanged;
    public event Action OnDamageTaken;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    private int consecutiveHits = 0;
    private float lastHitTime = -999f;
    private float consecutiveHitWindow = 1f;
    private bool isInvulnerable = false;
    private float invulnerabilityEndTime;
    private const float invulnerabilityDuration = 0.5f;
    private const int hitsForInvulnerability = 3;

    private void Start()
    {
        if (GameBalanceConfig.Instance != null)
        {
            maxHealth = GameBalanceConfig.Instance.PlayerMaxHealth;
        }
        else
        {
            maxHealth = 100f;
        }
        
        currentHealth = maxHealth;
        
        // Buscar DamageTween en este objeto o en hijos
        damageTween = GetComponentInChildren<DamageTween>();
        
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Update()
    {
        if (Time.time - lastHitTime > consecutiveHitWindow)
        {
            consecutiveHits = 0;
        }

        if (isInvulnerable && Time.time >= invulnerabilityEndTime)
        {
            isInvulnerable = false;
        }
    }

    public void TakeDamage(float damage)
    {
        if (isInvulnerable)
        {
            return;
        }

        consecutiveHits++;
        lastHitTime = Time.time;

        if (consecutiveHits >= hitsForInvulnerability)
        {
            isInvulnerable = true;
            invulnerabilityEndTime = Time.time + invulnerabilityDuration;
            consecutiveHits = 0;
            Debug.Log("[PlayerHealth] Invulnerability activated after 3 consecutive hits!");
        }

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnDamageTaken?.Invoke();
        
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
    
    /// <summary>
    /// Aumenta la vida máxima y cura al jugador por esa cantidad
    /// </summary>
    public void AddMaxHealth(float amount)
    {
        maxHealth += amount;
        currentHealth += amount; // También cura al jugador
        
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        Debug.Log($"[PlayerHealth] Max health increased by {amount}. New max: {maxHealth}");
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
