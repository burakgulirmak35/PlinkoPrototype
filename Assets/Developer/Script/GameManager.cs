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
    public bool isStarted = false;

    [Header("Player Data")]
    private int currentLevel = 1;

    private void Start()
    {
        InitUi();
        isStarted = false;
        LevelManager.Instance.CreateLevel(currentLevel);
    }

    private void InitUi()
    {
        txtPressToStart.gameObject.SetActive(true);
        txtHoldToSendBalls.gameObject.SetActive(false);
        txtScore.gameObject.SetActive(false);

        txtPressToStart.text = "Tap to Start";
        txtHoldToSendBalls.text = "Hold to Send Balls";

        txtBallCount.text = "0";
        txtScore.text = "0";
    }

    #region UI Updates

    public void UpdateBallUI()
    {
        txtBallCount.text = BallManager.Instance.GetRemainingBalls().ToString();
    }

    #endregion

    #region Event Subscriptions

    private void OnEnable()
    {
        GameEvents.OnTapStart += HandleTapStart;
        GameEvents.OnHoldStart += HandleHoldStart;
        GameEvents.OnHoldEnd += HandleHoldEnd;
    }

    private void OnDisable()
    {
        GameEvents.OnTapStart -= HandleTapStart;
        GameEvents.OnHoldStart -= HandleHoldStart;
        GameEvents.OnHoldEnd -= HandleHoldEnd;
    }

    private void HandleTapStart()
    {
        if (!isStarted)
        {
            isStarted = true;
            txtPressToStart.gameObject.SetActive(false);
            txtHoldToSendBalls.gameObject.SetActive(true);
        }
    }

    private void HandleHoldStart()
    {
        txtHoldToSendBalls.gameObject.SetActive(false);
    }

    private void HandleHoldEnd()
    {
        txtHoldToSendBalls.gameObject.SetActive(true);
    }

    #endregion
}
