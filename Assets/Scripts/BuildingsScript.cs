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

    private void Awake()
    {
        if (visual != null) return;

        foreach (Transform child in transform)
        {
            if (string.Equals(child.name, "visual", System.StringComparison.OrdinalIgnoreCase))
            {
                visual = child.gameObject;
                return;
            }
        }

        foreach (Transform child in transform)
        {
            if (child.GetComponentInChildren<Renderer>() != null)
            {
                visual = child.gameObject;
                return;
            }
        }

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isDestroying)
        {
            isDestroying = true;

            var fader = GetComponent<BuildingFader>();
            if (fader != null) fader.SuspendForDestruction();

            StartCoroutine(DestroySequence());
        }
    }

    private IEnumerator DestroySequence()
    {

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

        SpawnDestructionVFX();

        StartCoroutine(FadeAndDestroy());

        yield return new WaitForSeconds(dropSpawnDelay);

        SpawnExperienceOrbs();
        SpawnCollectibles();

        float totalFadeTime = shakeDuration + fadeOutDuration;
        float remainingTime = totalFadeTime - dropSpawnDelay;

        if (remainingTime > 0)
        {
            yield return new WaitForSeconds(remainingTime);
        }

        gameObject.SetActive(false);
        Destroy(gameObject, 1f);
    }

    private void SpawnDestructionVFX()
    {
        if (destructionVFXPrefab == null) return;

        Vector3 vfxPosition = GetSpawnCenter() + Vector3.up * 1f;
        GameObject vfxInstance = Instantiate(destructionVFXPrefab, vfxPosition, Quaternion.identity);

        BuildingDestructionVFX vfxComponent = vfxInstance.GetComponent<BuildingDestructionVFX>();
        if (vfxComponent != null)
        {
            vfxComponent.Initialize(vfxPosition, vfxScale);
        }
    }

    private IEnumerator FadeAndDestroy()
    {
        if (visual == null) yield break;

        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>();

        Vector3 originalPosition = visual.transform.localPosition;
        Vector3 originalScale    = visual.transform.localScale;
        Quaternion originalRotation = visual.transform.localRotation;

        float shakeElapsed = 0f;
        while (shakeElapsed < shakeDuration)
        {
            shakeElapsed += Time.deltaTime;
            float intensity = shakeIntensity * (1f - shakeElapsed / shakeDuration);
            visual.transform.localPosition = originalPosition + new Vector3(
                Random.Range(-intensity, intensity), 0f,
                Random.Range(-intensity, intensity));
            yield return null;
        }
        visual.transform.localPosition = originalPosition;

        if (renderers.Length == 0) yield break;

        bool anyVisible = false;
        foreach (var r in renderers) { if (r.isVisible) { anyVisible = true; break; } }

        if (!anyVisible)
        {
            visual.SetActive(false);
            yield break;
        }

        var fadeMats = new System.Collections.Generic.List<Material>(renderers.Length * 2);
        for (int i = 0; i < renderers.Length; i++)
        {
            var origMats = renderers[i].materials;
            var instMats = new Material[origMats.Length];
            for (int j = 0; j < origMats.Length; j++)
            {
                instMats[j] = new Material(origMats[j]);
                SetupTransparentMaterial(instMats[j]);
                fadeMats.Add(instMats[j]);
            }
            renderers[i].materials = instMats;
        }

        float fadeElapsed = 0f;
        while (fadeElapsed < fadeOutDuration)
        {
            fadeElapsed += Time.deltaTime;
            float progress     = fadeElapsed / fadeOutDuration;
            float alpha        = 1f - progress;

            for (int m = 0; m < fadeMats.Count; m++)
                SetMaterialAlpha(fadeMats[m], alpha);

            if (collapseDown)
            {
                float curve      = progress * progress;
                float scaleY     = Mathf.LerpUnclamped(1f, collapseAmount, curve);
                float uniformSc  = Mathf.LerpUnclamped(1f, 0.8f, curve);

                visual.transform.localScale = new Vector3(
                    originalScale.x * uniformSc,
                    originalScale.y * scaleY,
                    originalScale.z * uniformSc);

                visual.transform.localPosition = originalPosition
                    + Vector3.down * (originalScale.y * (1f - scaleY) * 0.5f);

                float rotAmt = curve * 5f;
                if (rotAmt > 0.01f)
                {
                    visual.transform.localRotation = originalRotation * Quaternion.Euler(
                        Random.Range(-rotAmt, rotAmt),
                        Random.Range(-rotAmt, rotAmt),
                        Random.Range(-rotAmt, rotAmt));
                }
            }

            yield return null;
        }

        for (int m = 0; m < fadeMats.Count; m++)
            SetMaterialAlpha(fadeMats[m], 0f);

    }

    private void SetupTransparentMaterial(Material mat)
    {

        if (mat.HasProperty("_Surface"))
        {

            mat.SetFloat("_Surface", 1);
            mat.SetFloat("_Blend", 0);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3000;
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        }
        else if (mat.HasProperty("_Mode"))
        {

            mat.SetFloat("_Mode", 3);
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

        if (mat.HasProperty("_BaseColor"))
        {

            Color color = mat.GetColor("_BaseColor");
            color.a = alpha;
            mat.SetColor("_BaseColor", color);
        }
        else if (mat.HasProperty("_Color"))
        {

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
