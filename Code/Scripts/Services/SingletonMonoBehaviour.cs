using UnityEngine;

/// <summary>
/// Base class for singleton MonoBehaviours that persist across scenes.
/// Provides consistent initialization and prevents duplicate instances.
/// </summary>
/// <typeparam name="T">The type of the singleton class</typeparam>
public abstract class SingletonMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;

    /// <summary>
    /// Gets the singleton instance. Attempts to find an existing instance if none is cached.
    /// Note: If called before any instance exists, this will return null. 
    /// Instances should be created in scene or via DontDestroyOnLoad.
    /// </summary>
    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<T>();
                if (instance == null)
                {
                    Debug.LogWarning($"[SingletonMonoBehaviour] No instance of {typeof(T).Name} found. Ensure it exists in the scene or is created before access.");
                }
            }
            return instance;
        }
    }

    protected virtual void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        instance = this as T;
        DontDestroyOnLoad(gameObject);
        OnAwakeAfterInit();
    }

    protected virtual void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    /// <summary>
    /// Called after singleton initialization in Awake.
    /// Override this instead of Awake when extending.
    /// </summary>
    protected virtual void OnAwakeAfterInit()
    {
    }
}
