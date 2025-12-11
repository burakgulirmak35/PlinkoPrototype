using System.Collections;
using System.Collections.Generic;
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

        [Header("Ball Settings")]
        [SerializeField] private GameObject ballPrefab;
        [SerializeField] private Transform ballPoolParent;

        [SerializeField] private int initialBallCount = 200;
        [SerializeField] private int poolSize = 50;
        [Header("Spawn Settings")]
        [SerializeField] private float spawnForce = 2f;
        [SerializeField] private float spawnInterval = 0.1f;

        private Queue<PlinkoBall> ballPool = new Queue<PlinkoBall>();
        private int availableBalls;

        private Coroutine spawnRoutine;

        private Vector2 spawnMin;
        private Vector2 spawnMax;

        private float sideForce = 0.5f;
        private float spawnYOffset = 5f;

        private void Start()
        {
            CreatePool();
            ResetBallAvailability();
        }

        #region Event Subscriptions

        private void OnEnable()
        {
            GameEvents.OnHoldStart += StartSpawning;
            GameEvents.OnHoldEnd += StopSpawning;
            GameEvents.OnLevelChanged += UpdateSpawnAreaFromLevel;

            GameEvents.OnLevelDataLoaded += ApplyLevelData;   // 🔥 yeni eklendi
        }

        private void OnDisable()
        {
            GameEvents.OnHoldStart -= StartSpawning;
            GameEvents.OnHoldEnd -= StopSpawning;
            GameEvents.OnLevelChanged -= UpdateSpawnAreaFromLevel;

            GameEvents.OnLevelDataLoaded -= ApplyLevelData;
        }

        private void ApplyLevelData(LevelData data)
        {
            // Yeni levelde top hakkını JSON’dan al
            initialBallCount = data.ballCount;

            // Yeni level başında top sayısını resetle
            ResetBallAvailability();
        }

        #endregion

        #region Pool Creation

        private void CreatePool()
        {
            int count = Mathf.Max(poolSize, 1);

            for (int i = 0; i < count; i++)
            {
                PlinkoBall ball = Instantiate(ballPrefab, ballPoolParent).GetComponent<PlinkoBall>();
                ball.gameObject.SetActive(false);
                ballPool.Enqueue(ball);
            }

            Debug.Log($"[BallManager] Created pool with {count} balls.");
        }

        // 🔥 Level başında top hakkını sıfırlar
        private void ResetBallAvailability()
        {
            availableBalls = initialBallCount;
            GameEvents.TriggerBallCountChanged(availableBalls);
        }

        private PlinkoBall GetBallFromPool()
        {
            if (ballPool.Count > 0)
                return ballPool.Dequeue();

            PlinkoBall newBall = Instantiate(ballPrefab, ballPoolParent).GetComponent<PlinkoBall>();
            newBall.gameObject.SetActive(false);
            return newBall;
        }

        public void ReturnBall(PlinkoBall ball)
        {
            ball.gameObject.SetActive(false);
            ball.transform.rotation = Quaternion.identity;
            ball.rb.velocity = Vector2.zero;
            ball.rb.angularVelocity = 0;

            ballPool.Enqueue(ball);
        }

        public int GetRemainingBalls()
        {
            return availableBalls;
        }

        #endregion

        #region Ball Spawning

        public void SpawnBall()
        {
            if (availableBalls <= 0)
                return;

            PlinkoBall ball = GetBallFromPool();

            float randomX = Random.Range(spawnMin.x, spawnMax.x);
            float randomY = Random.Range(spawnMin.y, spawnMax.y);

            ball.transform.position = new Vector2(randomX, randomY);
            ball.transform.rotation = Quaternion.identity;
            ball.gameObject.SetActive(true);

            float randomForceX = Random.Range(-sideForce, sideForce);
            Vector2 force = new Vector2(randomForceX, -spawnForce);
            ball.rb.AddForce(force, ForceMode2D.Impulse);

            availableBalls--;
            GameEvents.TriggerBallCountChanged(availableBalls);
        }

        private void StartSpawning()
        {
            if (spawnRoutine == null)
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
            while (true)
            {
                if (availableBalls > 0)
                    SpawnBall();
                else
                    yield break;

                yield return new WaitForSeconds(spawnInterval);
            }
        }

        #endregion

        #region Spawn Area Update

        private void UpdateSpawnAreaFromLevel(List<Vector2> topRow)
        {
            if (topRow == null || topRow.Count < 2)
                return;

            float minX = topRow[0].x;
            float maxX = topRow[topRow.Count - 1].x;
            float y = topRow[0].y + spawnYOffset;

            spawnMin = new Vector2(minX, y);
            spawnMax = new Vector2(maxX, y);
        }

        #endregion
    }
}
