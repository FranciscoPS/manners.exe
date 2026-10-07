using UnityEngine;

public class BuildingDestroyedVisual : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool useDestroyedVisual = true;

    [Header("Visuals")]
    [SerializeField] private GameObject normalVisual;
    [SerializeField] private GameObject destroyedVisual;

    [Header("Physics Pieces")]
    [SerializeField] private Rigidbody[] physicsPieces;

    [Header("Physics")]
    [SerializeField] private float pushForce = 8f;
    [SerializeField] private float upwardForce = 2f;
    [SerializeField] private float randomTorque = 5f;

    public bool UseDestroyedVisual => useDestroyedVisual;

    private void Awake()
    {
        if (!useDestroyedVisual)
            return;

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
        if (!useDestroyedVisual)
            return;

        if (normalVisual != null)
            normalVisual.SetActive(false);

        if (destroyedVisual != null)
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
