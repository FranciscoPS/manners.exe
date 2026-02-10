using UnityEngine;
using System;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    [Header("Current Session")]
    private int currentCoins = 0;
    private int currentDiamonds = 0;

    public event Action<int> OnCoinsChanged;
    public event Action<int> OnDiamondsChanged;

    public int CurrentCoins => currentCoins;
    public int CurrentDiamonds => currentDiamonds;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddCoins(int amount)
    {
        currentCoins += amount;
        OnCoinsChanged?.Invoke(currentCoins);
    }

    public bool SpendCoins(int amount)
    {
        if (currentCoins >= amount)
        {
            currentCoins -= amount;
            OnCoinsChanged?.Invoke(currentCoins);
            return true;
        }
        return false;
    }

    public void AddDiamonds(int amount)
    {
        currentDiamonds += amount;
        OnDiamondsChanged?.Invoke(currentDiamonds);
    }

    public bool SpendDiamonds(int amount)
    {
        if (currentDiamonds >= amount)
        {
            currentDiamonds -= amount;
            OnDiamondsChanged?.Invoke(currentDiamonds);
            return true;
        }
        return false;
    }

    public void ResetSessionCurrency()
    {
        currentCoins = 0;
        currentDiamonds = 0;
        OnCoinsChanged?.Invoke(currentCoins);
        OnDiamondsChanged?.Invoke(currentDiamonds);
    }
}