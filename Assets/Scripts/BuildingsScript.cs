using UnityEngine;
using System.Collections;

public class BuildingsScript : MonoBehaviour
{
    [Header("Destruction Settings")]
    [SerializeField] private float sinkSpeed = 1.5f;
    [SerializeField] private float sinkDuration = 2f;
    [SerializeField] private GameObject visual; 

    [Header("Experience Orb Settings")]
    [SerializeField] private int minOrbs = 3;
    [SerializeField] private int maxOrbs = 7;
    [SerializeField] private float orbSpawnRadius = 2f;
    [SerializeField] private float orbSpawnHeight = 1f;
    [SerializeField] private OrbConfiguration orbConfig;
    [SerializeField] private int defaultExperienceValue = 15;

    [Header("Collectible Drop Settings")]
    [SerializeField] private float coinDropChance = 0.7f;
    [SerializeField] private int minCoins = 2;
    [SerializeField] private int maxCoins = 5;
    [SerializeField] private float diamondDropChance = 0.15f;
    [SerializeField] private int minDiamonds = 1;
    [SerializeField] private int maxDiamonds = 2;

    private bool isDestroying = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isDestroying)
        {
            isDestroying = true;
            SpawnExperienceOrbs();
            SpawnCollectibles();
            StartCoroutine(SinkAndDestroy());
        }
    }

    private IEnumerator SinkAndDestroy()
    {
        float elapsedTime = 0f;

        while (elapsedTime < sinkDuration)
        {
            visual.transform.Translate(Vector3.down * sinkSpeed * Time.deltaTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        //Destroy(gameObject);
    }

    private void SpawnExperienceOrbs()
    {
        if (PoolManager.Instance == null)
        {
            Debug.LogWarning("PoolManager not initialized!");
            return;
        }

        int orbCount = Random.Range(minOrbs, maxOrbs + 1);
        
        Bounds bounds = GetComponent<Collider>().bounds;
        Vector3 spawnCenter = bounds.center;
        spawnCenter.y = bounds.max.y + orbSpawnHeight;

        for (int i = 0; i < orbCount; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * orbSpawnRadius;
            Vector3 spawnPosition = spawnCenter + new Vector3(randomCircle.x, Random.Range(0f, 2f), randomCircle.y);
            
            ExperienceOrb orb = PoolManager.Instance.SpawnOrb(spawnPosition, orbConfig);
            if (orb != null && orbConfig == null)
            {
                orb.SetExperienceValue(defaultExperienceValue);
            }
        }
    }

    private void SpawnCollectibles()
    {
        if (PoolManager.Instance == null) return;

        Bounds bounds = GetComponent<Collider>().bounds;
        Vector3 spawnCenter = bounds.center;
        spawnCenter.y = bounds.max.y + orbSpawnHeight;

        if (Random.value <= coinDropChance)
        {
            int coinCount = Random.Range(minCoins, maxCoins + 1);
            for (int i = 0; i < coinCount; i++)
            {
                Vector2 randomCircle = Random.insideUnitCircle * orbSpawnRadius;
                Vector3 spawnPosition = spawnCenter + new Vector3(randomCircle.x, Random.Range(0f, 2f), randomCircle.y);
                PoolManager.Instance.SpawnCollectible(spawnPosition, Collectible.CollectibleType.Coin, 1);
            }
        }

        if (Random.value <= diamondDropChance)
        {
            int diamondCount = Random.Range(minDiamonds, maxDiamonds + 1);
            for (int i = 0; i < diamondCount; i++)
            {
                Vector2 randomCircle = Random.insideUnitCircle * orbSpawnRadius;
                Vector3 spawnPosition = spawnCenter + new Vector3(randomCircle.x, Random.Range(0f, 2f), randomCircle.y);
                PoolManager.Instance.SpawnCollectible(spawnPosition, Collectible.CollectibleType.Diamond, 1);
            }
        }
    }
}
