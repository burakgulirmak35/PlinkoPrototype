using UnityEngine;

namespace PlinkoPrototype
{
    public class PlinkoBall : MonoBehaviour
    {
        public Rigidbody2D rb;

        public int BallId { get; set; }

        private bool hasScored;
        private Animator tempAnimator;
        private Bucket tempBucket;

        private void OnEnable()
        {
            hasScored = false;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (hasScored)
                return;

            // BUCKET → SCORE + REWARD SYSTEM
            if (collision.CompareTag("Bucket"))
            {
                // Eski davranışı koruyoruz (bucket collider child -> parent Bucket)
                tempBucket = collision.transform.parent != null
                    ? collision.transform.parent.GetComponent<Bucket>()
                    : null;

                // Daha dayanıklı fallback (prefab yapısı değişirse bozulmasın)
                if (tempBucket == null)
                    tempBucket = collision.GetComponentInParent<Bucket>();

                if (tempBucket != null)
                {
                    tempBucket.Score(transform.position);
                    hasScored = true;

                    int score = tempBucket.bucketScore;
                    string bucketId = tempBucket.name;

                    GameEvents.TriggerBallScored(score);

                    if (RewardValidator.Instance != null)
                        RewardValidator.Instance.RegisterReward(score, bucketId, BallId);

                    tempBucket = null;
                }

                if (BallManager.Instance != null)
                    BallManager.Instance.ReturnBall(this);

                return;
            }

            // PEG → Hit animasyonu
            if (collision.CompareTag("Peg"))
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
