using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour, IUpdateable, IFixedUpdateable
{
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private Animator animator;

    private Rigidbody rb;

    private Vector2 moveInput;
    private Vector3 moveDirection;
    private float baseMoveSpeed;
    private float speedModifier = 0f;
    private bool isPlayingMoveSound = false;
    private float moveSoundTimer = 0f; // Porcentaje de bonus (+10%, +20%, etc)
    private float lastAnimatorSpeed = 1f; // Cache para evitar setear animator.speed cada frame

    // IUpdateable implementation
    public bool IsActive => gameObject.activeInHierarchy && enabled;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        if (GameBalanceConfig.Instance != null)
        {
            baseMoveSpeed = GameBalanceConfig.Instance.PlayerMoveSpeed;
        }
        else
        {
            baseMoveSpeed = 5f;
        }
    }

    private void OnEnable()
    {
        // Registrar con UpdateManager
        if (UpdateManager.Instance != null)
        {
            UpdateManager.Instance.Register(this as IUpdateable);
            UpdateManager.Instance.Register(this as IFixedUpdateable);
        }
    }

    private void OnDisable()
    {
        // Reset timer cuando se desactiva el controller
        isPlayingMoveSound = false;
        moveSoundTimer = 0f;
        
        // Unregister del UpdateManager
        if (UpdateManager.Instance != null)
        {
            UpdateManager.Instance.Unregister(this as IUpdateable);
            UpdateManager.Instance.Unregister(this as IFixedUpdateable);
        }
    }

    // IUpdateable implementation
    public void OnUpdate(float deltaTime)
    {
        // Reset timer si el juego está pausado
        if (Time.timeScale == 0f)
        {
            isPlayingMoveSound = false;
            moveSoundTimer = 0f;
        }
        
        // Reproducir sonido de movimiento en intervalos
        if (isPlayingMoveSound && MusicManager.Instance != null && SFXDatabase.Instance != null)
        {
            moveSoundTimer -= deltaTime;
            
            if (moveSoundTimer <= 0f)
            {
                MusicManager.Instance.PlaySFXOneShot(SFXDatabase.Instance.playerMoveSFX, SFXDatabase.Instance.playerMoveVolume);
                
                // Ajustar intervalo basado en velocidad (más rápido = menos intervalo)
                float speedMultiplier = GetSpeedMultiplier();
                moveSoundTimer = SFXDatabase.Instance.moveSoundInterval / speedMultiplier;
            }
        }
        
        // Ajustar velocidad de animación SOLO si cambió (evitar set innecesarios cada frame)
        if (animator != null)
        {
            float targetSpeed = GetSpeedMultiplier();
            if (Mathf.Abs(lastAnimatorSpeed - targetSpeed) > 0.01f)
            {
                animator.speed = targetSpeed;
                lastAnimatorSpeed = targetSpeed;
            }
        }
    }
    
    /// <summary>
    /// Aplica un modificador de velocidad en porcentaje
    /// </summary>
    public void ApplySpeedModifier(float percentageIncrease)
    {
        speedModifier = percentageIncrease;
        // Forzar actualización de animator speed en el próximo update
        lastAnimatorSpeed = -1f;
    }
    
    /// <summary>
    /// Obtiene el multiplicador de velocidad (1.0 = normal, 1.2 = +20%, etc)
    /// </summary>
    private float GetSpeedMultiplier()
    {
        return 1f + (speedModifier / 100f);
    }
    
    /// <summary>
    /// Calcula la velocidad final con todos los modificadores
    /// </summary>
    private float GetFinalMoveSpeed()
    {
        return baseMoveSpeed * GetSpeedMultiplier();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    // IFixedUpdateable implementation
    public void OnFixedUpdate(float fixedDeltaTime)
    {
        moveDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
        
        float currentSpeed = GetFinalMoveSpeed();
        
        if (moveDirection.magnitude >= 0.1f)
        {
            rb.linearVelocity = new Vector3(moveDirection.x * currentSpeed, rb.linearVelocity.y, moveDirection.z * currentSpeed);
            
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * fixedDeltaTime);

            if (animator != null)
                animator.SetBool("isWalking", true);

            if (!isPlayingMoveSound)
            {
                isPlayingMoveSound = true;
                moveSoundTimer = 0f; // Reproducir inmediatamente al empezar a caminar
            }
        }
        else
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);

            if (animator != null)
                animator.SetBool("isWalking", false);

            if (isPlayingMoveSound)
            {
                isPlayingMoveSound = false;
                moveSoundTimer = 0f;
            }
        }
    }
}
