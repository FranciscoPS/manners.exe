using UnityEngine;

/// <summary>
/// Pool Prewarmer - Optimiza el inicio del juego pre-instanciando objetos
/// Previene stuttering durante gameplay cuando se crean objetos por primera vez
/// 
/// Uso: Adjuntar este script a un objeto en la primera escena del juego
/// </summary>
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
        // Pre-calentar después de un pequeño delay para no bloquear el inicio
        Invoke(nameof(PrewarmPools), prewarmDelay);
    }
    
    private void PrewarmPools()
    {
        if (SpawnFactory.Instance == null)
        {
            Debug.LogWarning("[PoolPrewarmer] SpawnFactory not found! Cannot prewarm pools.");
            return;
        }
        
        Debug.Log($"[PoolPrewarmer] Prewarming pools: {prewarmEnemyCount} enemies, {prewarmProjectileCount} projectiles, {prewarmOrbCount} orbs, {prewarmCollectibleCount} collectibles");
        
        SpawnFactory.Instance.PrewarmPools(
            prewarmEnemyCount,
            prewarmProjectileCount,
            prewarmOrbCount,
            prewarmCollectibleCount
        );
        
        Debug.Log("[PoolPrewarmer] Pool prewarming complete!");
    }
}
