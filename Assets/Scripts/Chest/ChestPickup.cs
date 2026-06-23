using UnityEngine;

/// <summary>
/// Se añade al cofre instanciado. Detecta por distancia cuando el jugador se
/// acerca; al recogerlo, abre la selección del cofre y NO desaparece hasta que
/// el jugador confirme la mejora. Si el jugador cierra la UI, el cofre permanece
/// en el mapa y sólo reabrirá cuando el jugador salga y vuelva a entrar en rango.
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
    private bool opened = false; // verdadero cuando el cofre fue finalmente recogido/consumido
    private bool selectionOpen = false; // verdadero mientras la UI del cofre esté visible
    private bool lastWasInRange = false; // para detectar transición de fuera->dentro y evitar reabrir inmediatamente
    private float nextCheckTime = 0f;
    private const float CheckInterval = 0.1f;

    private Vector3 basePosition;
    private bool baseCaptured = false;

    public bool IsActive => !opened && gameObject.activeInHierarchy;

    private void OnEnable()
    {
        opened = false;
        selectionOpen = false;
        lastWasInRange = false;
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
        bool isInRange = sqrDistance <= pickupRadius * pickupRadius;

        // Abrir SOLO al entrar en rango (transición fuera->dentro) y si no hay ya UI abierta.
        if (isInRange && !lastWasInRange && !selectionOpen)
        {
            Open();
        }

        lastWasInRange = isInRange;
    }

    private void Open()
    {
        if (opened || selectionOpen) return;

        selectionOpen = true;

        // Reutiliza LevelUpManager para mostrar la selección de cofre.
        if (LevelUpManager.Instance != null)
        {
            LevelUpManager.Instance.ShowChestSelection();
        }

        // No destruimos ni notificamos al ChestSpawner aquí: el cofre permanecerá hasta
        // que el jugador confirme la elección (LevelUpManager.OnUpgradeChosen llamará a ChestSpawner.CollectActiveChest()).
    }

    /// <summary>
    /// Llamado por ChestSpawner (a través de LevelUpManager) cuando la UI se cierra
    /// sin elegir la mejora, para permitir reabrir sólo tras salir/volver a entrar.
    /// </summary>
    public void OnSelectionClosed()
    {
        selectionOpen = false;
        // se deja lastWasInRange tal cual: requiere que el jugador salga y vuelva a entrar
        // para reabrir (evita reabrir inmediatamente mientras sigue en el mismo punto).
    }

    /// <summary>
    /// Llamado por ChestSpawner (a través de LevelUpManager) cuando la mejora fue tomada.
    /// Marca el cofre como recogido, limpia referencias y destruye objeto.
    /// </summary>
    public void OnCollected()
    {
        if (opened) return;
        opened = true;

        if (UpdateManager.Instance != null)
            UpdateManager.Instance.Unregister(this);

        Destroy(gameObject);
    }
}
