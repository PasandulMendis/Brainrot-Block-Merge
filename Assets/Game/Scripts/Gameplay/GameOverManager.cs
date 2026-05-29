using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class GameOverManager : MonoBehaviour
{
    [Header("Dependencies")]
    public MergeGameController gameController;
    public ScoreManager scoreManager;
    public SceneFader sceneFader;

    [Header("Phase 1: Revive Screen")]
    public GameObject revivePanel;
    public RectTransform reviveCard;
    public TextMeshProUGUI reviveScoreText;
    public TextMeshProUGUI reviveBestText;
    public GameObject reviveAdIcon;

    [Header("Phase 2: Final Game Over Screen")]
    public GameObject finalPanel;
    public RectTransform finalCard;
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI finalBestText;

    [Header("HUD Control")]
    public GameObject[] hudElementsToHide;

    [Header("Revive Settings")]
    public int maxRevives = 2;
    public int baseRemovalCount = 8;
    public int increasePerRevive = 4;
    public int saveTierThreshold = 6;
    public GameObject explosionEffect;
    public float popInterval = 0.10f;

    [Header("Animation Settings")]
    public float zoomOutAmount = 2.0f;
    public float zoomDuration = 1.0f;
    public float panelSwitchSpeed = 10f;

    private float defaultCamSize;
    private Camera mainCam;
    private int reviveCount = 0;

    void Start()
    {
        mainCam = Camera.main;
        if (mainCam != null) defaultCamSize = mainCam.orthographicSize;

        if (revivePanel != null) revivePanel.SetActive(false);
        if (finalPanel != null) finalPanel.SetActive(false);

        if (sceneFader == null) sceneFader = FindFirstObjectByType<SceneFader>();

        reviveCount = 0;
    }

    public void ShowGameOver()
    {
        ToggleHUD(false);
        StartCoroutine(AnimateCamera(defaultCamSize + zoomOutAmount));

        if (reviveCount < maxRevives)
        {
            if (reviveAdIcon != null) reviveAdIcon.SetActive(true);
            StartCoroutine(OpenPanelSequence(revivePanel, reviveCard, reviveScoreText, reviveBestText));
        }
        else
        {
            StartCoroutine(OpenPanelSequence(finalPanel, finalCard, finalScoreText, finalBestText));
        }
    }

    public void OnReviveClicked()
    {
        if (AdsManager.Instance != null)
        {
            AdsManager.Instance.ShowRewardedAd(OnAdCompleted);
        }
        else
        {
            OnAdCompleted();
        }
    }

    void OnAdCompleted()
    {
        StartCoroutine(ReviveSequence());
    }

    public void OnGiveUpClicked()
    {
        StartCoroutine(SwitchToFinalPanel());
    }

    public void OnRestartClicked()
    {
        if (PlayGamesManager.Instance != null) PlayGamesManager.Instance.SaveToCloud();

        if (AdsManager.Instance != null)
        {
            AdsManager.Instance.TryShowInterstitial(() =>
            {
                StartCoroutine(RestartSequence());
            });
        }
        else
        {
            StartCoroutine(RestartSequence());
        }
    }

    public void OnMainMenuClicked()
    {
        if (PlayGamesManager.Instance != null) PlayGamesManager.Instance.SaveToCloud();

        if (AdsManager.Instance != null)
        {
            AdsManager.Instance.TryShowInterstitial(() =>
            {
                TriggerMainMenuLoad();
            });
        }
        else
        {
            TriggerMainMenuLoad();
        }
    }

    private void TriggerMainMenuLoad()
    {
        if (sceneFader != null) sceneFader.FadeToScene("MainMenu");
        else SceneManager.LoadScene("MainMenu");
    }

    IEnumerator RestartSequence()
    {
        yield return StartCoroutine(ClosePanelSequence(finalPanel, finalCard));

        GameObject[] allBlocks = GameObject.FindGameObjectsWithTag("Player");

        foreach (GameObject block in allBlocks)
        {
            if (explosionEffect != null)
            {
                GameObject p = Instantiate(explosionEffect, block.transform.position, Quaternion.identity);
                Destroy(p, 2.0f);
            }

            if (gameController.audioManager != null) gameController.audioManager.PlayGenericPop();
            if (gameController.hapticManager != null) gameController.hapticManager.TriggerMergeHaptic();

            Destroy(block);
            yield return new WaitForSeconds(0.05f);
        }

        if (scoreManager != null) scoreManager.ResetScore();
        reviveCount = 0;

        StartCoroutine(AnimateCamera(defaultCamSize));
        ToggleHUD(true);

        if (gameController.spawner != null) gameController.spawner.Init();
        if (gameController.audioManager != null) gameController.audioManager.PlayMusic();

        if (gameController != null)
        {
            gameController.isGameOver = false;
            gameController.canDrop = true;
        }
    }

    IEnumerator ReviveSequence()
    {
        yield return StartCoroutine(ClosePanelSequence(revivePanel, reviveCard));

        int targetToRemove = baseRemovalCount + (reviveCount * increasePerRevive);
        targetToRemove = Mathf.Min(targetToRemove, 20);
        reviveCount++;

        GameObject[] allBlocks = GameObject.FindGameObjectsWithTag("Player");
        List<GameObject> sortedBlocks = allBlocks.OrderByDescending(b => b.transform.position.y).ToList();

        int removedCount = 0;
        foreach (GameObject block in sortedBlocks)
        {
            if (removedCount >= targetToRemove) break;

            Rigidbody2D rb = block.GetComponent<Rigidbody2D>();
            if (rb != null && rb.bodyType == RigidbodyType2D.Kinematic) continue;

            FellaLogic fella = block.GetComponent<FellaLogic>();
            if (fella != null && fella.fellaIndex >= saveTierThreshold) continue;

            if (explosionEffect != null)
            {
                GameObject p = Instantiate(explosionEffect, block.transform.position, Quaternion.identity);
                Destroy(p, 2.0f);
            }

            if (gameController.audioManager != null) gameController.audioManager.PlayGenericPop();
            if (gameController.hapticManager != null) gameController.hapticManager.TriggerMergeHaptic();

            Destroy(block);
            removedCount++;
            yield return new WaitForSeconds(popInterval);
        }

        StartCoroutine(AnimateCamera(defaultCamSize));
        ToggleHUD(true);

        if (gameController.audioManager != null) gameController.audioManager.PlayMusic();

        if (gameController != null)
        {
            gameController.isGameOver = false;
            gameController.canDrop = true;
        }
    }

    IEnumerator SwitchToFinalPanel()
    {
        yield return StartCoroutine(ClosePanelSequence(revivePanel, reviveCard));
        yield return StartCoroutine(OpenPanelSequence(finalPanel, finalCard, finalScoreText, finalBestText));
    }

    IEnumerator OpenPanelSequence(GameObject panelObj, RectTransform card, TextMeshProUGUI scoreTxt, TextMeshProUGUI bestTxt)
    {
        panelObj.SetActive(true);
        CanvasGroup cg = panelObj.GetComponent<CanvasGroup>();
        if (cg == null) cg = panelObj.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        card.localScale = Vector3.zero;

        if (scoreManager != null)
        {
            if (scoreTxt) scoreTxt.text = scoreManager.GetScore().ToString();
            if (bestTxt) bestTxt.text = "BEST: " + PlayerPrefs.GetInt("Brainrot_Best", 0).ToString();
        }

        float progress = 0;
        while (progress < 1)
        {
            progress += Time.unscaledDeltaTime * (panelSwitchSpeed * 0.8f);
            cg.alpha = progress;
            float scale = Mathf.Sin(progress * Mathf.PI * 0.5f) + (Mathf.Sin(progress * Mathf.PI) * 0.2f);
            if (progress >= 1) scale = 1;
            card.localScale = Vector3.one * scale;
            yield return null;
        }
        cg.alpha = 1f;
        card.localScale = Vector3.one;
    }

    IEnumerator ClosePanelSequence(GameObject panelObj, RectTransform card)
    {
        CanvasGroup cg = panelObj.GetComponent<CanvasGroup>();
        if (cg == null) cg = panelObj.AddComponent<CanvasGroup>();

        float progress = 0;
        while (progress < 1)
        {
            progress += Time.unscaledDeltaTime * panelSwitchSpeed;
            cg.alpha = 1f - progress;
            card.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, progress);
            yield return null;
        }
        panelObj.SetActive(false);
    }

    IEnumerator AnimateCamera(float targetSize)
    {
        if (mainCam == null) yield break;
        float startSize = mainCam.orthographicSize;
        float elapsed = 0f;
        while (elapsed < zoomDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / zoomDuration);
            mainCam.orthographicSize = Mathf.Lerp(startSize, targetSize, t);
            yield return null;
        }
        mainCam.orthographicSize = targetSize;
    }

    void ToggleHUD(bool show)
    {
        if (hudElementsToHide != null)
        {
            foreach (GameObject go in hudElementsToHide)
            {
                if (go != null) go.SetActive(show);
            }
        }
    }
}