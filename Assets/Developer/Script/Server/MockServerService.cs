using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace PlinkoPrototype
{
    public class MockServerService : MonoBehaviour
    {
        #region Singleton
        public static MockServerService Instance { get; private set; }

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

        [Header("Latency Simulation (ms)")]
        [SerializeField] private int minLatencyMs = 80;
        [SerializeField] private int maxLatencyMs = 250;

        public int MinLatency => minLatencyMs;
        public int MaxLatency => maxLatencyMs;

        private int serverWallet;
        private bool initialized = false;

        // 🔥 Fraud detection için kullanılan set
        private readonly HashSet<int> processedBallIds = new HashSet<int>();

        private async void Start()
        {
            await InitializeServerStateFromLocal();
        }

        private Task SimulateLatency()
        {
            int delay = Random.Range(minLatencyMs, maxLatencyMs);
            return Task.Delay(delay);
        }

        private async Task InitializeServerStateFromLocal()
        {
            await SimulateLatency();

            if (PlayerDataManager.Instance != null &&
                PlayerDataManager.Instance.Data != null)
            {
                serverWallet = PlayerDataManager.Instance.Data.totalMoney;
            }

            initialized = true;
        }

        public async Task<int> GetWalletAsync()
        {
            if (!initialized)
                await InitializeServerStateFromLocal();

            await SimulateLatency();
            return serverWallet;
        }

        // ---------------------------------------------------------
        // VALIDATE BATCH
        // ---------------------------------------------------------
        public async Task<int> ValidateRewardsAsync(List<RewardPackage> batch)
        {
            if (!initialized)
                await InitializeServerStateFromLocal();

            await SimulateLatency();

            if (batch == null || batch.Count == 0)
                return serverWallet;

            int totalDelta = 0;

            foreach (var reward in batch)
            {
                if (reward == null)
                    continue;

                // -------------------------
                // FRAUD DETECTION
                // -------------------------
                AnalyzeReward(reward);

                // negative or weird scores — reject scoring (but log)
                if (reward.bucketScore <= 0 || reward.bucketScore > 10000)
                {
                    Debug.LogWarning($"[MOCK SERVER] Ignored suspicious score value: {reward.bucketScore}");
                    continue;
                }

                totalDelta += reward.bucketScore;
            }

            serverWallet += totalDelta;

            // Persist server wallet to PlayerData
            if (PlayerDataManager.Instance != null &&
                PlayerDataManager.Instance.Data != null)
            {
                PlayerDataManager.Instance.Data.totalMoney = serverWallet;
                PlayerDataManager.Instance.SavePlayerData();
            }

            return serverWallet;
        }

        // ---------------------------------------------------------
        // FRAUD DETECTION LOGIC
        // ---------------------------------------------------------
        private void AnalyzeReward(RewardPackage reward)
        {
            // 1) Duplicate BallId detection
            if (processedBallIds.Contains(reward.ballId))
            {
                Debug.LogWarning(
                    $"[MOCK SERVER] Suspicious: Duplicate BallId detected → ballId={reward.ballId}, bucket={reward.bucketId}"
                );
            }
            else
            {
                processedBallIds.Add(reward.ballId);
            }

            // 2) Missing / manipulated bucket ID
            if (string.IsNullOrEmpty(reward.bucketId))
            {
                Debug.LogWarning(
                    $"[MOCK SERVER] Suspicious: Missing bucketId on reward ballId={reward.ballId}"
                );
            }

            // 3) Abnormally high score
            if (reward.bucketScore > 1000)
            {
                Debug.LogWarning(
                    $"[MOCK SERVER] High score anomaly → {reward.bucketScore} from bucket={reward.bucketId}"
                );
            }

            // 4) Impossible time manipulation (optional future check)
            // DateTime.Parse(reward.timeUtc) … 
        }

        public async Task NotifyClientResetAsync(int sessionEarnings)
        {
            await SimulateLatency();

            // Score → Wallet merge
            if (sessionEarnings > 0)
            {
                serverWallet += sessionEarnings;
                Debug.Log($"[MOCK SERVER] Added session earnings: +{sessionEarnings}");
            }

            // Persist wallet
            if (PlayerDataManager.Instance != null &&
                PlayerDataManager.Instance.Data != null)
            {
                PlayerDataManager.Instance.Data.totalMoney = serverWallet;
                PlayerDataManager.Instance.SavePlayerData();
            }

            // Fraud reset (bir önceki adımda eklediğimiz)
            processedBallIds.Clear();
        }


        #region Subscribetion

        private void OnEnable()
        {
            GameEvents.OnGameReset += ResetFraudData;
        }
        private void OnDisable()
        {
            GameEvents.OnGameReset -= ResetFraudData;
        }

        private void ResetFraudData()
        {
            processedBallIds.Clear();
            Debug.Log("[MOCK SERVER] Fraud tracking reset.");
        }
        #endregion
    }
}
