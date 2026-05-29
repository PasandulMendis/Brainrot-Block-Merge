using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class MergeGameController : MonoBehaviour
{
    [Header("Modules")]
    public InputHandler inputHandler;
    public Spawner spawner;
    public ScoreManager scoreManager;
    public HapticManager hapticManager;
    public AudioManager audioManager;
    public EffectsManager effectsManager;
    public GameOverManager gameOverManager;
    public EvolutionGallery evolutionGallery;
    public CurrencyManager currencyManager;
    public PackManager packManager;

    [Header("Game State")]
    public float dropCooldown = 0.5f;
    public float mergeGapDistance = 0.15f; 

    public bool isGameOver = false;
    public bool canDrop = true;

    private bool isGamePaused = false;
    private bool waitForRelease = false;

    void Start()
    {
        bool isBatterySaverOn = PlayerPrefs.GetInt("Setting_Battery", 0) == 1;
        Application.targetFrameRate = isBatterySaverOn ? 60 : 120;
        QualitySettings.vSyncCount = 0;

        if (inputHandler == null) inputHandler = GetComponent<InputHandler>();
        if (spawner == null) spawner = GetComponent<Spawner>();
        if (scoreManager == null) scoreManager = GetComponent<ScoreManager>();
        if (hapticManager == null) hapticManager = GetComponent<HapticManager>();
        if (audioManager == null) audioManager = GetComponent<AudioManager>();
        if (effectsManager == null) effectsManager = GetComponent<EffectsManager>();
        if (gameOverManager == null) gameOverManager = GetComponent<GameOverManager>();
        if (evolutionGallery == null) evolutionGallery = FindFirstObjectByType<EvolutionGallery>();
        if (currencyManager == null) currencyManager = GetComponent<CurrencyManager>();
        if (packManager == null) packManager = FindFirstObjectByType<PackManager>();

        StatTracker.Increment("GamesPlayed");

        if (packManager != null) packManager.ApplyActivePackToGame(spawner, audioManager);

        int savedDifficulty = PlayerPrefs.GetInt("GameDifficulty", 1);
        ApplyDifficultySettings(savedDifficulty);

        scoreManager.ResetScore();
        spawner.Init();
        if (evolutionGallery != null) evolutionGallery.InitializeGallery();

        int currentLevel = PlayerPrefs.GetInt("Player_Infinite_Level", 1);
        OnLevelStarted(currentLevel);
    }

    public void OnLevelStarted(int newLevel)
    {
        if (newLevel % 10 == 0)
        {
            ToastManager.Instance?.ShowToast("BOSS LEVEL!", 3f);
            SpawnBossBlock();
        }
    }

    private void SpawnBossBlock()
    {
        if (spawner != null && spawner.spawnPoint != null && spawner.fellaPrefabs.Length >= 8)
        {
            int randomTier = Random.Range(8, 10); 
            int bossTier = Mathf.Min(randomTier, spawner.fellaPrefabs.Length - 1);
            
            GameObject bossPrefab = spawner.fellaPrefabs[bossTier];

            Vector3 dropPos = new Vector3(0, spawner.spawnPoint.position.y - 2f, 0);
            GameObject boss = Instantiate(bossPrefab, dropPos, Quaternion.identity);
            
            Rigidbody2D rb = boss.GetComponent<Rigidbody2D>();
            if (rb != null) rb.mass = 50f; 

            FellaLogic logic = boss.GetComponent<FellaLogic>();
            if (logic == null) logic = boss.AddComponent<FellaLogic>();
            logic.Setup(bossTier, this);
            
            spawner.ApplySkinToFella(boss, bossTier);
        }
    }

    public void ApplyDifficultySettings(int difficultyLevel)
    {
        switch (difficultyLevel)
        {
            case 0: mergeGapDistance = 0.3f; break;
            case 1: mergeGapDistance = 0.15f; break;
            case 2: mergeGapDistance = 0f; break;
            default: mergeGapDistance = 0.15f; break;
        }
        PlayerPrefs.SetInt("GameDifficulty", difficultyLevel);
        PlayerPrefs.Save();
    }

    public void SetPause(bool paused)
    {
        isGamePaused = paused;
        if (!isGamePaused) waitForRelease = true;
    }

    void Update()
    {
        if (isGameOver || isGamePaused) return;

        if (waitForRelease)
        {
            if (!inputHandler.IsTouching()) waitForRelease = false;
            return;
        }

        if (!canDrop) return;

        if (inputHandler.IsTouching())
        {
            Vector3 targetPos = inputHandler.GetClampedPosition(spawner.spawnPoint.position.y);
            spawner.MoveSpawnPoint(targetPos);
        }

        if (inputHandler.WasReleased())
        {
            spawner.DropCurrent(this);
            if (audioManager != null) audioManager.PlayDrop();
            canDrop = false;
            StartCoroutine(CooldownRoutine());
        }
    }

    IEnumerator CooldownRoutine()
    {
        yield return new WaitForSeconds(dropCooldown);
        if (!isGameOver)
        {
            canDrop = true;
            spawner.SpawnPreview();
        }
    }

    public void Merge(FellaLogic fellaA, FellaLogic fellaB)
    {
        if (isGameOver) return;

        hapticManager.TriggerMergeHaptic();

        int index = fellaA.fellaIndex;
        int nextTierIndex = index + 1;

        StatTracker.Increment("Merge");
        StatTracker.Increment("Merge_Tier_" + nextTierIndex);

        CollectionManager.UnlockFella(nextTierIndex);
        if (evolutionGallery != null) evolutionGallery.UnlockTierVisual(nextTierIndex);

        if (audioManager != null)
        {
            audioManager.PlayGenericPop();
            audioManager.PlayMergeGrouped(nextTierIndex);
        }

        int scoreGained = (index + 1) * 2;
        scoreManager.AddScore(scoreGained);
        if (LevelManager.Instance != null) LevelManager.Instance.AddScoreProgress(scoreGained);

        if (LevelManager.Instance != null) LevelManager.Instance.OnMergeCreated(nextTierIndex);

        if (nextTierIndex == 10 && PlayGamesManager.Instance != null)
        {
            PlayGamesManager.Instance.UnlockAchievement(GPGSIds.achievement_the_final_form);
        }

        if (currencyManager != null)
        {
            int coinReward = currencyManager.GetRewardForTier(nextTierIndex);
            if (currencyManager != null && coinReward > 0) currencyManager.AddCoins(coinReward);
        }

        Vector3 spawnPos = (fellaA.transform.position + fellaB.transform.position) / 2;
        if (effectsManager != null) effectsManager.PlayMergeEffect(spawnPos);

        GameObject nextPrefab = spawner.GetNextTierPrefab(index);

        Destroy(fellaA.gameObject);
        Destroy(fellaB.gameObject);

        if (nextPrefab != null)
        {
            GameObject newFella = Instantiate(nextPrefab, spawnPos, Quaternion.identity);
            newFella.AddComponent<PopEffect>();

            spawner.ApplySkinToFella(newFella, nextTierIndex);

            FellaLogic newLogic = newFella.GetComponent<FellaLogic>();
            if (newLogic == null) newLogic = newFella.AddComponent<FellaLogic>();
            newLogic.Setup(nextTierIndex, this);

            Rigidbody2D rb = newFella.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.AddForce(Vector2.up * 2f, ForceMode2D.Impulse);
                float randomSpin = Random.Range(-50f, 50f);
                rb.AddTorque(randomSpin);
            }
        }
    }

    public void TriggerGameOver()
    {
        if (isGameOver) return;

        hapticManager.TriggerGameOverHaptic();
        if (audioManager != null) audioManager.PlayGameOver();

        isGameOver = true;
        if (gameOverManager != null) gameOverManager.ShowGameOver();
    }

    public void RestartGame()
    {
        Physics2D.simulationMode = SimulationMode2D.FixedUpdate;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}