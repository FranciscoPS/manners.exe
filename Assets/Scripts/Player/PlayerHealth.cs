using System;
using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour, IUpdateable
{
    [SerializeField] private Animator animator;
    [SerializeField] private float animationHitDelay = 0.3f;
    [SerializeField] private GameObject gameOverPanel;

    private float maxHealth;
    private float currentHealth;
    private DamageTween damageTween;

    private Coroutine hitAnmationCorrutine;

    public event Action<float, float> OnHealthChanged;
    public event Action OnDamageTaken;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => isDead;

    private int consecutiveHits = 0;
    private float lastHitTime = -999f;
    private float consecutiveHitWindow = 1f;
    private bool isInvulnerable = false;
    private bool isDead = false;
    private float invulnerabilityEndTime;
    private const float invulnerabilityDuration = 0.5f;
    private const int hitsForInvulnerability = 3;
    private float currentAnimationSpeed;

    private int playerLayer;
    private int enemyLayer;

    public bool IsActive => gameObject.activeInHierarchy && enabled && !isDead;

    private void Awake()
    {

        playerLayer = gameObject.layer;
        enemyLayer = LayerMask.NameToLayer("Enemy");

        if (playerLayer >= 0 && enemyLayer >= 0)
        {
            Physics.IgnoreLayerCollision(playerLayer, enemyLayer, false);
        }
    }

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

        isDead = false;
        isInvulnerable = false;

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (UpdateManager.Instance != null)
        {
            UpdateManager.Instance.Register(this);
        }
    }

    private void OnDestroy()
    {
        if (isInvulnerable)
        {
            Physics.IgnoreLayerCollision(playerLayer, enemyLayer, false);
        }

        if (UpdateManager.Instance != null)
        {
            UpdateManager.Instance.Unregister(this);
        }
    }

    public void OnUpdate(float deltaTime)
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
        if (isDead) return;

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

        if (FloatingTextManager.Instance != null)
        {
            Vector3 textPosition = transform.position + Vector3.up * 2f;
            FloatingTextManager.Instance.ShowDamage(damage, textPosition);
        }

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnDamageTaken?.Invoke();
        GameEvents.TriggerPlayerDamaged(damage);

        if (MusicManager.Instance != null && SFXDatabase.Instance != null && SFXDatabase.Instance.playerDamageSFX != null)
        {
            float randomPitch = UnityEngine.Random.Range(SFXDatabase.Instance.playerDamagePitchRange.x, SFXDatabase.Instance.playerDamagePitchRange.y);
            MusicManager.Instance.PlaySFXOneShot(SFXDatabase.Instance.playerDamageSFX, SFXDatabase.Instance.playerDamageVolume, randomPitch);
        }

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

    public void AddMaxHealth(float amount)
    {
        maxHealth += amount;
        currentHealth += amount;

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        if (MusicManager.Instance != null && SFXDatabase.Instance != null && SFXDatabase.Instance.playerDeathSFX != null)
        {
            MusicManager.Instance.PlaySFXOneShot(SFXDatabase.Instance.playerDeathSFX, SFXDatabase.Instance.playerDeathVolume);
        }

        PlayerController controller = GetComponent<PlayerController>();
        if (controller != null)
            controller.enabled = false;

        PlayerExperience exp = GetComponent<PlayerExperience>();
        if (exp != null)
            exp.enabled = false;

        AutoAttackSystem autoAttack = GetComponent<AutoAttackSystem>();
        if (autoAttack != null)
            autoAttack.enabled = false;

        Physics.IgnoreLayerCollision(playerLayer, enemyLayer, true);

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            rb.useGravity = false;
            rb.isKinematic = true;
        }

        if (animator != null)
            animator.SetTrigger("isPlayerDead");

        if (hitAnmationCorrutine != null)
        {
            StopCoroutine(hitAnmationCorrutine);
            hitAnmationCorrutine = null;
        }

        animator.speed = 1f;
        StartCoroutine(WaitForDeathAnimation());

    }

    private IEnumerator WaitForDeathAnimation()
    {
        yield return new WaitForSecondsRealtime(4.2f);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        Time.timeScale = 0f;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isDead) return;

        if (collision.gameObject.CompareTag("Enemy"))
        {
            EnemyController enemy = collision.gameObject.GetComponent<EnemyController>();
            if (enemy != null)
            {
                TakeDamage(enemy.ContactDamage);

                if (!isDead)
                {
                    if (hitAnmationCorrutine != null)
                        StopCoroutine(hitAnmationCorrutine);

                    hitAnmationCorrutine = StartCoroutine(ResetAnimationSpeed());
                }
            }
        }
    }

    private IEnumerator ResetAnimationSpeed()
    {
        if (isDead) yield break;
        animator.speed = 0f;
        currentAnimationSpeed = animator.speed;

        yield return new WaitForSeconds(animationHitDelay);

        if (!isDead)
            animator.speed = 1f;

        hitAnmationCorrutine = null;

    }
}
