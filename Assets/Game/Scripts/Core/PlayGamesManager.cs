using UnityEngine;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using GooglePlayGames.BasicApi.SavedGame;
using System.Text;
using System;
using System.Collections.Generic;

public class PlayGamesManager : MonoBehaviour
{
    public static PlayGamesManager Instance { get; private set; }

    private const string SAVE_FILENAME = "MergeFellahsSave";
    private Action onSyncCompleteCallback;

    [System.Serializable]
    public class CloudSaveData
    {
        public int currentLevel;
        public int bestScore;
        public int coinBalance;
        public int isVIP;
        
        public int anvilCount;
        public int shakeCount;

        public string equippedVoice;
        public string equippedBG;
        public string equippedBucket;
        public string equippedLaser;
        public string equippedPack;

        public string unlockedShopItemsMasterString;
        public string unlockedPacksMasterString;
        public string unlockedGalleryMasterString; 
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            PlayGamesPlatform.DebugLogEnabled = true;
            PlayGamesPlatform.Activate();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SignInAndSync(Action onComplete)
    {
        onSyncCompleteCallback = onComplete;
        SignInToGooglePlay();
    }

    private void SignInToGooglePlay()
    {
        PlayGamesPlatform.Instance.Authenticate((SignInStatus status) =>
        {
            if (status == SignInStatus.Success)
            {
                Debug.Log("Successfully signed in.");

                if (PlayerPrefs.GetInt("Cloud_Downloaded_Once", 0) == 0)
                {
                    Debug.Log("Fresh Install Detected! Pulling from Cloud...");
                    LoadFromCloud(); 
                }
                else
                {
                    Debug.Log("Already synced to this device. Skipping download.");
                    onSyncCompleteCallback?.Invoke();
                }
            }
            else
            {
                Debug.LogWarning("Failed to sign in. Skipping sync.");
                onSyncCompleteCallback?.Invoke();
            }
        });
    }

    public void UnlockAchievement(string achievementId)
    {
        if (PlayGamesPlatform.Instance.IsAuthenticated())
        {
            PlayGamesPlatform.Instance.ReportProgress(achievementId, 100.0f, (bool success) => { });
        }
    }

    public void SaveToCloud()
    {
        if (PlayGamesPlatform.Instance.IsAuthenticated())
        {
            OpenSavedGame(SAVE_FILENAME, true, (status, metadata) =>
            {
                if (status == SavedGameRequestStatus.Success)
                {
                    SaveGame(metadata);
                }
            });
        }
    }

    private void LoadFromCloud()
    {
        if (PlayGamesPlatform.Instance.IsAuthenticated())
        {
            OpenSavedGame(SAVE_FILENAME, false, (status, metadata) =>
            {
                if (status == SavedGameRequestStatus.Success)
                {
                    LoadGame(metadata);
                }
                else
                {
                    onSyncCompleteCallback?.Invoke();
                }
            });
        }
        else
        {
            onSyncCompleteCallback?.Invoke();
        }
    }

    private void OpenSavedGame(string filename, bool isSaving, Action<SavedGameRequestStatus, ISavedGameMetadata> callback)
    {
        ISavedGameClient savedGameClient = PlayGamesPlatform.Instance.SavedGame;
        DataSource ds = isSaving ? DataSource.ReadNetworkOnly : DataSource.ReadCacheOrNetwork;
        
        savedGameClient.OpenWithAutomaticConflictResolution(
            filename,
            ds,
            ConflictResolutionStrategy.UseLongestPlaytime,
            callback);
    }

