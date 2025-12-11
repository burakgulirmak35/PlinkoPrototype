using System.Collections;
using System.IO;
using PlinkoPrototype;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    #region Singleton
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    #endregion

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI txtPressToStart;
    [SerializeField] private TextMeshProUGUI txtHoldToSendBalls;
    [SerializeField] private TextMeshProUGUI txtBallCount;
    [SerializeField] private TextMeshProUGUI txtScore;
    [SerializeField] private TextMeshProUGUI txtResetTimer;

    [Header("Game State")]
    private int currentScore = 0;
    private int currentBallCount = 0;
    private bool isStarted = false;

    [Header("Level Info")]

    private int currentLevel = 1;

    [Header("Level End Settings")]
    private bool isLevelEnding = false;
    private float levelEndDelay = 5f;

    private Coroutine holdHintCoroutine;
    private float holdHintDelay = 5f;

    private void Start()
    {
        InitUI();
        LoadLevelData(currentLevel);
        UpdateScoreUI();
        StartCoroutine(UpdateResetCountdown());
        DetectMaxLevel();
    }

    // -------------------------------------------------------------
    // UI INITIALIZATION
    // -------------------------------------------------------------
    private void InitUI()
    {
        isStarted = false;
        currentScore = 0;

        txtPressToStart.gameObject.SetActive(true);
        txtHoldToSendBalls.gameObject.SetActive(false);

        txtResetTimer.text = "--:--";
        txtBallCount.text = "Balls: 0";
        txtScore.text = "Score: 0";
    }

    // -------------------------------------------------------------
    // EVENT SUBSCRIPTIONS
    // -------------------------------------------------------------
    private void OnEnable()
    {
        GameEvents.OnTapStart += HandleTapStart;
        GameEvents.OnHoldStart += HandleHoldStart;
        GameEvents.OnHoldEnd += HandleHoldEnd;
        GameEvents.OnBallScored += HandleBallScored;
        GameEvents.OnBallCountChanged += HandleBallCountChanged;
        GameEvents.OnLevelCompleted += HandleLevelCompleted;
        GameEvents.OnGameReset += HandleGameReset;
        GameEvents.OnLevelStarted += HandleLevelStarted;
    }

    private void OnDisable()
    {
        GameEvents.OnTapStart -= HandleTapStart;
        GameEvents.OnHoldStart -= HandleHoldStart;
        GameEvents.OnHoldEnd -= HandleHoldEnd;
        GameEvents.OnBallScored -= HandleBallScored;
        GameEvents.OnBallCountChanged -= HandleBallCountChanged;
        GameEvents.OnLevelCompleted -= HandleLevelCompleted;
        GameEvents.OnGameReset -= HandleGameReset;
        GameEvents.OnLevelStarted -= HandleLevelStarted;
    }

    // -------------------------------------------------------------
    // INPUT HANDLERS
    // -------------------------------------------------------------
    private void HandleTapStart()
    {
        if (!isStarted)
        {
            isStarted = true;
            txtPressToStart.gameObject.SetActive(false);
            txtHoldToSendBalls.gameObject.SetActive(true);
        }
        else
        {
            txtHoldToSendBalls.gameObject.SetActive(false);
            StopHoldHintTimer();
        }
    }

    private void HandleHoldStart()
    {
        txtHoldToSendBalls.gameObject.SetActive(false);
        StopHoldHintTimer();
    }

    private void HandleHoldEnd()
    {
        if (currentBallCount <= 0)
        {
            txtHoldToSendBalls.gameObject.SetActive(false);
            StopHoldHintTimer();
            return;
        }

        txtHoldToSendBalls.gameObject.SetActive(false);
        StartHoldHintTimer();
    }

    // -------------------------------------------------------------
    // SCORE / UI UPDATES
    // -------------------------------------------------------------
    private void HandleBallScored(int amount)
    {
        currentScore += amount;
        UpdateScoreUI();
    }

    private void UpdateScoreUI()
    {
        txtScore.text = "Score: " + currentScore.ToString();
    }

    private void HandleBallCountChanged(int count)
    {
        currentBallCount = count;
        txtBallCount.text = "Balls: " + count.ToString();

        if (count <= 0 && !isLevelEnding)
        {
            StopHoldHintTimer();
            StartCoroutine(LevelEndRoutine());
        }
    }

    // -------------------------------------------------------------
    // HOLD HINT TIMER
    // -------------------------------------------------------------
    private void StartHoldHintTimer()
    {
        StopHoldHintTimer();

        if (currentBallCount <= 0 || !isStarted)
            return;

        holdHintCoroutine = StartCoroutine(HoldHintRoutine());
    }

    private void StopHoldHintTimer()
    {
        if (holdHintCoroutine != null)
        {
            StopCoroutine(holdHintCoroutine);
            holdHintCoroutine = null;
        }
    }

    private IEnumerator HoldHintRoutine()
    {
        yield return new WaitForSeconds(holdHintDelay);

        if (currentBallCount > 0 && isStarted)
            txtHoldToSendBalls.gameObject.SetActive(true);
    }

    // -------------------------------------------------------------
    // LEVEL LOADING
    // -------------------------------------------------------------

    private int initialBalls;
    private void LoadLevelData(int level)
    {
        string path = Path.Combine(Application.streamingAssetsPath, $"Levels/level_{level}.json");

        if (!File.Exists(path))
        {
            Debug.LogError("LEVEL DATA NOT FOUND: " + path);
            return;
        }

        string json = File.ReadAllText(path);
        LevelData data = JsonUtility.FromJson<LevelData>(json);

        initialBalls = data.ballCount;
        GameEvents.TriggerLevelDataLoaded(data);

        // 🆕 Level load işlemi tamamlandı → LevelStarted event’i gönderiyoruz
        GameEvents.TriggerLevelStarted(level);
    }


    private int maxLevel;
    private void DetectMaxLevel()
    {
        string dir = Path.Combine(Application.streamingAssetsPath, "Levels");

        if (!Directory.Exists(dir))
        {
            Debug.LogError("Levels folder not found!");
            return;
        }

        string[] files = Directory.GetFiles(dir, "level_*.json");
        maxLevel = files.Length;

        Debug.Log("Detected Levels: " + maxLevel);
    }

    private void HandleLevelStarted(int level)
    {
        currentScore = 0;
        txtScore.text = "Score: 0";

        txtBallCount.text = "Balls: " + initialBalls;

        txtPressToStart.gameObject.SetActive(true);
        txtHoldToSendBalls.gameObject.SetActive(false);

        isStarted = false;
    }


    // -------------------------------------------------------------
    // LEVEL END → EVENT DRIVEN COMPLETION
    // -------------------------------------------------------------
    private IEnumerator LevelEndRoutine()
    {
        isLevelEnding = true;

        Debug.Log("Balls finished. Ending level in 5 seconds...");
        yield return new WaitForSeconds(levelEndDelay);

        GameEvents.TriggerLevelCompleted();
    }

    private void HandleLevelCompleted()
    {
        SaveEndOfGameData();

        currentLevel++;
        if (currentLevel > maxLevel)
            currentLevel = maxLevel;

        LoadLevelData(currentLevel);

        ResetUIForNewLevel();

        isLevelEnding = false;
    }


    private void ResetUIForNewLevel()
    {
        currentScore = 0;
        UpdateScoreUI();

        txtPressToStart.gameObject.SetActive(true);
        txtHoldToSendBalls.gameObject.SetActive(false);

        isStarted = false;
    }

    // -------------------------------------------------------------
    // END OF GAME PLAYER DATA SAVE
    // -------------------------------------------------------------
    private void SaveEndOfGameData()
    {
        int ballsUsed = initialBalls - currentBallCount;
        int moneyEarned = currentScore;

        PlayerSessionData session = new PlayerSessionData()
        {
            date = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            levelId = currentLevel,
            ballUsed = ballsUsed,
            moneyEarned = moneyEarned
        };

        PlayerDataManager.Instance.AddSession(session);
        PlayerDataManager.Instance.AddMoney(moneyEarned);

        Debug.Log("SESSION SAVED.");
    }

    // -------------------------------------------------------------
    // GAME RESET HANDLER
    // -------------------------------------------------------------
    private void HandleGameReset()
    {
        currentLevel = 1;
        LoadLevelData(currentLevel);
    }


    // -------------------------------------------------------------
    // GAME TIMER
    // -------------------------------------------------------------
    private IEnumerator UpdateResetCountdown()
    {
        while (true)
        {
            UpdateResetTimerUI();
            yield return new WaitForSeconds(1f);
        }
    }

    private void UpdateResetTimerUI()
    {
        string lastResetStr = PlayerDataManager.Instance.Data.lastResetUtc;
        if (string.IsNullOrEmpty(lastResetStr))
        {
            txtResetTimer.text = "--:--";
            return;
        }

        System.DateTime lastReset = System.DateTime.Parse(lastResetStr);
        System.DateTime nextReset = lastReset.AddMinutes(15);
        System.TimeSpan diff = nextReset - System.DateTime.UtcNow;

        if (diff.TotalSeconds <= 0)
        {
            txtResetTimer.text = "00:00";
            return;
        }

        txtResetTimer.text = $"{diff.Minutes:00}:{diff.Seconds:00}";
    }

}
