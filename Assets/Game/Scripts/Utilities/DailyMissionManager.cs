using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

public class DailyMissionManager : MonoBehaviour
{
    [Header("Dependencies")]
    public CurrencyManager currencyManager;

    [Header("UI References")]
    public GameObject missionPanel;
    public CanvasGroup panelCanvasGroup;
    public Transform listContainer;
    public GameObject missionItemTemplate;
    public TextMeshProUGUI panelCoinText;
    public RectTransform coinIconToAnimate;

    [Header("Timer UI")]
    public TextMeshProUGUI resetTimerText;

    [Header("Menu Cleanup")]
    public GameObject[] menuElementsToHide;

    [Header("Animation Settings")]
    public float fadeDuration = 0.3f;
    public float itemStaggerDelay = 0.1f;

    private const string LAST_LOGIN_KEY = "LastLoginDate";

    private Mission[] activeMissions = new Mission[6];
    private bool isPanelOpen = false;

    [System.Serializable]
    public class Mission
    {
        public string description;
        public string statKey;
        public int target;
        public int reward;
        public bool isClaimed;
    }

    private List<Mission> missionPool = new List<Mission>()
    {
        new Mission { description = "Collect 200 Coins", statKey = "CoinsEarned", target = 200, reward = 50 },
        new Mission { description = "Collect 500 Coins", statKey = "CoinsEarned", target = 500, reward = 100 },
        new Mission { description = "Score 3000 Points", statKey = "TotalScore", target = 3000, reward = 60 },
        new Mission { description = "Score 4000 Points", statKey = "TotalScore", target = 4000, reward = 80 },
        new Mission { description = "Score 5000 Points", statKey = "TotalScore", target = 5000, reward = 100 },
        new Mission { description = "Play 5 Times", statKey = "GamesPlayed", target = 5, reward = 40 },
        new Mission { description = "Play 10 Times", statKey = "GamesPlayed", target = 10, reward = 80 },
        new Mission { description = "Create a Tier 8", statKey = "Merge_Tier_8", target = 1, reward = 30 },
        new Mission { description = "Create a Tier 9", statKey = "Merge_Tier_9", target = 1, reward = 50 },
        new Mission { description = "Create a Tier 10", statKey = "Merge_Tier_10", target = 1, reward = 100 },
        new Mission { description = "Shake 3 Times", statKey = "Shake", target = 3, reward = 15 },
        new Mission { description = "Shake 5 Times", statKey = "Shake", target = 5, reward = 25 },
        new Mission { description = "Shake 10 Times", statKey = "Shake", target = 10, reward = 50 },
        new Mission { description = "Use Anvil 5 Times", statKey = "UsedAnvil", target = 5, reward = 40 },
        new Mission { description = "Use Anvil 10 Times", statKey = "UsedAnvil", target = 10, reward = 80 },
        new Mission { description = "Merge 100 Blocks", statKey = "Merge", target = 100, reward = 40 }
    };

    void Start()
    {
        if (missionPanel != null)
        {
            missionPanel.SetActive(false);
            if (panelCanvasGroup != null) panelCanvasGroup.alpha = 0f;
        }

        if (missionItemTemplate != null) missionItemTemplate.SetActive(false);

        CheckDailyReset();
        LoadMissions();
        UpdatePanelCoins();
    }

