using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

public class ShopScript : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject interactionText;
    [SerializeField] private GameObject shopPanel;
    
    [Header("Managers")]
    [SerializeField] private LevelUpManager levelUpManager;

    private bool playerInRange = false;
    private bool shopOpen = false;
    private float lastCloseTime = -999f;
    private const float REOPEN_COOLDOWN = 0.2f; // Cooldown para evitar reabrir inmediatamente

    private InputAction openShopAction;

    private void Awake()
    {
        openShopAction = new InputAction(
            name: "OpenShop",
            binding: "<Keyboard>/p"
        );
    }

    private void OnEnable()
    {
        openShopAction.Enable();
    }

    private void OnDisable()
    {
        openShopAction.Disable();
    }

    private void Start()
    {
        interactionText.SetActive(false);
        shopPanel.SetActive(false);
        
        if (levelUpManager == null)
        {
            levelUpManager = FindFirstObjectByType<LevelUpManager>();
        }
        
        // Registrar este shop con el LevelUpManager
        if (levelUpManager != null)
        {
            levelUpManager.RegisterShop(this);
        }
    }

    private void Update()
    {
        if (openShopAction.triggered)
        {
            // Si la tienda está abierta, cerrarla
            if (shopOpen)
            {
                CloseShop();
            }
            // Si no está abierta y el jugador está en rango, abrirla
            else if (playerInRange)
            {
                // Verificar que el LevelUpManager no esté activo y que haya pasado el cooldown
                if (levelUpManager != null && 
                    !levelUpManager.IsLevelUpActive() && 
                    Time.unscaledTime - lastCloseTime >= REOPEN_COOLDOWN)
                {
                    OpenShop();
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            interactionText.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            interactionText.SetActive(false);
        }
    }
    
    private void OpenShop()
    {
        shopOpen = true;
        interactionText.SetActive(false);
        
        // Use LevelUpManager in Shop mode
        if (levelUpManager != null)
        {
            levelUpManager.ShowShop();
        }
    }

    public void CloseShop()
    {
        shopOpen = false;
        lastCloseTime = Time.unscaledTime;

        if (levelUpManager != null)
        {
            levelUpManager.CloseLevelUp();
        }
    }
    
    /// <summary>
    /// Llamado por LevelUpManager cuando la tienda se cierra
    /// </summary>
    public void OnShopClosed()
    {
        shopOpen = false;
        lastCloseTime = Time.unscaledTime;
    }
}
