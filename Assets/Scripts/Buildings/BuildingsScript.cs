using System.Collections;
using UnityEngine;

public class BuildingsScript : MonoBehaviour
{
    [Header("Destruction Settings")]
    [SerializeField] private float sinkSpeed = 1.5f;
    [SerializeField] private GameObject visual;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float groundLevel = 0f;
    [SerializeField] private float sinkExtraDistance = 1f;

    [Header("Shader Destruction")]
    [SerializeField] private float shaderDestructionSpeed = 1f;
    [SerializeField] private DustBurst dustPrefab;

    [Header("Shake Settings")]
    [SerializeField] private float shakeIntensity = 0.25f;
    [SerializeField] private float shakeFrequency = 25f;

    [Header("Destruction Feedback")]
    [SerializeField] private float shakeForce = 0.5f;

    [Header("Drop Settings")]
    [SerializeField] private float dropSpawnDelay = 1f;

    [Header("Experience Orb Settings")]
    [SerializeField] private OrbConfiguration orbConfig;

    private bool isDestroying = false;

    private Material visualMaterial;
    private float dissolveAmount = 0f;

    private static readonly int DissolveID = Shader.PropertyToID("_DissolveAmount");

    private Vector3 baseLocalPosition;

    private void Awake()
    {
        if (visual == null) return;

        Renderer renderer = visual.GetComponent<Renderer>();
        if (renderer != null)
        {
            visualMaterial = renderer.material;
            visualMaterial.SetFloat(DissolveID, 0f);
        }

        baseLocalPosition = visual.transform.localPosition;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isDestroying)
        {
            isDestroying = true;
            StartCoroutine(DestroySequence());
        }
    }

    private IEnumerator DestroySequence()
    {
        if (GameSessionStats.Instance != null)
            GameSessionStats.Instance.RegisterBuildingDestroyed();

        if (CameraShakeManager.Instance != null)
            CameraShakeManager.Instance.Shake(shakeForce);

        if (MusicManager.Instance != null &&
            SFXDatabase.Instance != null &&
            SFXDatabase.Instance.buildingDestroySFX != null)
        {
            MusicManager.Instance.PlaySFXOneShot(
                SFXDatabase.Instance.buildingDestroySFX,
                SFXDatabase.Instance.buildingDestroyVolume
            );
        }

        if (dustPrefab != null)
        {
            DustBurst dust = Instantiate(dustPrefab, GetSpawnCenter(), Quaternion.identity);
            Renderer r = visual.GetComponent<Renderer>();
            float size = 3f;

            if (r != null)
            {
                size = r.bounds.size.x;
            }
            float fallDuration = 2.5f;
            dust.Play(size, fallDuration);
        }

        StartCoroutine(SinkAndDestroy());

        yield return new WaitForSeconds(dropSpawnDelay);

        SpawnExperienceOrbs();
        SpawnCollectibles();
    }

    private IEnumerator SinkAndDestroy()
    {
        if (visual == null) yield break;

        float targetSinkDistance = CalculateSinkDistance();
        float sunkDistance = 0f;

        while (sunkDistance < targetSinkDistance)
        {
            float delta = sinkSpeed * Time.deltaTime;
            sunkDistance += delta;

            baseLocalPosition += Vector3.down * delta;

            dissolveAmount += Time.deltaTime * shaderDestructionSpeed;
            dissolveAmount = Mathf.Clamp01(dissolveAmount);

            if (visualMaterial != null)
                visualMaterial.SetFloat(DissolveID, dissolveAmount);

            float currentShake = shakeIntensity * dissolveAmount;
            float shakeOffset = Mathf.Sin(Time.time * shakeFrequency) * currentShake;

            visual.transform.localPosition =
                baseLocalPosition + new Vector3(shakeOffset, 0f, 0f);

            yield return null;
        }
    }

    private float CalculateSinkDistance()
    {
        Renderer visualRenderer = visual.GetComponent<Renderer>();
        if (visualRenderer != null)
        {
            Bounds bounds = visualRenderer.bounds;
            float topY = bounds.max.y;
            float distanceToGround = topY - groundLevel;
            return distanceToGround + sinkExtraDistance;
        }

        return 5f;
    }

    private void SpawnExperienceOrbs()
    {
        if (PoolManager.Instance == null || GameBalanceConfig.Instance == null)
            return;

        int orbCount = Random.Range(
            GameBalanceConfig.Instance.BuildingMinOrbs,
            GameBalanceConfig.Instance.BuildingMaxOrbs + 1
        );

        Vector3 spawnCenter = GetSpawnCenter();

        for (int i = 0; i < orbCount; i++)
        {
            Vector2 randomCircle =
                Random.insideUnitCircle * GameBalanceConfig.Instance.BuildingOrbSpawnRadius;

            Vector3 spawnPosition =
                spawnCenter +
                new Vector3(randomCircle.x, Random.Range(0f, 2f), randomCircle.y);

            ExperienceOrb orb =
                PoolManager.Instance.SpawnOrb(spawnPosition, orbConfig);

            if (orb != null && orbConfig == null)
            {
                orb.SetExperienceValue(
                    GameBalanceConfig.Instance.BuildingDefaultExperienceValue
                );
            }
        }
    }

    private void SpawnCollectibles()
    {
        if (PoolManager.Instance == null || GameBalanceConfig.Instance == null)
            return;

        Vector3 spawnCenter = GetSpawnCenter();

        if (Random.value <= GameBalanceConfig.Instance.BuildingCoinDropChance)
        {
            int coinCount = Random.Range(
                GameBalanceConfig.Instance.BuildingMinCoins,
                GameBalanceConfig.Instance.BuildingMaxCoins + 1
            );

            for (int i = 0; i < coinCount; i++)
            {
                Vector2 randomCircle =
                    Random.insideUnitCircle * GameBalanceConfig.Instance.BuildingOrbSpawnRadius;

                Vector3 spawnPosition =
                    spawnCenter +
                    new Vector3(randomCircle.x, Random.Range(0f, 2f), randomCircle.y);

                PoolManager.Instance.SpawnCollectible(
                    spawnPosition,
                    Collectible.CollectibleType.Coin,
                    1
                );
            }
        }

        if (Random.value <= GameBalanceConfig.Instance.BuildingDiamondDropChance)
        {
            int diamondCount = Random.Range(
                GameBalanceConfig.Instance.BuildingMinDiamonds,
                GameBalanceConfig.Instance.BuildingMaxDiamonds + 1
            );

            for (int i = 0; i < diamondCount; i++)
            {
                Vector2 randomCircle =
                    Random.insideUnitCircle * GameBalanceConfig.Instance.BuildingOrbSpawnRadius;

                Vector3 spawnPosition =
                    spawnCenter +
                    new Vector3(randomCircle.x, Random.Range(0f, 2f), randomCircle.y);

                PoolManager.Instance.SpawnCollectible(
                    spawnPosition,
                    Collectible.CollectibleType.Diamond,
                    1
                );
            }
        }
    }

    private Vector3 GetSpawnCenter()
    {
        if (spawnPoint != null)
            return spawnPoint.position;

        return visual.transform.position + Vector3.up;
    }
}