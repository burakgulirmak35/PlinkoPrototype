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

        #region Settings

        [Header("Ball Settings")]
        [SerializeField] private GameObject ballPrefab;
        [SerializeField] private Transform ballPoolParent;
        [SerializeField] private int initialBallCount = 200;

        [Header("Spawn Settings")]
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private float spawnForce = 2f;
        [SerializeField] private float spawnInterval = 0.1f;

        [SerializeField] private float spawnYOffset = 1f;

        private Vector2 spawnMin;
        private Vector2 spawnMax;

        #endregion

        private Queue<PlinkoBall> ballPool = new Queue<PlinkoBall>();
        private int availableBalls;

        private Coroutine spawnRoutine;

        private void Start()
        {
            CreatePool();
        }

        #region Pool

        private void CreatePool()
        {
            availableBalls = initialBallCount;

            for (int i = 0; i < initialBallCount; i++)
            {
                PlinkoBall ball = Instantiate(ballPrefab, ballPoolParent).GetComponent<PlinkoBall>();
                ball.gameObject.SetActive(false);
                ballPool.Enqueue(ball);
            }

            Debug.Log($"[BallManager] Created pool with {initialBallCount} balls.");
        }

        private PlinkoBall GetBallFromPool()
        {
            if (ballPool.Count == 0)
                return null;

            return ballPool.Dequeue();
        }

        public void ReturnBall(PlinkoBall ball)
        {
            ball.gameObject.SetActive(false);
            ballPool.Enqueue(ball);
        }

        public int GetRemainingBalls()
        {
            return availableBalls;
        }

        #endregion

        #region Ball Spawning

        private float sideForce = 0.5f; // sağ-sol sapma gücü

        public void SpawnBall()
        {
            if (availableBalls <= 0)
                return;

            PlinkoBall ball = GetBallFromPool();
            if (ball == null)
                return;

            // Rastgele spawn pozisyonu
            float randomX = Random.Range(spawnMin.x, spawnMax.x);
            float randomY = Random.Range(spawnMin.y, spawnMax.y);
            ball.transform.position = new Vector2(randomX, randomY);

            ball.transform.rotation = Quaternion.identity;
            ball.gameObject.SetActive(true);

            ball.rb.velocity = Vector2.zero;
            ball.rb.angularVelocity = 0f;
            ball.transform.rotation = Quaternion.identity;

            // Random yatay kuvvet
            float randomForceX = Random.Range(-sideForce, sideForce);
            Vector2 force = new Vector2(randomForceX, -spawnForce);
            ball.rb.AddForce(force, ForceMode2D.Impulse);

            availableBalls--;
        }

        #endregion

        #region Event Handling

        private void OnEnable()
        {
            GameEvents.OnHoldStart += StartSpawning;
            GameEvents.OnHoldEnd += StopSpawning;

            // Level değiştiğinde spawn alanını güncelle
            GameEvents.OnLevelChanged += UpdateSpawnAreaFromLevel;
        }

        private void OnDisable()
        {
            GameEvents.OnHoldStart -= StartSpawning;
            GameEvents.OnHoldEnd -= StopSpawning;

            GameEvents.OnLevelChanged -= UpdateSpawnAreaFromLevel;
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
                {
                    SpawnBall();
                    GameManager.Instance.UpdateBallUI();
                }
                else
                {
                    yield break;
                }

                yield return new WaitForSeconds(spawnInterval);
            }
        }

        #endregion

        #region Spawn Area Update
        private void UpdateSpawnAreaFromLevel(List<Vector2> topRow)
        {
            if (topRow == null || topRow.Count < 2)
            {
                Debug.LogWarning("[BallManager] Top row invalid for spawn area.");
                return;
            }

            // X aralığı peg'lerden gelsin
            float minX = topRow[0].x;
            float maxX = topRow[topRow.Count - 1].x;

            // Y'yi spawnPoint'ten al; yoksa fallback olarak topRow Y'si
            float y;

            if (spawnPoint != null)
                y = spawnPoint.position.y + spawnYOffset;
            else
                y = topRow[0].y; // Eski davranış

            spawnMin = new Vector2(minX, y);
            spawnMax = new Vector2(maxX, y);

            Debug.Log($"[BallManager] Spawn Area Updated: {spawnMin.x} → {spawnMax.x} at Y={y}");
        }


        #endregion

        #region AddBall

        public void AddBalls(int amount)
        {
            availableBalls += amount;
            GameManager.Instance.UpdateBallUI();
        }

        #endregion
    }
}
