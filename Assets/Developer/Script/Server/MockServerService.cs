using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace PlinkoPrototype
{
    public class MockServerService : MonoBehaviour
    {
        public static MockServerService Instance { get; private set; }

        [SerializeField] private int minLatencyMs = 80;
        [SerializeField] private int maxLatencyMs = 250;
        public int MinLatency => minLatencyMs;
        public int MaxLatency => maxLatencyMs;
        private int serverWallet;
        private readonly HashSet<int> processedBallIds = new HashSet<int>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (PlayerDataManager.Instance != null &&
                PlayerDataManager.Instance.Data != null)
            {
                serverWallet = PlayerDataManager.Instance.Data.totalMoney;
            }

            Debug.Log($"[SERVER INIT] Wallet loaded: {serverWallet}");
        }


        private Task SimulateLatency()
        {
            return Task.Delay(UnityEngine.Random.Range(minLatencyMs, maxLatencyMs));
        }

        // -------------------------------------------------
        // SESSION RESUME
        // -------------------------------------------------
        public async Task<PlayerData> GetPlayerDataAsync()
        {
            await SimulateLatency();

            var pdm = PlayerDataManager.Instance;
            if (pdm == null)
                return null;

            var data = pdm.Data;
            if (data == null)
                return null;

            if (string.IsNullOrEmpty(data.lastResetUtc))
            {
                await PerformHardResetAsync();
                return pdm.Data;
            }

            // 15 dk kontrol
            System.DateTime lastReset = System.DateTime.Parse(data.lastResetUtc);
            var diff = System.DateTime.UtcNow - lastReset;

            if (diff.TotalMinutes >= 15)
            {
                await PerformHardResetAsync();
                return pdm.Data;
            }

            // Devam session
            return data;
        }


        // -------------------------------------------------
        // GAME STATE SAVE (AUTHORITATIVE)
        // -------------------------------------------------
        public void ReportGameState(int level, int ballsRemaining, int score, int ballsScoredThisLevel)
        {
            var data = PlayerDataManager.Instance.Data;

            data.savedLevel = level;
            data.savedTotalBallsRemaining = ballsRemaining;
            data.savedRoundScore = score;
            data.savedBallsScoredThisLevel = ballsScoredThisLevel;

            PlayerDataManager.Instance.Save();
        }



        // -------------------------------------------------
        // REWARD VALIDATION (TOP BAZLI HISTORY)
        // -------------------------------------------------
        public async Task<int> ValidateRewardsAsync(List<RewardPackage> batch)
        {
            await SimulateLatency();

            var data = PlayerDataManager.Instance.Data;

            foreach (var reward in batch)
            {
                if (processedBallIds.Contains(reward.ballId))
                    continue;

                processedBallIds.Add(reward.ballId);
                data.sessionRewards.Add(reward);

                serverWallet += reward.bucketScore;
            }

            data.totalMoney = serverWallet;
            PlayerDataManager.Instance.Save();

            return serverWallet;
        }

        // -------------------------------------------------
        // HARD RESET (15 DK)
        // -------------------------------------------------
        public async Task PerformHardResetAsync()
        {
            await SimulateLatency();

            var data = PlayerDataManager.Instance.Data;

            data.savedLevel = 1;
            data.savedRoundScore = 0;
            data.savedTotalBallsRemaining = 200;
            data.savedBallsScoredThisLevel = 0;

            data.sessionRewards.Clear();
            data.lastResetUtc = DateTime.UtcNow.ToString("o");

            processedBallIds.Clear();

            PlayerDataManager.Instance.Save();
        }

        // -------------------------------------------------
        // WALLET
        // -------------------------------------------------
        public async Task<int> GetWalletAsync()
        {
            await SimulateLatency();
            return serverWallet;
        }
    }
}
