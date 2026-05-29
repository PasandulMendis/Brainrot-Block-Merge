using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Purchasing;

public class VIPManager : MonoBehaviour
{
    [Header("Dependencies")]
    public CurrencyManager currencyManager;
    public ShopManager shopManager;
    public PackManager packManager;

    [Header("IAP Configuration")]
    public string vipProductID = "com.orizozointeractive.brainrotblockmerge.vip";
    public bool isTestMode = true;

    [Header("UI Panels")]
    public GameObject vipPanel;
    public CanvasGroup panelCanvasGroup;
    public GameObject buyButtonObject;
    public GameObject restoreButtonObject;
    public GameObject vipOpenButtonObject;

    [Header("Transaction Feedback")]
    public GameObject feedbackContainer;
    public Image feedbackIcon;
    public Sprite successTickSprite;
    public Sprite failCrossSprite;
    public TextMeshProUGUI feedbackText;

    [Header("Animation")]
    public float fadeDuration = 0.3f;

    public const string VIP_SAVE_KEY = "IsVIP_Unlocked";

    public static VIPManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        if (currencyManager == null) currencyManager = FindFirstObjectByType<CurrencyManager>();
        if (shopManager == null) shopManager = FindFirstObjectByType<ShopManager>();
        if (packManager == null) packManager = FindFirstObjectByType<PackManager>();

        if (vipPanel != null)
        {
            vipPanel.SetActive(false);
            if (panelCanvasGroup != null) panelCanvasGroup.alpha = 0f;
        }

        if (feedbackContainer != null) feedbackContainer.SetActive(false);

        if (IsVIP() && vipOpenButtonObject != null)
        {
            vipOpenButtonObject.SetActive(false);
        }

