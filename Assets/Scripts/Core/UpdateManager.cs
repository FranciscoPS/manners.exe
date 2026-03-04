using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Interface para objetos que necesitan actualización por frame
/// </summary>
public interface IUpdateable
{
    void OnUpdate(float deltaTime);
    bool IsActive { get; }
}

/// <summary>
/// Interface para objetos que necesitan actualización de física
/// </summary>
public interface IFixedUpdateable
{
    void OnFixedUpdate(float fixedDeltaTime);
    bool IsActive { get; }
}

/// <summary>
/// Interface para objetos que necesitan actualización después de Update
/// </summary>
public interface ILateUpdateable
{
    void OnLateUpdate(float deltaTime);
    bool IsActive { get; }
}

/// <summary>
/// Manager centralizado de Updates - reduce 24 Updates a 1 solo
/// Mejora rendimiento significativamente especialmente en WebGL
/// </summary>
public class UpdateManager : MonoBehaviour
{
    private static UpdateManager instance;
    public static UpdateManager Instance
    {
        get
        {
            if (instance == null && !isQuitting)
            {
                GameObject go = new GameObject("[UpdateManager]");
                instance = go.AddComponent<UpdateManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }
    
    private List<IUpdateable> updateables = new List<IUpdateable>(100);
    private List<IFixedUpdateable> fixedUpdateables = new List<IFixedUpdateable>(50);
    private List<ILateUpdateable> lateUpdateables = new List<ILateUpdateable>(20);
    
    // HashSets para deduplicación O(1) en Register.
    // List.Contains es O(n) — con 150 objetos registrados y 30 spawns en burst = 4500 comparaciones.
    private HashSet<IUpdateable>      updateableSet      = new HashSet<IUpdateable>();
    private HashSet<IFixedUpdateable> fixedUpdateableSet = new HashSet<IFixedUpdateable>();
    private HashSet<ILateUpdateable>  lateUpdateableSet  = new HashSet<ILateUpdateable>();
    
    // Listas temporales para añadir/remover sin modificar durante iteración
    private List<IUpdateable> updateablesToAdd = new List<IUpdateable>();
    private List<IUpdateable> updateablesToRemove = new List<IUpdateable>();
    private List<IFixedUpdateable> fixedUpdateablesToAdd = new List<IFixedUpdateable>();
    private List<IFixedUpdateable> fixedUpdateablesToRemove = new List<IFixedUpdateable>();
    private List<ILateUpdateable> lateUpdateablesToAdd = new List<ILateUpdateable>();
    private List<ILateUpdateable> lateUpdateablesToRemove = new List<ILateUpdateable>();
    
    private bool isUpdating = false;
    private bool isFixedUpdating = false;
    private bool isLateUpdating = false;
    private static bool isQuitting = false; // Flag para evitar warnings durante shutdown
    
    /// <summary>
    /// Resetea el estado estático cuando Unity reinicia el dominio de scripting (común en el editor)
    /// Esto resuelve el warning: "Some objects were not cleaned up when closing the scene"
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        isQuitting = false;
    }
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    private void OnDestroy()
    {
        // Limpiar la instancia si somos el singleton actual
        if (instance == this)
        {
            ClearAll();
            instance = null;
        }
    }
    
    private void OnApplicationQuit()
    {
        // Marcar que estamos cerrando para evitar recreación del singleton
        isQuitting = true;
        
        // Limpiar listas para evitar referencias colgantes
        ClearAll();
    }
    
    // ===== REGISTRATION =====
    
    public void Register(IUpdateable updateable)
    {
        if (updateable == null) return;
        
        if (isUpdating)
            updateablesToAdd.Add(updateable);
        else if (updateableSet.Add(updateable)) // HashSet.Add devuelve false si ya existe: O(1)
            updateables.Add(updateable);
    }
    
    public void Unregister(IUpdateable updateable)
    {
        if (updateable == null) return;
        
        if (isUpdating)
            updateablesToRemove.Add(updateable);
        else
        {
            updateableSet.Remove(updateable);
            updateables.Remove(updateable);
        }
    }
    
    public void Register(IFixedUpdateable updateable)
    {
        if (updateable == null) return;
        
        if (isFixedUpdating)
            fixedUpdateablesToAdd.Add(updateable);
        else if (fixedUpdateableSet.Add(updateable))
            fixedUpdateables.Add(updateable);
    }
    
    public void Unregister(IFixedUpdateable updateable)
    {
        if (updateable == null) return;
        
        if (isFixedUpdating)
            fixedUpdateablesToRemove.Add(updateable);
        else
        {
            fixedUpdateableSet.Remove(updateable);
            fixedUpdateables.Remove(updateable);
        }
    }
    
    public void Register(ILateUpdateable updateable)
    {
        if (updateable == null) return;
        
        if (isLateUpdating)
            lateUpdateablesToAdd.Add(updateable);
        else if (lateUpdateableSet.Add(updateable))
            lateUpdateables.Add(updateable);
    }
    
    public void Unregister(ILateUpdateable updateable)
    {
        if (updateable == null) return;
        
        if (isLateUpdating)
            lateUpdateablesToRemove.Add(updateable);
        else
        {
            lateUpdateableSet.Remove(updateable);
            lateUpdateables.Remove(updateable);
        }
    }
    
    // ===== UPDATE LOOPS =====
    
    private void Update()
    {
        isUpdating = true;
        float deltaTime = Time.deltaTime;
        
        for (int i = updateables.Count - 1; i >= 0; i--)
        {
            var item = updateables[i];
            
            // Si la referencia del UnityEngine.Object fue destruida, eliminarla de la lista
            if (item == null)
            {
                updateables.RemoveAt(i);
                continue;
            }
            
            bool isActive = false;
            try
            {
                // Algunas implementaciones pueden lanzar MissingReferenceException si el objeto fue destruido.
                isActive = item.IsActive;
            }
            catch (System.Exception)
            {
                // Si hay cualquier excepción al consultar IsActive, asumimos referencia inválida y eliminamos.
                updateables.RemoveAt(i);
                continue;
            }
            
            if (isActive)
            {
                try
                {
                    item.OnUpdate(deltaTime);
                }
                catch (MissingReferenceException)
                {
                    // Intentaron acceder a un UnityEngine.Object ya destruido -> eliminar referencia
                    updateables.RemoveAt(i);
                }
                catch (System.Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }
        
        isUpdating = false;
        ProcessPendingChanges(ref updateables, ref updateablesToAdd, ref updateablesToRemove);
    }
    
    private void FixedUpdate()
    {
        isFixedUpdating = true;
        float fixedDeltaTime = Time.fixedDeltaTime;
        
        for (int i = fixedUpdateables.Count - 1; i >= 0; i--)
        {
            var item = fixedUpdateables[i];
            
            if (item == null)
            {
                fixedUpdateables.RemoveAt(i);
                continue;
            }
            
            bool isActive = false;
            try
            {
                isActive = item.IsActive;
            }
            catch (System.Exception)
            {
                fixedUpdateables.RemoveAt(i);
                continue;
            }
            
            if (isActive)
            {
                try
                {
                    item.OnFixedUpdate(fixedDeltaTime);
                }
                catch (MissingReferenceException)
                {
                    fixedUpdateables.RemoveAt(i);
                }
                catch (System.Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }
        
        isFixedUpdating = false;
        ProcessPendingChanges(ref fixedUpdateables, ref fixedUpdateablesToAdd, ref fixedUpdateablesToRemove);
    }
    
    private void LateUpdate()
    {
        isLateUpdating = true;
        float deltaTime = Time.deltaTime;
        
        for (int i = lateUpdateables.Count - 1; i >= 0; i--)
        {
            var item = lateUpdateables[i];
            
            if (item == null)
            {
                lateUpdateables.RemoveAt(i);
                continue;
            }
            
            bool isActive = false;
            try
            {
                isActive = item.IsActive;
            }
            catch (System.Exception)
            {
                lateUpdateables.RemoveAt(i);
                continue;
            }
            
            if (isActive)
            {
                try
                {
                    item.OnLateUpdate(deltaTime);
                }
                catch (MissingReferenceException)
                {
                    lateUpdateables.RemoveAt(i);
                }
                catch (System.Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }
        
        isLateUpdating = false;
        ProcessPendingChanges(ref lateUpdateables, ref lateUpdateablesToAdd, ref lateUpdateablesToRemove);
    }
    
    private void ProcessPendingChanges<T>(ref List<T> list, ref List<T> toAdd, ref List<T> toRemove)
    {
        if (toAdd.Count > 0)
        {
            foreach (var item in toAdd)
            {
                if (!list.Contains(item))
                    list.Add(item);
            }
            toAdd.Clear();
        }
        
        if (toRemove.Count > 0)
        {
            foreach (var item in toRemove)
            {
                list.Remove(item);
            }
            toRemove.Clear();
        }
        // La pasada extra de nulos fue eliminada: iteraba TODA la lista cada frame aunque
        // no hubiera nulos. Los nulos se eliminan inline durante el loop de Update/FixedUpdate/LateUpdate.
    }
    
    /// <summary>
    /// Limpia todas las referencias (útil para scene transitions)
    /// </summary>
    public void ClearAll()
    {
        updateables.Clear();
        fixedUpdateables.Clear();
        lateUpdateables.Clear();
        updateablesToAdd.Clear();
        updateablesToRemove.Clear();
        fixedUpdateablesToAdd.Clear();
        fixedUpdateablesToRemove.Clear();
        lateUpdateablesToAdd.Clear();
        lateUpdateablesToRemove.Clear();
        // Limpiar HashSets de deduplicación también
        updateableSet.Clear();
        fixedUpdateableSet.Clear();
        lateUpdateableSet.Clear();
    }
    
    // Debug info
    public int GetUpdateableCount() => updateables.Count;
    public int GetFixedUpdateableCount() => fixedUpdateables.Count;
    public int GetLateUpdateableCount() => lateUpdateables.Count;
}
