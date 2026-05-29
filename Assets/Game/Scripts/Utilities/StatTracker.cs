using UnityEngine;
using System;

public static class StatTracker
{
    public static void Increment(string statName, int amount = 1)
    {
        int current = PlayerPrefs.GetInt("Stats_" + statName, 0);
        PlayerPrefs.SetInt("Stats_" + statName, current + amount);
        PlayerPrefs.Save();
    }

    public static int GetStat(string statName)
    {
        return PlayerPrefs.GetInt("Stats_" + statName, 0);
    }

    public static void ResetStat(string statName)
    {
        PlayerPrefs.DeleteKey("Stats_" + statName);
    }

    public static void ResetDailyStats()
    {
        PlayerPrefs.DeleteKey("Stats_Merge");
        PlayerPrefs.DeleteKey("Stats_Shake");
        PlayerPrefs.DeleteKey("Stats_Login");
    }
}