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
        if (playerInRange && !shopOpen && openShopAction.triggered)
        {
            OpenShop();
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
    }
}
