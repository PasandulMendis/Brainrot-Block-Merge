using UnityEngine;
using GoogleMobileAds.Api;
using System;

public class AdsManager : MonoBehaviour
{
    public static AdsManager Instance;

    private string bannerAdUnitId = "ca-app-pub-XXXXXXXXXXXXXXXX/NNNNNNNNNN";
    private string interstitialAdUnitId = "ca-app-pub-XXXXXXXXXXXXXXXX/NNNNNNNNNN";
    private string rewardedAdUnitId = "ca-app-pub-XXXXXXXXXXXXXXXX/NNNNNNNNNN";

    [Header("Settings")]
    public bool enableBannerAds = false;
    public bool showBannerOnStart = true;

    private int interstitialFrequency = 3;

    private BannerView bannerView;
    private InterstitialAd interstitialAd;
    private RewardedAd rewardedAd;

    private Action onRewardedCallback;
    private Action onInterstitialClosedCallback;

    private const string AD_COUNTER_KEY = "Ad_Interstitial_Count";

    private bool pendingReward = false;
    private bool pendingInterstitialClose = false;
    private string pendingToastMessage = "";

    private bool isAdMobInitialized = false;
    private bool needsInitialLoad = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        MobileAds.Initialize(initStatus => {
            needsInitialLoad = true;
        });
    }

    void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (isAdMobInitialized && showBannerOnStart && enableBannerAds)
        {
            LoadBannerAd();
        }
    }

    void Update()
    {
        if (needsInitialLoad)
        {
            needsInitialLoad = false;
            isAdMobInitialized = true;
            LoadInterstitialAd();
            LoadRewardedAd();
            if (showBannerOnStart && enableBannerAds) LoadBannerAd();
        }

        if (pendingReward)
        {
            pendingReward = false;
            onRewardedCallback?.Invoke();
        }

        if (pendingInterstitialClose)
        {
            pendingInterstitialClose = false;
            onInterstitialClosedCallback?.Invoke();
        }

        if (!string.IsNullOrEmpty(pendingToastMessage))
        {
            if (ToastManager.Instance != null)
            {
                ToastManager.Instance.ShowToast(pendingToastMessage);
            }
            pendingToastMessage = "";
        }
    }

    public void LoadBannerAd()
    {
        if (!enableBannerAds) return; 
        
        if (PlayerPrefs.GetInt("IsVIP_Unlocked", 0) == 1) return;

        if (bannerView != null)
        {
            bannerView.Destroy();
        }

        bannerView = new BannerView(bannerAdUnitId, AdSize.Banner, AdPosition.Bottom);
        AdRequest request = new AdRequest();
        bannerView.LoadAd(request);
    }

    public void HideBanner()
    {
        if (bannerView != null) bannerView.Hide();
    }

    public void ShowBanner()
    {
        if (!enableBannerAds) return;

        if (PlayerPrefs.GetInt("IsVIP_Unlocked", 0) == 1) return;

        if (bannerView != null) bannerView.Show();
        else LoadBannerAd();
    }

    public void CheckVIPAndHideBanner()
    {
        if (PlayerPrefs.GetInt("IsVIP_Unlocked", 0) == 1)
        {
            HideBanner();
            if (bannerView != null) bannerView.Destroy();
        }
    }

    public void LoadInterstitialAd()
    {
        if (interstitialAd != null)
        {
            interstitialAd.Destroy();
            interstitialAd = null;
        }

        AdRequest request = new AdRequest();
        InterstitialAd.Load(interstitialAdUnitId, request, (InterstitialAd ad, LoadAdError error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogError("Interstitial failed. Code: " + error.GetCode() + " Message: " + error.GetMessage());
                return;
            }

            interstitialAd = ad;

            interstitialAd.OnAdFullScreenContentClosed += () =>
            {
                pendingInterstitialClose = true;
                LoadInterstitialAd();
            };

            interstitialAd.OnAdFullScreenContentFailed += (AdError adError) =>
            {
                pendingInterstitialClose = true;
                LoadInterstitialAd();
            };
        });
    }

    public void TryShowInterstitial(Action onClosed)
    {
        if (PlayerPrefs.GetInt("IsVIP_Unlocked", 0) == 1)
        {
            onClosed?.Invoke();
            return;
        }

        int count = PlayerPrefs.GetInt(AD_COUNTER_KEY, 0);
        count++;
        PlayerPrefs.SetInt(AD_COUNTER_KEY, count);
        PlayerPrefs.Save();

        if (count % interstitialFrequency == 0)
        {
            if (interstitialAd != null && interstitialAd.CanShowAd())
            {
                onInterstitialClosedCallback = onClosed;
                interstitialAd.Show();
            }
            else
            {
                onClosed?.Invoke();
                LoadInterstitialAd();
            }
        }
        else
        {
            onClosed?.Invoke();
        }
    }

    public void LoadRewardedAd()
    {
        if (rewardedAd != null)
        {
            rewardedAd.Destroy();
            rewardedAd = null;
        }

        AdRequest request = new AdRequest();
        RewardedAd.Load(rewardedAdUnitId, request, (RewardedAd ad, LoadAdError error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogError("Rewarded failed. Code: " + error.GetCode() + " Message: " + error.GetMessage());
                return;
            }

            rewardedAd = ad;

            rewardedAd.OnAdFullScreenContentClosed += () =>
            {
                LoadRewardedAd();
            };

            rewardedAd.OnAdFullScreenContentFailed += (AdError adError) =>
            {
                pendingToastMessage = "Ad failed to load properly.";
                LoadRewardedAd();
            };
        });
    }

    public void ShowRewardedAd(Action onReward)
    {
        if (rewardedAd != null && rewardedAd.CanShowAd())
        {
            onRewardedCallback = onReward;
            rewardedAd.Show((Reward reward) =>
            {
                pendingReward = true;
            });
        }
        else
        {
            if (ToastManager.Instance != null)
                ToastManager.Instance.ShowToast("Ad is still loading, please wait...");
            LoadRewardedAd();
        }
    }
}