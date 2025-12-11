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

        #region Public Properties
        public int bucketScore { get; private set; }
        #endregion

        public void SetWidth(float width = 1)
        {
            // --- SpriteRenderer scale ayarı ---
            Vector3 spriteScale = bucketSprite.transform.localScale;
            spriteScale.x = width;
            bucketSprite.transform.localScale = spriteScale;

            // --- Sol/Sağ edge pozisyonları ---
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
    }
}

