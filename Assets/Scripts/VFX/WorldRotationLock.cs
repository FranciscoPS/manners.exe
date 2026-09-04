using UnityEngine;

public class WorldRotationLock : MonoBehaviour, ILateUpdateable
{
    [Tooltip("Rotación (grados) que el objeto mantiene respecto al mundo, sin importar cómo gire su padre (por ejemplo, el jugador).")]
    [SerializeField] private Vector3 worldEulerAngles = Vector3.zero;

    public bool IsActive => isActiveAndEnabled;

    private void OnEnable()
    {
        Apply();
        UpdateManager.Instance?.Register(this);
    }

    private void OnDisable()
    {
        UpdateManager.Instance?.Unregister(this);
    }

    public void OnLateUpdate(float deltaTime)
    {
        Apply();
    }

    private void Apply()
    {
        transform.rotation = Quaternion.Euler(worldEulerAngles);
    }
}
