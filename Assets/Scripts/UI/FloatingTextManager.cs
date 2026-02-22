using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class FloatingTextManager : MonoBehaviour
{
    public static FloatingTextManager Instance { get; private set; }
    
    [Header("Prefab")]
    [SerializeField] private GameObject floatingTextPrefab;
    
    [Header("Colors")]
    [SerializeField] private Color damageColor = new Color(1f, 0.2f, 0.2f); // Rojo para daño
    [SerializeField] private Color expColor = new Color(0.3f, 1f, 0.3f); // Verde para experiencia
    [SerializeField] private Color coinColor = new Color(1f, 0.84f, 0f); // Dorado para monedas
    [SerializeField] private Color diamondColor = new Color(0.3f, 0.8f, 1f); // Azul cyan para diamantes
    
    [Header("Pool Settings")]
    [SerializeField] private int poolSize = 20;
    
    private Canvas worldCanvas;
    private Queue<FloatingText> textPool = new Queue<FloatingText>();
    private Camera mainCamera;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // Buscar la cámara principal
        FindMainCamera();
        
        CreateWorldCanvas();
        
        if (worldCanvas != null)
        {
            InitializePool();
        }
        else
        {
            Debug.LogError("FloatingTextManager: Failed to create world canvas!");
        }
    }
    
    private void FindMainCamera()
    {
        // Intentar obtener por tag primero
        mainCamera = Camera.main;
        
        // Si no funciona, buscar cualquier cámara activa
        if (mainCamera == null)
        {
            mainCamera = FindFirstObjectByType<Camera>();
        }
    }
    
    private void CreateWorldCanvas()
    {
        // Crear un canvas en modo ScreenSpace - Overlay para los textos flotantes
        GameObject canvasObj = new GameObject("FloatingTextCanvas");
        canvasObj.transform.SetParent(transform);
        canvasObj.transform.localPosition = Vector3.zero;
        
        worldCanvas = canvasObj.AddComponent<Canvas>();
        worldCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        worldCanvas.sortingOrder = 100; // Asegurar que esté encima de otros UI
        
        // Añadir CanvasScaler
        var scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        
        // Añadir GraphicRaycaster (aunque no lo necesitemos para interacción)
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
    }
    
    private void InitializePool()
    {
        if (floatingTextPrefab == null)
        {
            Debug.LogError("FloatingTextManager: FloatingTextPrefab no asignado! Por favor asigna un prefab en el Inspector.");
            return;
        }
        
        for (int i = 0; i < poolSize; i++)
        {
            CreateNewFloatingText();
        }
    }
    
    private FloatingText CreateNewFloatingText()
    {
        GameObject obj = Instantiate(floatingTextPrefab, worldCanvas.transform);
        FloatingText floatingText = obj.GetComponent<FloatingText>();
        
        if (floatingText == null)
        {
            floatingText = obj.AddComponent<FloatingText>();
        }
        
        obj.SetActive(false);
        textPool.Enqueue(floatingText);
        
        return floatingText;
    }
    
    private FloatingText GetFromPool()
    {
        if (textPool.Count == 0)
        {
            return CreateNewFloatingText();
        }
        
        FloatingText text = textPool.Dequeue();
        text.gameObject.SetActive(true);
        return text;
    }
    
    public void ReturnToPool(FloatingText text)
    {
        if (text == null) return;
        
        text.gameObject.SetActive(false);
        textPool.Enqueue(text);
    }
    
    /// <summary>
    /// Muestra texto de daño (rojo)
    /// </summary>
    public void ShowDamage(float damage, Vector3 worldPosition)
    {
        ShowText(Mathf.RoundToInt(damage).ToString(), damageColor, worldPosition);
    }
    
    /// <summary>
    /// Muestra texto de experiencia ganada (verde)
    /// </summary>
    public void ShowExperience(int amount, Vector3 worldPosition)
    {
        ShowText($"+{amount}", expColor, worldPosition);
    }
    
    /// <summary>
    /// Muestra texto de monedas ganadas (dorado)
    /// </summary>
    public void ShowCoins(int amount, Vector3 worldPosition)
    {
        ShowText($"+{amount}", coinColor, worldPosition);
    }
    
    /// <summary>
    /// Muestra texto de diamantes ganados (azul cyan)
    /// </summary>
    public void ShowDiamonds(int amount, Vector3 worldPosition)
    {
        ShowText($"+{amount}", diamondColor, worldPosition);
    }
    
    /// <summary>
    /// Muestra texto personalizado
    /// </summary>
    public void ShowText(string text, Color color, Vector3 worldPosition)
    {
        if (worldCanvas == null)
        {
            Debug.LogWarning("FloatingTextManager: worldCanvas is null. Cannot show floating text.");
            return;
        }
        
        // Si no tenemos cámara, intentar buscarla de nuevo
        if (mainCamera == null)
        {
            FindMainCamera();
            
            if (mainCamera == null)
            {
                Debug.LogWarning("FloatingTextManager: Camera still not found after search attempt.");
                return;
            }
        }
        
        FloatingText floatingText = GetFromPool();
        if (floatingText == null)
        {
            Debug.LogWarning("FloatingTextManager: Could not get FloatingText from pool.");
            return;
        }
        
        // Convertir posición del mundo a posición de pantalla
        Vector3 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);
        
        // Verificar que la posición esté frente a la cámara
        if (screenPosition.z < 0)
        {
            ReturnToPool(floatingText);
            return;
        }
        
        floatingText.Initialize(text, color, screenPosition);
    }
}
