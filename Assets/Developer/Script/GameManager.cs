using System.Collections;
using System.IO;
using System.Threading.Tasks;
using PlinkoPrototype;
using TMPro;
using UnityEngine;
using System.Globalization;
using System;

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
    [SerializeField] private GameObject HoldToSendBallsObj;
    [SerializeField] private TextMeshProUGUI txtNoBalls;
    [SerializeField] private TextMeshProUGUI txtBallCount;
    [SerializeField] private TextMeshProUGUI txtWallet;
    [SerializeField] private TextMeshProUGUI txtResetTimer;
    [SerializeField] private TextMeshProUGUI txtLevel;
    [SerializeField] private TextMeshProUGUI txtScore;

    [Header("Game State")]
    [SerializeField] private GameState currentState = GameState.Idle;

    private int currentBallCount;
    private int currentLevel = 1;
    private int maxLevel;

    private int roundScore = 0;
    private int ballsScoredThisLevel = 0;
    private int ballsRequiredForLevel = 0;

    private Coroutine holdHintCoroutine;
    private float holdHintDelay = 5f;

    // 🔒 Restore sırasında server'a state yazmayı geçici durdurmak için
    private bool isRestoring = false;

    // -------------------------------------------------------------
    // UNITY
    // -------------------------------------------------------------
    private async void Start()
    {
        InitUI();

        RewardValidator.Instance.OnWalletUpdated += UpdateWalletUI;

        int wallet = await MockServerService.Instance.GetWalletAsync();
        RewardValidator.Instance.SyncWalletFromServer(wallet);

        isRestoring = true;
        await TryRestoreFromServer();
        isRestoring = false;

        DetectMaxLevel();
        StartCoroutine(UpdateResetCountdown());
    }

    private void OnEnable()
    {
        GameEvents.OnTapStart += HandleTapStart;
        GameEvents.OnHoldStart += HandleHoldStart;
        GameEvents.OnHoldEnd += HandleHoldEnd;
        GameEvents.OnBallCountChanged += HandleBallCountChanged;
        GameEvents.OnBallScored += HandleRoundScore;
        GameEvents.OnLevelStarted += HandleLevelStarted;
        GameEvents.OnLevelCompleted += HandleLevelCompleted;
        GameEvents.OnGameReset += HandleGameReset;
    }

    private void OnDisable()
    {
        GameEvents.OnTapStart -= HandleTapStart;
        GameEvents.OnHoldStart -= HandleHoldStart;
        GameEvents.OnHoldEnd -= HandleHoldEnd;
        GameEvents.OnBallCountChanged -= HandleBallCountChanged;
        GameEvents.OnBallScored -= HandleRoundScore;
        GameEvents.OnLevelStarted -= HandleLevelStarted;
        GameEvents.OnLevelCompleted -= HandleLevelCompleted;
        GameEvents.OnGameReset -= HandleGameReset;

        if (RewardValidator.Instance != null)
            RewardValidator.Instance.OnWalletUpdated -= UpdateWalletUI;
    }

    // -------------------------------------------------------------
    // INIT
    // -------------------------------------------------------------
    private void InitUI()
    {
        SetState(GameState.Idle);

        txtPressToStart.gameObject.SetActive(true);
        HoldToSendBallsObj.SetActive(false);

        txtNoBalls.gameObject.SetActive(false);

        txtBallCount.text = "Balls: 0";
        txtScore.text = "Score: 0";
        txtWallet.text = "Wallet: --";
        txtResetTimer.text = "--:--";
    }


    // -------------------------------------------------------------
    // SERVER RESTORE
    // -------------------------------------------------------------
    private async Task TryRestoreFromServer()
    {
        PlayerData data = await MockServerService.Instance.GetPlayerDataAsync();

        if (data == null)
        {
            currentLevel = 1;
            roundScore = 0;
            ballsScoredThisLevel = 0;

            txtScore.text = "Score: 0";

            LoadLevelData(currentLevel);
            GameEvents.TriggerBallCountRestore(200); // ilk oyun fallback

            GameEvents.TriggerGameStateRestored();
            return;
        }

        currentLevel = Mathf.Max(1, data.savedLevel);
        roundScore = Mathf.Max(0, data.savedRoundScore);
        ballsScoredThisLevel = Mathf.Max(0, data.savedBallsScoredThisLevel);

        txtScore.text = "Score: " + roundScore;

        LoadLevelData(currentLevel);
        GameEvents.TriggerBallCountRestore(data.savedTotalBallsRemaining);
        GameEvents.TriggerHistoryRestore(data.sessionRewards);
        GameEvents.TriggerGameStateRestored();
    }

    // -------------------------------------------------------------
    // INPUT
    // -------------------------------------------------------------
    private void HandleTapStart()
    {
        if (currentBallCount <= 0)
            return;

        if (currentState == GameState.Idle)
        {
            SetState(GameState.Playing);
            txtPressToStart.gameObject.SetActive(false);
            StartHoldHintTimer();
        }
        else if (currentState == GameState.Playing)
        {
            HoldToSendBallsObj.SetActive(false);
            StopHoldHintTimer();
        }
    }


    private void HandleHoldStart()
    {
        if (currentState != GameState.Playing)
            return;

        if (currentBallCount <= 0)
            return;

        HoldToSendBallsObj.SetActive(false);
        StopHoldHintTimer();
    }


    private void HandleHoldEnd()
    {
        if (currentState != GameState.Playing)
            return;

        if (currentBallCount <= 0)
            return;

        StartHoldHintTimer();
    }


    // -------------------------------------------------------------
    // BALL COUNT → SERVER
    // -------------------------------------------------------------
    private void HandleBallCountChanged(int count)
    {
        currentBallCount = count;
        txtBallCount.text = "Balls: " + count;

        if (currentBallCount <= 0)
        {
            txtPressToStart.gameObject.SetActive(false);
            HoldToSendBallsObj.SetActive(false);
            txtNoBalls.gameObject.SetActive(true);

            StopHoldHintTimer();
            SetState(GameState.Idle);
        }
        else
        {
            txtNoBalls.gameObject.SetActive(false);

            if (currentState == GameState.Idle)
                txtPressToStart.gameObject.SetActive(true);
        }

        if (isRestoring)
            return;

        MockServerService.Instance?.ReportGameState(
            currentLevel,
            currentBallCount,
            roundScore,
            ballsScoredThisLevel
        );
    }

    // -------------------------------------------------------------
    // SCORE → SERVER
    // -------------------------------------------------------------
    private void HandleRoundScore(int amount)
    {
        roundScore += amount;
        ballsScoredThisLevel++;

        txtScore.text = "Score: " + roundScore;

        if (!isRestoring)
        {
            MockServerService.Instance?.ReportGameState(
                currentLevel,
                currentBallCount,
                roundScore,
                ballsScoredThisLevel   // 🔥 YENİ
            );
        }

        if (ballsScoredThisLevel >= ballsRequiredForLevel &&
            currentState == GameState.Playing)
        {
            StopHoldHintTimer();
            StartCoroutine(LevelEndRoutine());
        }
    }


    // -------------------------------------------------------------
    // LEVEL
    // -------------------------------------------------------------
    private void LoadLevelData(int level)
    {
        string path = Path.Combine(Application.streamingAssetsPath, $"Levels/level_{level}.json");
        if (!File.Exists(path)) return;

        string json = File.ReadAllText(path);
        LevelData data = JsonUtility.FromJson<LevelData>(json);

        ballsRequiredForLevel = data.ballsRequiredForLevel;

        GameEvents.TriggerLevelDataLoaded(data);
        GameEvents.TriggerLevelStarted(level);
    }

    private void DetectMaxLevel()
    {
        string dir = Path.Combine(Application.streamingAssetsPath, "Levels");
        if (!Directory.Exists(dir)) return;

        maxLevel = Directory.GetFiles(dir, "level_*.json").Length;
    }

    private void HandleLevelStarted(int level)
    {
        currentLevel = level;
        SetState(GameState.Idle);

        txtLevel.text = "Level: " + level;
        HoldToSendBallsObj.SetActive(false);
    }

    private IEnumerator LevelEndRoutine()
    {
        SetState(GameState.LevelEnd);
        yield return null;

        RewardValidator.Instance?.FlushPendingRewards();
        GameEvents.TriggerLevelCompleted();
    }

    private void HandleLevelCompleted()
    {
        ballsScoredThisLevel = 0;

        currentLevel++;
        if (currentLevel > maxLevel)
            currentLevel = maxLevel;

        LoadLevelData(currentLevel);
        SetState(GameState.Idle);
    }

    // -------------------------------------------------------------
    // RESET
    // -------------------------------------------------------------
    private async void HandleGameReset()
    {
        SetState(GameState.Reset);

        txtPressToStart.gameObject.SetActive(true);
        HoldToSendBallsObj.SetActive(false);

        roundScore = 0;
        txtScore.text = "Score: 0";

        while (MockServerService.Instance == null)
            await Task.Yield();
        await MockServerService.Instance.PerformHardResetAsync();

        int wallet = await MockServerService.Instance.GetWalletAsync();
        RewardValidator.Instance?.SyncWalletFromServer(wallet);

        currentLevel = 1;
        LoadLevelData(currentLevel);
    }

    // -------------------------------------------------------------
    // HOLD HINT
    // -------------------------------------------------------------
    private void StartHoldHintTimer()
    {
        StopHoldHintTimer();

        if (currentBallCount <= 0)
            return;

        if (currentState != GameState.Playing)
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

        if (currentBallCount > 0 && currentState == GameState.Playing)
            HoldToSendBallsObj.SetActive(true);
    }

    // -------------------------------------------------------------
    // WALLET UI
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
        int.TryParse(txtWallet.text.Replace("Wallet:", ""), out int startValue);
        float elapsed = 0f;

        while (elapsed < walletAnimDuration)
        {
            elapsed += Time.deltaTime;
            int value = Mathf.RoundToInt(Mathf.Lerp(startValue, targetValue, elapsed / walletAnimDuration));
            txtWallet.text = $"Wallet: {value}";
            yield return null;
        }

        txtWallet.text = $"Wallet: {targetValue}";
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

    private bool resetTriggered = false;
    private void UpdateResetTimerUI()
    {
        if (PlayerDataManager.Instance == null ||
            PlayerDataManager.Instance.Data == null)
        {
            txtResetTimer.text = "--:--";
            return;
        }

        string lastResetStr = PlayerDataManager.Instance.Data.lastResetUtc;

        if (string.IsNullOrEmpty(lastResetStr))
        {
            txtResetTimer.text = "--:--";
            return;
        }

        if (!DateTime.TryParseExact(
            lastResetStr,
            "o",
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out DateTime lastReset))
        {
            txtResetTimer.text = "--:--";
            return;
        }

        TimeSpan remaining =
            TimeSpan.FromMinutes(15) -
            (DateTime.UtcNow - lastReset.ToUniversalTime());

        if (remaining.TotalSeconds <= 0)
        {
            txtResetTimer.text = "00:00";

            if (!resetTriggered)
            {
                resetTriggered = true;
                PlayerDataManager.Instance.ForceHardResetFromTimer();
            }

            return;
        }

        resetTriggered = false;

        txtResetTimer.text =
            $"{remaining.Minutes:00}:{remaining.Seconds:00}";
    }





    // -------------------------------------------------------------
    // STATE
    // -------------------------------------------------------------
    private void SetState(GameState newState)
    {
        if (currentState == newState) return;
        currentState = newState;
        GameEvents.TriggerGameStateChanged(currentState);
    }


    public int GetBallsRequiredForLevel()
    {
        return ballsRequiredForLevel;
    }

    public int GetBallsScoredThisLevel()
    {
        return ballsScoredThisLevel;
    }
}
