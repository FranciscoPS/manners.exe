using UnityEngine;

/// <summary>
/// Se a\u00f1ade al cofre instanciado. Detecta por distancia cuando el jugador se
/// acerca; al recogerlo, abre la selecci\u00f3n del cofre y desaparece.
/// </summary>
[DisallowMultipleComponent]
public class ChestPickup : MonoBehaviour, IUpdateable
{
    [Tooltip("Radio horizontal (XZ) para recoger el cofre. El cofre tiene escala 2, así que conviene 3+.")]
    [SerializeField] private float pickupRadius = 3f;

    [Header("Bob (salto visual)")]
    [SerializeField] private float bobHeight = 0.6f;
    [SerializeField] private float bobSpeed = 4.5f;

    private Transform player;
    private bool opened = false;
    private float nextCheckTime = 0f;
    private const float CheckInterval = 0.1f;

    private Vector3 basePosition;
    private bool baseCaptured = false;

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

        // Rebote vertical para que el cofre destaque en el mapa.
        if (!baseCaptured)
        {
            basePosition = transform.position;
            baseCaptured = true;
        }
        float bob = Mathf.Abs(Mathf.Sin(Time.time * bobSpeed)) * bobHeight;
        transform.position = basePosition + Vector3.up * bob;

        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (player == null) return;
        }

        if (Time.time < nextCheckTime) return;
        nextCheckTime = Time.time + CheckInterval;

        // Distancia solo en el plano horizontal: el salto vertical del cofre
        // y la altura del jugador no deben afectar la recogida.
        float dx = transform.position.x - player.position.x;
        float dz = transform.position.z - player.position.z;
        float sqrDistance = dx * dx + dz * dz;
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
