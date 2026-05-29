using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.Video;
using System.Collections;
using System;

public class LoadingScreenManager : MonoBehaviour
{
    [Header("Loading Targets")]
    public string sceneToLoad = "MainMenu";

    [Tooltip("Artificial delay so players actually see your logo/tips even on fast phones.")]
    public float minimumLoadTime = 2.5f;

    [Header("UI References")]
    public Image loadingBarFill;
    public TextMeshProUGUI progressText;
    public TextMeshProUGUI tipsText;

    [Header("Background Setup (Optional)")]
    public VideoPlayer backgroundVideo;
    public GameObject staticBackgroundImage;

    [Header("Loading Tips")]
    public string[] gameTips = new string[]
    {
        "Tip: Shake your phone to save Blocks stuck in corners!",
        "Tip: Higher tier Blocks drop more coins!",
        "Tip: Unlock new Block Packs in the Main Menu.",
        "Tip: The Anvil destroys everything underneath it. Use it wisely.",
        "Tip: Check the Shop to equip hilarious Voice Effects!",
        "Tip: VIP players get 80% off Anvils and Shakes!",
        "Loading Blocks...",
        "Polishing the Anvil...",
        "Connecting to Servers..."
    };

    private bool isCloudSyncFinished = false;

    void Start()
    {
        if (backgroundVideo != null && backgroundVideo.clip != null)
        {
            backgroundVideo.Play();
            if (staticBackgroundImage != null) staticBackgroundImage.SetActive(false);
        }
        else
        {
            if (staticBackgroundImage != null) staticBackgroundImage.SetActive(true);
        }

        StartCoroutine(CloudSyncRoutine());

        StartCoroutine(LoadSceneAsync());

        if (tipsText != null && gameTips.Length > 0)
        {
            StartCoroutine(CycleTipsRoutine());
        }
    }

    IEnumerator CloudSyncRoutine()
    {
        yield return new WaitForSeconds(0.5f);

        bool hasInternet = Application.internetReachability != NetworkReachability.NotReachable;

        if (hasInternet && PlayGamesManager.Instance != null)
        {
            PlayGamesManager.Instance.SignInAndSync(() => 
            {
                isCloudSyncFinished = true;
            });

            float timeout = 4.0f;
            while (!isCloudSyncFinished && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }
        }
        else
        {
            isCloudSyncFinished = true;
        }

        isCloudSyncFinished = true;
    }

    IEnumerator LoadSceneAsync()
    {
        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(sceneToLoad);
        asyncOperation.allowSceneActivation = false;

        float elapsedTime = 0f;
        float targetProgress = 0f;
        float displayedProgress = 0f;

        while (!asyncOperation.isDone)
        {
            elapsedTime += Time.deltaTime;
            targetProgress = Mathf.Clamp01(asyncOperation.progress / 0.9f);
            displayedProgress = Mathf.MoveTowards(displayedProgress, targetProgress, 2f * Time.deltaTime);

            if (loadingBarFill != null) loadingBarFill.fillAmount = displayedProgress;
            if (progressText != null) progressText.text = "LOADING " + Mathf.RoundToInt(displayedProgress * 100f) + "%";

            if (asyncOperation.progress >= 0.9f && elapsedTime >= minimumLoadTime && isCloudSyncFinished)
            {
                if (loadingBarFill != null) loadingBarFill.fillAmount = 1f;
                if (progressText != null) progressText.text = "LOADING 100%";

                yield return new WaitForSeconds(0.2f);
                asyncOperation.allowSceneActivation = true;
            }

            yield return null;
        }
    }

    IEnumerator CycleTipsRoutine()
    {
        while (true)
        {
            string newTip = gameTips[UnityEngine.Random.Range(0, gameTips.Length)];
            
            yield return StartCoroutine(FadeText(tipsText, 1f, 0f, 0.5f));
            tipsText.text = newTip;
            yield return StartCoroutine(FadeText(tipsText, 0f, 1f, 0.5f));
            yield return new WaitForSeconds(2f);
        }
    }

    IEnumerator FadeText(TextMeshProUGUI txt, float startAlpha, float endAlpha, float duration)
    {
        if (txt == null) yield break;
        float timer = 0f;
        Color c = txt.color;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            c.a = Mathf.Lerp(startAlpha, endAlpha, timer / duration);
            txt.color = c;
            yield return null;
        }
        c.a = endAlpha;
        txt.color = c;
    }
}