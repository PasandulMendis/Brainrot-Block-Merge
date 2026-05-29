using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;

public class ShakeManager : MonoBehaviour
{
    [Header("Configuration")]
    public Transform visualContainer;

    [Header("Interactive Shake Settings")]
    public float shakeDuration = 4.0f;
    public float tiltGravity = 20f;
    public float suddenJoltStrength = 15f;
    public float visualShakeStrength = 0.15f;
    public float cooldownTime = 5f;

    [Header("UI References")]
    public Button uiButton;
    public Image buttonImage;
    public TextMeshProUGUI buttonText;
    public GameObject shakeModeOverlay;
    public GameObject shakeTutorialUI;

    [Header("Badge References")]
    public GameObject badgeObject;
    public TextMeshProUGUI badgeText;

    [Header("Ad Integration")]
    public GameObject adIconObject;
    public int adRewardAmount = 3;

    private bool isShaking = false;
    private float cooldownTimer = 0f;
    private Vector3 initialPos;

    private float lastAdClickTime = 0f;

    private const string SHAKE_STAT_KEY = "Consumable_Shake";
    private const string TUTORIAL_KEY = "HasSeenShakeTutorial";

    void Start()
    {
        if (visualContainer != null) initialPos = visualContainer.position;
        if (shakeModeOverlay != null) shakeModeOverlay.SetActive(false);
        if (shakeTutorialUI != null) shakeTutorialUI.SetActive(false);

        if (PlayerPrefs.GetInt("FirstTimeShakeGiven", 0) == 0)
        {
            StatTracker.Increment(SHAKE_STAT_KEY, 1);
            PlayerPrefs.SetInt("FirstTimeShakeGiven", 1);
            PlayerPrefs.Save();
        }
    }

    void Update()
    {
        int remainingShakes = StatTracker.GetStat(SHAKE_STAT_KEY);

        if (badgeObject != null)
        {
            badgeObject.SetActive(remainingShakes > 0);
            if (badgeText != null) badgeText.text = remainingShakes.ToString();
        }

        if (!isShaking && Physics2D.simulationMode == SimulationMode2D.FixedUpdate)
        {
            if (LevelManager.Instance != null)
            {
                Physics2D.gravity = LevelManager.Instance.GetCurrentGravity();
            }
        }

        if (cooldownTimer > 0)
        {
            if (Physics2D.simulationMode == SimulationMode2D.FixedUpdate)
            {
                cooldownTimer -= Time.deltaTime;
            }
            UpdateButtonVisuals(false, Color.gray, Mathf.Ceil(cooldownTimer).ToString(), false);
        }
        else if (remainingShakes <= 0)
        {
            UpdateButtonVisuals(true, Color.white, "SHAKE", true);
        }
        else
        {
            UpdateButtonVisuals(true, Color.white, "SHAKE", false);
        }
    }

    void UpdateButtonVisuals(bool interactable, Color color, string text, bool showAdIcon)
    {
        if (uiButton != null) uiButton.interactable = interactable;
        if (buttonImage != null) buttonImage.color = color;
        if (buttonText != null) buttonText.text = text;
        if (adIconObject != null) adIconObject.SetActive(showAdIcon);
    }

    public void ActivateShake()
    {
        if (isShaking || cooldownTimer > 0) return;

        int remainingShakes = StatTracker.GetStat(SHAKE_STAT_KEY);

        if (remainingShakes > 0)
        {
            StartCoroutine(ShakeRoutine());
        }
        else
        {
            if (Time.time - lastAdClickTime < 2f) return;
            lastAdClickTime = Time.time;

            if (AdsManager.Instance != null)
            {
                AdsManager.Instance.ShowRewardedAd(OnAdCompleted);
            }
        }
    }

    void OnAdCompleted()
    {
        StatTracker.Increment(SHAKE_STAT_KEY, adRewardAmount);
        cooldownTimer = 0;
    }

