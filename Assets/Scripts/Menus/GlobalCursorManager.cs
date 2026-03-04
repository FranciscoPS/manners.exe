using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// Gestiona el cursor globalmente. Cambia a cursor "pointer" cuando el ratón
/// está sobre cualquier Selectable (Button, Toggle, Slider, etc.) activo en la escena.
/// Añadir UNA sola vez en cualquier GameObject persistente o en cada escena.
/// </summary>
public class GlobalCursorManager : MonoBehaviour
{
    public static GlobalCursorManager Instance { get; private set; }

    [Header("Cursors")]
    [Tooltip("Textura del cursor cuando está sobre un botón. Si es null usa el cursor del sistema.")]
    [SerializeField] private Texture2D pointerCursor;
    [SerializeField] private Vector2   pointerHotspot = Vector2.zero;

    private bool isShowingPointer = false;
    private readonly List<RaycastResult> _raycastResults = new List<RaycastResult>(8);

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    private void Update()
    {
        if (EventSystem.current == null) return;
        if (Mouse.current == null) return;

        var pointerData = new PointerEventData(EventSystem.current)
        {
            position = Mouse.current.position.ReadValue()
        };

        _raycastResults.Clear();
        EventSystem.current.RaycastAll(pointerData, _raycastResults);

        bool overInteractable = false;
        foreach (var result in _raycastResults)
        {
            // Buscar cualquier Selectable activo e interactuable en el objeto o sus padres
            var selectable = result.gameObject.GetComponentInParent<Selectable>();
            if (selectable != null && selectable.interactable && selectable.isActiveAndEnabled)
            {
                overInteractable = true;
                break;
            }
        }

        if (overInteractable != isShowingPointer)
        {
            isShowingPointer = overInteractable;
            if (overInteractable)
                Cursor.SetCursor(pointerCursor, pointerHotspot, CursorMode.Auto);
            else
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
    }
}
