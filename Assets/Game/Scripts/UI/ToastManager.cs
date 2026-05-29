using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ToastManager : MonoBehaviour
{
    public static ToastManager Instance;

    private CanvasGroup canvasGroup;
    private RectTransform toastRect;
    private TextMeshProUGUI toastText;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CreateToastUI();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void CreateToastUI()
    {
        GameObject canvasObj = new GameObject("ToastCanvas");
        canvasObj.transform.SetParent(this.transform);

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);

        GameObject panelObj = new GameObject("ToastPanel");
        panelObj.transform.SetParent(canvasObj.transform, false);

        Image bg = panelObj.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

        toastRect = panelObj.GetComponent<RectTransform>();
        toastRect.anchorMin = new Vector2(0.5f, 1f);
        toastRect.anchorMax = new Vector2(0.5f, 1f);
        toastRect.pivot = new Vector2(0.5f, 1f);
        toastRect.sizeDelta = new Vector2(900, 150);
        toastRect.anchoredPosition = new Vector2(0, 300);

        canvasGroup = panelObj.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;

        GameObject textObj = new GameObject("ToastText");
        textObj.transform.SetParent(panelObj.transform, false);

        toastText = textObj.AddComponent<TextMeshProUGUI>();
        toastText.alignment = TextAlignmentOptions.Center;
        toastText.fontSize = 50;
        toastText.color = Color.white;
        toastText.fontStyle = FontStyles.Bold;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.offsetMin = new Vector2(20, 20);
        textRect.offsetMax = new Vector2(-20, -20);
    }

    public void ShowToast(string message, float duration = 2f)
    {
        StopAllCoroutines();
        StartCoroutine(ToastRoutine(message, duration));
    }

    IEnumerator ToastRoutine(string message, float duration)
    {
        toastText.text = message;

        float t = 0;
        Vector2 startPos = toastRect.anchoredPosition;
        float startAlpha = canvasGroup.alpha;

        while (t < 0.25f)
        {
            t += Time.unscaledDeltaTime;
            float progress = t / 0.25f;
            float ease = Mathf.Sin(progress * Mathf.PI * 0.5f);

            canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, ease);
            toastRect.anchoredPosition = Vector2.Lerp(startPos, new Vector2(0, -100), ease);
            yield return null;
        }

        canvasGroup.alpha = 1f;
        toastRect.anchoredPosition = new Vector2(0, -100);

        yield return new WaitForSecondsRealtime(duration);

        t = 0;
        while (t < 0.25f)
        {
            t += Time.unscaledDeltaTime;
            float progress = t / 0.25f;
            float ease = 1f - Mathf.Cos(progress * Mathf.PI * 0.5f);

            canvasGroup.alpha = 1f - ease;
            toastRect.anchoredPosition = Vector2.Lerp(new Vector2(0, -100), new Vector2(0, 300), ease);
            yield return null;
        }

        canvasGroup.alpha = 0f;
    }
}