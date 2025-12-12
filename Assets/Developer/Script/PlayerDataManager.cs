using System.IO;
using UnityEngine;
using System.Collections.Generic;
using PlinkoPrototype;

[System.Serializable]
public class PlayerSessionData
{
    public string date;
    public int levelId;
    public int ballUsed;
    public int moneyEarned;
}

[System.Serializable]
public class PlayerData
{
    public int savedLevel;
    public int savedRoundScore;
    public int savedTotalBallsRemaining;
    public int savedBallsScoredThisLevel;
    public int totalMoney = 0;
    public List<PlayerSessionData> sessionHistory = new List<PlayerSessionData>();
    public string lastResetUtc;
}

public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance { get; private set; }

    private string filePath;
    public PlayerData Data { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        filePath = Path.Combine(Application.persistentDataPath, "player_data.json");
        LoadPlayerData();
        CheckForAutoReset();
    }

    private void LoadPlayerData()
    {
        if (!File.Exists(filePath))
        {
            // İlk defa açılıyorsa default oluştur
            Data = new PlayerData();
            SavePlayerData();
            return;
        }

        string json = File.ReadAllText(filePath);
        Data = JsonUtility.FromJson<PlayerData>(json);
    }

    public void SavePlayerData()
    {
        string json = JsonUtility.ToJson(Data, true);
        File.WriteAllText(filePath, json);
    }

    public void AddMoney(int amount)
    {
        Data.totalMoney += amount;
        SavePlayerData();
    }

    public void AddSession(PlayerSessionData session)
    {
        Data.sessionHistory.Add(session);
        SavePlayerData();
    }

    private const int RESET_INTERVAL_MINUTES = 15;

    private void CheckForAutoReset()
    {
        if (string.IsNullOrEmpty(Data.lastResetUtc))
        {
            Data.lastResetUtc = System.DateTime.UtcNow.ToString("o");
            SavePlayerData();
            return;
        }

        System.DateTime lastReset = System.DateTime.Parse(Data.lastResetUtc);
        System.DateTime now = System.DateTime.UtcNow;

        if ((now - lastReset).TotalMinutes >= RESET_INTERVAL_MINUTES)
        {
            Data.lastResetUtc = now.ToString("o");
            SavePlayerData();
            GameEvents.TriggerGameReset();
        }
    }
}
