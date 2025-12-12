using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlinkoPrototype
{
    /// <summary>
    /// Client-side reward toplama ve server'a batch halinde gönderme yöneticisi.
    /// Case gereksinimindeki: güvenli, performanslı, batch-based economy yapısı.
    /// </summary>
    public class RewardValidator : MonoBehaviour
    {
        #region Singleton
        public static RewardValidator Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        #endregion

        [Header("Batch Settings")]
        [Tooltip("Bu sayıya ulaşıldığında batch otomatik olarak server'a gönderilir.")]
        [SerializeField] private int maxItemsPerBatch = 20;

        [Tooltip("En fazla bu kadar saniyede bir batch gönderilir.")]
        [SerializeField] private float maxBatchInterval = 2f;

        [Tooltip("Level bitişinde pending batch otomatik gönderilsin mi?")]
        [SerializeField] private bool autoFlushOnLevelEnd = true;

        /// <summary>
        /// UI'da anlık gösterilen local cüzdan (optimistic).
        /// </summary>
        public int LocalWallet { get; private set; }

        /// <summary>
        /// Server tarafından doğrulanmış gerçek cüzdan
        /// </summary>
        public int ServerWallet { get; private set; }

        /// <summary>
        /// Wallet güncellenince tetiklenir (GameManager UI için)
        /// </summary>
        public event Action<int> OnWalletUpdated;

        /// <summary>
        /// Debug HUD için batch gönderimi event'i
        /// </summary>
        public event Action OnBatchSent;

        /// <summary>
        /// HUD için pending reward sayısı
        /// </summary>
        public int PendingCount => pendingRewards.Count;

        private readonly List<RewardPackage> pendingRewards = new List<RewardPackage>();
        private bool isSending = false;
        private float timeSinceLastSend = 0f;

        private void Start()
        {
            StartCoroutine(BatchTimerRoutine());
            StartCoroutine(InitializeWalletFromServer());
        }

        // ---------------------------------------------------------
        // INITIAL SYNC
        // ---------------------------------------------------------
        private IEnumerator InitializeWalletFromServer()
        {
            while (MockServerService.Instance == null)
                yield return null;

            var task = MockServerService.Instance.GetWalletAsync();

            while (!task.IsCompleted)
                yield return null;

            if (task.Exception == null)
            {
                ServerWallet = task.Result;
                LocalWallet = ServerWallet;
                OnWalletUpdated?.Invoke(LocalWallet);
            }
        }

        // ---------------------------------------------------------
        // BATCH TIMER
        // ---------------------------------------------------------
        private IEnumerator BatchTimerRoutine()
        {
            while (true)
            {
                timeSinceLastSend += Time.unscaledDeltaTime;

                if (timeSinceLastSend >= maxBatchInterval)
                {
                    FlushPendingRewards();
                }

                yield return null;
            }
        }

        // ---------------------------------------------------------
        // REGISTER REWARD
        // ---------------------------------------------------------
        public void RegisterReward(int bucketScore, string bucketId = null, int ballId = 0)
        {
            if (bucketScore <= 0)
                return;

            RewardPackage package = new RewardPackage(bucketScore, bucketId, ballId);

            pendingRewards.Add(package);

            // Optimistic UI update
            LocalWallet += bucketScore;
            OnWalletUpdated?.Invoke(LocalWallet);

            if (pendingRewards.Count >= maxItemsPerBatch)
                FlushPendingRewards();
        }

        // ---------------------------------------------------------
        // FLUSH → SEND TO SERVER
        // ---------------------------------------------------------
        public void FlushPendingRewards()
        {
            if (isSending)
                return;

            if (pendingRewards.Count == 0)
            {
                timeSinceLastSend = 0f;
                return;
            }

            StartCoroutine(SendBatchRoutine());
        }

        private IEnumerator SendBatchRoutine()
        {
            isSending = true;

            // batch'i kopyala ve pending'i temizle
            List<RewardPackage> batch = new List<RewardPackage>(pendingRewards);
            pendingRewards.Clear();

            timeSinceLastSend = 0f;

            while (MockServerService.Instance == null)
                yield return null;

            var task = MockServerService.Instance.ValidateRewardsAsync(batch);

            while (!task.IsCompleted)
                yield return null;

            if (task.Exception == null)
            {
                ServerWallet = task.Result;
                LocalWallet = ServerWallet;
                OnWalletUpdated?.Invoke(LocalWallet);

                // Debug HUD event
                OnBatchSent?.Invoke();
            }
            else
            {
                Debug.LogWarning("[RewardValidator] Server validation error → " + task.Exception);
            }

            isSending = false;
        }

        // ---------------------------------------------------------
        // LEVEL END → AUTO FLUSH
        // ---------------------------------------------------------
        public void OnLevelCompleted()
        {
            if (autoFlushOnLevelEnd)
                FlushPendingRewards();
        }

        public void SyncWalletFromServer(int wallet)
        {
            ServerWallet = wallet;
            LocalWallet = wallet;
            OnWalletUpdated?.Invoke(wallet);
        }
    }
}
