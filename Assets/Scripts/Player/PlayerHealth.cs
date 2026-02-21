using System;
using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private float animationHitDelay = 0.3f;

    private float maxHealth;
    private float currentHealth;
    private DamageTween damageTween;

    private IEnumerator hitAnmationCorrutine; 

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
    private float currentAnimationSpeed; 

    private int playerLayer;
    private int enemyLayer;

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
        
        damageTween = GetComponentInChildren<DamageTween>();
        
        playerLayer = gameObject.layer;
        enemyLayer = LayerMask.NameToLayer("Enemy");
        
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void OnDestroy()
    {
        if (isInvulnerable)
        {
            Physics.IgnoreLayerCollision(playerLayer, enemyLayer, false);
        }
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
            Physics.IgnoreLayerCollision(playerLayer, enemyLayer, false);
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
            Physics.IgnoreLayerCollision(playerLayer, enemyLayer, true);
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
        currentHealth += amount;
        
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Die()
    {
        if (isInvulnerable)
        {
            Physics.IgnoreLayerCollision(playerLayer, enemyLayer, false);
        }

        if (animator != null)
            animator.SetBool("isDead", true);

        StartCoroutine(WaitForDeathAnimation());
    
    }

    private IEnumerator WaitForDeathAnimation()
    {
        yield return new WaitForSeconds(4f);
        Time.timeScale = 0f;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            EnemyController enemy = collision.gameObject.GetComponent<EnemyController>();
            if (enemy != null)
            {
                currentAnimationSpeed = animator.speed;
                TakeDamage(enemy.ContactDamage);

                if (hitAnmationCorrutine == null)
                {
                    hitAnmationCorrutine = ResetAnimationSpeed();
                    StartCoroutine(hitAnmationCorrutine);
                }

                hitAnmationCorrutine = ResetAnimationSpeed();
                StartCoroutine(hitAnmationCorrutine);
            }
        }
    }

    private IEnumerator ResetAnimationSpeed()
    {
        currentAnimationSpeed = animator.speed;
        animator.speed = 0f;

        yield return new WaitForSeconds(animationHitDelay);

        animator.speed = 1f;
        hitAnmationCorrutine = null;

    }
}
