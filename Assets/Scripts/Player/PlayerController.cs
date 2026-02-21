using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
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

    private void OnDisable()
    {
        // Reset timer cuando se desactiva el controller
        isPlayingMoveSound = false;
        moveSoundTimer = 0f;
    }

    private void Update()
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
            moveSoundTimer -= Time.deltaTime;
            
            if (moveSoundTimer <= 0f)
            {
                MusicManager.Instance.PlaySFXOneShot(SFXDatabase.Instance.playerMoveSFX, SFXDatabase.Instance.playerMoveVolume);
                
                // Ajustar intervalo basado en velocidad (más rápido = menos intervalo)
                float currentSpeed = GetFinalMoveSpeed();
                float speedRatio = Mathf.Clamp01((currentSpeed - baseMoveSpeed) / (baseMoveSpeed * 0.5f));
                float speedMultiplier = 1f + (speedRatio * 0.5f); // 1.0 a 1.5 basado en velocidad
                moveSoundTimer = SFXDatabase.Instance.moveSoundInterval / speedMultiplier;
            }
        }
    }
    
    /// <summary>
    /// Aplica un modificador de velocidad en porcentaje
    /// </summary>
    public void ApplySpeedModifier(float percentageIncrease)
    {
        speedModifier = percentageIncrease;
        Debug.Log($"[PlayerController] Speed modifier set to +{percentageIncrease}%");
    }
    
    /// <summary>
    /// Calcula la velocidad final con todos los modificadores
    /// </summary>
    private float GetFinalMoveSpeed()
    {
        return baseMoveSpeed * (1f + speedModifier / 100f);
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    private void FixedUpdate()
    {
        moveDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
        
        float currentSpeed = GetFinalMoveSpeed();
        
        if (moveDirection.magnitude >= 0.1f)
        {
            rb.linearVelocity = new Vector3(moveDirection.x * currentSpeed, rb.linearVelocity.y, moveDirection.z * currentSpeed);
            
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);

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
