using System.Collections;
using System.Collections.Generic;
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

    [Header("Game State")]
    private int currentScore;
    public bool isStarted = false;

    [Header("Player Data")]
    private int currentLevel = 1;

    private void Start()
    {
        InitUi();
        isStarted = false;
        LevelManager.Instance.CreateLevel(currentLevel);

        currentScore = 0;
        UpdateScoreUI();
    }

    private void InitUi()
    {
        txtPressToStart.gameObject.SetActive(true);
        txtHoldToSendBalls.gameObject.SetActive(false);
        txtScore.gameObject.SetActive(false);

        txtPressToStart.text = "Tap to Start";
        txtHoldToSendBalls.text = "Hold to Send Balls";

        txtBallCount.text = "Balls: 0";
        txtScore.text = "Score: 0";
    }

    #region Event Subscriptions

    private void OnEnable()
    {
        GameEvents.OnTapStart += HandleTapStart;
        GameEvents.OnHoldStart += HandleHoldStart;
        GameEvents.OnHoldEnd += HandleHoldEnd;
        GameEvents.OnBallScored += HandleBallScored;
        GameEvents.OnBallCountChanged += HandleBallCountChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnTapStart -= HandleTapStart;
        GameEvents.OnHoldStart -= HandleHoldStart;
        GameEvents.OnHoldEnd -= HandleHoldEnd;
        GameEvents.OnBallScored -= HandleBallScored;
        GameEvents.OnBallCountChanged -= HandleBallCountChanged;
    }

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


    private void HandleBallScored(int amount)
    {
        currentScore += amount;
        UpdateScoreUI();
    }

    private void UpdateScoreUI()
    {
        if (txtScore != null)
            txtScore.text = "Score: " + currentScore.ToString();
    }

    private void HandleBallCountChanged(int count)
    {
        currentBallCount = count;
        UpdateBallUI(count);

        if (currentBallCount <= 0)
        {
            txtHoldToSendBalls.gameObject.SetActive(false);
            StopHoldHintTimer();
        }
    }


    public void UpdateBallUI(int value)
    {
        txtBallCount.text = "Balls: " + value.ToString();
    }

    #endregion

    #region NotifTimer

    private float holdHintDelay = 5f; // 5 sn bekleme süresi
    private bool isHoldHintTimerRunning;
    private int currentBallCount;

    // 🆕 Coroutine referansı
    private Coroutine holdHintCoroutine;

    private void StartHoldHintTimer()
    {
        StopHoldHintTimer();

        if (currentBallCount <= 0)
            return;

        isHoldHintTimerRunning = true;
        holdHintCoroutine = StartCoroutine(HoldHintRoutine());
    }

    private void StopHoldHintTimer()
    {
        isHoldHintTimerRunning = false;

        if (holdHintCoroutine != null)
        {
            StopCoroutine(holdHintCoroutine);
            holdHintCoroutine = null;
        }
    }

    private IEnumerator HoldHintRoutine()
    {
        yield return new WaitForSeconds(holdHintDelay);

        isHoldHintTimerRunning = false;
        holdHintCoroutine = null;

        if (currentBallCount > 0 && isStarted)
        {
            txtHoldToSendBalls.gameObject.SetActive(true);
        }
    }


    #endregion

}