    void Update()
    {
        if (isPanelOpen)
        {
            if (resetTimerText != null) UpdateCountdown();

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ClosePanel();
            }
        }
    }

    void UpdateCountdown()
    {
        DateTime now = DateTime.Now;
        DateTime tomorrow = now.AddDays(1).Date;
        TimeSpan diff = tomorrow - now;
        resetTimerText.text = string.Format("Resets in {0:D2}:{1:D2}:{2:D2}", diff.Hours, diff.Minutes, diff.Seconds);
    }

    public void OpenPanel()
    {
        StopAllCoroutines();

        missionPanel.SetActive(true);
        ToggleMenuHUD(false);
        ClearList();
        isPanelOpen = true;

        UpdatePanelCoins();

        StartCoroutine(AnimatePanelOpen());
    }

    public void ClosePanel()
    {
        if (!isPanelOpen) return;

        StopAllCoroutines();

        isPanelOpen = false;
        StartCoroutine(AnimatePanelClose());
    }

    IEnumerator AnimatePanelOpen()
    {
        if (panelCanvasGroup != null)
        {
            float t = 0f;
            while (t < 1f) { t += Time.deltaTime / fadeDuration; panelCanvasGroup.alpha = Mathf.SmoothStep(0f, 1f, t); yield return null; }
            panelCanvasGroup.alpha = 1f;
        }
        StartCoroutine(BuildListRoutine());
    }

    IEnumerator AnimatePanelClose()
    {
        if (panelCanvasGroup != null)
        {
            float t = 0f;
            while (t < 1f) { t += Time.deltaTime / fadeDuration; panelCanvasGroup.alpha = Mathf.SmoothStep(1f, 0f, t); yield return null; }
            panelCanvasGroup.alpha = 0f;
        }
        ClearList();
        missionPanel.SetActive(false);
        ToggleMenuHUD(true);
    }

    void ClearList() { foreach (Transform child in listContainer) { if (child.gameObject != missionItemTemplate) Destroy(child.gameObject); } }

    IEnumerator BuildListRoutine()
    {
        for (int i = 0; i < activeMissions.Length; i++)
        {
            GameObject row = CreateMissionRow(i, activeMissions[i]);
            row.transform.localScale = new Vector3(1f, 0f, 1f);

            CanvasGroup rowGroup = row.GetComponent<CanvasGroup>();
            if (rowGroup == null) rowGroup = row.AddComponent<CanvasGroup>();
            rowGroup.alpha = 0f;

            StartCoroutine(AnimateRowIn(row.transform, rowGroup));
            yield return new WaitForSeconds(itemStaggerDelay);
        }
    }

    IEnumerator AnimateRowIn(Transform rowTrans, CanvasGroup rowGroup)
    {
        float t = 0f;
        while (t < 1f)
        {
            if (rowTrans == null) yield break;

            t += Time.deltaTime * 6f;
            rowTrans.localScale = new Vector3(1f, Mathf.Lerp(0f, 1f, t), 1f);
            rowGroup.alpha = t;
            yield return null;
        }
        if (rowTrans != null)
        {
            rowTrans.localScale = Vector3.one;
            rowGroup.alpha = 1f;
        }
    }

    void CheckDailyReset()
    {
        string lastDate = PlayerPrefs.GetString(LAST_LOGIN_KEY, "");
        string today = DateTime.Now.ToString("yyyyMMdd");

        if (lastDate != today)
        {
            StatTracker.ResetDailyStats();
            StatTracker.ResetStat("UsedAnvil");
            StatTracker.ResetStat("Shake");
            StatTracker.ResetStat("GamesPlayed");
            StatTracker.ResetStat("Merge");
            StatTracker.ResetStat("CoinsEarned");
            StatTracker.ResetStat("TotalScore");
            for (int i = 0; i < 12; i++) StatTracker.ResetStat("Merge_Tier_" + i);

            StatTracker.Increment("Login", 1);
            GenerateNewMissions();
            PlayerPrefs.SetString(LAST_LOGIN_KEY, today);
            PlayerPrefs.Save();
        }
    }

    void GenerateNewMissions()
    {
        SaveMission(0, "Daily Login", "Login", 1, 50, false);
        List<Mission> poolCopy = new List<Mission>(missionPool);

        for (int i = 1; i <= 5; i++)
        {
            if (poolCopy.Count == 0) break;
            int r = UnityEngine.Random.Range(0, poolCopy.Count);
            Mission picked = poolCopy[r];
            poolCopy.RemoveAt(r);
            SaveMission(i, picked.description, picked.statKey, picked.target, picked.reward, false);
        }
    }

    void SaveMission(int index, string desc, string key, int target, int reward, bool claimed)
    {
        PlayerPrefs.SetString($"Mission_{index}_Desc", desc);
        PlayerPrefs.SetString($"Mission_{index}_Key", key);
        PlayerPrefs.SetInt($"Mission_{index}_Target", target);
        PlayerPrefs.SetInt($"Mission_{index}_Reward", reward);
        PlayerPrefs.SetInt($"Mission_{index}_Claimed", claimed ? 1 : 0);
    }

    void LoadMissions()
    {
        for (int i = 0; i < activeMissions.Length; i++)
        {
            activeMissions[i] = new Mission();
            activeMissions[i].description = PlayerPrefs.GetString($"Mission_{i}_Desc", "Error");
            activeMissions[i].statKey = PlayerPrefs.GetString($"Mission_{i}_Key", "");
            activeMissions[i].target = PlayerPrefs.GetInt($"Mission_{i}_Target", 1);
            activeMissions[i].reward = PlayerPrefs.GetInt($"Mission_{i}_Reward", 10);
            activeMissions[i].isClaimed = PlayerPrefs.GetInt($"Mission_{i}_Claimed", 0) == 1;
        }
    }

    GameObject CreateMissionRow(int index, Mission m)
    {
        GameObject row = Instantiate(missionItemTemplate, listContainer);
        row.SetActive(true);
        row.name = "Mission_" + index;
        UpdateRowVisuals(row, m, index);
        return row;
    }

    void UpdateRowVisuals(GameObject row, Mission m, int index)
    {
        TextMeshProUGUI descText = row.transform.Find("DescText").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI progressText = row.transform.Find("ProgressText").GetComponent<TextMeshProUGUI>();

        Button claimBtn = row.transform.Find("ClaimButton").GetComponent<Button>();
        Image btnBackground = claimBtn.GetComponent<Image>();

        Transform rewardTextTrans = claimBtn.transform.Find("RewardText");
        TextMeshProUGUI rewardText = (rewardTextTrans != null) ? rewardTextTrans.GetComponent<TextMeshProUGUI>() : null;

        Transform claimTextTrans = claimBtn.transform.Find("ClaimText");
        TextMeshProUGUI claimText = (claimTextTrans != null) ? claimTextTrans.GetComponent<TextMeshProUGUI>() : null;

        Transform coinIconTrans = claimBtn.transform.Find("CoinIcon");
        GameObject coinIconObj = (coinIconTrans != null) ? coinIconTrans.gameObject : null;

        Transform doneImageTrans = claimBtn.transform.Find("DoneImage");
        GameObject doneImageObj = (doneImageTrans != null) ? doneImageTrans.gameObject : null;

        int currentProgress = StatTracker.GetStat(m.statKey);
        int displayProgress = Mathf.Min(currentProgress, m.target);

        descText.text = m.description;
        progressText.text = $"{displayProgress}/{m.target}";

        claimBtn.onClick.RemoveAllListeners();

        if (m.isClaimed)
        {
            claimBtn.interactable = false;
            if (btnBackground != null) btnBackground.enabled = false;

            if (rewardText != null) rewardText.gameObject.SetActive(false);
            if (claimText != null) claimText.gameObject.SetActive(false);
            if (coinIconObj != null) coinIconObj.SetActive(false);
            if (doneImageObj != null) doneImageObj.SetActive(true);
        }
        else if (currentProgress >= m.target)
        {
            claimBtn.interactable = true;
            if (btnBackground != null) btnBackground.enabled = true;

            if (rewardText != null) rewardText.gameObject.SetActive(false);
            if (claimText != null) claimText.gameObject.SetActive(true);
            if (coinIconObj != null) coinIconObj.SetActive(false);
            if (doneImageObj != null) doneImageObj.SetActive(false);

            int captureIndex = index;
            claimBtn.onClick.AddListener(() => ClaimMission(captureIndex));
        }
        else
        {
            claimBtn.interactable = false;
            if (btnBackground != null) btnBackground.enabled = true;

            if (rewardText != null)
            {
                rewardText.gameObject.SetActive(true);
                rewardText.text = "+" + m.reward;
            }
            if (claimText != null) claimText.gameObject.SetActive(false);

            if (coinIconObj != null) coinIconObj.SetActive(true);
            if (doneImageObj != null) doneImageObj.SetActive(false);
        }
    }

    public void ClaimMission(int index)
    {
        Mission m = activeMissions[index];
        if (m.isClaimed) return;

        if (currencyManager != null) currencyManager.AddCoins(m.reward);

        m.isClaimed = true;
        PlayerPrefs.SetInt($"Mission_{index}_Claimed", 1);
        PlayerPrefs.Save();

        StartCoroutine(ShakeCoinIcon());
        UpdatePanelCoins();

        Transform existingRow = listContainer.Find("Mission_" + index);
        if (existingRow != null) UpdateRowVisuals(existingRow.gameObject, m, index);
    }

    void UpdatePanelCoins()
    {
        if (currencyManager != null && panelCoinText != null)
        {
            panelCoinText.text = currencyManager.GetBalance().ToString("N0");
        }
    }

    IEnumerator ShakeCoinIcon()
    {
        if (coinIconToAnimate == null) yield break;
        float t = 0;
        while (t < 0.4f) { t += Time.deltaTime; coinIconToAnimate.localScale = Vector3.one * (1f + Mathf.Sin(t * 10f) * 0.4f); yield return null; }
        coinIconToAnimate.localScale = Vector3.one;
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