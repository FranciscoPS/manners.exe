using UnityEngine;

public class ShopManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject shopPanel;

    [SerializeField] private ShopScript shopScript;

    public void OnExitButtonPressed()
    {
        shopScript.CloseShop();
    }
}
