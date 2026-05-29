using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;

public class PauseManager : MonoBehaviour
{
    [Header("Settings")]
    public bool useHardwareBackButton = true;

    [Header("Dependencies")]
    public MergeGameController gameController;

    [Header("UI Reference")]
    public GameObject pausePanel;
    public CanvasGroup uiCanvasGroup;

    [Header("HUD Control")]
    public GameObject[] hudElementsToHide;

    [Header("Animation")]
    public float fadeDuration = 0.25f;

    [Header("Difficulty Slider UI")]
    public RectTransform difficultyIndicator;
    public float relaxedX = -150f;
    public float mediumX = 0f;
    public float hardX = 150f;
    public float indicatorMoveSpeed = 15f;

    [Header("Jelly Physics UI")]
    public Image jellyTickImage;

    [Header("Interactive Shake UI")]
    public Image interactiveShakeTickImage;

    private float targetIndicatorX = 0f;
    private bool isPaused = false;
    private bool isDraggingIndicator = false;
    private bool isJellyEnabled = true;
    private bool isInteractiveShakeEnabled = true;

    void Start()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (gameController == null) gameController = FindFirstObjectByType<MergeGameController>();

        isJellyEnabled = PlayerPrefs.GetInt("Setting_JellyBody", 1) == 1;
        UpdateJellyUI();

        isInteractiveShakeEnabled = PlayerPrefs.GetInt("Setting_InteractiveShake", 1) == 1;
        UpdateInteractiveShakeUI();

