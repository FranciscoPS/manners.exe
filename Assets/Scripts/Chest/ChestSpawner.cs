using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Genera un Cofre cerca del centro del mapa cada cierto intervalo (2 min por
/// defecto). Solo existe un cofre a la vez y permanece hasta que el jugador lo
/// recoge. Se autocrea y registra en el UpdateManager; no requiere configuraci\u00f3n
/// en escena.
/// </summary>
public class ChestSpawner : MonoBehaviour, IUpdateable
{
    private static ChestSpawner instance;
    private static bool isQuitting = false;

    [Header("Spawn Timing")]
    [SerializeField] private float spawnInterval = 120f;
    [Tooltip("Retraso del PRIMER cofre tras iniciar la partida. Igual al intervalo normal (2 min).")]
    [SerializeField] private float firstSpawnDelay = 120f;

    [Header("Spawn Position")]
    [SerializeField] private Vector3 centerPoint = Vector3.zero;
    [Tooltip("Radio MÁXIMO alrededor del centro donde puede aparecer el cofre.")]
    [SerializeField] private float spawnRadius = 6f;
    [Tooltip("Radio MÍNIMO: el cofre nunca aparece más cerca del centro que esto (evita que salga siempre en el mismo punto).")]
    [SerializeField] private float minSpawnRadius = 2.5f;
    [SerializeField] private float spawnHeight = 0f;

    [Header("Prefab")]
    [Tooltip("Si se deja vac\u00edo se carga 'Cofre' desde Resources.")]
    [SerializeField] private GameObject chestPrefab;

    private float timer = 0f;
    private bool firstChestSpawned = false;
    private GameObject activeChest;

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

    /// <summary>Llamado por el cofre al ser recogido: reinicia el temporizador.</summary>
    public static void NotifyChestCollected()
    {
        if (instance != null)
        {
            instance.activeChest = null;
            instance.timer = 0f;
        }
    }

    /// <summary>Posicion del cofre activo, si existe. La usa el minimapa para dibujar el icono.</summary>
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
        timer = 0f;
        firstChestSpawned = false;
        activeChest = null;
    }

    public void OnUpdate(float deltaTime)
    {
        // Un solo cofre a la vez: espera a que lo recojan.
        if (activeChest != null) return;

        timer += deltaTime;
        float threshold = firstChestSpawned ? spawnInterval : firstSpawnDelay;
        if (timer < threshold) return;

        // Solo durante una partida real (hay un jugador en escena).
        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO == null)
        {
            timer = 0f;
            return;
        }

        SpawnChest();
        firstChestSpawned = true;
        timer = 0f;
    }

    private void SpawnChest()
    {
        GameObject prefab = chestPrefab != null ? chestPrefab : Resources.Load<GameObject>("Cofre");
        if (prefab == null)
        {
            Debug.LogWarning("[ChestSpawner] No se encontr\u00f3 el prefab 'Cofre' en Resources.");
            return;
        }

        // Ángulo aleatorio + radio aleatorio entre min y max: siempre desplazado del
        // centro y nunca dos veces exactamente en el mismo punto.
        float angle = Random.value * Mathf.PI * 2f;
        float radius = Random.Range(minSpawnRadius, spawnRadius);
        Vector3 pos = centerPoint + new Vector3(Mathf.Cos(angle) * radius, spawnHeight, Mathf.Sin(angle) * radius);

        activeChest = Instantiate(prefab, pos, prefab.transform.rotation);

        if (activeChest.GetComponent<ChestPickup>() == null)
        {
            activeChest.AddComponent<ChestPickup>();
        }

        ChestAnnouncement.Show("¡Un cofre ha aparecido en el centro del mapa!");
    }
}