    private void SaveGame(ISavedGameMetadata gameMetadata)
    {
        CloudSaveData data = new CloudSaveData();

        data.currentLevel = PlayerPrefs.GetInt("Player_Infinite_Level", 1);
        data.bestScore = PlayerPrefs.GetInt("Brainrot_Best", 0);
        data.coinBalance = PlayerPrefs.GetInt("MemeCoins_Balance", 0);
        data.isVIP = PlayerPrefs.GetInt("IsVIP_Unlocked", 0);

        data.anvilCount = PlayerPrefs.GetInt("Stats_Consumable_Anvil", 0);
        data.shakeCount = PlayerPrefs.GetInt("Stats_Consumable_Shake", 0);

        data.equippedVoice = PlayerPrefs.GetString("Shop_Equipped_voice", "voice_normal");
        data.equippedBG = PlayerPrefs.GetString("Shop_Equipped_bg", "");
        data.equippedBucket = PlayerPrefs.GetString("Shop_Equipped_bucket", "");
        data.equippedLaser = PlayerPrefs.GetString("Shop_Equipped_laser", "laser_default");
        data.equippedPack = PlayerPrefs.GetString("SelectedPackID", "classic");

        data.unlockedShopItemsMasterString = PlayerPrefs.GetString("Master_Shop_Unlocks", "");
        data.unlockedPacksMasterString = PlayerPrefs.GetString("Master_Pack_Unlocks", "");

        string masterGallery = "";
        string[] knownPacks = { "classic", "pack1", "pack2", "pack3", "pack4", "pack5", "pack6", "pack7", "pack8", "pack9", "pack10" };
        foreach(string p in knownPacks)
        {
            for(int i = 0; i < 15; i++)
            {
                if (PlayerPrefs.GetInt($"Unlock_{p}_{i}", 0) == 1) masterGallery += $"{p}_{i},";
            }
        }
        data.unlockedGalleryMasterString = masterGallery;

        string json = JsonUtility.ToJson(data);
        byte[] rawData = Encoding.UTF8.GetBytes(json);

        SavedGameMetadataUpdate.Builder builder = new SavedGameMetadataUpdate.Builder();
        builder.WithUpdatedDescription($"Lvl {data.currentLevel} | Score {data.bestScore}");
        SavedGameMetadataUpdate updatedMetadata = builder.Build();

        ISavedGameClient savedGameClient = PlayGamesPlatform.Instance.SavedGame;
        savedGameClient.CommitUpdate(gameMetadata, updatedMetadata, rawData, (status, metadata) =>
        {
            Debug.Log("Cloud Save Upload Status: " + status);
        });
    }

    private void LoadGame(ISavedGameMetadata gameMetadata)
    {
        ISavedGameClient savedGameClient = PlayGamesPlatform.Instance.SavedGame;
        savedGameClient.ReadBinaryData(gameMetadata, (status, data) =>
        {
            if (status == SavedGameRequestStatus.Success && data != null && data.Length > 0)
            {
                string json = Encoding.UTF8.GetString(data);
                CloudSaveData loadedData = JsonUtility.FromJson<CloudSaveData>(json);
                
                PlayerPrefs.SetInt("Player_Infinite_Level", loadedData.currentLevel);
                PlayerPrefs.SetInt("Brainrot_Best", loadedData.bestScore);
                PlayerPrefs.SetInt("MemeCoins_Balance", loadedData.coinBalance);
                PlayerPrefs.SetInt("IsVIP_Unlocked", loadedData.isVIP);

                PlayerPrefs.SetInt("Stats_Consumable_Anvil", loadedData.anvilCount);
                PlayerPrefs.SetInt("Stats_Consumable_Shake", loadedData.shakeCount);

                PlayerPrefs.SetString("Shop_Equipped_voice", loadedData.equippedVoice);
                PlayerPrefs.SetString("Shop_Equipped_bg", loadedData.equippedBG);
                PlayerPrefs.SetString("Shop_Equipped_bucket", loadedData.equippedBucket);
                PlayerPrefs.SetString("Shop_Equipped_laser", loadedData.equippedLaser);
                PlayerPrefs.SetString("SelectedPackID", loadedData.equippedPack);

                if (!string.IsNullOrEmpty(loadedData.unlockedShopItemsMasterString)) {
                    string[] shopIds = loadedData.unlockedShopItemsMasterString.Split(new char[]{','}, StringSplitOptions.RemoveEmptyEntries);
                    foreach(string id in shopIds) PlayerPrefs.SetInt("Shop_Unlocked_" + id, 1);
                }

                if (!string.IsNullOrEmpty(loadedData.unlockedPacksMasterString)) {
                    string[] packIds = loadedData.unlockedPacksMasterString.Split(new char[]{','}, StringSplitOptions.RemoveEmptyEntries);
                    foreach(string id in packIds) PlayerPrefs.SetInt("PackUnlocked_" + id, 1);
                }

                if (!string.IsNullOrEmpty(loadedData.unlockedGalleryMasterString)) {
                    string[] galIds = loadedData.unlockedGalleryMasterString.Split(new char[]{','}, StringSplitOptions.RemoveEmptyEntries);
                    foreach(string id in galIds) PlayerPrefs.SetInt("Unlock_" + id, 1);
                }

                PlayerPrefs.SetInt("Cloud_Downloaded_Once", 1);
                PlayerPrefs.Save();
                
                Debug.Log("Fresh Install Cloud Save Pulled and Applied Perfectly!");
            }
            
            onSyncCompleteCallback?.Invoke();
        });
    }
}