using System.Collections.Generic;
using UnityEngine;

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
    private bool selectionOpen = false;
    private bool lastWasInRange = false;
    private float nextCheckTime = 0f;
    private const float CheckInterval = 0.1f;

    private Vector3 basePosition;
    private bool baseCaptured = false;

    private ChestItemData chosenItem;
    private bool hasOpenedOnce = false;

    public bool IsActive => !opened && gameObject.activeInHierarchy;

    private void OnEnable()
    {
        opened = false;
        selectionOpen = false;
        lastWasInRange = false;
        hasOpenedOnce = false;
        if (UpdateManager.Instance != null)
            UpdateManager.Instance.Register(this);

        List<ChestItemData> items = ChestItemProvider.GetRandomItems(1);
        if (items != null && items.Count > 0)
            chosenItem = items[0];
        else
            chosenItem = null;

        Animator chestAnimator = GetComponentInChildren<Animator>();
        if (chestAnimator != null)
            chestAnimator.speed = 0f;
    }

    private void OnDisable()
    {
        if (UpdateManager.Instance != null)
            UpdateManager.Instance.Unregister(this);
    }

    public void OnUpdate(float deltaTime)
    {
        if (opened) return;

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

        float dx = transform.position.x - player.position.x;
        float dz = transform.position.z - player.position.z;
        float sqrDistance = dx * dx + dz * dz;
        bool isInRange = sqrDistance <= pickupRadius * pickupRadius;

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

        if (hasOpenedOnce)
        {
            if (LevelUpManager.Instance != null)
                LevelUpManager.Instance.ShowChestSelection(chosenItem);
            return;
        }

        hasOpenedOnce = true;

        ChestOpeningSequence.Play(chosenItem, gameObject, () =>
        {
            if (LevelUpManager.Instance != null)
                LevelUpManager.Instance.ShowChestSelection(chosenItem);
        });
    }

    public void OnSelectionClosed()
    {
        selectionOpen = false;

    }

    public void OnCollected()
    {
        if (opened) return;
        opened = true;

        if (UpdateManager.Instance != null)
            UpdateManager.Instance.Unregister(this);

        Destroy(gameObject);
    }
}
