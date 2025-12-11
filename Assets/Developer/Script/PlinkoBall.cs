using UnityEngine;
using PlinkoPrototype;

namespace PlinkoPrototype
{
    public class PlinkoBall : MonoBehaviour
    {
        public Rigidbody2D rb;

        private bool hasScored; // 🆕

        private void OnEnable()
        {
            // Pool’dan geri geldiğinde sıfırla
            hasScored = false;
        }

        private Animator tempAnimator;
        private Bucket tempBucket;
        private void OnTriggerEnter2D(Collider2D collision)
        {
            // Zaten skor aldıysa hiç uğraşma
            if (hasScored)
                return;

            if (collision.CompareTag("Bucket"))
            {
                tempBucket = collision.transform.parent.GetComponent<Bucket>();
                if (tempBucket != null)
                {
                    hasScored = true;
                    GameEvents.TriggerBallScored(tempBucket.bucketScore);
                    Debug.Log($"Ball scored {tempBucket.bucketScore} points.");
                    tempBucket = null;
                }
                BallManager.Instance.ReturnBall(this);
            }
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
