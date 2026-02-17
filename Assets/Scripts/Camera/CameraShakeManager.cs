using UnityEngine;
using Unity.Cinemachine;

public class CameraShakeManager : MonoBehaviour
{
    public static CameraShakeManager Instance { get; private set; }

    [Header("Impulse Source")]
    [SerializeField] private CinemachineImpulseSource impulseSource;

    [Header("Shake Presets")]
    [SerializeField] private float lightShakeForce = 0.5f;
    [SerializeField] private float mediumShakeForce = 1f;
    [SerializeField] private float heavyShakeForce = 2f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        if (impulseSource == null)
        {
            impulseSource = GetComponent<CinemachineImpulseSource>();
        }
    }

    public void ShakeLight()
    {
        GenerateImpulse(lightShakeForce);
    }

    public void ShakeMedium()
    {
        GenerateImpulse(mediumShakeForce);
    }

    public void ShakeHeavy()
    {
        GenerateImpulse(heavyShakeForce);
    }

    public void Shake(float force)
    {
        GenerateImpulse(force);
    }

    private void GenerateImpulse(float force)
    {
        if (impulseSource != null)
        {
            impulseSource.GenerateImpulse(force);
        }
    }
}
