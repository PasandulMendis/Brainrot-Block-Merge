using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI highScoreText;
    public TextMeshProUGUI levelText;
    public RectTransform playButtonTransform;

    [Header("Dependencies")]
    public SceneFader sceneFader;

    [Header("Configuration")]
    public string gameSceneName = "SampleScene";

    [Header("Pulse Animation")]
    public float pulseSpeed = 3.0f;
    public float pulseSize = 1.1f;

    void Start()
    {
        Application.targetFrameRate = 120;
        QualitySettings.vSyncCount = 0;

        if (sceneFader == null) sceneFader = FindFirstObjectByType<SceneFader>();

        int bestScore = PlayerPrefs.GetInt("Brainrot_Best", 0);
        if (highScoreText != null)
        {
            highScoreText.text = "BEST: " + bestScore.ToString();
        }

        if (levelText != null)
        {
            int currentLevel = PlayerPrefs.GetInt("Player_Infinite_Level", 1);
            levelText.text = "LEVEL " + currentLevel;
        }

        if (playButtonTransform != null)
        {
            StartCoroutine(PulseButton());
        }
    }

    public void OnPlayClicked()
    {
        if (sceneFader != null)
        {
            sceneFader.FadeToScene(gameSceneName);
        }
        else
        {
            SceneManager.LoadScene(gameSceneName);
        }
    }

    public void OnExitClicked()
    {
        Debug.Log("Exiting Game...");
        Application.Quit();
    }

    IEnumerator PulseButton()
    {
        while (true)
        {
            float scale = 1.0f + Mathf.PingPong(Time.time * (pulseSpeed * 0.1f), pulseSize - 1.0f);
            playButtonTransform.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }
    }
}