using System;

namespace PlinkoPrototype
{
    [Serializable]
    public class RewardPackage
    {
        /// <summary>
        /// Bu topun kazandırdığı ham skor (bucket score).
        /// </summary>
        public int bucketScore;

        /// <summary>
        /// İleride anti-cheat / analytics için kullanılabilecek, top’a özel id.
        /// Şimdilik opsiyonel, istersen daha sonra doldururuz.
        /// </summary>
        public int ballId;

        /// <summary>
        /// Hangi bucket’a düştüğünü anlamak için (UI / debug amaçlı).
        /// Örneğin: "Bucket_3" vs.
        /// </summary>
        public string bucketId;

        /// <summary>
        /// Topun düştüğü zaman (UTC, ISO 8601 formatında).
        /// </summary>
        public string timeUtc;

        public RewardPackage(int bucketScore, string bucketId = null, int ballId = 0)
        {
            this.bucketScore = bucketScore;
            this.bucketId = bucketId;
            this.ballId = ballId;
            timeUtc = DateTime.UtcNow.ToString("o");
        }
    }
}
