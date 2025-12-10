using UnityEngine;

namespace PlinkoPrototype
{
    public class PlinkoBall : MonoBehaviour
    {
        public Rigidbody2D rb;
        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Bucket"))
            {
                BallManager.Instance.ReturnBall(this);
            }
        }
    }
}
