using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class PackManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject packMenuPanel;
    public CanvasGroup panelCanvasGroup;
    public Transform listContainer;
    public GameObject packCardTemplate;
    public ScrollRect packScrollView;

    [Header("Menu Cleanup")]
    public GameObject[] menuElementsToHide;

    [Header("Animation Settings")]
    public float fadeDuration = 0.3f;
    public float cardPopSpeed = 12f;
    public float cardStaggerDelay = 0.05f;
    public float clickBounce = 1.15f;

    [System.Serializable]
    public class FellaPack
    {
        public string packID;
        public string packName;
        public Sprite packBanner;

        [Header("Unlock Settings")]
        public bool isUnlockedByDefault = false;
        public int requiredAds = 2;

        [Header("The Assets")]
        public GameObject[] fellaPrefabs;

        public AudioClip[] mergeSounds;
        public AudioClip genericPopSound;
    }

    [Header("Available Packs")]
    public List<FellaPack> availablePacks = new List<FellaPack>();

    private const string PACK_SAVE_KEY = "SelectedPackID";
    private Dictionary<string, Transform> cardMap = new Dictionary<string, Transform>();

    private Dictionary<string, Coroutine> activePunchRoutines = new Dictionary<string, Coroutine>();
    private bool isPanelOpen = false;

    private float lastAdClickTime = 0f;

    void Start()
    {
        if (packMenuPanel != null)
        {
            packMenuPanel.SetActive(false);
            if (panelCanvasGroup != null) panelCanvasGroup.alpha = 0f;
        }

        if (packCardTemplate != null && listContainer != null)
        {
            if (packCardTemplate.transform.parent == listContainer)
            {
                packCardTemplate.transform.SetParent(packMenuPanel.transform);
            }
            packCardTemplate.SetActive(false);
        }

        SyncCloudMasterString();
    }


    private void SyncCloudMasterString()
    {
        string masterPacks = "";
        foreach (var pack in availablePacks) {
            if (PlayerPrefs.GetInt("PackUnlocked_" + pack.packID, 0) == 1) {
                masterPacks += pack.packID + ",";
            }
        }
        PlayerPrefs.SetString("Master_Pack_Unlocks", masterPacks);
        PlayerPrefs.Save();
    }

    void Update()
    {
        if (isPanelOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            ClosePackMenu();
        }
    }

    public void ApplyActivePackToGame(Spawner spawner, AudioManager audio)
    {
        string currentID = GetSelectedPackID();
        CollectionManager.SetCurrentPack(currentID);

        FellaPack pack = null;
        foreach (var p in availablePacks) { if (p.packID == currentID) { pack = p; break; } }

        if (pack != null)
        {
            if (spawner != null && pack.fellaPrefabs != null && pack.fellaPrefabs.Length > 0)
            {
                spawner.SwapPrefabs(pack.fellaPrefabs);
            }

            if (audio != null)
            {
                if (pack.mergeSounds != null && pack.mergeSounds.Length > 0)
                    audio.mergeClips = pack.mergeSounds;
                if (pack.genericPopSound != null)
                    audio.genericPopClips = new AudioClip[] { pack.genericPopSound };
            }
        }
    }

    public void OpenPackMenu()
    {
        StopAllCoroutines();
        activePunchRoutines.Clear();

        packMenuPanel.SetActive(true);
        isPanelOpen = true;
        ToggleMenuHUD(false);
        GeneratePackList();

        if (packScrollView != null)
        {
            packScrollView.verticalNormalizedPosition = 1f;
        }

        StartCoroutine(AnimatePanelOpen());
    }

    public void ClosePackMenu()
    {
        if (!isPanelOpen) return;

        StopAllCoroutines();
        activePunchRoutines.Clear();
        isPanelOpen = false;

        if (gameObject.activeInHierarchy)
            StartCoroutine(AnimatePanelClose());
        else
        {
            packMenuPanel.SetActive(false);
            ToggleMenuHUD(true);
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

    void GeneratePackList()
    {
        for (int i = listContainer.childCount - 1; i >= 0; i--)
        {
            if (listContainer.GetChild(i).gameObject != packCardTemplate)
                Destroy(listContainer.GetChild(i).gameObject);
        }
        cardMap.Clear();

        string currentID = GetSelectedPackID();

        for (int i = 0; i < availablePacks.Count; i++)
        {
            CreatePackCard(availablePacks[i], currentID);
        }
    }

    void CreatePackCard(FellaPack pack, string currentID)
    {
        if (packCardTemplate == null) return;

        GameObject card = Instantiate(packCardTemplate, listContainer);
        card.SetActive(true);
        card.name = "Pack_" + pack.packID;

        cardMap[pack.packID] = card.transform;
        card.transform.localScale = Vector3.zero;

        Transform bannerObj = FindDeepChild(card.transform, "BannerImage");
        Transform titleObj = FindDeepChild(card.transform, "TitleText");

        if (bannerObj != null) bannerObj.GetComponent<Image>().sprite = pack.packBanner;
        if (titleObj != null) titleObj.GetComponent<TextMeshProUGUI>().text = pack.packName;

        UpdateSingleCardVisuals(card.transform, pack, currentID);
    }

    void UpdateSingleCardVisuals(Transform card, FellaPack pack, string currentID)
    {
        Transform btnObj = FindDeepChild(card, "SelectButton");
        Transform selectedImgObj = FindDeepChild(card, "SelectedImage");
        Transform lockedOverlayObj = FindDeepChild(card, "LockedOverlay");
        Transform unlockBtnObj = FindDeepChild(card, "UnlockAdButton");
        Transform progressTextObj = FindDeepChild(card, "AdProgressText");

        bool isSelected = (pack.packID == currentID);

        bool isUnlocked = pack.isUnlockedByDefault || PlayerPrefs.GetInt("PackUnlocked_" + pack.packID, 0) == 1 || VIPManager.IsVIP();

        int adsWatched = PlayerPrefs.GetInt("PackAds_" + pack.packID, 0);

        if (isUnlocked)
        {
            if (lockedOverlayObj != null) lockedOverlayObj.gameObject.SetActive(false);
            if (unlockBtnObj != null) unlockBtnObj.gameObject.SetActive(false);
            if (progressTextObj != null) progressTextObj.gameObject.SetActive(false);

            if (selectedImgObj != null) selectedImgObj.gameObject.SetActive(isSelected);

            if (btnObj != null)
            {
                btnObj.gameObject.SetActive(!isSelected);
                if (!isSelected)
                {
                    Button btn = btnObj.GetComponent<Button>();
                    if (btn != null)
                    {
                        btn.onClick.RemoveAllListeners();
                        string idToSelect = pack.packID;
                        btn.onClick.AddListener(() => SelectPack(idToSelect));
                    }
                }
            }
        }
        else
        {
            if (btnObj != null) btnObj.gameObject.SetActive(false);
            if (selectedImgObj != null) selectedImgObj.gameObject.SetActive(false);

            if (lockedOverlayObj != null) lockedOverlayObj.gameObject.SetActive(true);

            if (progressTextObj != null)
            {
                progressTextObj.gameObject.SetActive(true);
                TextMeshProUGUI tmpText = progressTextObj.GetComponent<TextMeshProUGUI>();
                if (tmpText != null) tmpText.text = $"{adsWatched}/{pack.requiredAds}";
            }

            if (unlockBtnObj != null)
            {
                unlockBtnObj.gameObject.SetActive(true);
                Button unlockBtn = unlockBtnObj.GetComponent<Button>();
                if (unlockBtn != null)
                {
                    unlockBtn.onClick.RemoveAllListeners();
                    string idToUnlock = pack.packID;
                    unlockBtn.onClick.AddListener(() => WatchAdToUnlock(idToUnlock));
                }
            }
        }
    }

    void RefreshUIState()
    {
        string currentID = GetSelectedPackID();
        foreach (var pack in availablePacks)
        {
            if (cardMap.ContainsKey(pack.packID) && cardMap[pack.packID] != null)
            {
                UpdateSingleCardVisuals(cardMap[pack.packID], pack, currentID);
            }
        }
    }


    public void SelectPack(string packID)
    {
        PlayerPrefs.SetString(PACK_SAVE_KEY, packID);
        PlayerPrefs.Save();

        RefreshUIState();
        TriggerCardPunch(packID);
    }

    public void WatchAdToUnlock(string packID)
    {
        if (Time.time - lastAdClickTime < 2f) return;
        lastAdClickTime = Time.time;

        if (AdsManager.Instance != null)
        {
            AdsManager.Instance.ShowRewardedAd(() => OnUnlockAdCompleted(packID));
        }
    }

    private void OnUnlockAdCompleted(string packID)
    {
        FellaPack pack = availablePacks.Find(p => p.packID == packID);
        if (pack == null) return;

        int adsWatched = PlayerPrefs.GetInt("PackAds_" + packID, 0);
        adsWatched++;

        if (adsWatched >= pack.requiredAds)
        {
            PlayerPrefs.SetInt("PackUnlocked_" + packID, 1);
            SyncCloudMasterString();
        }
        else
        {
            PlayerPrefs.SetInt("PackAds_" + packID, adsWatched);
        }

        PlayerPrefs.Save();

        if (PlayGamesManager.Instance != null)
        {
            PlayGamesManager.Instance.SaveToCloud();
        }

        RefreshUIState();
        TriggerCardPunch(packID);
    }

    public string GetSelectedPackID()
    {
        if (availablePacks.Count == 0) return "";

        string savedID = PlayerPrefs.GetString(PACK_SAVE_KEY, availablePacks[0].packID);

        bool packExists = availablePacks.Exists(p => p.packID == savedID);

        if (!packExists)
        {
            savedID = availablePacks[0].packID;
            PlayerPrefs.SetString(PACK_SAVE_KEY, savedID);
            PlayerPrefs.Save();
        }

        return savedID;
    }


    void TriggerCardPunch(string packID)
    {
        if (cardMap.ContainsKey(packID) && cardMap[packID] != null)
        {
            Transform target = cardMap[packID];

            if (activePunchRoutines.ContainsKey(packID) && activePunchRoutines[packID] != null)
            {
                StopCoroutine(activePunchRoutines[packID]);
            }

            if (gameObject.activeInHierarchy)
            {
                activePunchRoutines[packID] = StartCoroutine(PunchCard(target));
            }
        }
    }

    IEnumerator AnimatePanelOpen()
    {
        if (panelCanvasGroup != null)
        {
            float t = 0f;
            while (t < 1f) { t += Time.deltaTime / fadeDuration; panelCanvasGroup.alpha = Mathf.SmoothStep(0f, 1f, t); yield return null; }
            panelCanvasGroup.alpha = 1f;
        }
        StartCoroutine(AnimateCardsIn());
    }

    IEnumerator AnimateCardsIn()
    {
        foreach (var kvp in cardMap) if (kvp.Value != null) kvp.Value.localScale = Vector3.zero;

        foreach (var pack in availablePacks)
        {
            if (cardMap.ContainsKey(pack.packID))
            {
                Transform card = cardMap[pack.packID];
                StartCoroutine(PopCard(card));
                yield return new WaitForSeconds(cardStaggerDelay);
            }
        }
    }

    IEnumerator PopCard(Transform target)
    {
        float t = 0f;
        while (t < 1f)
        {
            if (target == null) yield break;
            t += Time.unscaledDeltaTime * cardPopSpeed;
            float scale = Mathf.Sin(t * Mathf.PI * 0.5f) + (Mathf.Sin(t * Mathf.PI) * 0.2f);
            if (t >= 1f) scale = 1f;
            target.localScale = Vector3.one * scale;
            yield return null;
        }
        if (target != null) target.localScale = Vector3.one;
    }

    IEnumerator PunchCard(Transform target)
    {
        float t = 0f;
        while (t < 0.15f)
        {
            if (target == null) yield break;
            t += Time.unscaledDeltaTime;
            float scale = Mathf.Lerp(1f, clickBounce, t / 0.15f);
            target.localScale = Vector3.one * scale;
            yield return null;
        }
        t = 0f;
        while (t < 0.15f)
        {
            if (target == null) yield break;
            t += Time.unscaledDeltaTime;
            float scale = Mathf.Lerp(clickBounce, 1f, t / 0.15f);
            target.localScale = Vector3.one * scale;
            yield return null;
        }
        if (target != null) target.localScale = Vector3.one;
    }

    IEnumerator AnimatePanelClose()
    {
        if (panelCanvasGroup != null)
        {
            float t = 0f;
            while (t < 1f) { t += Time.deltaTime / fadeDuration; panelCanvasGroup.alpha = Mathf.SmoothStep(1f, 0f, t); yield return null; }
            panelCanvasGroup.alpha = 0f;
        }

        for (int i = listContainer.childCount - 1; i >= 0; i--)
        {
            if (listContainer.GetChild(i).gameObject != packCardTemplate)
                Destroy(listContainer.GetChild(i).gameObject);
        }
        cardMap.Clear();
        activePunchRoutines.Clear();

        packMenuPanel.SetActive(false);
        ToggleMenuHUD(true);
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