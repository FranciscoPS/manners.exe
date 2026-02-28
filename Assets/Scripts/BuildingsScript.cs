using UnityEngine;
using System.Collections;

public class BuildingsScript : MonoBehaviour
{
    [Header("Destruction Settings")]
    [SerializeField] private GameObject visual;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float fadeOutDuration = 1f;
    [SerializeField] private float shakeDuration = 0.4f;
    [SerializeField] private float shakeIntensity = 0.15f;
    [SerializeField] private bool collapseDown = true;
    [SerializeField] private float collapseAmount = 0.7f;

    [Header("Destruction VFX")]
    [SerializeField] private GameObject destructionVFXPrefab;
    [SerializeField] private float vfxScale = 1f;

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

        // Spawn VFX de polvo/destrucción
        SpawnDestructionVFX();

        // Iniciar fade del edificio (sin mover geometría - compatible con occlusion culling)
        StartCoroutine(FadeAndDestroy());
        
        yield return new WaitForSeconds(dropSpawnDelay);
        
        SpawnExperienceOrbs();
        SpawnCollectibles();
    }

    private void SpawnDestructionVFX()
    {
        if (destructionVFXPrefab == null) return;

        Vector3 vfxPosition = GetSpawnCenter() + Vector3.up * 1f;
        GameObject vfxInstance = Instantiate(destructionVFXPrefab, vfxPosition, Quaternion.identity);
        
        // Configurar escala si tiene el componente
        BuildingDestructionVFX vfxComponent = vfxInstance.GetComponent<BuildingDestructionVFX>();
        if (vfxComponent != null)
        {
            vfxComponent.Initialize(vfxPosition, vfxScale);
        }
    }

    private IEnumerator FadeAndDestroy()
    {
        if (visual == null)
        {
            yield break;
        }

        // Guardar posición y escala originales del visual
        Vector3 originalPosition = visual.transform.localPosition;
        Vector3 originalScale = visual.transform.localScale;
        Quaternion originalRotation = visual.transform.localRotation;

        // FASE 1: SHAKE - Temblor antes del colapso
        float shakeElapsed = 0f;
        while (shakeElapsed < shakeDuration)
        {
            shakeElapsed += Time.deltaTime;
            
            // Temblor en X y Z (horizontal) con frecuencia alta
            float shakeProgress = shakeElapsed / shakeDuration;
            float intensity = shakeIntensity * (1f - shakeProgress); // Decrece con el tiempo
            
            Vector3 shakeOffset = new Vector3(
                Random.Range(-intensity, intensity),
                0f, // No temblor en Y
                Random.Range(-intensity, intensity)
            );
            
            visual.transform.localPosition = originalPosition + shakeOffset;
            
            yield return null;
        }

        // Restaurar posición después del shake
        visual.transform.localPosition = originalPosition;

        // Obtener todos los renderers del visual
        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            yield break;
        }

        // Guardar materiales originales y crear copias para fade
        Material[][] originalMaterials = new Material[renderers.Length][];
        Material[][] fadeMaterials = new Material[renderers.Length][];
        
        for (int i = 0; i < renderers.Length; i++)
        {
            originalMaterials[i] = renderers[i].materials;
            fadeMaterials[i] = new Material[originalMaterials[i].Length];
            
            for (int j = 0; j < originalMaterials[i].Length; j++)
            {
                // Crear copia del material para no afectar otros objetos
                fadeMaterials[i][j] = new Material(originalMaterials[i][j]);
                
                // Configurar material para transparencia
                SetupTransparentMaterial(fadeMaterials[i][j]);
            }
            
            renderers[i].materials = fadeMaterials[i];
        }

        // FASE 2: FADE + COLAPSO - Fade out mientras el edificio se derrumba
        float fadeElapsed = 0f;
        while (fadeElapsed < fadeOutDuration)
        {
            fadeElapsed += Time.deltaTime;
            float progress = fadeElapsed / fadeOutDuration;
            
            // Alpha fade
            float alpha = 1f - progress;
            
            // Aplicar alpha a todos los materiales
            foreach (var materialArray in fadeMaterials)
            {
                foreach (var mat in materialArray)
                {
                    SetMaterialAlpha(mat, alpha);
                }
            }
            
            // Colapso visual (scale down en Y y uniform shrink)
            if (collapseDown)
            {
                // Curva de colapso: empieza lento, acelera al final
                float collapseCurve = progress * progress; // Ease-in
                
                // Scale en Y (colapsa hacia abajo)
                float scaleY = Mathf.Lerp(1f, collapseAmount, collapseCurve);
                
                // Scale uniform (edificio se encoge también)
                float uniformScale = Mathf.Lerp(1f, 0.8f, collapseCurve);
                
                Vector3 newScale = new Vector3(
                    originalScale.x * uniformScale,
                    originalScale.y * scaleY,
                    originalScale.z * uniformScale
                );
                
                visual.transform.localScale = newScale;
                
                // Bajar posición en Y para compensar el scale (parece que se hunde)
                float yOffset = originalScale.y * (1f - scaleY) * 0.5f;
                visual.transform.localPosition = originalPosition + Vector3.down * yOffset;
                
                // Rotación ligera aleatoria (efecto de derrumbe)
                float rotationAmount = collapseCurve * 5f; // Máximo 5 grados
                Vector3 randomRotation = new Vector3(
                    Random.Range(-rotationAmount, rotationAmount),
                    Random.Range(-rotationAmount, rotationAmount),
                    Random.Range(-rotationAmount, rotationAmount)
                );
                
                visual.transform.localRotation = originalRotation * Quaternion.Euler(randomRotation);
            }
            
            yield return null;
        }

        // Asegurar alpha 0 y escala final
        foreach (var materialArray in fadeMaterials)
        {
            foreach (var mat in materialArray)
            {
                SetMaterialAlpha(mat, 0f);
            }
        }

        // Desactivar el GameObject (NO destruir para permitir pooling futuro)
        gameObject.SetActive(false);
        
        // Opcional: destruir después de un delay para liberar memoria
        Destroy(gameObject, 1f);
    }

    private void SetupTransparentMaterial(Material mat)
    {
        // Intentar configurar material para transparencia (URP/Standard)
        if (mat.HasProperty("_Surface"))
        {
            // URP Lit/Unlit
            mat.SetFloat("_Surface", 1); // Transparent
            mat.SetFloat("_Blend", 0); // Alpha
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3000;
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        }
        else if (mat.HasProperty("_Mode"))
        {
            // Standard Shader (Built-in)
            mat.SetFloat("_Mode", 3); // Transparent
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
        }
    }

    private void SetMaterialAlpha(Material mat, float alpha)
    {
        // Intentar setear alpha en diferentes propiedades según el shader
        if (mat.HasProperty("_BaseColor"))
        {
            // URP
            Color color = mat.GetColor("_BaseColor");
            color.a = alpha;
            mat.SetColor("_BaseColor", color);
        }
        else if (mat.HasProperty("_Color"))
        {
            // Standard
            Color color = mat.GetColor("_Color");
            color.a = alpha;
            mat.SetColor("_Color", color);
        }
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
            
            ExperienceOrb orb = SpawnFactory.Instance.CreateExperienceOrb(spawnPosition, orbConfig);
            if (orb != null && orbConfig == null)
            {
                orb.SetExperienceValue(GameBalanceConfig.Instance.BuildingDefaultExperienceValue);
            }
        }
    }

    private void SpawnCollectibles()
    {
        if (SpawnFactory.Instance == null || GameBalanceConfig.Instance == null) return;

        Vector3 spawnCenter = GetSpawnCenter();

        if (Random.value <= GameBalanceConfig.Instance.BuildingCoinDropChance)
        {
            int coinCount = Random.Range(GameBalanceConfig.Instance.BuildingMinCoins, GameBalanceConfig.Instance.BuildingMaxCoins + 1);
            for (int i = 0; i < coinCount; i++)
            {
                Vector2 randomCircle = Random.insideUnitCircle * GameBalanceConfig.Instance.BuildingOrbSpawnRadius;
                Vector3 spawnPosition = spawnCenter + new Vector3(randomCircle.x, Random.Range(0f, 2f), randomCircle.y);
                SpawnFactory.Instance.CreateCollectible(spawnPosition, Collectible.CollectibleType.Coin, 1);
            }
        }

        if (Random.value <= GameBalanceConfig.Instance.BuildingDiamondDropChance)
        {
            int diamondCount = Random.Range(GameBalanceConfig.Instance.BuildingMinDiamonds, GameBalanceConfig.Instance.BuildingMaxDiamonds + 1);
            for (int i = 0; i < diamondCount; i++)
            {
                Vector2 randomCircle = Random.insideUnitCircle * GameBalanceConfig.Instance.BuildingOrbSpawnRadius;
                Vector3 spawnPosition = spawnCenter + new Vector3(randomCircle.x, Random.Range(0f, 2f), randomCircle.y);
                SpawnFactory.Instance.CreateCollectible(spawnPosition, Collectible.CollectibleType.Diamond, 1);
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
