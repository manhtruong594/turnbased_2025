using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IUpdatable
{
    void OnUpdate(float deltaTime);
}

public class Updater : MonoBehaviour
{
    private static Updater instance;
    public static Updater Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("Updater");
                instance = go.AddComponent<Updater>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    private readonly List<IUpdatable> updatables = new List<IUpdatable>();
    private readonly List<IUpdatable> toAdd = new List<IUpdatable>();
    private readonly List<IUpdatable> toRemove = new List<IUpdatable>();
    
    private bool isUpdating;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        isUpdating = true;
        float deltaTime = Time.deltaTime;
        
        for (int i = 0; i < updatables.Count; i++)
        {
            updatables[i]?.OnUpdate(deltaTime);
        }
        
        isUpdating = false;
        ProcessPendingChanges();
    }

    public void Register(IUpdatable updatable)
    {
        if (updatable == null) return;
        
        if (isUpdating)
        {
            if (!toAdd.Contains(updatable))
                toAdd.Add(updatable);
        }
        else
        {
            if (!updatables.Contains(updatable))
                updatables.Add(updatable);
        }
    }

    public void Unregister(IUpdatable updatable)
    {
        if (updatable == null) return;
        
        if (isUpdating)
        {
            if (!toRemove.Contains(updatable))
                toRemove.Add(updatable);
        }
        else
        {
            updatables.Remove(updatable);
        }
    }

    private void ProcessPendingChanges()
    {
        if (toAdd.Count > 0)
        {
            foreach (var updatable in toAdd)
            {
                if (!updatables.Contains(updatable))
                    updatables.Add(updatable);
            }
            toAdd.Clear();
        }

        if (toRemove.Count > 0)
        {
            foreach (var updatable in toRemove)
            {
                updatables.Remove(updatable);
            }
            toRemove.Clear();
        }
    }

    public Coroutine StartManagedCoroutine(IEnumerator coroutine)
    {
        return StartCoroutine(coroutine);
    }

    public void DelayedAction(float delay, System.Action action)
    {
        StartCoroutine(DelayedActionCoroutine(delay, action));
    }
    
    private IEnumerator DelayedActionCoroutine(float delay, System.Action action)
    {
        yield return new WaitForSeconds(delay);
        action?.Invoke();
    }

    public void StopManagedCoroutine(Coroutine coroutine)
    {
        if (coroutine != null)
            StopCoroutine(coroutine);
    }
}
