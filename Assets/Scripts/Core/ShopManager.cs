using UnityEngine;
using System.Collections.Generic;

public class ShopManager : MonoBehaviour
{
    private static ShopManager instance;
    public static ShopManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<ShopManager>();
            }
            return instance;
        }
    }

    [Header("Shop References")]
    [SerializeField] private List<ShopScript> allShops = new List<ShopScript>();

    [Header("Rotation Settings")]
    [SerializeField] private bool rotateOnPurchase = true;
    [SerializeField] private bool randomRotation = false;

    private int currentActiveShopIndex = 0;
    private ShopScript currentActiveShop = null;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    private void Start()
    {
        InitializeShops();
    }

    private void InitializeShops()
    {
        if (allShops.Count == 0)
        {
            ShopScript[] foundShops = FindObjectsByType<ShopScript>(FindObjectsSortMode.None);
            allShops = new List<ShopScript>(foundShops);
        }

        if (allShops.Count == 0)
        {
            return;
        }

        for (int i = 0; i < allShops.Count; i++)
        {
            if (allShops[i] != null)
            {
                allShops[i].SetShopIndex(i);
                allShops[i].SetActive(false);
            }
        }

        if (randomRotation)
        {
            currentActiveShopIndex = Random.Range(0, allShops.Count);
        }
        else
        {
            currentActiveShopIndex = 0;
        }

        ActivateShop(currentActiveShopIndex);
    }

    public void RegisterShop(ShopScript shop)
    {
        if (!allShops.Contains(shop))
        {
            allShops.Add(shop);
        }
    }

    public void OnShopPurchaseMade()
    {
        if (!rotateOnPurchase || allShops.Count <= 1)
            return;

        int previousShopIndex = currentActiveShopIndex;

        if (randomRotation)
        {
            int newIndex = Random.Range(0, allShops.Count);
            while (newIndex == currentActiveShopIndex && allShops.Count > 1)
            {
                newIndex = Random.Range(0, allShops.Count);
            }
            currentActiveShopIndex = newIndex;
        }
        else
        {
            currentActiveShopIndex = (currentActiveShopIndex + 1) % allShops.Count;
        }

        if (currentActiveShop != null)
        {
            currentActiveShop.SetActive(false);
        }

        ActivateShop(currentActiveShopIndex);

        GameEvents.TriggerShopLocationChanged(currentActiveShopIndex);
    }

    private void ActivateShop(int index)
    {
        if (index < 0 || index >= allShops.Count)
        {
            return;
        }

        ShopScript shop = allShops[index];
        if (shop != null)
        {
            shop.SetActive(true);
            currentActiveShop = shop;
        }
    }

    public ShopScript GetActiveShop()
    {
        return currentActiveShop;
    }

    public int GetActiveShopIndex()
    {
        return currentActiveShopIndex;
    }

    public int GetTotalShopCount()
    {
        return allShops.Count;
    }

    public bool IsShopAvailable()
    {
        if (LevelUpManager.Instance == null)
            return false;

        return LevelUpManager.Instance.IsShopAvailable();
    }
}
