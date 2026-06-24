using System.Collections;
using UnityEngine;

/// <summary>
/// Sistema de destrucción de edificios refactorizado para ser completamente estático
/// - Usa solo shader dissolve (no movimiento geométrico)
/// - Partículas de explosión como enemigos
/// - Optimizado para Occlusion Culling
/// </summary>
public class BuildingsScript : MonoBehaviour
{
    [Header("Visual Settings")]
    [SerializeField] private GameObject visual;
    [SerializeField] private Transform spawnPoint;

    [Header("Shader Destruction")]
    [Tooltip("Velocidad del efecto de disolución del shader")]
    [SerializeField] private float shaderDestructionSpeed = 1f;
    
    [Header("Destruction VFX")]
    [Tooltip("Partículas de explosión al destruir (como enemigos)")]
    [SerializeField] private GameObject explosionPrefab;
    [Tooltip("Partículas de polvo adicionales durante destrucción")]
    [SerializeField] private DustBurst dustPrefab;

    [Header("Destruction Feedback")]
    [SerializeField] private float shakeForce = 0.5f;

    [Header("Drop Settings")]
    [Tooltip("Delay antes de spawnar drops después de iniciar destrucción")]
    [SerializeField] private float dropSpawnDelay = 1f;

    [Header("Experience Orb Settings")]
    [SerializeField] private OrbConfiguration orbConfig;

    private bool isDestroying = false;
    private Material visualMaterial;
    private Renderer visualRenderer;
    private float dissolveAmount = 0f;

    private static readonly int DissolveID = Shader.PropertyToID("_DissolveAmount");

    private void Awake()
    {
        if (visual == null) return;

        visualRenderer = visual.GetComponent<Renderer>();
        if (visualRenderer != null)
        {
            // Crear instancia del material para no afectar otros edificios
            visualMaterial = visualRenderer.material;
            visualMaterial.SetFloat(DissolveID, 0f);
        }
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
        // Registrar edificio destruido en estadísticas
        if (GameSessionStats.Instance != null)
            GameSessionStats.Instance.RegisterBuildingDestroyed();

        // Camera shake feedback
        if (CameraShakeManager.Instance != null)
            CameraShakeManager.Instance.Shake(shakeForce);

        // Reproducir sonido de destrucción
        if (MusicManager.Instance != null &&
            SFXDatabase.Instance != null &&
            SFXDatabase.Instance.buildingDestroySFX != null)
        {
            MusicManager.Instance.PlaySFXOneShot(
                SFXDatabase.Instance.buildingDestroySFX,
                SFXDatabase.Instance.buildingDestroyVolume
            );
        }

        // Spawnar efecto de explosión (como enemigos)
        if (explosionPrefab != null)
        {
            Vector3 explosionPosition = GetSpawnCenter();
            Instantiate(explosionPrefab, explosionPosition, Quaternion.identity);
        }

        // Efecto de polvo adicional (DustBurst)
        if (dustPrefab != null)
        {
            DustBurst dust = Instantiate(dustPrefab, GetSpawnCenter(), Quaternion.identity);
            
            // Calcular tamaño basado en el renderer
            float size = 3f;
            if (visualRenderer != null)
            {
                size = visualRenderer.bounds.size.x;
            }
            
            float fallDuration = 2.5f;
            dust.Play(fallDuration);
        }

        // Iniciar efecto de disolución del shader (sin movimiento)
        StartCoroutine(DissolveAndHide());

        // Esperar antes de spawnar drops
        yield return new WaitForSeconds(dropSpawnDelay);

        SpawnExperienceOrbs();
        SpawnCollectibles();
    }

    /// <summary>
    /// Efecto de disolución del shader sin mover el edificio (100% estático)
    /// Al terminar, solo desactiva el renderer para occlusion culling
    /// </summary>
    private IEnumerator DissolveAndHide()
    {
        if (visual == null || visualMaterial == null) yield break;

        // Animar el shader dissolve de 0 a 1
        while (dissolveAmount < 1f)
        {
            dissolveAmount += Time.deltaTime * shaderDestructionSpeed;
            dissolveAmount = Mathf.Clamp01(dissolveAmount);

            visualMaterial.SetFloat(DissolveID, dissolveAmount);

            yield return null;
        }

        // Al terminar la disolución, desactivar el renderer
        // El GameObject permanece (estático para occlusion culling)
        if (visualRenderer != null)
        {
            visualRenderer.enabled = false;
        }

        // Opcionalmente desactivar el collider para que no se pueda triggerar de nuevo
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }
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