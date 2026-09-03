using UnityEngine;
using DG.Tweening;

public class ShopVisualFeedback : MonoBehaviour
{
    [Header("Visual References")]
    [SerializeField] private GameObject visualSphere;
    [SerializeField] private Renderer sphereRenderer;

    [Header("Blink Settings")]
    [SerializeField] private Color availableColor = new Color(0f, 1f, 0f, 1f);
    [SerializeField] private Color cooldownColor = new Color(1f, 0f, 0f, 1f);
    [SerializeField][Range(0f, 1f)] private float sphereAlpha = 0.3f;
    [SerializeField] private float blinkSpeed = 2f;
    [SerializeField] private float minEmission = 0.5f;
    [SerializeField] private float maxEmission = 2f;

    [Header("Salto del edificio")]
    [Tooltip("Transform del modelo de la tienda que salta. Vacío = se usa automáticamente el primer hijo con Renderer que no sea la esfera.")]
    [SerializeField] private Transform bounceTarget;
    [Tooltip("Activo: el edificio solo salta mientras la tienda está disponible (sin cooldown) y se queda quieto en cooldown. Desactivado: salta siempre.")]
    [SerializeField] private bool bounceOnlyWhenAvailable = true;
    [SerializeField] private SquashStretchBounceSettings bounce = new SquashStretchBounceSettings
    {
        jumpHeight = 0.5f,
        jumpDuration = 0.5f,
        squashAmount = 0.22f,
        stretchAmount = 0.18f,
        anticipationDuration = 0.15f,
        recoverDuration = 0.4f,
        restBetweenJumps = 1f
    };

    [Header("Managers")]
    [SerializeField] private LevelUpManager levelUpManager;

    private Material sphereMaterial;
    private bool isOnCooldown = false;
    private static readonly int EmissionColorProperty = Shader.PropertyToID("_EmissionColor");
    private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");

    private Sequence bounceTween;
    private Vector3 bounceBaseScale;
    private float bounceBaseLocalY;
    private bool bounceBaseCaptured = false;

    private void Start()
    {
        if (levelUpManager == null)
        {
            levelUpManager = FindFirstObjectByType<LevelUpManager>();
        }

        if (sphereRenderer == null && visualSphere != null)
        {
            sphereRenderer = visualSphere.GetComponent<Renderer>();
        }

        if (sphereRenderer != null)
        {
            sphereMaterial = sphereRenderer.material;
            sphereMaterial.EnableKeyword("_EMISSION");

            if (sphereMaterial.HasProperty("_Surface"))
            {
                sphereMaterial.SetFloat("_Surface", 1);
            }

            if (sphereMaterial.HasProperty("_Blend"))
            {
                sphereMaterial.SetFloat("_Blend", 0);
            }

            sphereMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            sphereMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            sphereMaterial.SetInt("_ZWrite", 0);
            sphereMaterial.DisableKeyword("_ALPHATEST_ON");
            sphereMaterial.EnableKeyword("_ALPHABLEND_ON");
            sphereMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            sphereMaterial.renderQueue = 3000;
        }

        ResolveBounceTarget();
        UpdateVisualState();
    }

    private void OnDisable()
    {
        StopBounce();
        if (bounceBaseCaptured)
            SquashStretchBounce.ResetPose(bounceTarget, bounceBaseScale, bounceBaseLocalY);
    }

    private void Update()
    {
        if (levelUpManager == null)
            return;

        bool shopAvailable = levelUpManager.IsShopAvailable();

        if (sphereMaterial != null)
        {
            if (!shopAvailable)
            {
                isOnCooldown = true;
                BlinkSphere();
            }
            else
            {
                if (isOnCooldown)
                {
                    isOnCooldown = false;
                }
                SetAvailableState();
            }
        }

        UpdateBounce(shopAvailable);
    }

    private void ResolveBounceTarget()
    {
        if (bounceTarget == null)
            bounceTarget = FindBounceTarget();

        if (bounceTarget == null)
            return;

        bounceBaseScale = bounceTarget.localScale;
        bounceBaseLocalY = bounceTarget.localPosition.y;
        bounceBaseCaptured = true;
    }

    private Transform FindBounceTarget()
    {
        Transform sphere = visualSphere != null ? visualSphere.transform : null;

        foreach (Transform child in transform)
        {
            if (child == sphere) continue;
            if (child.GetComponentInChildren<Canvas>(true) != null) continue;
            if (child.GetComponentInChildren<Renderer>(true) != null) return child;
        }

        return null;
    }

    private void UpdateBounce(bool shopAvailable)
    {
        if (!bounceBaseCaptured) return;

        bool shouldBounce = !bounceOnlyWhenAvailable || shopAvailable;

        if (shouldBounce)
        {
            if (bounceTween == null)
                bounceTween = SquashStretchBounce.PlayLoop(bounceTarget, bounce, bounceBaseScale, bounceBaseLocalY);
        }
        else if (bounceTween != null)
        {
            StopBounce();
            SquashStretchBounce.Settle(bounceTarget, bounceBaseScale, bounceBaseLocalY);
        }
    }

    private void StopBounce()
    {
        if (bounceTween == null) return;

        bounceTween.Kill();
        bounceTween = null;
    }

    private void BlinkSphere()
    {
        float t = (Mathf.Sin(Time.time * blinkSpeed) + 1f) / 2f;
        float emission = Mathf.Lerp(minEmission, maxEmission, t);

        Color baseColor = new Color(cooldownColor.r, cooldownColor.g, cooldownColor.b, sphereAlpha);
        Color emissionColor = new Color(cooldownColor.r, cooldownColor.g, cooldownColor.b, 1f) * emission;

        sphereMaterial.SetColor(EmissionColorProperty, emissionColor);
        sphereMaterial.SetColor(BaseColorProperty, baseColor);
    }

    private void SetAvailableState()
    {
        Color baseColor = new Color(availableColor.r, availableColor.g, availableColor.b, sphereAlpha);
        Color emissionColor = new Color(availableColor.r, availableColor.g, availableColor.b, 1f) * maxEmission;

        sphereMaterial.SetColor(EmissionColorProperty, emissionColor);
        sphereMaterial.SetColor(BaseColorProperty, baseColor);
    }

    private void UpdateVisualState()
    {
        if (levelUpManager != null && sphereMaterial != null)
        {
            if (levelUpManager.IsShopAvailable())
            {
                SetAvailableState();
            }
            else
            {
                isOnCooldown = true;
            }
        }
    }

    private void OnDestroy()
    {
        StopBounce();

        if (sphereMaterial != null)
        {
            Destroy(sphereMaterial);
        }
    }
}
