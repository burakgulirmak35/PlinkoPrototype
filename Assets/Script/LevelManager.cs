using UnityEngine;

namespace PlinkoPrototype
{
    public class LevelManager : MonoBehaviour
    {
        [Header("Bottom Slot Settings")]
        [SerializeField] private Transform bottomWall;
        [SerializeField] private GameObject slotDividerPrefab;
        [SerializeField] private int slotCount = 5;
        [SerializeField] private float dividerOffsetY = 0f;

        private Transform slotParent;

        private void Start()
        {
            GenerateSlots();
        }

        private void GenerateSlots()
        {
            if (slotParent != null)
                Destroy(slotParent.gameObject);

            slotParent = new GameObject("GeneratedSlots").transform;
            slotParent.SetParent(this.transform);

            // BottomWall genişliği
            BoxCollider2D col = bottomWall.GetComponent<BoxCollider2D>();
            float width = col.size.x * bottomWall.localScale.x;

            // Slot hesaplama
            float leftEdge = bottomWall.position.x - width / 2f;
            float slotWidth = width / slotCount;

            // Divider oluşturma
            for (int i = 1; i < slotCount; i++)
            {
                float x = leftEdge + slotWidth * i;

                Vector3 pos = new Vector3(x, bottomWall.position.y + dividerOffsetY, 0);
                Instantiate(slotDividerPrefab, pos, Quaternion.identity, slotParent);
            }

            Debug.Log($"[LevelManager] {slotCount} slot oluşturuldu.");
        }

        // İstersen Inspector'dan çağırılabilir
        public void Regenerate()
        {
            GenerateSlots();
        }
    }
}
