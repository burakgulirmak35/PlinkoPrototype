using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace PlinkoPrototype
{
    public class BallManager : MonoBehaviour
    {
        #region Singleton
        public static BallManager Instance { get; private set; }

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

        // ------------------------------------------------
        // SETTINGS
        // ------------------------------------------------
        [Header("Ball Settings")]
        [SerializeField] private Transform fakeBall;
        [SerializeField] private GameObject ballPrefab;
        [SerializeField] private Transform ballPoolParent;

        [SerializeField] private int poolSize = 50;

        [Header("Spawn Settings")]
        [SerializeField] private float spawnForce = 2f;
        [SerializeField] private float spawnInterval = 0.1f;
        [SerializeField] private float spawnYOffset = 2f;

        // ------------------------------------------------
        // STATE
        // ------------------------------------------------
        private readonly Queue<PlinkoBall> ballPool = new Queue<PlinkoBall>();
        private int availableBalls;
        private Coroutine spawnRoutine;

        private Vector2 spawnMin;
        private Vector2 spawnMax;

        private float sideForce = 0.5f;
        private int ballIdCounter = 0;

        private GameState currentState;

        // ------------------------------------------------
        // LIFECYCLE
        // ------------------------------------------------
        private void Start()
        {
            CreatePool();
        }

        private void OnEnable()
        {
            GameEvents.OnHoldStart += StartSpawning;
            GameEvents.OnHoldEnd += StopSpawning;
            GameEvents.OnLevelChanged += UpdateSpawnAreaFromLevel;
            GameEvents.OnGameStateChanged += HandleGameStateChanged;
            GameEvents.OnBallCountRestore += ApplyBallCountFromServer;
            GameEvents.OnGameReset += HandleGameReset;
        }

        private void OnDisable()
        {
            GameEvents.OnHoldStart -= StartSpawning;
            GameEvents.OnHoldEnd -= StopSpawning;
            GameEvents.OnLevelChanged -= UpdateSpawnAreaFromLevel;
            GameEvents.OnGameStateChanged -= HandleGameStateChanged;
            GameEvents.OnBallCountRestore -= ApplyBallCountFromServer;
            GameEvents.OnGameReset -= HandleGameReset;
        }

        // ------------------------------------------------
        // BALL COUNT (SERVER DRIVEN)
        // ------------------------------------------------
        private void ApplyBallCountFromServer(int count)
        {
            availableBalls = Mathf.Max(0, count);
            GameEvents.TriggerBallCountChanged(availableBalls);

            Debug.Log($"[BALL] Applied from server: {availableBalls}");
        }

        private void HandleGameReset()
        {
            // Reset event’i zaten server 200 ile gönderir
            // burada ekstra set yok
            StopSpawning();
            StopFakeBall();
        }

        // ------------------------------------------------
        // POOL
        // ------------------------------------------------
        private void CreatePool()
        {
            for (int i = 0; i < poolSize; i++)
            {
                PlinkoBall ball = Instantiate(ballPrefab, ballPoolParent)
                    .GetComponent<PlinkoBall>();

                ball.gameObject.SetActive(false);
                ballPool.Enqueue(ball);
            }
        }

        private PlinkoBall GetBallFromPool()
        {
            if (ballPool.Count > 0)
                return ballPool.Dequeue();

            PlinkoBall ball = Instantiate(ballPrefab, ballPoolParent)
                .GetComponent<PlinkoBall>();

            ball.gameObject.SetActive(false);
            return ball;
        }

        public void ReturnBall(PlinkoBall ball)
        {
            if (ball == null) return;

            ball.gameObject.SetActive(false);

            if (ball.rb != null)
            {
                ball.rb.velocity = Vector2.zero;
                ball.rb.angularVelocity = 0f;
            }

            ballPool.Enqueue(ball);
        }

        // ------------------------------------------------
        // SPAWN
        // ------------------------------------------------
        private void StartSpawning()
        {
            if (currentState != GameState.Playing)
                return;

            if (spawnRoutine == null && availableBalls > 0)
                spawnRoutine = StartCoroutine(SpawnRoutine());
        }

        private void StopSpawning()
        {
            if (spawnRoutine != null)
            {
                StopCoroutine(spawnRoutine);
                spawnRoutine = null;
            }
        }

        private IEnumerator SpawnRoutine()
        {
            while (availableBalls > 0)
            {
                SpawnBall();
                yield return new WaitForSeconds(spawnInterval);
            }

            spawnRoutine = null;
        }

        private void SpawnBall()
        {
            if (availableBalls <= 0)
                return;

            PlinkoBall ball = GetBallFromPool();

            float randomX = Random.Range(spawnMin.x, spawnMax.x);
            float y = spawnMin.y;

            ball.transform.position = new Vector2(randomX, y);
            ball.BallId = ++ballIdCounter;
            ball.transform.rotation = Quaternion.identity;
            ball.gameObject.SetActive(true);

            if (ball.rb != null)
            {
                float randomForceX = Random.Range(-sideForce, sideForce);
                ball.rb.AddForce(
                    new Vector2(randomForceX, -spawnForce),
                    ForceMode2D.Impulse
                );
            }

            availableBalls--;
            GameEvents.TriggerBallCountChanged(availableBalls);
        }

        // ------------------------------------------------
        // SPAWN AREA
        // ------------------------------------------------
        private void UpdateSpawnAreaFromLevel(List<Vector2> topRow)
        {
            if (topRow == null || topRow.Count < 2)
                return;

            float minX = topRow[0].x;
            float maxX = topRow[topRow.Count - 1].x;
            float y = topRow[0].y + spawnYOffset;

            spawnMin = new Vector2(minX, y);
            spawnMax = new Vector2(maxX, y);

            StartFakeBall();
        }

        // ------------------------------------------------
        // FAKE BALL
        // ------------------------------------------------
        private Tween fakeBallTween;
        private float fakeBallDuration = 5f;
        private bool isLoopingFakeBall = false;

        private void StartFakeBall()
        {
            if (isLoopingFakeBall) return;
            isLoopingFakeBall = true;

            fakeBall.DOKill();
            fakeBall.gameObject.SetActive(true);

            fakeBall.position = spawnMin;
            fakeBall.localScale = Vector3.one * 0.5f;

            fakeBallTween = fakeBall
                .DOMoveX(spawnMax.x, fakeBallDuration)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Yoyo);
        }

        private void StopFakeBall()
        {
            if (fakeBallTween != null)
            {
                fakeBallTween.Kill();
                fakeBallTween = null;
            }

            isLoopingFakeBall = false;
            fakeBall.gameObject.SetActive(false);
        }

        // ------------------------------------------------
        // GAME STATE
        // ------------------------------------------------
        private void HandleGameStateChanged(GameState state)
        {
            currentState = state;

            if (currentState != GameState.Playing)
                StopSpawning();
        }
    }
}
