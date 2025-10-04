using UnityEngine;

public class SystemManager : Singleton<SystemManager>
{
    #region Awake Methods
    private void Awake()
    {
        if (CheckOverlap())
        {
            DontDestroySelf();
            SetUpOnAwake();
        }
    }

    private void SetUpOnAwake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        Time.fixedDeltaTime = 1.0f / 60.0f;

        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        Application.runInBackground = true;
    }
    #endregion
}
