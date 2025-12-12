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

    private void Start()
    {
        InitUI();
        LoadLevelData(currentLevel);
        StartCoroutine(UpdateResetCountdown());
        DetectMaxLevel();

        // 🔥 Başlangıçta wallet UI'yi server state ile senkronla
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
    // UI INITIALIZATION
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
    // WALLET
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

        // Mevcut UI'daki sayıyı çöz (örneğin "Wallet: 120" → 120)
        if (int.TryParse(txtWallet.text.Replace("Wallet:", "").Trim(), out int current))
            startValue = current;

        float elapsed = 0f;

        while (elapsed < walletAnimDuration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / walletAnimDuration;
            t = Mathf.SmoothStep(0f, 1f, t);

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
    }

    // -------------------------------------------------------------
    // BALL COUNT
    // -------------------------------------------------------------
    private void HandleBallCountChanged(int count)
    {
        currentBallCount = count;
        txtBallCount.text = "Balls: " + count;

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

        GameEvents.TriggerLevelStarted(level);
    }

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
    }

    private void HandleLevelStarted(int level)
    {
        txtLevel.text = "Level: " + level;

        txtBallCount.text = "Balls: " + initialBalls;
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

        // Pending reward batch varsa server'a gönder
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
        int ballsUsed = initialBalls - currentBallCount;

        PlayerSessionData session = new PlayerSessionData()
        {
            date = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            levelId = currentLevel,
            ballUsed = ballsUsed,
            moneyEarned = 0 // artık score yok → wallet server'dan geliyor
        };

        PlayerDataManager.Instance.AddSession(session);
    }

    // -------------------------------------------------------------
    // GAME RESET HANDLER
    // -------------------------------------------------------------
    private async void HandleGameReset()
    {
        // 1) SCORE → WALLET MERGE
        int earnedThisSession = roundScore;
        roundScore = 0;
        txtScore.text = "Score: 0";

        // 2) SERVER'A BİLDİR + WALLET’A EKLET
        if (MockServerService.Instance != null)
        {
            await MockServerService.Instance.NotifyClientResetAsync(earnedThisSession);
        }

        // 3) WALLET UI Güncelle
        if (RewardValidator.Instance != null)
        {
            int wallet = await MockServerService.Instance.GetWalletAsync();
            RewardValidator.Instance.SyncWalletFromServer(wallet);
        }

        // 4) SESSION KAYDI (15 dk kazanımı)
        PlayerSessionData session = new PlayerSessionData()
        {
            date = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            levelId = currentLevel,
            ballUsed = 0, // reset sırasında hesaplanmaz
            moneyEarned = earnedThisSession
        };

        PlayerDataManager.Instance.AddSession(session);

        // 5) LEVEL RESET
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
