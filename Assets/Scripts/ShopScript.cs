using UnityEngine;
using UnityEngine.InputSystem;

public class ShopScript : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject interactionText;

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

        Time.timeScale = 0f;

        interactionText.SetActive(false);
    }
}
