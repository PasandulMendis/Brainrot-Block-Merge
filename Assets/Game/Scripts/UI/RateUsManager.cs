using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class RateUsManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The button on the Main Menu that opens this popup")]
    public GameObject rateUsTriggerButton;
    public GameObject popupPanel;
    public CanvasGroup panelCanvasGroup;

    [Header("Animation Setup")]
    [Tooltip("Drag the 5 Star RectTransforms here in order from left to right")]
    public RectTransform[] starRects;

    [Tooltip("How high the stars start before dropping down")]
    public float dropHeight = 300f;
    public float starDropDuration = 0.4f;
    public float staggerDelay = 0.1f;
    public float fadeDuration = 0.3f;

    private const string RATED_KEY = "HasClickedRateUs";
    private bool isPopupOpen = false;

    private Vector2[] originalStarPositions;

    void Start()
    {
        if (PlayerPrefs.GetInt(RATED_KEY, 0) == 1)
        {
            if (rateUsTriggerButton != null) rateUsTriggerButton.SetActive(false);

            gameObject.SetActive(false);
            return;
        }

        if (popupPanel != null)
        {
            popupPanel.SetActive(false);
            if (panelCanvasGroup != null) panelCanvasGroup.alpha = 0f;
        }

        if (starRects != null && starRects.Length > 0)
        {
            originalStarPositions = new Vector2[starRects.Length];
            for (int i = 0; i < starRects.Length; i++)
            {
                if (starRects[i] != null)
                {
                    originalStarPositions[i] = starRects[i].anchoredPosition;
                }
            }
        }
    }

    public void OpenRateUsPopup()
    {
        if (isPopupOpen) return;
        isPopupOpen = true;

        for (int i = 0; i < starRects.Length; i++)
        {
            if (starRects[i] != null)
            {
                starRects[i].anchoredPosition = originalStarPositions[i] + new Vector2(0, dropHeight);
                starRects[i].localScale = Vector3.zero;
            }
        }

        popupPanel.SetActive(true);
        StartCoroutine(FadePanel(1f));
        StartCoroutine(AnimateStarsDrop());
    }

    public void OnRateUsClicked()
    {
        PlayerPrefs.SetInt(RATED_KEY, 1);
        PlayerPrefs.Save();

        if (rateUsTriggerButton != null) rateUsTriggerButton.SetActive(false);

#if UNITY_ANDROID
            Application.OpenURL("market://details?id=" + Application.identifier);
#else
        Application.OpenURL("https://play.google.com/store/apps/details?id=" + Application.identifier);
#endif

        ClosePopup();
    }

    public void OnLaterClicked()
    {
        ClosePopup();
    }

    private void ClosePopup()
    {
        if (!isPopupOpen) return;
        isPopupOpen = false;
        StartCoroutine(FadePanel(0f, true));
    }

    private IEnumerator FadePanel(float targetAlpha, bool closeAtEnd = false)
    {
        if (panelCanvasGroup == null) yield break;

        float startAlpha = panelCanvasGroup.alpha;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / fadeDuration;
            panelCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        panelCanvasGroup.alpha = targetAlpha;
        if (closeAtEnd && popupPanel != null) popupPanel.SetActive(false);
    }

    private IEnumerator AnimateStarsDrop()
    {
        yield return new WaitForSeconds(fadeDuration * 0.5f);

        for (int i = 0; i < starRects.Length; i++)
        {
            if (starRects[i] != null)
            {
                StartCoroutine(DropSingleStar(starRects[i], originalStarPositions[i]));

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayGenericPop();
                }

                yield return new WaitForSeconds(staggerDelay);
            }
        }
    }

    private IEnumerator DropSingleStar(RectTransform star, Vector2 targetPos)
    {
        Vector2 startPos = targetPos + new Vector2(0, dropHeight);
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / starDropDuration;

            float ease = 1f - Mathf.Pow(1f - t, 3f);
            float bounce = Mathf.Sin(t * Mathf.PI * 0.8f) * 0.2f * (1f - t);
            float scaleEase = ease + bounce;

            star.anchoredPosition = Vector2.Lerp(startPos, targetPos, ease);
            star.localScale = Vector3.one * Mathf.Clamp01(scaleEase);

            yield return null;
        }

        star.anchoredPosition = targetPos;
        star.localScale = Vector3.one;
    }
}