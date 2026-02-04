using UnityEngine;
using System.Collections;

public class BuildingsScript : MonoBehaviour
{
    [Header("Destruction Settings")]
    [SerializeField] private float sinkSpeed = 1.5f;
    [SerializeField] private float sinkDuration = 2f;

    [Header("Experience Orb Settings")]
    [SerializeField] private GameObject experienceOrbPrefab;
    [SerializeField] private int minOrbs = 3;
    [SerializeField] private int maxOrbs = 7;
    [SerializeField] private float orbSpawnRadius = 2f;
    [SerializeField] private float orbSpawnHeight = 1f;

    private bool isDestroying = false;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player") && !isDestroying)
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
            transform.Translate(Vector3.down * sinkSpeed * Time.deltaTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }

    private void SpawnExperienceOrbs()
    {
        if (experienceOrbPrefab == null) return;

        int orbCount = Random.Range(minOrbs, maxOrbs + 1);
        
        Bounds bounds = GetComponent<Collider>().bounds;
        Vector3 spawnCenter = bounds.center;
        spawnCenter.y = bounds.max.y + orbSpawnHeight;

        for (int i = 0; i < orbCount; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * orbSpawnRadius;
            Vector3 spawnPosition = spawnCenter + new Vector3(randomCircle.x, Random.Range(0f, 2f), randomCircle.y);
            
            GameObject orb = Instantiate(experienceOrbPrefab, spawnPosition, Quaternion.identity);
            orb.transform.SetParent(null);
        }
    }
}
