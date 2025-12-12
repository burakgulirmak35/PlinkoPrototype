using UnityEngine;
using TMPro;

namespace PlinkoPrototype
{
    public class Bucket : MonoBehaviour
    {
        [Header("Sprite & UI")]
        [SerializeField] private SpriteRenderer bucketSprite;
        [SerializeField] private TextMeshProUGUI textScore;

        [Header("Edges (optional)")]
        [SerializeField] private Transform leftEdge;
        [SerializeField] private Transform rightEdge;

        [Header("Bucket Animator")]
        [SerializeField] private Animator bucketAnimator;

        public int bucketScore { get; private set; }

        [Header("Coin Text Pool")]
        [SerializeField] private CoinText coinTextPrefab;
        [SerializeField] private Transform coinTextSpawnParent;
        [SerializeField] private int coinPoolSize = 3;

        private CoinText[] coinPool;
        private int coinPoolIndex = 0;

        private void Awake()
        {
            if (coinTextSpawnParent == null)
                coinTextSpawnParent = transform;

            coinPool = new CoinText[Mathf.Max(coinPoolSize, 1)];

            for (int i = 0; i < coinPool.Length; i++)
            {
                CoinText c = Instantiate(coinTextPrefab, coinTextSpawnParent);
                c.gameObject.SetActive(false);
                coinPool[i] = c;
            }
        }

        public void SetWidth(float width = 1)
        {
            bucketSprite.transform.localScale = new Vector2(width, 1);
            leftEdge.localPosition = new Vector3(-width * 0.5f, leftEdge.localPosition.y, 0f);
            rightEdge.localPosition = new Vector3(width * 0.5f, rightEdge.localPosition.y, 0f);
        }

        public void SetScore(int score)
        {
            bucketScore = score;
            textScore.text = "x" + bucketScore;
        }

        public void SetColor(string hex)
        {
            if (bucketSprite == null) return;

            if (ColorUtility.TryParseHtmlString(hex, out Color c))
                bucketSprite.color = c;
        }

        public void Score(Vector2 pos)
        {
            if (bucketAnimator != null)
                bucketAnimator.Play("Score");

            CoinText c = coinPool[coinPoolIndex];
            if (c == null) return;

            c.gameObject.SetActive(false); // zorla reset
            coinPoolIndex = (coinPoolIndex + 1) % coinPool.Length;

            c.transform.position = pos;
            c.SetEntryText("+" + bucketScore);
            c.gameObject.SetActive(true);
        }
    }
}
