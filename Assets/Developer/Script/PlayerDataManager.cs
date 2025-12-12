using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using PlinkoPrototype;
using System.Globalization;

#region DATA MODELS

[Serializable]
public class PlayerSessionData
{
    public string date;
    public int levelId;
    public int ballUsed;
    public int moneyEarned;
}

[Serializable]
public class PlayerData
{
    public int savedLevel = 1;
    public int savedRoundScore = 0;

    // 🔥 0 OLMAMALI
    public int savedTotalBallsRemaining = 200;

    public int savedBallsScoredThisLevel = 0;
    public int totalMoney = 0;

    // 🔥 Top bazlı history
    public List<RewardPackage> sessionRewards = new List<RewardPackage>();
    public List<PlayerSessionData> sessionHistory = new List<PlayerSessionData>();

    public string lastResetUtc;
}

#endregion

public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance { get; private set; }

    public PlayerData Data { get; private set; }

    private string filePath;

    private const int DEFAULT_BALL_COUNT = 200;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        filePath = Path.Combine(Application.persistentDataPath, "player_data.json");
        Debug.Log("savepath: " + filePath);

        LoadOrCreate();
    }

    private void LoadOrCreate()
    {
        if (!File.Exists(filePath))
        {
            CreateFreshData();
            return;
        }

        string json = File.ReadAllText(filePath);
        Data = JsonUtility.FromJson<PlayerData>(json);

        // lastReset yoksa → temiz başla
        if (string.IsNullOrEmpty(Data.lastResetUtc))
        {
            PerformHardReset();
            return;
        }

        // ISO 8601 UTC bekliyoruz
        if (!DateTime.TryParseExact(
            Data.lastResetUtc,
            "o",
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out DateTime lastReset))
        {
            Debug.LogWarning("[PlayerData] Invalid date format. Forcing reset.");
            PerformHardReset();
            return;
        }

        // ⚠️ Kritik satır
        TimeSpan diff = DateTime.UtcNow - lastReset.ToUniversalTime();
        double minutesPassed = diff.TotalMinutes;

        Debug.Log($"[PlayerData] Minutes passed since reset: {minutesPassed}");

        if (minutesPassed >= 15)
        {
            PerformHardReset();
        }
    }

    private void PerformHardReset()
    {
        Data.savedLevel = 1;
        Data.savedRoundScore = 0;
        Data.savedBallsScoredThisLevel = 0;

        Data.savedTotalBallsRemaining = DEFAULT_BALL_COUNT;

        Data.sessionRewards.Clear();
        Data.sessionHistory.Clear();
        Data.lastResetUtc = DateTime.UtcNow.ToString("o");

        Save();

        GameEvents.TriggerBallCountRestore(DEFAULT_BALL_COUNT);
        GameEvents.TriggerGameReset();

        Debug.Log("[PlayerData] Hard reset completed. Balls = 200");
    }




    private void CreateFreshData()
    {
        Data = new PlayerData
        {
            savedLevel = 1,
            savedRoundScore = 0,
            savedTotalBallsRemaining = DEFAULT_BALL_COUNT,
            savedBallsScoredThisLevel = 0,
            totalMoney = 0,
            lastResetUtc = System.DateTime.UtcNow.ToString("o")
        };

        Save();

        Debug.Log("[PlayerData] Fresh data created with 200 balls.");
    }

    public void Save()
    {
        string json = JsonUtility.ToJson(Data, true);
        File.WriteAllText(filePath, json);
    }

    public void ForceHardResetFromTimer()
    {
        Debug.Log("[PlayerData] Timer reached zero → forcing hard reset");
        PerformHardReset();
    }

}
