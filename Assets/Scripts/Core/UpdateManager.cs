using UnityEngine;
using System.Collections.Generic;

public interface IUpdateable
{
    void OnUpdate(float deltaTime);
    bool IsActive { get; }
}

public interface IFixedUpdateable
{
    void OnFixedUpdate(float fixedDeltaTime);
    bool IsActive { get; }
}

public interface ILateUpdateable
{
    void OnLateUpdate(float deltaTime);
    bool IsActive { get; }
}

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

    private HashSet<IUpdateable>      updateableSet      = new HashSet<IUpdateable>();
    private HashSet<IFixedUpdateable> fixedUpdateableSet = new HashSet<IFixedUpdateable>();
    private HashSet<ILateUpdateable>  lateUpdateableSet  = new HashSet<ILateUpdateable>();

    private List<IUpdateable> updateablesToAdd = new List<IUpdateable>();
    private List<IUpdateable> updateablesToRemove = new List<IUpdateable>();
    private List<IFixedUpdateable> fixedUpdateablesToAdd = new List<IFixedUpdateable>();
    private List<IFixedUpdateable> fixedUpdateablesToRemove = new List<IFixedUpdateable>();
    private List<ILateUpdateable> lateUpdateablesToAdd = new List<ILateUpdateable>();
    private List<ILateUpdateable> lateUpdateablesToRemove = new List<ILateUpdateable>();

    private bool isUpdating = false;
    private bool isFixedUpdating = false;
    private bool isLateUpdating = false;
    private static bool isQuitting = false;

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

        if (instance == this)
        {
            ClearAll();
            instance = null;
        }
    }

    private void OnApplicationQuit()
    {

        isQuitting = true;

        ClearAll();
    }

    public void Register(IUpdateable updateable)
    {
        if (updateable == null) return;

        if (isUpdating)
        {
            updateablesToRemove.Remove(updateable);
            if (!updateablesToAdd.Contains(updateable)) updateablesToAdd.Add(updateable);
        }
        else
        {
            if (updateableSet.Add(updateable))
                updateables.Add(updateable);
        }
    }

    public void Unregister(IUpdateable updateable)
    {
        if (updateable == null) return;

        if (isUpdating)
        {
            updateablesToAdd.Remove(updateable);
            if (!updateablesToRemove.Contains(updateable)) updateablesToRemove.Add(updateable);
        }
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
        {
            fixedUpdateablesToRemove.Remove(updateable);
            if (!fixedUpdateablesToAdd.Contains(updateable)) fixedUpdateablesToAdd.Add(updateable);
        }
        else if (fixedUpdateableSet.Add(updateable))
            fixedUpdateables.Add(updateable);
    }

    public void Unregister(IFixedUpdateable updateable)
    {
        if (updateable == null) return;

        if (isFixedUpdating)
        {
            fixedUpdateablesToAdd.Remove(updateable);
            if (!fixedUpdateablesToRemove.Contains(updateable)) fixedUpdateablesToRemove.Add(updateable);
        }
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
        {
            lateUpdateablesToRemove.Remove(updateable);
            if (!lateUpdateablesToAdd.Contains(updateable)) lateUpdateablesToAdd.Add(updateable);
        }
        else if (lateUpdateableSet.Add(updateable))
            lateUpdateables.Add(updateable);
    }

    public void Unregister(ILateUpdateable updateable)
    {
        if (updateable == null) return;

        if (isLateUpdating)
        {
            lateUpdateablesToAdd.Remove(updateable);
            if (!lateUpdateablesToRemove.Contains(updateable)) lateUpdateablesToRemove.Add(updateable);
        }
        else
        {
            lateUpdateableSet.Remove(updateable);
            lateUpdateables.Remove(updateable);
        }
    }

    private void Update()
    {
        isUpdating = true;
        float deltaTime = Time.deltaTime;

        for (int i = updateables.Count - 1; i >= 0; i--)
        {
            var item = updateables[i];

            if (item == null)
            {
                updateableSet.Remove(item);
                updateables.RemoveAt(i);
                continue;
            }

            bool isActive = false;
            try
            {

                isActive = item.IsActive;
            }
            catch (System.Exception)
            {

                updateableSet.Remove(item);
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

                    updateableSet.Remove(item);
                    updateables.RemoveAt(i);
                }
                catch (System.Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }

        isUpdating = false;
        ProcessPendingChanges(updateables, updateableSet, updateablesToAdd, updateablesToRemove);
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
                fixedUpdateableSet.Remove(item);
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
                fixedUpdateableSet.Remove(item);
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
                    fixedUpdateableSet.Remove(item);
                    fixedUpdateables.RemoveAt(i);
                }
                catch (System.Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }

        isFixedUpdating = false;
        ProcessPendingChanges(fixedUpdateables, fixedUpdateableSet, fixedUpdateablesToAdd, fixedUpdateablesToRemove);
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
                lateUpdateableSet.Remove(item);
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
                lateUpdateableSet.Remove(item);
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
                    lateUpdateableSet.Remove(item);
                    lateUpdateables.RemoveAt(i);
                }
                catch (System.Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }

        isLateUpdating = false;
        ProcessPendingChanges(lateUpdateables, lateUpdateableSet, lateUpdateablesToAdd, lateUpdateablesToRemove);
    }

    private static void ProcessPendingChanges<T>(List<T> list, HashSet<T> set, List<T> toAdd, List<T> toRemove)
    {
        foreach (var item in toRemove)
        {
            set.Remove(item);
            list.Remove(item);
        }
        toRemove.Clear();

        foreach (var item in toAdd)
        {
            if (item is UnityEngine.Object unityObject && unityObject == null) continue;
            if (set.Add(item)) list.Add(item);
        }
        toAdd.Clear();
    }
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

        updateableSet.Clear();
        fixedUpdateableSet.Clear();
        lateUpdateableSet.Clear();
    }

    public int GetUpdateableCount() => updateables.Count;
    public int GetFixedUpdateableCount() => fixedUpdateables.Count;
    public int GetLateUpdateableCount() => lateUpdateables.Count;
}
