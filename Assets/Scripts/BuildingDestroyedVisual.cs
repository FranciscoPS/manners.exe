using UnityEngine;

public class BuildingDestroyedVisual : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private GameObject normalVisual;
    [SerializeField] private GameObject destroyedVisual;

    [Header("Physics Pieces")]
    [SerializeField] private Rigidbody[] physicsPieces;

    [Header("Physics")]
    [SerializeField] private float pushForce = 8f;
    [SerializeField] private float upwardForce = 2f;
    [SerializeField] private float randomTorque = 5f;

    private void Awake()
    {
        if (destroyedVisual != null)
            destroyedVisual.SetActive(false);

        foreach (Rigidbody rb in physicsPieces)
        {
            if (rb == null)
                continue;

            rb.isKinematic = true;
            rb.useGravity = true;
        }
    }

    public void DestroyBuilding(Vector3 impactDirection)
    {
        normalVisual.SetActive(false);
        destroyedVisual.SetActive(true);

        foreach (Rigidbody rb in physicsPieces)
        {
            if (rb == null)
                continue;

            rb.isKinematic = false;

            Vector3 force =
                impactDirection.normalized * pushForce +
                Vector3.up * upwardForce;

            rb.AddForce(force, ForceMode.Impulse);

            rb.AddTorque(Random.insideUnitSphere * randomTorque, ForceMode.Impulse);
        }
    }
}
