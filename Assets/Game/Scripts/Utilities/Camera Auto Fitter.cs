using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraAutoFit : MonoBehaviour
{
    [Header("Target Aspect Ratio")]
    [Tooltip("The standard width ratio (e.g., 9 for 9:16 portrait)")]
    public float targetAspectWidth = 9f;
    [Tooltip("The standard height ratio (e.g., 16 for 9:16 portrait)")]
    public float targetAspectHeight = 16f;

    [Header("Camera Settings")]
    [Tooltip("What is the Camera's Size when looking at a normal phone?")]
    public float defaultOrthographicSize = 5.5f;

    void Start()
    {
        AdjustCamera();
    }

#if UNITY_EDITOR
    void Update()
    {
        AdjustCamera();
    }
#endif

    void AdjustCamera()
    {
        Camera cam = GetComponent<Camera>();
        if (cam == null) return;

        float targetAspect = targetAspectWidth / targetAspectHeight;
        float windowAspect = (float)Screen.width / (float)Screen.height;

        float scaleHeight = targetAspect / windowAspect;

        if (scaleHeight < 1f)
        {
            cam.orthographicSize = defaultOrthographicSize;
        }
        else
        {
            cam.orthographicSize = defaultOrthographicSize * scaleHeight;
        }
    }
}