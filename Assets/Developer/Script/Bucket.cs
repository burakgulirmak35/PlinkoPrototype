using UnityEngine;
using TMPro;

public class Bucket : MonoBehaviour
{
    [Header("Sprite & UI")]
    [SerializeField] private SpriteRenderer bucketSprite;
    [SerializeField] private TextMeshProUGUI textScore;

    [Header("Edges (optional)")]
    [SerializeField] private Transform leftEdge;
    [SerializeField] private Transform rightEdge;

    public void SetWidth(float width = 1)
    {
        // --- SpriteRenderer scale ayarı ---
        Vector3 spriteScale = bucketSprite.transform.localScale;
        spriteScale.x = width;
        bucketSprite.transform.localScale = spriteScale;

        // --- Sol/Sağ edge pozisyonları ---
        leftEdge.localPosition = new Vector3(-width * 0.5f, leftEdge.position.y, 0f);
        rightEdge.localPosition = new Vector3(width * 0.5f, rightEdge.position.y, 0f);
    }

    public void SetScore(int score)
    {
        textScore.text = score.ToString();
    }
}
