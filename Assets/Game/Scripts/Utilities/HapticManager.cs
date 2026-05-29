using UnityEngine;
using System.Collections;

public class HapticManager : MonoBehaviour
{
    private bool isHapticsEnabled = true;

    [Header("Old Phone Fallback Settings")]
    [Tooltip("Vibration duration in milliseconds. 15-30ms creates a crisp 'pop' on old phones without feeling mushy!")]
    public long fallbackMergeVibrateMs = 20;

    [Tooltip("Vibration duration for Game Over. 400ms is a solid, long rumble.")]
    public long gameOverVibrateMs = 400;

#if UNITY_ANDROID && !UNITY_EDITOR
    private AndroidJavaObject vibrator;
#endif

    void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Failed to initialize Android Vibrator: " + e.Message);
        }
#endif
    }

    public void SetHapticsEnabled(bool isEnabled)
    {
        isHapticsEnabled = isEnabled;
    }

    public void TriggerMergeHaptic()
    {
        if (!isHapticsEnabled) return;

#if UNITY_ANDROID && !UNITY_EDITOR
        try 
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject decorView = currentActivity.Call<AndroidJavaObject>("getWindow").Call<AndroidJavaObject>("getDecorView");
            
            bool success = decorView.Call<bool>("performHapticFeedback", 6); 

            if (!success)
            {
                CustomVibrate(fallbackMergeVibrateMs);
            }
        } 
        catch 
        { 
            CustomVibrate(fallbackMergeVibrateMs); 
        }
#else
#endif
    }

    public void TriggerGameOverHaptic()
    {
        if (!isHapticsEnabled) return;

#if UNITY_ANDROID && !UNITY_EDITOR
        CustomVibrate(gameOverVibrateMs);
#else
        Handheld.Vibrate();
#endif
    }

    private void CustomVibrate(long milliseconds)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (vibrator != null)
        {
            try
            {
                if (vibrator.Call<bool>("hasVibrator"))
                {
                    vibrator.Call("vibrate", milliseconds);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Custom Vibrate failed: " + e.Message);
            }
        }
#endif
    }

    public void PlayShakeRattle(float duration)
    {
        if (!isHapticsEnabled) return;
        StopAllCoroutines();
        StartCoroutine(RattleRoutine(duration));
    }

    IEnumerator RattleRoutine(float duration)
    {
        float timer = 0f;
        float interval = 0.08f;

        while (timer < duration)
        {
            TriggerMergeHaptic();
            timer += interval;
            yield return new WaitForSeconds(interval);
        }
    }
}