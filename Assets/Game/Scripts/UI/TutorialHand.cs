using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TutorialHand : MonoBehaviour
{
    [Header("Settings")]
    public float moveDistance = 2.0f;
    public float speed = 2.0f;
    public float fadeSpeed = 5.0f;

    [Header("Dependencies")]
    public InputHandler inputHandler;

    private Vector3 startPos;
    private bool isTutorialDone = false;
    private CanvasGroup canvasGroup;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        int bestScore = PlayerPrefs.GetInt("Brainrot_Best", 0);

        if (bestScore > 0)
        {
            Destroy(gameObject);
            return;
        }

        startPos = transform.position;

        canvasGroup = GetComponent<CanvasGroup>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (isTutorialDone) return;

        float offset = Mathf.Sin(Time.time * speed) * moveDistance;
        transform.position = startPos + new Vector3(offset, 0, 0);

        if (inputHandler != null && inputHandler.IsTouching())
        {
            EndTutorial();
        }
    }

    void EndTutorial()
    {
        isTutorialDone = true;
        StartCoroutine(FadeAndDestroy());
    }

    IEnumerator FadeAndDestroy()
    {
        float alpha = 1f;
        while (alpha > 0)
        {
            alpha -= Time.deltaTime * fadeSpeed;

            if (canvasGroup != null) canvasGroup.alpha = alpha;

            if (spriteRenderer != null)
            {
                Color c = spriteRenderer.color;
                c.a = alpha;
                spriteRenderer.color = c;
            }

            yield return null;
        }
        Destroy(gameObject);
    }
}