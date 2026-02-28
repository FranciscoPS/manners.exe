using UnityEngine;
using UnityEngine.InputSystem;

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
    private const float REOPEN_COOLDOWN = 0.2f;
    private int shopIndex = -1;

    private InputAction openShopAction;

    private void Awake()
    {
        openShopAction = new InputAction(
            name: "OpenShop",
            binding: "<Keyboard>/p"
        );

        if (levelUpManager == null)
        {
            levelUpManager = FindFirstObjectByType<LevelUpManager>();
        }

        if (levelUpManager != null)
        {
            levelUpManager.RegisterShop(this);
        }

        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.RegisterShop(this);
        }
    }

    private void OnEnable()
    {
        if (openShopAction != null)
        {
            openShopAction.Enable();
        }
    }

    private void OnDisable()
    {
        if (openShopAction != null)
        {
            openShopAction.Disable();
        }
    }

    private void Start()
    {
        if (interactionText != null)
        {
            interactionText.SetActive(false);
        }

        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (openShopAction == null || !openShopAction.triggered)
            return;

        if (shopOpen)
        {
            CloseShop();
        }
        else if (playerInRange)
        {
            if (levelUpManager != null &&
                !levelUpManager.IsLevelUpActive() &&
                Time.unscaledTime - lastCloseTime >= REOPEN_COOLDOWN)
            {
                OpenShop();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            if (interactionText != null)
            {
                interactionText.SetActive(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            if (interactionText != null)
            {
                interactionText.SetActive(false);
            }
        }
    }

    private void OpenShop()
    {
        shopOpen = true;
        if (interactionText != null)
        {
            interactionText.SetActive(false);
        }

        if (levelUpManager != null)
        {
            levelUpManager.ShowShop();
        }
    }

    public void CloseShop()
    {
        shopOpen = false;
        lastCloseTime = Time.unscaledTime;
        playerInRange = false;

        if (levelUpManager != null)
        {
            levelUpManager.CloseLevelUp();
        }
    }

    public void OnShopClosed()
    {
        shopOpen = false;
        lastCloseTime = Time.unscaledTime;
    }

    public void SetActive(bool active)
    {
        if (!active && shopOpen)
        {
            shopOpen = false;
            if (levelUpManager != null)
            {
                levelUpManager.CloseLevelUp();
            }
        }

        gameObject.SetActive(active);
    }

    public void SetShopIndex(int index)
    {
        shopIndex = index;
    }

    public int GetShopIndex()
    {
        return shopIndex;
    }

    public bool IsActive()
    {
        return gameObject.activeSelf;
    }
}
