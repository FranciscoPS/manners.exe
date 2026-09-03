using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

[DisallowMultipleComponent]
public class ChestPickup : MonoBehaviour, IUpdateable
{
    [Tooltip("Radio horizontal (XZ) para recoger el cofre. El cofre tiene escala 2, así que conviene 3+.")]
    [SerializeField] private float pickupRadius = 3f;

    [Header("Salto (squash & stretch)")]
    [SerializeField] private SquashStretchBounceSettings bounce = new SquashStretchBounceSettings
    {
        jumpHeight = 0.6f,
        jumpDuration = 0.5f,
        squashAmount = 0.25f,
        stretchAmount = 0.2f,
        anticipationDuration = 0.1f,
        recoverDuration = 0.3f,
        restBetweenJumps = 0.15f
    };

    private Transform player;
    private bool opened = false;
    private bool selectionOpen = false;
    private bool lastWasInRange = false;
    private float nextCheckTime = 0f;
    private const float CheckInterval = 0.1f;

    private Vector3 baseScale;
    private float baseLocalY;
    private bool baseCaptured = false;
    private Sequence bounceTween;

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

        StopBounce();
        if (baseCaptured)
            SquashStretchBounce.ResetPose(transform, baseScale, baseLocalY);
    }

    public void OnUpdate(float deltaTime)
    {
        if (opened) return;

        if (!baseCaptured)
        {
            baseScale = transform.localScale;
            baseLocalY = transform.localPosition.y;
            baseCaptured = true;
        }

        if (bounceTween == null && !selectionOpen)
            bounceTween = SquashStretchBounce.PlayLoop(transform, bounce, baseScale, baseLocalY);

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
        StopBounce();
        if (baseCaptured)
            SquashStretchBounce.Settle(transform, baseScale, baseLocalY);

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

        StopBounce();
        Destroy(gameObject);
    }

    private void StopBounce()
    {
        if (bounceTween == null) return;

        bounceTween.Kill();
        bounceTween = null;
    }
}
