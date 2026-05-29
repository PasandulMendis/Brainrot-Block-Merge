using UnityEngine;
using System.Collections;

public class PopEffect : MonoBehaviour
{
    private Vector3 targetScale;
    private Rigidbody2D rb;

    void Awake()
    {
        targetScale = transform.localScale;
        transform.localScale = Vector3.one * 0.1f;
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        StartCoroutine(AnimatePop());
    }

    IEnumerator AnimatePop()
    {
        float timer = 0f;
        float duration = 0.3f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = timer / duration;

            float scale = 1.0f + (2.0f * Mathf.Pow(progress - 1.0f, 3.0f) + (progress - 1.0f));

            transform.localScale = targetScale * Mathf.Clamp01(scale + 1.0f);

            if (rb != null) rb.WakeUp();

            yield return null;
        }

        transform.localScale = targetScale;
        Destroy(this);
    }
}