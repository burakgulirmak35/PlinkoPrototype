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
    [SerializeField] private TextMeshProUGUI txtWallet;
    [SerializeField] private TextMeshProUGUI txtResetTimer;
    [SerializeField] private TextMeshProUGUI txtLevel;
    [SerializeField] private TextMeshProUGUI txtScore;

    [Header("Game State")]
    private bool isStarted = false;
    private int currentBallCount = 0;

    [Header("Level Info")]
    private int currentLevel = 1;
    private int maxLevel;

    [Header("Level End Settings")]
    private bool isLevelEnding = false;
    private float levelEndDelay = 5f;

    private Coroutine holdHintCoroutine;
    private float holdHintDelay = 5f;

    private int initialBalls;

    // 🔹 NEW SYSTEM
    private int ballsScoredThisLevel = 0;
    private int ballsRequiredForLevel = 0;

    private void Start()
    {
        InitUI();
        LoadLevelData(currentLevel);
        StartCoroutine(UpdateResetCountdown());
        DetectMaxLevel();

        if (RewardValidator.Instance != null)
        {
            RewardValidator.Instance.OnWalletUpdated += UpdateWalletUI;
            UpdateWalletUI(RewardValidator.Instance.LocalWallet);
        }
    }

    private void OnEnable()
    {
        GameEvents.OnTapStart += HandleTapStart;
        GameEvents.OnHoldStart += HandleHoldStart;
        GameEvents.OnHoldEnd += HandleHoldEnd;
        GameEvents.OnBallCountChanged += HandleBallCountChanged;
        GameEvents.OnLevelCompleted += HandleLevelCompleted;
        GameEvents.OnGameReset += HandleGameReset;
        GameEvents.OnLevelStarted += HandleLevelStarted;
        GameEvents.OnBallScored += HandleRoundScore;

        if (RewardValidator.Instance != null)
            RewardValidator.Instance.OnWalletUpdated += UpdateWalletUI;
    }

    private void OnDisable()
    {
        GameEvents.OnTapStart -= HandleTapStart;
        GameEvents.OnHoldStart -= HandleHoldStart;
        GameEvents.OnHoldEnd -= HandleHoldEnd;
        GameEvents.OnBallCountChanged -= HandleBallCountChanged;
        GameEvents.OnLevelCompleted -= HandleLevelCompleted;
        GameEvents.OnGameReset -= HandleGameReset;
        GameEvents.OnLevelStarted -= HandleLevelStarted;
        GameEvents.OnBallScored -= HandleRoundScore;

        if (RewardValidator.Instance != null)
            RewardValidator.Instance.OnWalletUpdated -= UpdateWalletUI;
    }

    // -------------------------------------------------------------
    // UI INIT
    // -------------------------------------------------------------
    private void InitUI()
    {
        isStarted = false;

        txtPressToStart.gameObject.SetActive(true);
        txtHoldToSendBalls.gameObject.SetActive(false);

        txtResetTimer.text = "--:--";
        txtBallCount.text = "Balls: 0";
        txtScore.text = "Score: 0";
        txtWallet.text = "Wallet: --";
    }

    // -------------------------------------------------------------
    // INPUT
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
    // WALLET / SCORE
    // -------------------------------------------------------------
    private Coroutine walletAnimRoutine;
    private float walletAnimDuration = 0.35f;

    private void UpdateWalletUI(int wallet)
    {
        if (walletAnimRoutine != null)
            StopCoroutine(walletAnimRoutine);

        walletAnimRoutine = StartCoroutine(AnimateWallet(wallet));
    }

    private IEnumerator AnimateWallet(int targetValue)
    {
        int startValue = 0;
        if (int.TryParse(txtWallet.text.Replace("Wallet:", "").Trim(), out int current))
            startValue = current;

        float elapsed = 0f;
        while (elapsed < walletAnimDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / walletAnimDuration);
            int value = Mathf.RoundToInt(Mathf.Lerp(startValue, targetValue, t));
            txtWallet.text = $"Wallet: {value}";
            yield return null;
        }

        txtWallet.text = $"Wallet: {targetValue}";
    }

    private int roundScore = 0;
    private void HandleRoundScore(int amount)
    {
        roundScore += amount;
        txtScore.text = "Score: " + roundScore;

        // 🔥 NEW LEVEL RULE
        ballsScoredThisLevel++;

        if (ballsScoredThisLevel >= ballsRequiredForLevel && !isLevelEnding)
        {
            StopHoldHintTimer();
            StartCoroutine(LevelEndRoutine());
        }
    }

    // -------------------------------------------------------------
    // BALL COUNT (LEVEL END REMOVED FROM HERE)
    // -------------------------------------------------------------
    private void HandleBallCountChanged(int count)
    {
        currentBallCount = count;
        txtBallCount.text = "Balls: " + count;
    }

    // -------------------------------------------------------------
    // HOLD HINT
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
    // LEVEL LOAD
    // -------------------------------------------------------------
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

        ballsRequiredForLevel = data.ballsRequiredForLevel;
        ballsScoredThisLevel = 0;

        GameEvents.TriggerLevelDataLoaded(data);
        GameEvents.TriggerLevelStarted(level);
    }

    private void DetectMaxLevel()
    {
        string dir = Path.Combine(Application.streamingAssetsPath, "Levels");
        if (!Directory.Exists(dir)) return;

        string[] files = Directory.GetFiles(dir, "level_*.json");
        maxLevel = files.Length;
    }

    private void HandleLevelStarted(int level)
    {
        txtLevel.text = "Level: " + level;
        txtPressToStart.gameObject.SetActive(true);
        txtHoldToSendBalls.gameObject.SetActive(false);
        isStarted = false;
    }

    // -------------------------------------------------------------
    // LEVEL END
    // -------------------------------------------------------------
    private IEnumerator LevelEndRoutine()
    {
        isLevelEnding = true;

        yield return new WaitForSeconds(levelEndDelay);

        if (RewardValidator.Instance != null)
            RewardValidator.Instance.FlushPendingRewards();

        GameEvents.TriggerLevelCompleted();
    }

    private void HandleLevelCompleted()
    {
        SaveEndOfGameData();

        currentLevel++;
        if (currentLevel > maxLevel)
            currentLevel = maxLevel;

        LoadLevelData(currentLevel);

        txtPressToStart.gameObject.SetActive(true);
        txtHoldToSendBalls.gameObject.SetActive(false);

        isStarted = false;
        isLevelEnding = false;
    }

    private void SaveEndOfGameData()
    {
        PlayerSessionData session = new PlayerSessionData()
        {
            date = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            levelId = currentLevel,
            ballUsed = 0,
            moneyEarned = 0
        };

        PlayerDataManager.Instance.AddSession(session);
    }

    // -------------------------------------------------------------
    // RESET
    // -------------------------------------------------------------
    private async void HandleGameReset()
    {
        int earnedThisSession = roundScore;
        roundScore = 0;
        txtScore.text = "Score: 0";

        if (MockServerService.Instance != null)
            await MockServerService.Instance.NotifyClientResetAsync(earnedThisSession);

        if (RewardValidator.Instance != null)
        {
            int wallet = await MockServerService.Instance.GetWalletAsync();
            RewardValidator.Instance.SyncWalletFromServer(wallet);
        }

        PlayerSessionData session = new PlayerSessionData()
        {
            date = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            levelId = currentLevel,
            ballUsed = 0,
            moneyEarned = earnedThisSession
        };

        PlayerDataManager.Instance.AddSession(session);

        currentLevel = 1;
        LoadLevelData(currentLevel);
    }

    // -------------------------------------------------------------
    // TIMER
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
