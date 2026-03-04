using UnityEngine;

public class PoolPrewarmer : MonoBehaviour
{
    [Header("Prewarm Configuration")]
    [Tooltip("Número de enemigos a pre-instanciar")]
    [SerializeField] private int prewarmEnemyCount = 30;

    [Tooltip("Número de proyectiles a pre-instanciar")]
    [SerializeField] private int prewarmProjectileCount = 50;

    [Tooltip("Número de orbes de experiencia a pre-instanciar")]
    [SerializeField] private int prewarmOrbCount = 40;

    [Tooltip("Número de coleccionables (monedas/diamantes) a pre-instanciar")]
    [SerializeField] private int prewarmCollectibleCount = 30;

    [Header("Timing")]
    [Tooltip("Retardo antes de pre-calentar (útil para pantallas de carga)")]
    [SerializeField] private float prewarmDelay = 0.5f;

    private void Start()
    {

        Invoke(nameof(PrewarmPools), prewarmDelay);
    }

    private void PrewarmPools()
    {
        if (SpawnFactory.Instance == null)
        {
            return;
        }

        SpawnFactory.Instance.PrewarmPools(
            prewarmEnemyCount,
            prewarmProjectileCount,
            prewarmOrbCount,
            prewarmCollectibleCount
        );

    }
}