    IEnumerator ShakeRoutine()
    {
        isShaking = true;
        cooldownTimer = cooldownTime;

        StatTracker.Increment(SHAKE_STAT_KEY, -1);
        StatTracker.Increment("Shake");

        if (StatTracker.GetStat("Shake") == 10 && PlayGamesManager.Instance != null)
        {
            PlayGamesManager.Instance.UnlockAchievement(GPGSIds.achievement_earthquake);
        }

        if (shakeModeOverlay != null) shakeModeOverlay.SetActive(true);

        bool isFirstTime = PlayerPrefs.GetInt(TUTORIAL_KEY, 0) == 0;
        if (isFirstTime && shakeTutorialUI != null) shakeTutorialUI.SetActive(true);

        bool is3DShakeEnabled = PlayerPrefs.GetInt("Setting_InteractiveShake", 1) == 1;
        float randomGravityTimer = 0f;

        Rigidbody2D[] fellaBodies = GetDynamicFellas();
        foreach (Rigidbody2D rb in fellaBodies)
        {
            rb.WakeUp();
            rb.AddTorque(Random.Range(-50, 50)); 
        }

        float elapsed = 0f;
        Vector3 lastAcceleration = Input.acceleration;

        while (elapsed < shakeDuration)
        {
            if (Physics2D.simulationMode != SimulationMode2D.FixedUpdate)
            {
                yield return null;
                continue;
            }

            elapsed += Time.deltaTime;

            if (isFirstTime && shakeTutorialUI != null)
            {
                if (elapsed < 1.5f)
                {
                    float angle = Mathf.Sin(elapsed * 25f) * 20f;
                    shakeTutorialUI.transform.localRotation = Quaternion.Euler(0, 0, angle);
                }
                else
                {
                    shakeTutorialUI.SetActive(false);
                    isFirstTime = false;
                    PlayerPrefs.SetInt(TUTORIAL_KEY, 1);
                    PlayerPrefs.Save();
                }
            }

            if (visualContainer != null)
            {
                float x = Random.Range(-1f, 1f) * visualShakeStrength;
                float y = Random.Range(-1f, 1f) * visualShakeStrength;
                visualContainer.position = initialPos + new Vector3(x, y, 0);
            }

            if (is3DShakeEnabled)
            {
#if UNITY_EDITOR
                Physics2D.gravity = new Vector2(Random.Range(-1f, 1f), Random.Range(-0.5f, 1.5f)).normalized * tiltGravity;
#else
                Vector3 currentAcc = Input.acceleration;
                Vector2 tiltDir = new Vector2(currentAcc.x, currentAcc.y);
                if (tiltDir.sqrMagnitude > 0.05f)
                {
                    Physics2D.gravity = tiltDir.normalized * tiltGravity;
                }

                Vector3 accelerationDelta = currentAcc - lastAcceleration;
                if (accelerationDelta.sqrMagnitude > 0.25f)
                {
                    Vector2 joltForce = new Vector2(accelerationDelta.x, accelerationDelta.y) * suddenJoltStrength;
                    foreach (Rigidbody2D rb in fellaBodies)
                    {
                        if (rb != null) rb.AddForce(joltForce, ForceMode2D.Impulse);
                    }
                }
                lastAcceleration = currentAcc;
#endif
            }
            else
            {
                randomGravityTimer -= Time.deltaTime;
                if (randomGravityTimer <= 0f)
                {
                    Physics2D.gravity = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized * tiltGravity;
                    randomGravityTimer = 0.3f;

                    foreach (Rigidbody2D rb in fellaBodies)
                    {
                        if (rb != null)
                        {
                            rb.AddForce(new Vector2(Random.Range(-5f, 5f), Random.Range(-5f, 5f)), ForceMode2D.Impulse);
                            rb.AddTorque(Random.Range(-30, 30));
                        }
                    }
                }
            }

            yield return null;
        }

        if (visualContainer != null) visualContainer.position = initialPos;
        if (shakeModeOverlay != null) shakeModeOverlay.SetActive(false);
        
        isShaking = false;

        if (isFirstTime)
        {
            PlayerPrefs.SetInt(TUTORIAL_KEY, 1);
            PlayerPrefs.Save();
            if (shakeTutorialUI != null) shakeTutorialUI.SetActive(false);
        }
    }

    private Rigidbody2D[] GetDynamicFellas()
    {
        GameObject[] allBlocks = GameObject.FindGameObjectsWithTag("Player");
        System.Collections.Generic.List<Rigidbody2D> activeBodies = new System.Collections.Generic.List<Rigidbody2D>();

        foreach (GameObject block in allBlocks)
        {
            Rigidbody2D rb = block.GetComponent<Rigidbody2D>();
            if (rb != null && rb.bodyType != RigidbodyType2D.Kinematic)
            {
                activeBodies.Add(rb);
            }
        }
        return activeBodies.ToArray();
    }

    void OnDisable()
    {
        isShaking = false;
        if (LevelManager.Instance != null) Physics2D.gravity = LevelManager.Instance.GetCurrentGravity();
    }

    void OnDestroy()
    {
        if (LevelManager.Instance != null) Physics2D.gravity = LevelManager.Instance.GetCurrentGravity();
    }

    void OnApplicationPause(bool isPaused)
    {
        if (isPaused && LevelManager.Instance != null) Physics2D.gravity = LevelManager.Instance.GetCurrentGravity();
    }
}