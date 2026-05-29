using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AnvilManager : MonoBehaviour
{
    [Header("Dependencies")]
    public Spawner spawner;

    [Header("UI References")]
    public Button uiButton;
    public Image buttonImage;
    public TextMeshProUGUI buttonText;
    public GameObject badgeObject;
    public TextMeshProUGUI badgeText;

    [Header("Ad Integration")]
    public GameObject adIconObject;
    public int adRewardAmount = 1;

    private const string ANVIL_KEY = "Consumable_Anvil";

    void Start()
    {
        if (spawner == null) spawner = FindFirstObjectByType<Spawner>();

        if (PlayerPrefs.GetInt("FirstTimeAnvil", 0) == 0)
        {
            StatTracker.Increment(ANVIL_KEY, 1);
            PlayerPrefs.SetInt("FirstTimeAnvil", 1);
            PlayerPrefs.Save();
        }
    }

    void Update()
    {
        int count = StatTracker.GetStat(ANVIL_KEY);

        if (badgeObject != null)
        {
            if (count > 0)
            {
                badgeObject.SetActive(true);
                if (badgeText != null) badgeText.text = count.ToString();
            }
            else
            {
                badgeObject.SetActive(false);
            }
        }

        if (spawner != null && spawner.IsAnvilMode)
        {
            if (buttonText != null) buttonText.text = "ACTIVE";

            if (buttonImage != null) buttonImage.color = Color.white;

            if (uiButton != null) uiButton.interactable = false;
            if (adIconObject != null) adIconObject.SetActive(false);
        }
        else if (count > 0)
        {
            if (buttonText != null) buttonText.text = "ANVIL";
            if (buttonImage != null) buttonImage.color = Color.white;
            if (uiButton != null) uiButton.interactable = true;
            if (adIconObject != null) adIconObject.SetActive(false);
        }
        else
        {
            if (buttonText != null) buttonText.text = "FREE";

            if (buttonImage != null) buttonImage.color = Color.white;

            if (uiButton != null) uiButton.interactable = true;
            if (adIconObject != null) adIconObject.SetActive(true);
        }
    }

    public void OnAnvilClicked()
    {
        int count = StatTracker.GetStat(ANVIL_KEY);

        if (count > 0)
        {
            if (spawner != null)
            {
                StatTracker.Increment(ANVIL_KEY, -1);

                StatTracker.Increment("UsedAnvil");

                if (StatTracker.GetStat("UsedAnvil") == 10 && PlayGamesManager.Instance != null)
                {
                    PlayGamesManager.Instance.UnlockAchievement(GPGSIds.achievement_hammer_time);
                }

                spawner.ActivateAnvil();
            }
        }
        else
        {
            if (AdsManager.Instance != null)
            {
                AdsManager.Instance.ShowRewardedAd(OnAdCompleted);
            }
        }
    }

    void OnAdCompleted()
    {
        StatTracker.Increment(ANVIL_KEY, adRewardAmount);
    }
}