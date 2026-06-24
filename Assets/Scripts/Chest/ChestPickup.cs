using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Se añade al cofre instanciado. Detecta por distancia cuando el jugador se
/// acerca; al acercarse por primera vez abre la UI de selección (sin destruir
/// el cofre). El ítem ofrecido se elige al spawn y se guarda en la instancia:
/// cerrar la UI (Espacio) no cambia el ítem hasta que el jugador confirme.
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
    private bool lastWasInRange = false; // para detectar transición fuera->dentro y evitar reabrir inmediatamente
    private float nextCheckTime = 0f;
    private const float CheckInterval = 0.1f;

    private Vector3 basePosition;
    private bool baseCaptured = false;

    // Item elegido para este cofre (persistente mientras el cofre exista).
    private ChestItemData chosenItem;

    public bool IsActive => !opened && gameObject.activeInHierarchy;

    private void OnEnable()
    {
        opened = false;
        selectionOpen = false;
        lastWasInRange = false;
        if (UpdateManager.Instance != null)
            UpdateManager.Instance.Register(this);

        // Elegir y guardar el ítem del cofre en el momento del spawn.
        List<ChestItemData> items = ChestItemProvider.GetRandomItems(1);
        if (items != null && items.Count > 0)
            chosenItem = items[0];
        else
            chosenItem = null;
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

        // Distancia solo en el plano horizontal
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

        // Reutiliza LevelUpManager para mostrar la selección de cofre con el item ya elegido.
        if (LevelUpManager.Instance != null)
        {
            LevelUpManager.Instance.ShowChestSelection(chosenItem);
        }

        // NO destruimos ni notificamos al ChestSpawner aquí: el cofre permanece hasta
        // que el jugador CONFIRME la mejora.
    }

    /// <summary>
    /// Llamado por LevelUpManager cuando la UI se cierra sin elegir la mejora.
    /// </summary>
    public void OnSelectionClosed()
    {
        selectionOpen = false;
        // Mantener chosenItem tal cual: persistirá hasta que el jugador confirme la mejora.
        // Se requiere salir/volver a entrar (o lógica distinta) para reabrir.
    }

    /// <summary>
    /// Llamado por ChestSpawner cuando el jugador confirma y la mejora se aplica.
    /// Marca el cofre como recogido y lo destruye.
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
