using UnityEngine;
using System.Collections;

public class BuildingsScript : MonoBehaviour
{
    [Header("Destruction Settings")]
    [SerializeField] private float sinkSpeed = 1.5f;
    [SerializeField] private float sinkDuration = 2f;

    private bool isDestroying = false;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player") && !isDestroying)
        {
            isDestroying = true;
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
}
