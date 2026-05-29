using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneFader : MonoBehaviour
{
    [Header("UI Reference")]
    public CanvasGroup blackPanelGroup;

    [Header("Settings")]
    public float fadeDuration = 0.5f;
    public bool fadeOnStart = true;

    void Start()
    {
        if (fadeOnStart)
        {
            StartCoroutine(FadeIn());
        }
        else
        {
            blackPanelGroup.alpha = 0f;
            blackPanelGroup.blocksRaycasts = false;
        }
    }

    public void FadeToScene(string sceneName)
    {
        StartCoroutine(FadeOutAndLoad(sceneName));
    }

    IEnumerator FadeIn()
    {
        blackPanelGroup.alpha = 1f;
        blackPanelGroup.blocksRaycasts = true;

        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            blackPanelGroup.alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            yield return null;
        }

        blackPanelGroup.alpha = 0f;
        blackPanelGroup.blocksRaycasts = false;
    }

    IEnumerator FadeOutAndLoad(string sceneName)
    {
        blackPanelGroup.blocksRaycasts = true;

        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            blackPanelGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
            yield return null;
        }

        blackPanelGroup.alpha = 1f;

        SceneManager.LoadScene(sceneName);
    }
}