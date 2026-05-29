using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

public class FreeCoinsManager : MonoBehaviour
{
    [Header("Dependencies")]
    public CurrencyManager currencyManager;

    [Header("Shop Integration")]
    [Tooltip("Drag the Shop Panel here so we can freeze it when popup is open")]
    public CanvasGroup shopCanvasGroup;

    [Header("UI References - Shop Top Bar")]
    public Button openPopupButton;
    public RectTransform plusIcon;
    public TextMeshProUGUI liveCoinText;

    [Header("UI References - Popup Menu")]
    public GameObject popupPanel;
    public CanvasGroup popupCanvasGroup;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI statusText;
    public Button watchAdButton;

    [Header("Audio Setup")]
    public AudioClip openMenuSound;
    public AudioClip closeMenuSound;
    private AudioSource audioSource;

    [Header("Settings")]
    public int coinReward = 200;
    public int maxDailyAds = 5;
    public float fadeDuration = 0.25f;

    [HideInInspector]
    public bool isPopupOpen = false;

    private const string LAST_DATE_KEY = "FreeCoins_LastDate";
    private const string AD_COUNT_KEY = "FreeCoins_AdCount";
    private int currentAdCount = 0;

    private float lastAdClickTime = 0f;

    void Start()
    {
        if (currencyManager == null) currencyManager = FindFirstObjectByType<CurrencyManager>();

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        if (popupPanel != null)
        {
            popupPanel.SetActive(false);
            if (popupCanvasGroup == null) popupCanvasGroup = popupPanel.AddComponent<CanvasGroup>();
            popupCanvasGroup.alpha = 0f;
            popupCanvasGroup.blocksRaycasts = false;
        }

        CheckDailyReset();
        UpdateUI();
    }

    void CheckDailyReset()
    {
        string lastDate = PlayerPrefs.GetString(LAST_DATE_KEY, "");
        string today = DateTime.Now.ToString("yyyyMMdd");

        if (lastDate != today)
        {
            PlayerPrefs.SetInt(AD_COUNT_KEY, 0);
            PlayerPrefs.SetString(LAST_DATE_KEY, today);
            PlayerPrefs.Save();
        }

        currentAdCount = PlayerPrefs.GetInt(AD_COUNT_KEY, 0);
    }

    void UpdateUI()
    {
        int remaining = maxDailyAds - currentAdCount;

        if (statusText != null) statusText.text = $"{remaining}/{maxDailyAds} Remaining Today";
        if (titleText != null) titleText.text = $"GET {coinReward} COINS!";

        if (remaining > 0)
        {
            if (openPopupButton != null) openPopupButton.interactable = true;
            if (watchAdButton != null) watchAdButton.interactable = true;
            if (plusIcon != null) plusIcon.gameObject.SetActive(true);
        }
        else
        {
            if (openPopupButton != null) openPopupButton.interactable = false;
            if (watchAdButton != null) watchAdButton.interactable = false;
            if (plusIcon != null) plusIcon.gameObject.SetActive(false);
        }

        if (liveCoinText != null && currencyManager != null)
        {
            liveCoinText.text = currencyManager.GetBalance().ToString("N0");
        }
    }

    public void OpenPopup()
    {
        if (currentAdCount >= maxDailyAds) return;
        if (isPopupOpen) return;

        isPopupOpen = true;

        CanvasGroup targetShopGroup = shopCanvasGroup != null ? shopCanvasGroup : GetComponent<CanvasGroup>();
        if (targetShopGroup != null) targetShopGroup.interactable = false;

        if (popupCanvasGroup != null)
        {
            popupCanvasGroup.blocksRaycasts = true;
            popupCanvasGroup.interactable = true;
        }

        if (gameObject.activeInHierarchy)
        {
            if (audioSource != null && openMenuSound != null) audioSource.PlayOneShot(openMenuSound);

            StopAllCoroutines();
            popupPanel.SetActive(true);
            StartCoroutine(FadePanel(1f));
        }
    }

    public void ClosePopup()
    {
        if (!isPopupOpen) return;
        isPopupOpen = false;

        CanvasGroup targetShopGroup = shopCanvasGroup != null ? shopCanvasGroup : GetComponent<CanvasGroup>();
        if (targetShopGroup != null) targetShopGroup.interactable = true;

        if (popupCanvasGroup != null)
        {
            popupCanvasGroup.blocksRaycasts = false;
            popupCanvasGroup.interactable = false;
        }

        if (gameObject.activeInHierarchy)
        {
            if (audioSource != null && closeMenuSound != null) audioSource.PlayOneShot(closeMenuSound);

            StopAllCoroutines();
            StartCoroutine(FadePanel(0f, true));
        }
        else
        {
            if (popupPanel != null) popupPanel.SetActive(false);
            if (popupCanvasGroup != null) popupCanvasGroup.alpha = 0f;
        }
    }

    IEnumerator FadePanel(float targetAlpha, bool closeAtEnd = false)
    {
        if (popupCanvasGroup == null) yield break;

        float startAlpha = popupCanvasGroup.alpha;
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime / fadeDuration;
            popupCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }
        popupCanvasGroup.alpha = targetAlpha;

        if (closeAtEnd && popupPanel != null) popupPanel.SetActive(false);
    }

    public void OnWatchAdClicked()
    {
        if (currentAdCount >= maxDailyAds) return;

        if (Time.time - lastAdClickTime < 2f) return;
        lastAdClickTime = Time.time;

        if (AdsManager.Instance != null)
        {
            AdsManager.Instance.ShowRewardedAd(OnAdSuccess);
        }
    }

    void OnAdSuccess()
    {
        if (currencyManager != null) currencyManager.AddCoins(coinReward);

        currentAdCount++;
        PlayerPrefs.SetInt(AD_COUNT_KEY, currentAdCount);
        PlayerPrefs.Save();

        UpdateUI();
        ClosePopup();
    }
}