using UnityEngine;

public class PulseEffect : MonoBehaviour
{
    [Header("Settings")]
    public float pulseSpeed = 3.0f;
    public float pulseSize = 1.1f;

    private Vector3 originalScale;

    void Start()
    {
        originalScale = transform.localScale;
    }

    void Update()
    {
        float scaleOffset = Mathf.PingPong(Time.time * (pulseSpeed * 0.1f), pulseSize - 1.0f);
        float finalScale = 1.0f + scaleOffset;

        transform.localScale = originalScale * finalScale;
    }

    void OnEnable()
    {
        if (originalScale != Vector3.zero)
            transform.localScale = originalScale;
    }
}