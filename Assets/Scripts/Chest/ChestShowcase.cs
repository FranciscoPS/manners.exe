using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class ChestShowcase : MonoBehaviour
{
    private const string LayerName = "ChestShowcase";
    private const string FallbackPrefabName = "ChestForAnimation";
    private const int MinTextureSize = 64;
    private const int MaxTextureSize = 2048;
    private static readonly Vector3 RigWorldPosition = new Vector3(0f, -5000f, 0f);

    private RawImage view;
    private Camera showcaseCamera;
    private RenderTexture renderTexture;
    private GameObject chestInstance;
    private Animator chestAnimator;
    private int stateHash;
    private float clipLength;
    private Bounds restBounds;
    private bool loadFailed;

    public bool IsActive => renderTexture != null;
    public float BurstTime { get; private set; }
    public float FreezeTime { get; private set; }

    public static ChestShowcase Create(Transform parent, RawImage view)
    {
        GameObject go = new GameObject("ChestShowcaseRig");
        go.transform.SetParent(parent, false);
        go.transform.position = RigWorldPosition;

        ChestShowcase showcase = go.AddComponent<ChestShowcase>();
        showcase.view = view;
        view.enabled = false;
        return showcase;
    }

    public bool TryBegin(ChestOpeningConfig config)
    {
        if (!config.showcaseEnabled || !EnsureChest(config)) return false;

        BurstTime = Mathf.Clamp(config.showcaseBurstClipTime, 0f, clipLength);
        FreezeTime = config.showcaseFreezeClipTime <= 0f
            ? clipLength
            : Mathf.Clamp(config.showcaseFreezeClipTime, BurstTime, clipLength);

        chestInstance.SetActive(true);
        Seek(0f, 0f);
        SetupView(config);
        FrameCamera(config);

        showcaseCamera.enabled = true;
        view.color = Color.white;
        view.enabled = true;
        return true;
    }

    public void Seek(float clipTime, float speed)
    {
        if (chestAnimator == null) return;

        if (stateHash == 0)
            stateHash = chestAnimator.GetCurrentAnimatorStateInfo(0).fullPathHash;

        if (stateHash != 0)
            chestAnimator.Play(stateHash, 0, clipLength > 0f ? clipTime / clipLength : 0f);

        chestAnimator.speed = speed;
        chestAnimator.Update(0f);
    }

    public void SetAlpha(float alpha)
    {
        Color color = view.color;
        color.a = alpha;
        view.color = color;
    }

    public void End()
    {
        if (renderTexture == null) return;

        if (showcaseCamera != null)
        {
            showcaseCamera.enabled = false;
            showcaseCamera.targetTexture = null;
        }

        if (view != null)
        {
            view.texture = null;
            view.enabled = false;
        }

        if (chestInstance != null)
            chestInstance.SetActive(false);

        RenderTexture.ReleaseTemporary(renderTexture);
        renderTexture = null;
    }

    private void OnDestroy()
    {
        End();
    }

    private bool EnsureChest(ChestOpeningConfig config)
    {
        if (chestInstance != null) return true;
        if (loadFailed) return false;

        GameObject prefab = config.showcasePrefab != null
            ? config.showcasePrefab
            : Resources.Load<GameObject>(FallbackPrefabName);

        if (prefab == null)
        {
            loadFailed = true;
            Debug.LogWarning($"[ChestShowcase] No se encontró el prefab del cofre 3D ('{FallbackPrefabName}' en Resources ni en ChestOpeningConfig). La cinemática se reproduce sin modelo.");
            return false;
        }

        int layer = LayerMask.NameToLayer(LayerName);
        if (layer < 0)
            Debug.LogWarning($"[ChestShowcase] Falta la capa '{LayerName}' en Project Settings > Tags and Layers. La cámara del cofre 3D tendrá que hacer culling de toda la escena.");

        chestInstance = Instantiate(prefab, transform);
        chestInstance.name = prefab.name;
        ConfigureRenderers(chestInstance, layer);
        ConfigureAnimator(config);
        restBounds = CollectBounds(chestInstance);
        CreateCamera(layer);

        chestInstance.SetActive(false);
        return true;
    }

    private static void ConfigureRenderers(GameObject root, int layer)
    {
        if (layer >= 0)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = layer;
        }

        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;

            if (renderer is SkinnedMeshRenderer skinned)
                skinned.skinnedMotionVectors = false;
        }
    }

    private void ConfigureAnimator(ChestOpeningConfig config)
    {
        chestAnimator = chestInstance.GetComponentInChildren<Animator>();
        if (chestAnimator == null)
            chestAnimator = chestInstance.AddComponent<Animator>();

        if (chestAnimator.runtimeAnimatorController == null)
            chestAnimator.runtimeAnimatorController = config.showcaseController;

        chestAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
        chestAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        chestAnimator.applyRootMotion = false;

        RuntimeAnimatorController controller = chestAnimator.runtimeAnimatorController;
        if (controller == null)
        {
            Debug.LogWarning("[ChestShowcase] El cofre 3D no tiene Animator Controller. Asigna 'ChestANIM' en el prefab o en ChestOpeningConfig > Showcase Controller.");
            return;
        }

        chestAnimator.Update(0f);
        AnimatorStateInfo info = chestAnimator.GetCurrentAnimatorStateInfo(0);
        stateHash = info.fullPathHash;
        clipLength = info.length;

        if (clipLength <= 0.01f && controller.animationClips.Length > 0)
            clipLength = controller.animationClips[0].length;
    }

    private static Bounds CollectBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return new Bounds(root.transform.position, Vector3.one);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return bounds;
    }

    private void CreateCamera(int layer)
    {
        GameObject cameraObject = new GameObject("ChestShowcaseCamera");
        cameraObject.transform.SetParent(transform, false);

        showcaseCamera = cameraObject.AddComponent<Camera>();
        showcaseCamera.enabled = false;
        showcaseCamera.clearFlags = CameraClearFlags.SolidColor;
        showcaseCamera.backgroundColor = Color.clear;
        showcaseCamera.cullingMask = layer >= 0 ? 1 << layer : ~0;
        showcaseCamera.nearClipPlane = 0.1f;
        showcaseCamera.farClipPlane = 200f;
        showcaseCamera.depth = -50f;
        showcaseCamera.allowHDR = false;
        showcaseCamera.allowMSAA = false;
        showcaseCamera.useOcclusionCulling = false;

        UniversalAdditionalCameraData cameraData = showcaseCamera.GetUniversalAdditionalCameraData();
        cameraData.renderType = CameraRenderType.Base;
        cameraData.renderPostProcessing = false;
        cameraData.renderShadows = false;
        cameraData.requiresColorOption = CameraOverrideOption.Off;
        cameraData.requiresDepthOption = CameraOverrideOption.Off;
        cameraData.antialiasing = AntialiasingMode.None;
        cameraData.volumeLayerMask = 0;
        cameraData.stopNaN = false;
        cameraData.dithering = false;
        cameraData.allowXRRendering = false;
    }

    private void SetupView(ChestOpeningConfig config)
    {
        RectTransform viewRect = view.rectTransform;
        viewRect.sizeDelta = config.showcaseViewSize;
        viewRect.anchoredPosition = config.showcaseViewOffset;

        float canvasScale = view.canvas != null ? view.canvas.scaleFactor : 1f;
        float pixelScale = canvasScale * Mathf.Max(0.1f, config.showcaseRenderScale);
        int width = ToTextureSize(config.showcaseViewSize.x * pixelScale);
        int height = ToTextureSize(config.showcaseViewSize.y * pixelScale);

        renderTexture = RenderTexture.GetTemporary(width, height, 16, RenderTextureFormat.ARGB32);
        renderTexture.filterMode = FilterMode.Bilinear;
        showcaseCamera.targetTexture = renderTexture;
        view.texture = renderTexture;
    }

    private static int ToTextureSize(float pixels)
    {
        return Mathf.Clamp(Mathf.RoundToInt(pixels), MinTextureSize, MaxTextureSize);
    }

    private void FrameCamera(ChestOpeningConfig config)
    {
        float fieldOfView = Mathf.Clamp(config.showcaseFieldOfView, 5f, 120f);
        float radius = Mathf.Max(0.01f, restBounds.extents.magnitude);
        float distance = radius * Mathf.Max(0.2f, config.showcaseFramePadding) / Mathf.Sin(fieldOfView * 0.5f * Mathf.Deg2Rad);
        Vector3 focus = restBounds.center + Vector3.up * (restBounds.size.y * config.showcaseFocusHeight);
        Quaternion rotation = Quaternion.Euler(config.showcaseCameraPitch, config.showcaseCameraYaw, 0f);

        showcaseCamera.fieldOfView = fieldOfView;
        showcaseCamera.transform.SetPositionAndRotation(focus - rotation * Vector3.forward * distance, rotation);
    }
}
