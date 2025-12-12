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

        #region Score Data
        public int bucketScore { get; private set; }
        #endregion

        [Header("Coin Text Pool")]
        [SerializeField] private CoinText coinTextPrefab;
        [SerializeField] private Transform coinTextSpawnParent;
        [SerializeField] private int coinPoolSize = 3;

        private CoinText[] coinPool;
        private int coinPoolIndex = 0;

        private void Awake()
        {
            // Coin pool oluştur
            coinPool = new CoinText[coinPoolSize];

            for (int i = 0; i < coinPoolSize; i++)
            {
                CoinText c = Instantiate(coinTextPrefab, coinTextSpawnParent);
                c.gameObject.SetActive(false);
                coinPool[i] = c;
            }
        }

        // ----------------------------------------------
        // Bucket Settings
        // ----------------------------------------------
        public void SetWidth(float width = 1)
        {
            bucketSprite.size = new Vector2(width, 1);
            leftEdge.localPosition = new Vector3(-width * 0.5f, leftEdge.localPosition.y, 0f);
            rightEdge.localPosition = new Vector3(width * 0.5f, rightEdge.localPosition.y, 0f);
        }

        public void SetScore(int score)
        {
            bucketScore = score;
            textScore.text = score.ToString();
        }

        public void SetColor(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out Color c))
                bucketSprite.color = c;
        }

        public void Score(Vector2 pos)
        {
            bucketAnimator.Play("Score");
            CoinText c = coinPool[coinPoolIndex];
            c.gameObject.SetActive(false); // zorla reset
            coinPoolIndex = (coinPoolIndex + 1) % coinPoolSize;
            Vector3 spawnPos = pos;
            c.transform.position = spawnPos;
            c.SetEntryText("+" + bucketScore);
            c.gameObject.SetActive(true);
        }
    }
}
