using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI highScoreText;

    private int currentScore = 0;
    private int highScore = 0;
    private const string HIGH_SCORE_KEY = "Brainrot_Best";

    void Awake()
    {
        highScore = PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0);
    }

    void Start()
    {
        UpdateUI();
    }

    public void ResetScore()
    {
        currentScore = 0;
        UpdateUI();
    }

    public void LoadSavedScore(int savedScore)
    {
        currentScore = savedScore;
        UpdateUI();
    }

    public void AddScore(int amount)
    {
        currentScore += amount;

        StatTracker.Increment("TotalScore", amount);

        if (currentScore >= 3000 && PlayGamesManager.Instance != null)
        {
            PlayGamesManager.Instance.UnlockAchievement(GPGSIds.achievement_high_roller);
        }

        if (currentScore > highScore)
        {
            highScore = currentScore;
            PlayerPrefs.SetInt(HIGH_SCORE_KEY, highScore);
            PlayerPrefs.Save();

            if (highScore % 500 == 0)
            {
                if (PlayGamesManager.Instance != null) PlayGamesManager.Instance.SaveToCloud();
            }
        }

        UpdateUI();
    }

    public int GetScore()
    {
        return currentScore;
    }

    private void UpdateUI()
    {
        if (scoreText != null) scoreText.text = currentScore.ToString();
        if (highScoreText != null) highScoreText.text = highScore.ToString();
    }
}