using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Detecta qué edificios quedan entre la cámara y los objetivos relevantes
/// (jugador y/o enemigos) y les ordena desvanecerse para que el objetivo siga
/// siendo visible.
///
/// La detección es por screen-space + bounds del renderer (sin física), throttled
/// y registrada en el UpdateManager central del proyecto. Pensada para WebGL:
/// coste de CPU mínimo y cero asignaciones por frame.
///
/// El singleton se autocrea la primera vez que un <see cref="BuildingFader"/> se habilita.
/// </summary>
public class BuildingTransparencyManager : MonoBehaviour, IUpdateable
{
    public static BuildingTransparencyManager Instance { get; private set; }

    [Header("Detección")]
    [Tooltip("Cada cuántos segundos se recalcula la oclusión (no por frame).")]
    [SerializeField] private float detectionInterval = 0.1f;

    [Tooltip("Revelar edificios que tapen al jugador.")]
    [SerializeField] private bool revealForPlayer = true;

    [Tooltip("Revelar edificios que tapen a enemigos cercanos.")]
    [SerializeField] private bool revealForEnemies = true;

    [Tooltip("Radio alrededor del jugador para buscar enemigos candidatos.")]
    [SerializeField] private float enemySearchRadius = 40f;

    [Tooltip("Distancia máxima edificio-jugador para considerarlo (descarta los lejanos).")]
    [SerializeField] private float maxBuildingDistance = 70f;

    [Tooltip("Margen extra (fracción de pantalla) alrededor del rect del edificio.")]
    [Range(0f, 0.25f)]
    [SerializeField] private float screenPadding = 0.04f;

    [Tooltip("El edificio debe estar al menos esta distancia más cerca de la cámara que el objetivo.")]
    [SerializeField] private float depthBias = 0.5f;

    [Header("Fade")]
    [Tooltip("Velocidad de interpolación del desvanecimiento (unidades de fade por segundo).")]
    [SerializeField] private float fadeSpeed = 4f;

    [Tooltip("Opacidad de los edificios cuando tapan algo. 0 = invisible, 1 = opaco. Sube el valor para que se vean más.")]
    [Range(0f, 1f)]
    [SerializeField] private float minVisibleAlpha = 0.3f;

    private static readonly List<BuildingFader> faders = new List<BuildingFader>(64);

    private float timer;
    private Camera cam;
    private Transform player;
    private int enemyMask;
    private readonly Collider[] enemyBuffer = new Collider[64];
    private readonly List<Vector3> targetWorld = new List<Vector3>(32);

    public bool IsActive => isActiveAndEnabled;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        faders.Clear();
        Instance = null;
    }

    public static void Register(BuildingFader fader)
    {
        if (fader == null) return;
        if (!faders.Contains(fader)) faders.Add(fader);
        EnsureExists();
    }

    public static void Unregister(BuildingFader fader)
    {
        faders.Remove(fader);
    }

    public static void EnsureExists()
    {
        if (Instance != null) return;
        var go = new GameObject("[BuildingTransparencyManager]");
        Instance = go.AddComponent<BuildingTransparencyManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        enemyMask = LayerMask.GetMask("Enemy");
    }

    private void OnEnable()
    {
        if (UpdateManager.Instance != null)
            UpdateManager.Instance.Register(this as IUpdateable);
    }

    private void OnDisable()
    {
        if (UpdateManager.Instance != null)
            UpdateManager.Instance.Unregister(this as IUpdateable);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Permite ajustar la opacidad en vivo desde el inspector mientras se juega:
        // reaplica el valor a todos los edificios (incluidos los ya transparentes).
        if (!Application.isPlaying) return;
        for (int i = 0; i < faders.Count; i++)
            faders[i]?.ForceApply(minVisibleAlpha);
    }
#endif

    public void OnUpdate(float deltaTime)
    {
        // Interpolación suave por frame (solo edificios que lo necesitan).
        for (int i = 0; i < faders.Count; i++)
        {
            var f = faders[i];
            if (f != null && f.NeedsTick) f.Tick(deltaTime, fadeSpeed, minVisibleAlpha);
        }

        // Detección throttled.
        timer += deltaTime;
        if (timer < detectionInterval) return;
        timer = 0f;
        Detect();
    }

    private void Detect()
    {
        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null) return;
        }
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        // Reunir objetivos en mundo.
        targetWorld.Clear();
        if (revealForPlayer && player != null)
            targetWorld.Add(player.position + Vector3.up * 0.8f);

        if (revealForEnemies && player != null && enemyMask != 0)
        {
            int n = Physics.OverlapSphereNonAlloc(
                player.position, enemySearchRadius, enemyBuffer, enemyMask, QueryTriggerInteraction.Collide);
            for (int i = 0; i < n; i++)
            {
                if (enemyBuffer[i] != null) targetWorld.Add(enemyBuffer[i].bounds.center);
            }
        }

        Vector3 camPos = cam.transform.position;
        Vector3 playerPos = player != null ? player.position : camPos;

        if (targetWorld.Count == 0)
        {
            for (int i = 0; i < faders.Count; i++) faders[i]?.SetOccluded(false);
            return;
        }

        float maxDistSqr = maxBuildingDistance * maxBuildingDistance;
        float padX = Screen.width * screenPadding;
        float padY = Screen.height * screenPadding;

        for (int b = 0; b < faders.Count; b++)
        {
            var f = faders[b];
            if (f == null) continue;

            Bounds bb = f.WorldBounds;
            if ((bb.center - playerPos).sqrMagnitude > maxDistSqr)
            {
                f.SetOccluded(false);
                continue;
            }

            if (!BoundsToScreenRect(bb, out Rect rect))
            {
                f.SetOccluded(false);
                continue;
            }

            rect.xMin -= padX; rect.xMax += padX;
            rect.yMin -= padY; rect.yMax += padY;

            float buildingCamDist = (bb.center - camPos).magnitude;

            bool occluded = false;
            for (int t = 0; t < targetWorld.Count; t++)
            {
                Vector3 sp = cam.WorldToScreenPoint(targetWorld[t]);
                if (sp.z <= 0f) continue; // detrás de la cámara

                float targetCamDist = (targetWorld[t] - camPos).magnitude;
                if (buildingCamDist < targetCamDist - depthBias &&
                    rect.Contains(new Vector2(sp.x, sp.y)))
                {
                    occluded = true;
                    break;
                }
            }

            f.SetOccluded(occluded);
        }
    }

    /// <summary>Proyecta las 8 esquinas del bounding box a pantalla y devuelve su rect envolvente.</summary>
    private bool BoundsToScreenRect(Bounds b, out Rect rect)
    {
        rect = default;
        Vector3 c = b.center;
        Vector3 e = b.extents;

        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;

        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = c + new Vector3(
                (i & 1) == 0 ? -e.x : e.x,
                (i & 2) == 0 ? -e.y : e.y,
                (i & 4) == 0 ? -e.z : e.z);

            Vector3 sp = cam.WorldToScreenPoint(corner);
            if (sp.z <= 0f) return false; // alguna esquina detrás de la cámara

            if (sp.x < minX) minX = sp.x;
            if (sp.x > maxX) maxX = sp.x;
            if (sp.y < minY) minY = sp.y;
            if (sp.y > maxY) maxY = sp.y;
        }

        rect = new Rect(minX, minY, maxX - minX, maxY - minY);
        return true;
    }
}
