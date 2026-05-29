using UnityEngine;
using System.Collections.Generic;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    [Header("Dependencies")]
    public CurrencyManager currencyManager;
    public LevelUpUI levelUpUI;

    public enum GoalType { Score, Fusion, Collection }

    [Header("Current State (Read Only)")]
    public int currentLevel;
    public GoalType currentGoalType;
    
    public int targetScore;
    public int currentScoreProgress;
    
    public int targetFusionTier;
    
    public int targetCollectionTierA;
    public int targetCollectionAmountA;
    public int currentCollectionAmountA;

    [Header("Spawn Dilution Settings")]
    public int dilutionLevel1 = 40;
    public int dilutionLevel2 = 200;

    [Header("Bad Spawn Settings")]
    public int badSpawnStartLevel = 5;
    public float badSpawnChance = 0.05f;

    [Header("Gravity Escalation (Difficulty)")]
    [Tooltip("The normal starting gravity for the game")]
    public float baseGravity = -11.5f;

    [Space(10)]
    public int gravityLevel1 = 30;
    public float gravityForce1 = -14.0f;

    [Space(10)]
    public int gravityLevel2 = 50;
    public float gravityForce2 = -17.0f;

    [Space(10)]
    public int gravityLevel3 = 80;
    public float gravityForce3 = -22.0f;

    private const string LEVEL_KEY = "Player_Infinite_Level";

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        if (currencyManager == null) currencyManager = FindFirstObjectByType<CurrencyManager>();
        if (levelUpUI == null) levelUpUI = FindFirstObjectByType<LevelUpUI>();

        LoadLevelState();
        UpdateUI(false);
    }

    public Vector2 GetCurrentGravity()
    {
        if (currentLevel >= gravityLevel3) return new Vector2(0, gravityForce3);
        if (currentLevel >= gravityLevel2) return new Vector2(0, gravityForce2);
        if (currentLevel >= gravityLevel1) return new Vector2(0, gravityForce1);

        return new Vector2(0, baseGravity);
    }

    private void SaveLevelState()
    {
        PlayerPrefs.SetInt(LEVEL_KEY, currentLevel);
        PlayerPrefs.SetInt("LM_GoalType", (int)currentGoalType);
        PlayerPrefs.SetInt("LM_TargetScore", targetScore);
        PlayerPrefs.SetInt("LM_CurrentScore", currentScoreProgress);
        PlayerPrefs.SetInt("LM_TargetFusion", targetFusionTier);
        PlayerPrefs.SetInt("LM_TargetColTier", targetCollectionTierA);
        PlayerPrefs.SetInt("LM_TargetColAmount", targetCollectionAmountA);
        PlayerPrefs.SetInt("LM_CurrentColAmount", currentCollectionAmountA);
        PlayerPrefs.SetInt("LM_HasSavedState", 1);
        PlayerPrefs.Save();
    }

    private void LoadLevelState()
    {
        currentLevel = PlayerPrefs.GetInt(LEVEL_KEY, 1);
        
        if (PlayerPrefs.GetInt("LM_HasSavedState", 0) == 1)
        {
            currentGoalType = (GoalType)PlayerPrefs.GetInt("LM_GoalType", 0);
            targetScore = PlayerPrefs.GetInt("LM_TargetScore", 300);
            currentScoreProgress = PlayerPrefs.GetInt("LM_CurrentScore", 0);
            targetFusionTier = PlayerPrefs.GetInt("LM_TargetFusion", 4);
            targetCollectionTierA = PlayerPrefs.GetInt("LM_TargetColTier", 3);
            targetCollectionAmountA = PlayerPrefs.GetInt("LM_TargetColAmount", 2);
            currentCollectionAmountA = PlayerPrefs.GetInt("LM_CurrentColAmount", 0);
        }
        else
        {
            GenerateGoalForLevel();
        }

        Physics2D.gravity = GetCurrentGravity();
    }

    public void AddScoreProgress(int amount)
    {
        if (currentGoalType == GoalType.Score)
        {
            currentScoreProgress += amount;
            UpdateUI(false);
            SaveLevelState();

            if (currentScoreProgress >= targetScore)
            {
                TriggerLevelUp();
            }
        }
    }

    public void OnMergeCreated(int createdTierIndex)
    {
        if (currentGoalType == GoalType.Fusion)
        {
            if (createdTierIndex == targetFusionTier) TriggerLevelUp();
        }
        else if (currentGoalType == GoalType.Collection)
        {
            if (createdTierIndex == targetCollectionTierA)
            {
                currentCollectionAmountA++;
                UpdateUI(false);
                SaveLevelState();

                if (currentCollectionAmountA >= targetCollectionAmountA) TriggerLevelUp();
            }
        }
    }

    private void GenerateGoalForLevel()
    {
        currentScoreProgress = 0;
        currentCollectionAmountA = 0;

        float rand = Random.value;

        if (currentLevel <= 2 || rand < 0.70f)
        {
            currentGoalType = GoalType.Score;
            
            if (currentLevel <= 10) targetScore = 300 + ((currentLevel - 1) * 100); 
            else targetScore = 1200 + ((currentLevel - 10) * 50); 
        }
        else if (rand < 0.85f)
        {
            currentGoalType = GoalType.Fusion;
            targetFusionTier = Mathf.Clamp(4 + (currentLevel / 10), 4, 10);
        }
        else
        {
            currentGoalType = GoalType.Collection;
            targetCollectionTierA = Mathf.Clamp(3 + (currentLevel / 15), 3, 8);
            targetCollectionAmountA = Random.Range(2, 5); 
        }

        SaveLevelState();
    }

    private void TriggerLevelUp()
    {
        if (PlayGamesManager.Instance != null)
        {
            if (currentLevel == 10) PlayGamesManager.Instance.UnlockAchievement(GPGSIds.achievement_getting_started);
            else if (currentLevel == 25) PlayGamesManager.Instance.UnlockAchievement(GPGSIds.achievement_gaining_momentum);
            else if (currentLevel == 50) PlayGamesManager.Instance.UnlockAchievement(GPGSIds.achievement_merge_master);
            else if (currentLevel == 100) PlayGamesManager.Instance.UnlockAchievement(GPGSIds.achievement_century_club);
        }
        
        int coinReward = 50 * ((currentLevel / 50) + 1);
        if (currencyManager != null) currencyManager.AddCoins(coinReward);

        if (levelUpUI != null) levelUpUI.ShowLevelUp(currentLevel, currentLevel + 1, coinReward);

        int leftoverScore = 0;
        if (currentGoalType == GoalType.Score && currentScoreProgress > targetScore)
        {
            leftoverScore = currentScoreProgress - targetScore;
        }

        currentLevel++;
        PlayerPrefs.SetInt(LEVEL_KEY, currentLevel);
        
        GenerateGoalForLevel(); 

        if (currentGoalType == GoalType.Score && leftoverScore > 0)
        {
            currentScoreProgress = leftoverScore;
        }

        SaveLevelState();
        UpdateUI(true);

        Physics2D.gravity = GetCurrentGravity();

        MergeGameController gc = FindFirstObjectByType<MergeGameController>();
        if (gc != null) gc.OnLevelStarted(currentLevel);

        if (PlayGamesManager.Instance != null)
        {
            PlayGamesManager.Instance.SaveToCloud();
        }
    }

    private void UpdateUI(bool isNewLevel)
    {
        if (levelUpUI == null) return;

        string desc = "";
        float progress = 0f;

        switch (currentGoalType)
        {
            case GoalType.Score:
                desc = $"Score {targetScore} Points";
                progress = (float)currentScoreProgress / targetScore;
                break;
            case GoalType.Fusion:
                desc = $"Create a Tier {targetFusionTier}";
                progress = 0f; 
                break;
            case GoalType.Collection:
                desc = $"Merge {targetCollectionAmountA} Tier {targetCollectionTierA}s";
                progress = (float)currentCollectionAmountA / targetCollectionAmountA;
                break;
        }

        levelUpUI.UpdateLiveProgress(desc, progress, currentLevel);
    }
}