        Physics2D.simulationMode = SimulationMode2D.FixedUpdate;
    }

    void Update()
    {
        if (useHardwareBackButton)
        {
            if (Input.GetKeyDown(KeyCode.Escape)) TogglePause();
        }

        if (difficultyIndicator != null && pausePanel != null && pausePanel.activeInHierarchy)
        {
            HandleIndicatorDrag();

            if (!isDraggingIndicator)
            {
                Vector2 currentPos = difficultyIndicator.anchoredPosition;
                Vector2 targetPos = new Vector2(targetIndicatorX, currentPos.y);
                difficultyIndicator.anchoredPosition = Vector2.Lerp(currentPos, targetPos, Time.unscaledDeltaTime * indicatorMoveSpeed);
            }
        }
    }

    private void HandleIndicatorDrag()
    {
        if (Pointer.current == null) return;

        Vector2 inputPos = Pointer.current.position.ReadValue();
        bool pressedThisFrame = Pointer.current.press.wasPressedThisFrame;
        bool isPressing = Pointer.current.press.isPressed;
        bool releasedThisFrame = Pointer.current.press.wasReleasedThisFrame;

        if (pressedThisFrame)
        {
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, difficultyIndicator.position);
            float dynamicHitRadius = Screen.height * 0.08f;

            if (Vector2.Distance(screenPoint, inputPos) < dynamicHitRadius)
            {
                isDraggingIndicator = true;
            }
        }

        if (isDraggingIndicator && isPressing)
        {
            RectTransform trackRect = difficultyIndicator.parent as RectTransform;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(trackRect, inputPos, null, out Vector2 localPoint))
            {
                float minX = Mathf.Min(relaxedX, hardX);
                float maxX = Mathf.Max(relaxedX, hardX);
                float clampedX = Mathf.Clamp(localPoint.x, minX, maxX);
                difficultyIndicator.anchoredPosition = new Vector2(clampedX, difficultyIndicator.anchoredPosition.y);
            }
        }

        if (isDraggingIndicator && releasedThisFrame)
        {
            isDraggingIndicator = false;
            float currentX = difficultyIndicator.anchoredPosition.x;
            float distToRelaxed = Mathf.Abs(currentX - relaxedX);
            float distToMedium = Mathf.Abs(currentX - mediumX);
            float distToHard = Mathf.Abs(currentX - hardX);

            if (distToRelaxed < distToMedium && distToRelaxed < distToHard) ChangeDifficulty(0);
            else if (distToHard < distToRelaxed && distToHard < distToMedium) ChangeDifficulty(2);
            else ChangeDifficulty(1);
        }
    }

    public void OnPauseButtonClicked() => TogglePause();

    public void TogglePause()
    {
        if (gameController != null && gameController.isGameOver) return;

        isPaused = !isPaused;
        StopAllCoroutines();

        if (isPaused)
        {
            Physics2D.simulationMode = SimulationMode2D.Script;
            if (gameController != null) gameController.SetPause(true);
            ToggleHUD(false);

            if (pausePanel != null)
            {
                UpdateDifficultyUI();
                UpdateJellyUI();
                UpdateInteractiveShakeUI();
                pausePanel.SetActive(true);
                StartCoroutine(AnimateFade(0f, 1f));
            }
        }
        else
        {
            Physics2D.simulationMode = SimulationMode2D.FixedUpdate;
            if (gameController != null) gameController.SetPause(false);
            ToggleHUD(true);

            if (pausePanel != null) StartCoroutine(AnimateFade(1f, 0f));
        }
    }

    public void ToggleJellyPhysics()
    {
        isJellyEnabled = !isJellyEnabled;
        PlayerPrefs.SetInt("Setting_JellyBody", isJellyEnabled ? 1 : 0);
        PlayerPrefs.Save();
        UpdateJellyUI();

        FellaLogic[] allFellas = FindObjectsByType<FellaLogic>(FindObjectsSortMode.None);
        foreach (FellaLogic fella in allFellas)
        {
            JellyBody jelly = fella.GetComponent<JellyBody>();
            if (!isJellyEnabled && jelly != null) Destroy(jelly);
            else if (isJellyEnabled && jelly == null) fella.gameObject.AddComponent<JellyBody>();
        }
    }

    private void UpdateJellyUI() { if (jellyTickImage != null) jellyTickImage.enabled = isJellyEnabled; }

    public void ToggleInteractiveShake()
    {
        isInteractiveShakeEnabled = !isInteractiveShakeEnabled;
        PlayerPrefs.SetInt("Setting_InteractiveShake", isInteractiveShakeEnabled ? 1 : 0);
        PlayerPrefs.Save();
        UpdateInteractiveShakeUI();
    }

    private void UpdateInteractiveShakeUI() { if (interactiveShakeTickImage != null) interactiveShakeTickImage.enabled = isInteractiveShakeEnabled; }

    public void OnRelaxedClicked() { ChangeDifficulty(0); }
    public void OnMediumClicked() { ChangeDifficulty(1); }
    public void OnHardClicked() { ChangeDifficulty(2); }

    private void ChangeDifficulty(int level)
    {
        if (gameController != null)
        {
            gameController.ApplyDifficultySettings(level);
            UpdateDifficultyUI();
        }
    }

    private void UpdateDifficultyUI()
    {
        int currentDiff = PlayerPrefs.GetInt("GameDifficulty", 1);
        if (currentDiff == 0) targetIndicatorX = relaxedX;
        else if (currentDiff == 1) targetIndicatorX = mediumX;
        else if (currentDiff == 2) targetIndicatorX = hardX;
    }

    public void OnResumeClicked()
    {
        TogglePause();
    }

    public void OnMainMenuClicked()
    {
        Physics2D.simulationMode = SimulationMode2D.FixedUpdate;
        Time.timeScale = 1f;

        if (AdsManager.Instance != null)
        {
            AdsManager.Instance.TryShowInterstitial(() =>
            {
                SceneManager.LoadScene("MainMenu");
            });
        }
        else
        {
            SceneManager.LoadScene("MainMenu");
        }
    }

    public void OnExitClicked()
    {
        Application.Quit();
    }

    void ToggleHUD(bool show)
    {
        if (hudElementsToHide != null)
        {
            foreach (GameObject go in hudElementsToHide) if (go != null) go.SetActive(show);
        }
    }

    IEnumerator AnimateFade(float startAlpha, float endAlpha)
    {
        if (uiCanvasGroup == null) yield break;

        float timer = 0f;
        uiCanvasGroup.alpha = startAlpha;

        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            uiCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, timer / fadeDuration);
            yield return null;
        }

        uiCanvasGroup.alpha = endAlpha;
        if (endAlpha == 0f) pausePanel.SetActive(false);
    }
}