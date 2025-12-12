#if UNITY_EDITOR

using UnityEngine;
using TMPro;

namespace PlinkoPrototype
{
    public class RewardValidatorDebugHUD : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI txtDebug;

        private float lastBatchSendTime = 0f;
        private float timeSinceLastBatch => Time.time - lastBatchSendTime;

        private void OnEnable()
        {
            if (RewardValidator.Instance != null)
            {
                RewardValidator.Instance.OnBatchSent += HandleBatchSent;
            }
        }

        private void OnDisable()
        {
            if (RewardValidator.Instance != null)
            {
                RewardValidator.Instance.OnBatchSent -= HandleBatchSent;
            }
        }

        private void HandleBatchSent()
        {
            lastBatchSendTime = Time.time;
        }

        private void Update()
        {
            if (txtDebug == null)
                return;

            if (RewardValidator.Instance == null || MockServerService.Instance == null)
            {
                txtDebug.text = "RewardValidator or MockServerService not initialized.";
                return;
            }

            var rv = RewardValidator.Instance;
            var ms = MockServerService.Instance;

            txtDebug.text =
                $"<b>Reward Validator Debug</b>\n" +
                $"- Pending Rewards: {rv.PendingCount}" +
                $"- Local Wallet: {rv.LocalWallet}" +
                $"- Server Wallet: {rv.ServerWallet}" +
                $"- Last Batch: {timeSinceLastBatch:0.0}s ago" +
                $"- Latency Range: {ms.MinLatency}–{ms.MaxLatency} ms";
        }
    }
}

#endif
