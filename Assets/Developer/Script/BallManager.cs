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

        // Oyuncunun toplam top hakkı
        [SerializeField] private int initialBallCount = 200;

        // Başta pool'a eklenecek top sayısı (aynı anda sahnede olabilecek max civarı)
        [SerializeField] private int poolSize = 50;


        [Header("Spawn Settings")]
        [SerializeField] private float spawnForce = 2f;
        [SerializeField] private float spawnInterval = 0.1f;

        private Vector2 spawnMin;
        private Vector2 spawnMax;

        #endregion

        private Queue<PlinkoBall> ballPool = new Queue<PlinkoBall>();
        private int availableBalls;

        private Coroutine spawnRoutine;

        private void Start()
        {
            CreatePool();
            // Oyuncunun başlangıç hakkını UI'a bildir
            GameEvents.TriggerBallCountChanged(availableBalls);
        }

        #region Pool

        private void CreatePool()
        {
            // Oyuncunun hakları
            availableBalls = initialBallCount;

            int count = Mathf.Max(poolSize, 1);

            for (int i = 0; i < count; i++)
            {
                PlinkoBall ball = Instantiate(ballPrefab, ballPoolParent).GetComponent<PlinkoBall>();
                ball.gameObject.SetActive(false);
                ballPool.Enqueue(ball);
            }

            Debug.Log($"[BallManager] Created pool with {count} pooled balls. Player balls: {availableBalls}");
        }

        private PlinkoBall GetBallFromPool()
        {
            if (ballPool.Count > 0)
                return ballPool.Dequeue();

            // Havuz boşsa ekstra top yarat (nadir bir durum olmalı)
            Debug.LogWarning("[BallManager] Pool empty, instantiating extra ball.");
            PlinkoBall newBall = Instantiate(ballPrefab, ballPoolParent).GetComponent<PlinkoBall>();
            newBall.gameObject.SetActive(false);
            return newBall;
        }

        public void ReturnBall(PlinkoBall ball)
        {
            ball.gameObject.SetActive(false);
            ball.transform.rotation = Quaternion.identity;
            ball.rb.velocity = Vector2.zero;
            ball.rb.angularVelocity = 0f;

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

            // Random yatay kuvvet
            float randomForceX = Random.Range(-sideForce, sideForce);
            Vector2 force = new Vector2(randomForceX, -spawnForce);
            ball.rb.AddForce(force, ForceMode2D.Impulse);

            availableBalls--;
            GameEvents.TriggerBallCountChanged(availableBalls);
        }

        #endregion

        #region Event Handling

        private void OnEnable()
        {
            GameEvents.OnHoldStart += StartSpawning;
            GameEvents.OnHoldEnd += StopSpawning;
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

        private float spawnYOffset = 5f;

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

            // Y: en üst peg satırının üstüne offset
            float y = topRow[0].y + spawnYOffset;

            spawnMin = new Vector2(minX, y);
            spawnMax = new Vector2(maxX, y);

            Debug.Log($"[BallManager] Spawn Area Updated: {spawnMin.x} → {spawnMax.x} at Y={y}");
        }

        #endregion

        #region AddBall

        public void AddBalls(int amount)
        {
            availableBalls += amount;
            GameEvents.TriggerBallCountChanged(availableBalls);
        }

        #endregion
    }
}
