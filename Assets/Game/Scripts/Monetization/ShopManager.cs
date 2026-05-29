using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class ShopManager : MonoBehaviour
{
    [Header("Dependencies")]
    public CurrencyManager currencyManager;
    public FreeCoinsManager freeCoinsManager;

    [Header("UI Panels")]
    public GameObject shopPanel;
    public CanvasGroup panelCanvasGroup;
    public TextMeshProUGUI shopCoinText;

    [Header("Cabinet UI - Floor 1: Backgrounds")]
    public Transform backgroundsContainer;
    public GameObject backgroundCardTemplate;

    [Header("Cabinet UI - Floor 2: Power-Ups & Lasers")]
    public Transform powerupsContainer;
    public GameObject powerupCardTemplate;
    public Transform lasersContainer;
    public GameObject laserCardTemplate;

    [Header("Cabinet UI - Floor 3: Voice Packs")]
    public Transform voicesContainer;
    public GameObject voiceCardTemplate;

    [Header("Cabinet UI - Floor 4: Buckets")]
    public Transform bucketsContainer;
    public GameObject bucketCardTemplate;

    [Header("Menu Cleanup")]
    public GameObject[] menuElementsToHide;

    [Header("Animation Settings")]
    public float fadeDuration = 0.3f;
    public float clickBounce = 1.2f;

    [Header("Audio Settings")]
    public AudioClip errorSound;
    private AudioSource shopAudio;

    [Header("Text Colors")]
    public Color priceDefaultColor = new Color(1f, 0.8f, 0f);

    [System.Serializable]
    public class ShopItem
    {
        public string itemID;
        public string displayName;
        public Sprite previewIcon;
        public int price;
        public bool isUnlockedByDefault;
        public bool isConsumable;
        public int consumableAmount = 1;
    }

    [System.Serializable]
    public class ShopCategory
    {
        public string categoryName;
        public string categoryID;
        public List<ShopItem> items = new List<ShopItem>();
    }

    [Header("Shop Content")]
    public List<ShopCategory> shopCategories = new List<ShopCategory>();

    private bool isShopGenerated = false;
    private bool isPanelOpen = false;
    private Dictionary<string, GameObject> cardMap = new Dictionary<string, GameObject>();

    void Start()
    {
        if (currencyManager == null) currencyManager = FindFirstObjectByType<CurrencyManager>();
        GetFreeCoinsManager();

        shopAudio = gameObject.AddComponent<AudioSource>();
        shopAudio.playOnAwake = false;

        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
            if (panelCanvasGroup != null) panelCanvasGroup.alpha = 0f;
        }

        if (backgroundCardTemplate != null) backgroundCardTemplate.SetActive(false);
        if (powerupCardTemplate != null) powerupCardTemplate.SetActive(false);
        if (laserCardTemplate != null) laserCardTemplate.SetActive(false);
        if (voiceCardTemplate != null) voiceCardTemplate.SetActive(false);
        if (bucketCardTemplate != null) bucketCardTemplate.SetActive(false);

        SyncCloudMasterString();

        StartCoroutine(PreloadShop());
    }

    private void SyncCloudMasterString()
    {
        string masterShop = "";
        foreach (var cat in shopCategories) {
            foreach (var item in cat.items) {
                if (PlayerPrefs.GetInt("Shop_Unlocked_" + item.itemID, 0) == 1) {
                    masterShop += item.itemID + ",";
                }
            }
        }
        PlayerPrefs.SetString("Master_Shop_Unlocks", masterShop);
        PlayerPrefs.Save();
    }

    private FreeCoinsManager GetFreeCoinsManager()
    {
        if (freeCoinsManager != null) return freeCoinsManager;
        FreeCoinsManager[] managers = Resources.FindObjectsOfTypeAll<FreeCoinsManager>();
        if (managers.Length > 0) freeCoinsManager = managers[0];
        return freeCoinsManager;
    }

    void Update()
    {
        if (isPanelOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            FreeCoinsManager fcm = GetFreeCoinsManager();
            if (fcm != null && fcm.isPopupOpen) fcm.ClosePopup();
            else CloseShop();
        }
    }

    public void CloseShop()
    {
        if (!isPanelOpen) return;

        FreeCoinsManager fcm = GetFreeCoinsManager();
        if (fcm != null && fcm.isPopupOpen)
        {
            fcm.ClosePopup();
            return;
        }

        StopAllCoroutines();

        foreach (var card in cardMap.Values)
        {
            if (card != null)
            {
                card.transform.localScale = Vector3.one;
                card.transform.localRotation = Quaternion.identity;
            }
        }

        isPanelOpen = false;

        if (gameObject.activeInHierarchy) StartCoroutine(FadePanel(0f, true));
        else if (shopPanel != null) shopPanel.SetActive(false);
    }

    IEnumerator PreloadShop()
    {
        yield return null;
        if (!isShopGenerated)
        {
            GenerateShopUI();
            isShopGenerated = true;
        }
        if (shopPanel != null) shopPanel.SetActive(false);
    }

    public void OpenShop()
    {
        StopAllCoroutines();

        shopPanel.SetActive(true);
        isPanelOpen = true;
        ToggleMenuHUD(false);
        UpdatePanelCoins();

        if (!isShopGenerated)
        {
            GenerateShopUI();
            isShopGenerated = true;
        }
        else
        {
            RefreshUIState();
        }

        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(FadePanel(1f));
        }
    }

    IEnumerator ErrorShake(Transform target)
    {
        if (target == null) yield break;
        Quaternion startRot = target.localRotation;
        float t = 0f;
        while (t < 0.3f)
        {
            t += Time.unscaledDeltaTime;
            float angle = Mathf.Sin(t * 60f) * 15f * (1f - (t / 0.3f));
            target.localRotation = Quaternion.Euler(0, 0, angle);
            yield return null;
        }
        target.localRotation = startRot;
    }

    IEnumerator PunchCard(Transform target)
    {
        float t = 0f;
        while (t < 0.2f)
        {
            if (target == null) yield break;
            t += Time.unscaledDeltaTime;
            float scale = Mathf.Lerp(1f, clickBounce, t / 0.1f);
            target.localScale = Vector3.one * scale;
            yield return null;
        }
        t = 0f;
        while (t < 0.2f)
        {
            if (target == null) yield break;
            t += Time.unscaledDeltaTime;
            float scale = Mathf.Lerp(clickBounce, 1f, t / 0.2f);
            target.localScale = Vector3.one * scale;
            yield return null;
        }
        if (target != null) target.localScale = Vector3.one;
    }

    void GenerateShopUI()
    {
        cardMap.Clear();

        foreach (ShopCategory category in shopCategories)
        {
            if (category.categoryID == "bg" && backgroundCardTemplate != null)
                PopulateShelf(category, backgroundCardTemplate, backgroundsContainer);

            else if (category.categoryID == "powerups" && powerupCardTemplate != null)
                PopulateShelf(category, powerupCardTemplate, powerupsContainer);

            else if (category.categoryID == "laser" && laserCardTemplate != null)
                PopulateShelf(category, laserCardTemplate, lasersContainer);

            else if (category.categoryID == "voice" && voiceCardTemplate != null)
                PopulateShelf(category, voiceCardTemplate, voicesContainer);

            else if (category.categoryID == "bucket" && bucketCardTemplate != null)
                PopulateShelf(category, bucketCardTemplate, bucketsContainer);
        }
    }

    void PopulateShelf(ShopCategory category, GameObject template, Transform container)
    {
        if (container == null) return;

        foreach (ShopItem item in category.items)
        {
            GameObject card = Instantiate(template, container);
            card.SetActive(true);
            card.name = "ItemCard_" + item.itemID;
            cardMap[item.itemID] = card;

            Transform iconTrans = FindDeepChild(card.transform, "PreviewIcon");
            Transform nameTrans = FindDeepChild(card.transform, "NameText");

            if (iconTrans != null) iconTrans.GetComponent<Image>().sprite = item.previewIcon;
            if (nameTrans != null) nameTrans.GetComponent<TextMeshProUGUI>().text = item.displayName;

            UpdateSingleCardVisuals(card, item, category.categoryID);
        }
    }

    Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            Transform result = FindDeepChild(child, name);
            if (result != null) return result;
        }
        return null;
    }

    public void RefreshUIState()
    {
        foreach (ShopCategory category in shopCategories)
        {
            foreach (ShopItem item in category.items)
            {
                if (cardMap.ContainsKey(item.itemID))
                {
                    UpdateSingleCardVisuals(cardMap[item.itemID], item, category.categoryID);
                }
            }
        }
    }

    void UpdateSingleCardVisuals(GameObject card, ShopItem item, string categoryID)
    {
        if (card == null) return;

        Transform priceTrans = FindDeepChild(card.transform, "PriceText");
        Transform btnTrans = FindDeepChild(card.transform, "ActionButton");
        Transform coinIconTrans = FindDeepChild(card.transform, "CoinIcon");
        Transform inUseBadgeTrans = FindDeepChild(card.transform, "InUseBadge");
        Transform ribbonTrans = FindDeepChild(card.transform, "ribbon");

        if (btnTrans == null) return;

        TextMeshProUGUI priceText = (priceTrans != null) ? priceTrans.GetComponent<TextMeshProUGUI>() : null;
        Button actionBtn = btnTrans.GetComponent<Button>();
        GameObject coinObj = (coinIconTrans != null) ? coinIconTrans.gameObject : null;
        GameObject inUseObj = (inUseBadgeTrans != null) ? inUseBadgeTrans.gameObject : null;
        GameObject ribbonObj = (ribbonTrans != null) ? ribbonTrans.gameObject : null;

        actionBtn.onClick.RemoveAllListeners();

        int actualPrice = item.price;
        if (item.isConsumable && VIPManager.IsVIP())
        {
            actualPrice = Mathf.RoundToInt(item.price * 0.2f);
        }

        if (item.isConsumable)
        {
            if (priceText != null)
            {
                priceText.gameObject.SetActive(true);
                priceText.text = actualPrice.ToString();
                priceText.color = VIPManager.IsVIP() ? Color.green : priceDefaultColor;
            }
            if (inUseObj != null) inUseObj.SetActive(false);
            if (coinObj != null) coinObj.SetActive(true);
            if (ribbonObj != null) ribbonObj.SetActive(true);

            if (actionBtn != null)
            {
                actionBtn.interactable = true;
                actionBtn.onClick.AddListener(() => TryBuyItem(item, card));
            }
            return;
        }

        bool isUnlocked = IsItemUnlocked(item.itemID, item.isUnlockedByDefault);
        string equippedID = GetEquippedItemID(categoryID);
        bool isEquipped = (equippedID == item.itemID) || (equippedID == "" && item.isUnlockedByDefault);

        if (isEquipped)
        {
            if (priceText != null) priceText.gameObject.SetActive(false);
            if (coinObj != null) coinObj.SetActive(false);
            if (inUseObj != null) inUseObj.SetActive(true);
            if (ribbonObj != null) ribbonObj.SetActive(false);
            if (actionBtn != null) actionBtn.interactable = false;
        }
        else if (isUnlocked)
        {
            if (priceText != null) priceText.gameObject.SetActive(false);
            if (coinObj != null) coinObj.SetActive(false);
            if (inUseObj != null) inUseObj.SetActive(false);
            if (ribbonObj != null) ribbonObj.SetActive(false);

            if (actionBtn != null)
            {
                actionBtn.interactable = true;
                actionBtn.onClick.AddListener(() => EquipItem(item.itemID, categoryID));
            }
        }
        else
        {
            if (priceText != null)
            {
                priceText.gameObject.SetActive(true);
                priceText.text = item.price.ToString();
                priceText.color = priceDefaultColor;
            }
            if (coinObj != null) coinObj.SetActive(true);
            if (inUseObj != null) inUseObj.SetActive(false);
            if (ribbonObj != null) ribbonObj.SetActive(true);

            if (actionBtn != null)
            {
                actionBtn.interactable = true;
                actionBtn.onClick.AddListener(() => TryBuyItem(item, card));
            }
        }
    }

    public void TryBuyItem(ShopItem item, GameObject card)
    {
        int actualPrice = item.price;
        if (item.isConsumable && VIPManager.IsVIP()) actualPrice = Mathf.RoundToInt(item.price * 0.2f);

        int liveBalance = currencyManager != null ? currencyManager.GetBalance() : 0;

        if (liveBalance >= actualPrice)
        {
            currencyManager.SpendCoins(actualPrice);

            if (item.isConsumable) 
            {
                StatTracker.Increment("Consumable_" + item.itemID, item.consumableAmount);
            }
            else 
            {
                PlayerPrefs.SetInt("Shop_Unlocked_" + item.itemID, 1);
                SyncCloudMasterString();
            }

            PlayerPrefs.Save();
            
            if (PlayGamesManager.Instance != null)
            {
                PlayGamesManager.Instance.UnlockAchievement(GPGSIds.achievement_window_shopper);
                CheckCollectorAchievement();
                PlayGamesManager.Instance.SaveToCloud();
            }

            UpdatePanelCoins();
            RefreshUIState();

            if (card != null && gameObject.activeInHierarchy) StartCoroutine(PunchCard(card.transform));
        }
        else
        {
            if (shopAudio != null && errorSound != null)
            {
                shopAudio.PlayOneShot(errorSound);
            }
            if (card != null && gameObject.activeInHierarchy)
            {
                StartCoroutine(ErrorShake(card.transform));
            }
        }
    }

    private void CheckCollectorAchievement()
    {
        if (PlayGamesManager.Instance == null) return;

        bool allUnlocked = true;
        foreach (ShopCategory cat in shopCategories)
        {
            foreach (ShopItem item in cat.items)
            {
                if (!item.isConsumable && !IsItemUnlocked(item.itemID, item.isUnlockedByDefault))
                {
                    allUnlocked = false;
                    break;
                }
            }
        }
        
        if (allUnlocked)
        {
            PlayGamesManager.Instance.UnlockAchievement(GPGSIds.achievement_the_collector);
        }
    }

    public void EquipItem(string itemID, string categoryID)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();

        PlayerPrefs.SetString("Shop_Equipped_" + categoryID, itemID);
        PlayerPrefs.Save();

        RefreshUIState();
        if (cardMap.ContainsKey(itemID) && gameObject.activeInHierarchy) StartCoroutine(PunchCard(cardMap[itemID].transform));
    }

    public bool IsItemUnlocked(string itemID, bool isDefault)
    {
        if (isDefault) return true;
        if (VIPManager.IsVIP()) return true;
        return PlayerPrefs.GetInt("Shop_Unlocked_" + itemID, 0) == 1;
    }

    public string GetEquippedItemID(string categoryID)
    {
        return PlayerPrefs.GetString("Shop_Equipped_" + categoryID, "");
    }

    public void UpdatePanelCoins()
    {
        if (currencyManager != null && shopCoinText != null) shopCoinText.text = currencyManager.GetBalance().ToString("N0");
    }

    System.Collections.IEnumerator FadePanel(float targetAlpha, bool closeAtEnd = false)
    {
        if (panelCanvasGroup != null)
        {
            float startAlpha = panelCanvasGroup.alpha;
            float t = 0f;
            while (t < 1f) { t += Time.deltaTime / fadeDuration; panelCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t); yield return null; }
            panelCanvasGroup.alpha = targetAlpha;
        }
        if (closeAtEnd) { if (shopPanel != null) shopPanel.SetActive(false); ToggleMenuHUD(true); }
    }

    void ToggleMenuHUD(bool show)
    {
        if (menuElementsToHide != null)
        {
            foreach (GameObject go in menuElementsToHide)
            {
                if (go != null)
                {
                    if (show && VIPManager.Instance != null && VIPManager.IsVIP())
                    {
                        if (VIPManager.Instance.IsThisTheVIPButton(go))
                        {
                            go.SetActive(false);
                            continue;
                        }
                    }

                    if (show && PlayerPrefs.GetInt("HasClickedRateUs", 0) == 1)
                    {
                        if (go.name == "btn_RateUs")
                        {
                            go.SetActive(false);
                            continue;
                        }
                    }

                    go.SetActive(show);
                }
            }
        }
    }
}