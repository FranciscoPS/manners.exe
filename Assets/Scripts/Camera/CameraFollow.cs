using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 15f, -10f);
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private float lookDownAngle = 45f;

    private void Start()
    {
        transform.rotation = Quaternion.Euler(lookDownAngle, 0f, 0f);
    }

    private void LateUpdate()
    {
        if (target != null)
        {
            Vector3 desiredPosition = target.position + offset;
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
            transform.position = smoothedPosition;
        }
    }
}
