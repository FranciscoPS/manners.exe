using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 10f;
    
    private Rigidbody rb;
    private Vector2 moveInput;
    private Vector3 moveDirection;
    private float baseMoveSpeed;
    private float speedModifier = 0f; // Porcentaje de bonus (+10%, +20%, etc)

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
        }
        else
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        }
    }
}
