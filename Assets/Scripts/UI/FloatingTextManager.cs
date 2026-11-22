using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class FloatingTextManager : MonoBehaviour
{
    public static FloatingTextManager Instance { get; private set; }

    [Header("Prefab")]
    [SerializeField] private GameObject floatingTextPrefab;

    [Header("Colors")]
    [SerializeField] private Color damageColor = new Color(1f, 0.2f, 0.2f);
    [SerializeField] private Color expColor = new Color(0.3f, 1f, 0.3f);
    [SerializeField] private Color coinColor = new Color(1f, 0.84f, 0f);
    [SerializeField] private Color diamondColor = new Color(0.3f, 0.8f, 1f);

    [Header("Pool Settings")]
    [SerializeField] private int poolSize = 20;
    [Tooltip("Máximo de números flotantes en pantalla para que aparezca un número de daño nuevo. Con cientos de golpes por segundo (hordas del overtime) se omiten los demás: serían un muro de números y costarían varios ms por frame. Experiencia, monedas y diamantes se muestran siempre.")]
    [Min(1)] [SerializeField] private int maxDamageNumbers = 60;

    [SerializeField] private Canvas worldCanvas;
    private Queue<FloatingText> textPool = new Queue<FloatingText>();
    private Camera mainCamera;
    private int activeTexts;

    public int ActiveTexts => activeTexts;
    public int MaxDamageNumbers => maxDamageNumbers;

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

        FindMainCamera();

        if (worldCanvas != null)
        {
            InitializePool();
        }
        else Debug.LogError("Asigna el canvas de números flotantes de la escena.", this);
    }

    private void FindMainCamera()
    {

        mainCamera = Camera.main;

        if (mainCamera == null)
        {
            mainCamera = FindFirstObjectByType<Camera>();
        }
    }

    private void InitializePool()
    {
        if (floatingTextPrefab == null)
        {
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

        if (floatingText == null) { Destroy(obj); return null; }

        obj.SetActive(false);
        textPool.Enqueue(floatingText);

        return floatingText;
    }

    private FloatingText GetFromPool()
    {
        if (textPool.Count == 0)
        {
            CreateNewFloatingText();
        }
        if (textPool.Count == 0) return null;
        FloatingText text = textPool.Dequeue();
        text.gameObject.SetActive(true);
        activeTexts++;
        return text;
    }

    public void ReturnToPool(FloatingText text)
    {
        if (text == null) return;

        if (text.gameObject.activeSelf) activeTexts = Mathf.Max(0, activeTexts - 1);
        text.gameObject.SetActive(false);
        textPool.Enqueue(text);
    }

    public void ShowDamage(float damage, Vector3 worldPosition)
    {
        if (activeTexts >= maxDamageNumbers) return;
        ShowText(Mathf.RoundToInt(damage).ToString(), damageColor, worldPosition);
    }

    public void ShowExperience(int amount, Vector3 worldPosition)
    {
        ShowText($"+{amount}", expColor, worldPosition);
    }

    public void ShowCoins(int amount, Vector3 worldPosition)
    {
        ShowText($"+{amount}", coinColor, worldPosition);
    }

    public void ShowDiamonds(int amount, Vector3 worldPosition)
    {
        ShowText($"+{amount}", diamondColor, worldPosition);
    }

    public void ShowText(string text, Color color, Vector3 worldPosition)
    {
        if (worldCanvas == null)
        {
            return;
        }

        if (mainCamera == null)
        {
            FindMainCamera();

            if (mainCamera == null)
            {
                return;
            }
        }

        Vector3 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);

        if (screenPosition.z < 0 || screenPosition.x < 0f || screenPosition.y < 0f || screenPosition.x > Screen.width || screenPosition.y > Screen.height)
        {
            return;
        }

        FloatingText floatingText = GetFromPool();
        if (floatingText == null)
        {
            return;
        }

        floatingText.Initialize(text, color, screenPosition);
    }
}
