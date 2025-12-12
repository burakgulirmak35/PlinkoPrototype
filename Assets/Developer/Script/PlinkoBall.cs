using UnityEngine;

namespace PlinkoPrototype
{
    public class PlinkoBall : MonoBehaviour
    {
        public Rigidbody2D rb;

        /// <summary>
        /// BallManager tarafından atanan benzersiz top kimliği.
        /// Server-side validation & analytics için kullanılır.
        /// </summary>
        public int BallId { get; set; }

        private bool hasScored;
        private Animator tempAnimator;
        private Bucket tempBucket;

        private void OnEnable()
        {
            // Pool’dan geri geldiğinde sıfırlansın
            hasScored = false;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (hasScored)
                return;

            // -------------------------------
            // BUCKET → SCORE + REWARD SYSTEM
            // -------------------------------
            if (collision.CompareTag("Bucket"))
            {
                tempBucket = collision.transform.parent.GetComponent<Bucket>();

                if (tempBucket != null)
                {
                    tempBucket.Score(transform.position);
                    hasScored = true;

                    int score = tempBucket.bucketScore;
                    string bucketId = tempBucket.name;

                    // 1) UI History için event
                    GameEvents.TriggerBallScored(score);

                    // 2) Server batch için reward kaydı
                    RewardValidator.Instance.RegisterReward(score, bucketId, BallId);

                    Debug.Log($"Ball {BallId} scored {score} points in bucket {bucketId}.");

                    tempBucket = null;
                }

                BallManager.Instance.ReturnBall(this);
            }

            // -------------------------------
            // PEG → Hit animasyonu
            // -------------------------------
            else if (collision.CompareTag("Peg"))
            {
                tempAnimator = collision.GetComponent<Animator>();
                if (tempAnimator != null)
                {
                    tempAnimator.Play("Hit");
                    tempAnimator = null;
                }
            }
        }
    }
}
