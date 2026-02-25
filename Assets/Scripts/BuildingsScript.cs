using UnityEngine;
using System.Collections;

public class BuildingsScript : MonoBehaviour
{
    [Header("Destruction Settings")]
    [SerializeField] private float sinkSpeed = 1.5f;
    [SerializeField] private GameObject visual;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float groundLevel = 0f;
    [SerializeField] private float sinkExtraDistance = 1f;

    [Header("Destruction Feedback")]
    [SerializeField] private float shakeForce = 0.5f;

    [Header("Drop Settings")]
    [SerializeField] private float dropSpawnDelay = 1f;

    [Header("Experience Orb Settings")]
    [SerializeField] private OrbConfiguration orbConfig;

    private bool isDestroying = false;

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
        // Registrar edificio destruido en estadísticas
        if (GameSessionStats.Instance != null)
        {
            GameSessionStats.Instance.RegisterBuildingDestroyed();
        }
        
        if (CameraShakeManager.Instance != null)
        {
            CameraShakeManager.Instance.Shake(shakeForce);
        }

        if (MusicManager.Instance != null && SFXDatabase.Instance != null && SFXDatabase.Instance.buildingDestroySFX != null)
        {
            MusicManager.Instance.PlaySFXOneShot(SFXDatabase.Instance.buildingDestroySFX, SFXDatabase.Instance.buildingDestroyVolume);
        }

        StartCoroutine(SinkAndDestroy());
        
        yield return new WaitForSeconds(dropSpawnDelay);
        
        SpawnExperienceOrbs();
        SpawnCollectibles();
    }

    private IEnumerator SinkAndDestroy()
    {
        if (visual == null)
        {
            yield break;
        }

        float targetSinkDistance = CalculateSinkDistance();
        float sunkDistance = 0f;

        while (sunkDistance < targetSinkDistance)
        {
            float deltaMovement = sinkSpeed * Time.deltaTime;
            visual.transform.Translate(Vector3.down * deltaMovement);
            sunkDistance += deltaMovement;
            yield return null;
        }
    }

    private float CalculateSinkDistance()
    {
        if (visual == null)
        {
            return 5f;
        }

        Renderer visualRenderer = visual.GetComponent<Renderer>();
        if (visualRenderer != null)
        {
            Bounds bounds = visualRenderer.bounds;
            float topY = bounds.max.y;
            float distanceToGround = topY - groundLevel;
            return distanceToGround + sinkExtraDistance;
        }

        float fallbackHeight = visual.transform.position.y - groundLevel;
        return fallbackHeight + sinkExtraDistance;
    }

    private void SpawnExperienceOrbs()
    {
        if (PoolManager.Instance == null || GameBalanceConfig.Instance == null)
        {
            return;
        }

        int orbCount = Random.Range(GameBalanceConfig.Instance.BuildingMinOrbs, GameBalanceConfig.Instance.BuildingMaxOrbs + 1);
        Vector3 spawnCenter = GetSpawnCenter();

        for (int i = 0; i < orbCount; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * GameBalanceConfig.Instance.BuildingOrbSpawnRadius;
            Vector3 spawnPosition = spawnCenter + new Vector3(randomCircle.x, Random.Range(0f, 2f), randomCircle.y);
            
            ExperienceOrb orb = PoolManager.Instance.SpawnOrb(spawnPosition, orbConfig);
            if (orb != null && orbConfig == null)
            {
                orb.SetExperienceValue(GameBalanceConfig.Instance.BuildingDefaultExperienceValue);
            }
        }
    }

    private void SpawnCollectibles()
    {
        if (PoolManager.Instance == null || GameBalanceConfig.Instance == null) return;

        Vector3 spawnCenter = GetSpawnCenter();

        if (Random.value <= GameBalanceConfig.Instance.BuildingCoinDropChance)
        {
            int coinCount = Random.Range(GameBalanceConfig.Instance.BuildingMinCoins, GameBalanceConfig.Instance.BuildingMaxCoins + 1);
            for (int i = 0; i < coinCount; i++)
            {
                Vector2 randomCircle = Random.insideUnitCircle * GameBalanceConfig.Instance.BuildingOrbSpawnRadius;
                Vector3 spawnPosition = spawnCenter + new Vector3(randomCircle.x, Random.Range(0f, 2f), randomCircle.y);
                PoolManager.Instance.SpawnCollectible(spawnPosition, Collectible.CollectibleType.Coin, 1);
            }
        }

        if (Random.value <= GameBalanceConfig.Instance.BuildingDiamondDropChance)
        {
            int diamondCount = Random.Range(GameBalanceConfig.Instance.BuildingMinDiamonds, GameBalanceConfig.Instance.BuildingMaxDiamonds + 1);
            for (int i = 0; i < diamondCount; i++)
            {
                Vector2 randomCircle = Random.insideUnitCircle * GameBalanceConfig.Instance.BuildingOrbSpawnRadius;
                Vector3 spawnPosition = spawnCenter + new Vector3(randomCircle.x, Random.Range(0f, 2f), randomCircle.y);
                PoolManager.Instance.SpawnCollectible(spawnPosition, Collectible.CollectibleType.Diamond, 1);
            }
        }
    }

    private Vector3 GetSpawnCenter()
    {
        if (spawnPoint != null)
        {
            return spawnPoint.position;
        }
        
        if (visual != null)
        {
            Renderer visualRenderer = visual.GetComponent<Renderer>();
            if (visualRenderer != null)
            {
                Bounds bounds = visualRenderer.bounds;
                Vector3 center = bounds.center;
                center.y = bounds.max.y + 1f;
                return center;
            }
            return visual.transform.position + Vector3.up;
        }
        
        return transform.position + Vector3.up;
    }
}