        Invoke(nameof(CheckForSilentReceipt), 3f);
    }

    void Update()
    {
        if (vipPanel != null && vipPanel.activeInHierarchy)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (feedbackContainer != null && feedbackContainer.activeInHierarchy) return;
                CloseVIPPanel();
            }
        }
    }

    public static bool IsVIP()
    {
        return PlayerPrefs.GetInt(VIP_SAVE_KEY, 0) == 1;
    }

    public bool IsThisTheVIPButton(GameObject obj)
    {
        return (vipOpenButtonObject != null && obj == vipOpenButtonObject);
    }

    void CheckForSilentReceipt()
    {
        if (IsVIP()) return;

        if (CodelessIAPStoreListener.Instance != null && CodelessIAPStoreListener.Instance.StoreController != null)
        {
            Product p = CodelessIAPStoreListener.Instance.StoreController.products.WithID(vipProductID);
            if (p != null && p.hasReceipt)
            {
                PlayerPrefs.SetInt(VIP_SAVE_KEY, 1);

                if (PlayerPrefs.GetInt("VIP_Coins_Claimed", 0) == 0)
                {
                    if (currencyManager != null) currencyManager.AddCoins(2500);
                    PlayerPrefs.SetInt("VIP_Coins_Claimed", 1);
                }

                PlayerPrefs.Save();

                if (vipOpenButtonObject != null) vipOpenButtonObject.SetActive(false);
                if (AdsManager.Instance != null) AdsManager.Instance.CheckVIPAndHideBanner();

                Debug.Log("VIP silently auto-restored via receipt check on boot!");
            }
        }
    }

    public void OpenVIPPanel()
    {
        if (IsVIP())
        {
            if (ToastManager.Instance != null) ToastManager.Instance.ShowToast("You are already a VIP!");
            return;
        }

        vipPanel.SetActive(true);
        if (buyButtonObject != null) buyButtonObject.SetActive(true);
        if (restoreButtonObject != null) restoreButtonObject.SetActive(true);
        if (feedbackContainer != null) feedbackContainer.SetActive(false);

        StartCoroutine(FadePanel(1f));
    }

    public void CloseVIPPanel()
    {
        StartCoroutine(FadePanel(0f, true));
    }

    IEnumerator FadePanel(float targetAlpha, bool closeAtEnd = false)
    {
        MonoBehaviour softGlow = null;
        if (vipPanel != null) softGlow = vipPanel.GetComponent("SoftGlowUI") as MonoBehaviour;

        if (softGlow != null) softGlow.enabled = false;

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.blocksRaycasts = true;
            panelCanvasGroup.interactable = false;

            float startAlpha = panelCanvasGroup.alpha;
            float t = 0;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / fadeDuration;
                panelCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }
            panelCanvasGroup.alpha = targetAlpha;
            panelCanvasGroup.interactable = (targetAlpha == 1f);
        }

        if (softGlow != null && targetAlpha == 1f) softGlow.enabled = true;
        if (closeAtEnd && vipPanel != null) vipPanel.SetActive(false);
    }

    public void OnBuyButtonClicked()
    {
        if (isTestMode)
        {
            Debug.Log("TEST MODE ACTIVE: Instantly simulating a successful VIP purchase!");
            OnPurchaseSuccessful();
        }
    }

    public void OnRestoreButtonClicked()
    {
        if (isTestMode)
        {
            Debug.Log("TEST MODE ACTIVE: Instantly simulating a successful Restore!");
            OnSilentAutoRestore(null);
        }
    }

    public void OnPurchaseSuccessful()
    {
        StartCoroutine(ProcessSuccessRoutine("WELCOME TO VIP!"));
    }

    public void OnPurchaseFailed()
    {
        StartCoroutine(ProcessFailRoutine());
    }

    public void OnCodelessIAPRestore(bool success, string message)
    {
        if (success)
        {
            bool actuallyOwnsIt = false;

            if (CodelessIAPStoreListener.Instance != null && CodelessIAPStoreListener.Instance.StoreController != null)
            {
                Product p = CodelessIAPStoreListener.Instance.StoreController.products.WithID(vipProductID);
                if (p != null && p.hasReceipt) actuallyOwnsIt = true;
            }

            if (actuallyOwnsIt)
            {
                if (!IsVIP())
                {
                    PlayerPrefs.SetInt(VIP_SAVE_KEY, 1);
                    PlayerPrefs.Save();

                    if (vipOpenButtonObject != null) vipOpenButtonObject.SetActive(false);
                    if (AdsManager.Instance != null) AdsManager.Instance.CheckVIPAndHideBanner();
                }
                StartCoroutine(ProcessSuccessRoutine("PURCHASES RESTORED!"));
            }
            else
            {
                StartCoroutine(ShowNoPurchasesFound());
            }
        }
        else
        {
            StartCoroutine(ProcessFailRoutine());
        }
    }

    public void OnSilentAutoRestore(Product product)
    {
        if (!IsVIP())
        {
            PlayerPrefs.SetInt(VIP_SAVE_KEY, 1);

            if (PlayerPrefs.GetInt("VIP_Coins_Claimed", 0) == 0)
            {
                if (currencyManager != null) currencyManager.AddCoins(2500);
                PlayerPrefs.SetInt("VIP_Coins_Claimed", 1);
            }

            PlayerPrefs.Save();

            if (vipOpenButtonObject != null) vipOpenButtonObject.SetActive(false);
            if (AdsManager.Instance != null) AdsManager.Instance.CheckVIPAndHideBanner();

            if (vipPanel != null && vipPanel.activeInHierarchy)
            {
                StartCoroutine(ProcessSuccessRoutine("WELCOME TO VIP!"));
            }
        }
    }

    IEnumerator ProcessSuccessRoutine(string successMessage)
    {
        if (buyButtonObject != null) buyButtonObject.SetActive(false);
        if (restoreButtonObject != null) restoreButtonObject.SetActive(false);
        if (vipOpenButtonObject != null) vipOpenButtonObject.SetActive(false);

        if (feedbackContainer != null)
        {
            feedbackContainer.SetActive(true);
            if (feedbackIcon != null) feedbackIcon.sprite = successTickSprite;
            if (feedbackText != null) feedbackText.text = successMessage;

            feedbackIcon.transform.localScale = Vector3.zero;
            float t = 0;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime * 10f;
                float scale = Mathf.Sin(t * Mathf.PI * 0.5f) + (Mathf.Sin(t * Mathf.PI) * 0.2f);
                if (t >= 1) scale = 1;
                feedbackIcon.transform.localScale = Vector3.one * scale;
                yield return null;
            }
        }

        PlayerPrefs.SetInt(VIP_SAVE_KEY, 1);

        if (PlayerPrefs.GetInt("VIP_Coins_Claimed", 0) == 0)
        {
            if (currencyManager != null) currencyManager.AddCoins(2500);
            PlayerPrefs.SetInt("VIP_Coins_Claimed", 1);
        }

        PlayerPrefs.Save();

        if (AdsManager.Instance != null) AdsManager.Instance.CheckVIPAndHideBanner();

        yield return new WaitForSecondsRealtime(2.0f);
        CloseVIPPanel();
    }

    IEnumerator ProcessFailRoutine()
    {
        if (buyButtonObject != null) buyButtonObject.SetActive(false);
        if (restoreButtonObject != null) restoreButtonObject.SetActive(false);

        if (feedbackContainer != null)
        {
            feedbackContainer.SetActive(true);
            if (feedbackIcon != null) feedbackIcon.sprite = failCrossSprite;
            if (feedbackText != null) feedbackText.text = "PURCHASE FAILED";

            feedbackIcon.transform.localScale = Vector3.one;
            float t = 0;
            while (t < 0.5f)
            {
                t += Time.unscaledDeltaTime;
                feedbackIcon.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 50f) * 15f * (1f - (t / 0.5f)));
                yield return null;
            }
            feedbackIcon.transform.localRotation = Quaternion.identity;
        }

        yield return new WaitForSecondsRealtime(2.0f);
        if (feedbackContainer != null) feedbackContainer.SetActive(false);

        if (buyButtonObject != null) buyButtonObject.SetActive(true);
        if (restoreButtonObject != null) restoreButtonObject.SetActive(true);
    }

    IEnumerator ShowNoPurchasesFound()
    {
        if (buyButtonObject != null) buyButtonObject.SetActive(false);
        if (restoreButtonObject != null) restoreButtonObject.SetActive(false);

        if (feedbackContainer != null)
        {
            feedbackContainer.SetActive(true);
            if (feedbackIcon != null) feedbackIcon.sprite = failCrossSprite;
            if (feedbackText != null) feedbackText.text = "NO PURCHASES FOUND";

            feedbackIcon.transform.localScale = Vector3.one;
            float t = 0;
            while (t < 0.5f)
            {
                t += Time.unscaledDeltaTime;
                feedbackIcon.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 50f) * 15f * (1f - (t / 0.5f)));
                yield return null;
            }
            feedbackIcon.transform.localRotation = Quaternion.identity;
        }

        yield return new WaitForSecondsRealtime(2.0f);

        if (feedbackContainer != null) feedbackContainer.SetActive(false);
        if (buyButtonObject != null) buyButtonObject.SetActive(true);
        if (restoreButtonObject != null) restoreButtonObject.SetActive(true);
    }
}