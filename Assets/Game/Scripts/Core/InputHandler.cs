using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{
    [Header("Horizontal Constraints")]
    [Tooltip("How far left/right the spawner can go")]
    public float xLimit = 3.0f;

    [Header("Vertical Constraints (Safe Zones)")]
    [Tooltip("Any touch ABOVE this Y line will be ignored (Top UI Protection)")]
    public float inputMaxY = 4.0f;

    [Tooltip("Any touch BELOW this Y line will be ignored (Bottom UI Protection)")]
    public float inputMinY = -4.0f;

    [Header("Debug Visuals")]
    public bool showGizmos = true;

    private Camera mainCamera;

    private bool isCurrentlyTouching = false;
    private bool wasReleasedThisFrame = false;
    private Vector2 currentScreenPos = Vector2.zero;

    private int trackedTouchId = -1;

    void Awake()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        wasReleasedThisFrame = false;
        bool foundActiveTouch = false;

        if (Touchscreen.current != null && Touchscreen.current.touches.Count > 0)
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    trackedTouchId = touch.touchId.ReadValue();
                    isCurrentlyTouching = true;
                }
            }

            foreach (var touch in Touchscreen.current.touches)
            {
                if (touch.touchId.ReadValue() == trackedTouchId)
                {
                    var phase = touch.phase.ReadValue();

                    if (phase == UnityEngine.InputSystem.TouchPhase.Began ||
                        phase == UnityEngine.InputSystem.TouchPhase.Moved ||
                        phase == UnityEngine.InputSystem.TouchPhase.Stationary)
                    {
                        foundActiveTouch = true;
                        currentScreenPos = touch.position.ReadValue();
                    }
                    else if (phase == UnityEngine.InputSystem.TouchPhase.Ended)
                    {
                        if (isCurrentlyTouching)
                        {
                            wasReleasedThisFrame = true;
                            currentScreenPos = touch.position.ReadValue();
                        }
                        isCurrentlyTouching = false;
                        trackedTouchId = -1;
                    }
                    else if (phase == UnityEngine.InputSystem.TouchPhase.Canceled ||
                             phase == UnityEngine.InputSystem.TouchPhase.None)
                    {
                        isCurrentlyTouching = false;
                        trackedTouchId = -1;
                    }

                    break;
                }
            }

            if (!foundActiveTouch && !wasReleasedThisFrame)
            {
                isCurrentlyTouching = false;
            }
        }
        else if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                isCurrentlyTouching = true;
            }

            if (Mouse.current.leftButton.isPressed)
            {
                foundActiveTouch = true;
                currentScreenPos = Mouse.current.position.ReadValue();
            }
            else if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                if (isCurrentlyTouching)
                {
                    wasReleasedThisFrame = true;
                    currentScreenPos = Mouse.current.position.ReadValue();
                }
                isCurrentlyTouching = false;
            }
        }
    }

    public bool IsTouching()
    {
        if (!isCurrentlyTouching) return false;

        Vector3 worldPos = GetCurrentWorldPos();

        if (worldPos.y > inputMaxY) return false;
        if (worldPos.y < inputMinY) return false;

        return true;
    }

    public bool WasReleased()
    {
        if (!wasReleasedThisFrame) return false;

        Vector3 worldPos = GetCurrentWorldPos();

        if (worldPos.y > inputMaxY) return false;
        if (worldPos.y < inputMinY) return false;

        return true;
    }

    public Vector3 GetClampedPosition(float currentY)
    {
        Vector3 worldPos = GetCurrentWorldPos();
        float newX = Mathf.Clamp(worldPos.x, -xLimit, xLimit);
        return new Vector3(newX, currentY, 0);
    }

    private Vector3 GetCurrentWorldPos()
    {
        if (mainCamera == null) return Vector3.zero;
        return mainCamera.ScreenToWorldPoint(currentScreenPos);
    }

    void OnDrawGizmos()
    {
        if (!showGizmos) return;
        Gizmos.color = Color.green;
        Gizmos.DrawLine(new Vector3(-xLimit, -10, 0), new Vector3(-xLimit, 10, 0));
        Gizmos.DrawLine(new Vector3(xLimit, -10, 0), new Vector3(xLimit, 10, 0));
        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector3(-10, inputMaxY, 0), new Vector3(10, inputMaxY, 0));
        Gizmos.DrawLine(new Vector3(-10, inputMinY, 0), new Vector3(10, inputMinY, 0));
    }
}