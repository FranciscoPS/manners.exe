using UnityEngine;

public class ShopScript : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject interactionText;

    private void Start()
    {
        interactionText.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            interactionText.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            interactionText.SetActive(false);
        }
    }
}
