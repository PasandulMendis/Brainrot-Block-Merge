using UnityEngine;

public static class CollectionManager
{
    private static string currentPackID = "classic";

    public static void SetCurrentPack(string packID)
    {
        currentPackID = packID;
    }

    public static void UnlockFella(int tierIndex)
    {
        string key = $"Unlock_{currentPackID}_{tierIndex}";
        if (PlayerPrefs.GetInt(key, 0) == 0)
        {
            PlayerPrefs.SetInt(key, 1);
            PlayerPrefs.Save();
            Debug.Log($"Unlocked Tier {tierIndex} for pack '{currentPackID}'");
        }
    }

    public static bool IsUnlocked(int tierIndex)
    {
        return IsContentUnlocked(currentPackID, tierIndex);
    }

    public static bool IsContentUnlocked(string packID, int tierIndex)
    {
        if (tierIndex <= 1) return true;

        string key = $"Unlock_{packID}_{tierIndex}";
        return PlayerPrefs.GetInt(key, 0) == 1;
    }
}