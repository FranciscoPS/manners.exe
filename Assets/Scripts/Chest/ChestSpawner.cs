using UnityEngine;
using UnityEngine.SceneManagement;

public class ChestSpawner : MonoBehaviour, IUpdateable
{
    private static ChestSpawner instance;
    private static bool isQuitting = false;

    [Header("Spawn Timing")]
    [Tooltip("Cada cuántos segundos de JUEGO aparece un cofre. Cuenta el cronómetro de partida, no el tiempo real: si se pausa, NO avanza.")]
    [SerializeField] private float spawnInterval = 60f;
    [Tooltip("Segundo de JUEGO en que aparece el PRIMER cofre tras iniciar la partida (igual al intervalo normal: 1 min).")]
    [SerializeField] private float firstSpawnDelay = 60f;

    [Header("Spawn Position")]
    [SerializeField] private Vector3 centerPoint = Vector3.zero;
    [Tooltip("Radio MÁXIMO alrededor del centro donde puede aparecer el cofre.")]
    [SerializeField] private float spawnRadius = 6f;
    [Tooltip("Radio MÍNIMO: el cofre nunca aparece más cerca del centro que esto (evita que salga siempre en el mismo punto).")]
    [SerializeField] private float minSpawnRadius = 2.5f;
    [Tooltip("Separación angular mínima (grados) respecto al cofre anterior: garantiza que dos cofres seguidos NO salgan en la misma dirección.")]
    [SerializeField] private float minAngularSeparationDeg = 100f;
    [SerializeField] private float spawnHeight = 0f;

    [Header("Prefab")]
    [Tooltip("Si se deja vacío se carga 'Cofre' desde Resources.")]
    [SerializeField] private GameObject chestPrefab;

    private float nextSpawnTime = 0f;
    private Transform cachedPlayer;
    private float lastSpawnAngle = -999f;
    private GameObject activeChest;
    private float timer;

    public bool IsActive => this != null && enabled;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        isQuitting = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        EnsureExists();
    }

    public static void EnsureExists()
    {
        if (isQuitting || instance != null) return;

        GameObject go = new GameObject("ChestSpawner");
        instance = go.AddComponent<ChestSpawner>();
        DontDestroyOnLoad(go);
    }

    /// <summary>Llamado por LevelUpManager cuando el jugador CONFIRMA la mejora del cofre.</summary>
    public static void CollectActiveChest()
    {
        if (instance == null) return;
        if (instance.activeChest != null)
        {
            ChestPickup pickup = instance.activeChest.GetComponent<ChestPickup>();
            if (pickup != null)
            {
                pickup.OnCollected();
            }

            // Reiniciar referencia y temporizador
            instance.activeChest = null;
            instance.timer = 0f;
        }
    }

    /// <summary>
    /// Notifica al cofre activo que la selección fue cerrada sin tomar la mejora.
    /// LevelUpManager.CloseLevelUp() llamará a este método.
    /// </summary>
    public static void NotifyChestSelectionClosed()
    {
        if (instance == null) return;
        if (instance.activeChest != null)
        {
            ChestPickup pickup = instance.activeChest.GetComponent<ChestPickup>();
            if (pickup != null)
            {
                pickup.OnSelectionClosed();
            }
        }
    }

    public static void NotifyChestCollected()
    {
        if (instance != null)
        {
            instance.activeChest = null;
            instance.nextSpawnTime = GetCurrentGameTime() + instance.spawnInterval;
        }
    }

    private static float GetCurrentGameTime()
    {
        return GameTimeManager.Instance != null ? GameTimeManager.Instance.GetGameTime() : 0f;
    }

    public static bool TryGetActiveChestPosition(out Vector3 position)
    {
        if (instance != null && instance.activeChest != null)
        {
            position = instance.activeChest.transform.position;
            return true;
        }

        position = Vector3.zero;
        return false;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;

        nextSpawnTime = firstSpawnDelay;

        if (UpdateManager.Instance != null)
            UpdateManager.Instance.Register(this);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            if (UpdateManager.Instance != null)
                UpdateManager.Instance.Unregister(this);

            instance = null;
        }
    }

    private void OnApplicationQuit()
    {
        isQuitting = true;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        nextSpawnTime = firstSpawnDelay;
        cachedPlayer = null;
        lastSpawnAngle = -999f;
        activeChest = null;
    }

    public void OnUpdate(float deltaTime)
    {
        if (activeChest != null) return;

        if (cachedPlayer == null)
        {
            GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO == null) return;
            cachedPlayer = playerGO.transform;
        }

        if (GetCurrentGameTime() < nextSpawnTime) return;

        SpawnChest();
        nextSpawnTime = GetCurrentGameTime() + spawnInterval;
    }

    private void SpawnChest()
    {
        GameObject prefab = chestPrefab != null ? chestPrefab : Resources.Load<GameObject>("Cofre");
        if (prefab == null)
        {
            Debug.LogWarning("[ChestSpawner] No se encontró el prefab 'Cofre' en Resources.");
            return;
        }

        float angle;
        if (lastSpawnAngle < -900f)
        {
            angle = Random.value * Mathf.PI * 2f;
        }
        else
        {
            float minSep = minAngularSeparationDeg * Mathf.Deg2Rad;
            float offset = Random.Range(minSep, Mathf.PI * 2f - minSep);
            angle = lastSpawnAngle + offset;
        }
        lastSpawnAngle = angle;

        float radius = Random.Range(minSpawnRadius, spawnRadius);
        Vector3 pos = centerPoint + new Vector3(Mathf.Cos(angle) * radius, spawnHeight, Mathf.Sin(angle) * radius);

        activeChest = Instantiate(prefab, pos, prefab.transform.rotation);
        GameEvents.TriggerChestSpawned();

        if (activeChest.GetComponent<ChestPickup>() == null)
        {
            activeChest.AddComponent<ChestPickup>();
        }

        ChestAnnouncement.Show("¡Un cofre ha aparecido en el centro del mapa!");
    }
}
