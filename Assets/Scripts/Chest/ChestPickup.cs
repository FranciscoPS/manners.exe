using UnityEngine;

/// <summary>
/// Se a\u00f1ade al cofre instanciado. Detecta por distancia cuando el jugador se
/// acerca; al recogerlo, abre la selecci\u00f3n del cofre y desaparece.
/// </summary>
[DisallowMultipleComponent]
public class ChestPickup : MonoBehaviour, IUpdateable
{
    [SerializeField] private float pickupRadius = 1.8f;

    private Transform player;
    private bool opened = false;
    private float nextCheckTime = 0f;
    private const float CheckInterval = 0.1f;

    public bool IsActive => !opened && gameObject.activeInHierarchy;

    private void OnEnable()
    {
        opened = false;
        if (UpdateManager.Instance != null)
            UpdateManager.Instance.Register(this);
    }

    private void OnDisable()
    {
        if (UpdateManager.Instance != null)
            UpdateManager.Instance.Unregister(this);
    }

    public void OnUpdate(float deltaTime)
    {
        if (opened) return;

        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (player == null) return;
        }

        if (Time.time < nextCheckTime) return;
        nextCheckTime = Time.time + CheckInterval;

        float sqrDistance = (transform.position - player.position).sqrMagnitude;
        if (sqrDistance <= pickupRadius * pickupRadius)
        {
            Open();
        }
    }

    private void Open()
    {
        if (opened) return;
        opened = true;

        ChestSpawner.NotifyChestCollected();

        if (LevelUpManager.Instance != null)
        {
            LevelUpManager.Instance.ShowChestSelection();
        }

        if (UpdateManager.Instance != null)
            UpdateManager.Instance.Unregister(this);

        Destroy(gameObject);
    }
}
