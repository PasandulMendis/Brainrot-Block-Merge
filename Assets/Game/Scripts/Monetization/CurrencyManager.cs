using UnityEngine;
using TMPro;
using System.Collections;

public class CurrencyManager : MonoBehaviour
{
    [Header("UI Reference")]
    public TextMeshProUGUI coinText;

    [Header("Visual Feedback")]
    public RectTransform coinIconTransform;

    [Header("Economy Settings")]
    public int[] tierRewards;

    private int currentCoins;
    private const string COIN_KEY = "MemeCoins_Balance";

    void Awake()
    {
        SyncCoins();
    }

    void Start()
    {
        UpdateUI();
    }

    private void SyncCoins()
    {
        currentCoins = PlayerPrefs.GetInt(COIN_KEY, 0);
    }

    public void AddCoins(int amount)
    {
        if (amount <= 0) return;

        SyncCoins();
        currentCoins += amount;

        if (currentCoins > 99999) currentCoins = 99999;

        PlayerPrefs.SetInt(COIN_KEY, currentCoins);
        PlayerPrefs.Save();

        StatTracker.Increment("CoinsEarned", amount);
        UpdateUI();

        if (coinIconTransform != null)
        {
            StopAllCoroutines();
            StartCoroutine(ShakeIcon());
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCoinSound();
        }
    }

    public void SpendCoins(int amount)
    {
        if (amount <= 0) return;

        SyncCoins();
        currentCoins -= amount;

        if (currentCoins < 0) currentCoins = 0;

        PlayerPrefs.SetInt(COIN_KEY, currentCoins);
        PlayerPrefs.Save();

        if (PlayGamesManager.Instance != null) PlayGamesManager.Instance.SaveToCloud();

        UpdateUI();

        if (coinIconTransform != null)
        {
            StopAllCoroutines();
            StartCoroutine(ShakeIcon());
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCoinSound();
        }
    }

    public int GetBalance()
    {
        SyncCoins();
        return currentCoins;
    }

    public int GetRewardForTier(int tierIndex)
    {
        if (tierRewards != null && tierIndex < tierRewards.Length)
        {
            return tierRewards[tierIndex];
        }
        return 0;
    }

    private void UpdateUI()
    {
        if (coinText != null)
        {
            coinText.text = PlayerPrefs.GetInt(COIN_KEY, 0).ToString("N0");
        }
    }

    IEnumerator ShakeIcon()
    {
        float timer = 0f;
        float duration = 0.4f;
        Vector3 defaultScale = Vector3.one;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = timer / duration;
            float scaleBump = Mathf.Sin(progress * Mathf.PI) * 0.4f;
            float currentScale = 1f + scaleBump;
            float shakeAngle = Mathf.Sin(timer * 50f) * 15f * (1f - progress);

            coinIconTransform.localScale = defaultScale * currentScale;
            coinIconTransform.localRotation = Quaternion.Euler(0, 0, shakeAngle);
            yield return null;
        }
        coinIconTransform.localScale = defaultScale;
        coinIconTransform.localRotation = Quaternion.identity;
    }
}