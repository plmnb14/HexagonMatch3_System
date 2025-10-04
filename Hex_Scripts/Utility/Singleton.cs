using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    #region Fields
    public static T Instance
    {
        get 
        { 
            if(null == _instance)
                _instance = FindAnyObjectByType<T>();

            return _instance;
        }
    }
    private static T _instance;
    #endregion

    #region Methods
    protected bool CheckOverlap()
    {
        if(Instance != this)
        {
            Destroy(this);
            return false;
        }

        return true;
    }

    protected void DontDestroySelf() { DontDestroyOnLoad(this); }
    #endregion
}
