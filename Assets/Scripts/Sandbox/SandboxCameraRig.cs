using UnityEngine;

public class SandboxCameraRig : MonoBehaviour, ILateUpdateable
{
    private Transform target;
    private Vector3 offset = new Vector3(0f, 26f, -13f);
    private float damping = 8f;

    public bool IsActive => isActiveAndEnabled && target != null;

    public void Configure(Transform followTarget, Vector3 cameraOffset, float pitch, float dampingSpeed)
    {
        target = followTarget;
        offset = cameraOffset;
        damping = Mathf.Max(0.01f, dampingSpeed);

        transform.rotation = Quaternion.Euler(pitch, 0f, 0f);

        if (target != null)
            transform.position = target.position + offset;
    }

    private void OnEnable()
    {
        UpdateManager.Instance?.Register(this);
    }

    private void OnDisable()
    {
        UpdateManager.Instance?.Unregister(this);
    }

    public void OnLateUpdate(float deltaTime)
    {
        Vector3 desired = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-damping * deltaTime));
    }
}
