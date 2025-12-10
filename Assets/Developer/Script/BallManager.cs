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
        [SerializeField] private Transform spawnPoint; // Eskiden sabit nokta
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

        /// <summary>
        /// LevelManager’dan gelen en üst satır peglerine göre spawn alanını ayarlar
        /// </summary>
        /// <param name="topRow">En üst satır peg pozisyonları</param>
        private void UpdateSpawnAreaFromLevel(List<Vector2> topRow)
        {
            if (topRow == null || topRow.Count < 2)
            {
                Debug.LogWarning("[BallManager] Top row invalid for spawn area.");
                return;
            }

            // En soldaki ve sağdaki pegler arası
            spawnMin = new Vector2(topRow[0].x, topRow[0].y);
            spawnMax = new Vector2(topRow[topRow.Count - 1].x, topRow[0].y); // Y sabit

            Debug.Log($"[BallManager] Spawn Area Updated: {spawnMin.x} → {spawnMax.x} at Y={spawnMin.y}");
        }

        #endregion
    }
}
