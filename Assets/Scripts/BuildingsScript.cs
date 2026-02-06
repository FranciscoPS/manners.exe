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

    private bool isDestroying = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isDestroying)
        {
            isDestroying = true;
            SpawnExperienceOrbs();
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
            
            PoolManager.Instance.Spawn<ExperienceOrb>(
                PoolManager.PoolType.ExperienceOrb,
                spawnPosition,
                Quaternion.identity,
                orb =>
                {
                    if (orbConfig != null)
                    {
                        orbConfig.ApplyToOrb(orb);
                    }
                    else
                    {
                        orb.SetExperienceValue(defaultExperienceValue);
                    }
                }
            );
        }
    }
}
