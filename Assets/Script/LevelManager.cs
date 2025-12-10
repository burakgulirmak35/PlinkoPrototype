using System.Collections.Generic;
using UnityEngine;

namespace PlinkoPrototype
{
    public class LevelManager : MonoBehaviour
    {
        [Header("Walls")]
        [SerializeField] private Transform leftWall;
        [SerializeField] private Transform rightWall;
        [SerializeField] private Transform bottomWall;
        [SerializeField] private float wallThickness = 1f;
        [SerializeField] private float bottomThickness = 1f;

        [Header("Slots")]
        [SerializeField] private int slotCount = 5;
        [SerializeField] private GameObject slotDividerPrefab;
        [SerializeField] private Transform slotParent;
        [SerializeField] private List<Transform> slotDividers = new List<Transform>();

        [Header("Pegs / Engeller")]
        [SerializeField] private GameObject pegPrefab;
        [SerializeField] private Transform pegParent;
        [SerializeField] private int rows = 5;               // Üst üste kaç sıra
        [SerializeField] private int pegsPerRow = 4;         // Her sırada kaç tane
        [SerializeField] private float yOffsetFromBottom = 1f; // Bottom'dan başlangıç yüksekliği
        [SerializeField] private float ySpacing = 0.5f;      // Y eksenindeki sıra aralığı
        [SerializeField] private List<Transform> pegs = new List<Transform>();

        private void Start()
        {
            PositionWalls();
            RegenerateSlots();
            GeneratePegs();
        }

        #region Wall Positioning
        void PositionWalls()
        {
            Camera cam = Camera.main;

            Vector3 bottomLeft = cam.ScreenToWorldPoint(new Vector3(0, 0, 0));
            Vector3 topRight = cam.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, 0));

            float leftX = bottomLeft.x;
            float rightX = topRight.x;
            float topY = topRight.y;
            float bottomY = bottomLeft.y;

            // Sol duvar
            leftWall.position = new Vector3(leftX + wallThickness / 2f, 0f, 0f);
            leftWall.localScale = new Vector3(wallThickness, topY - bottomY, 1f);

            // Sağ duvar
            rightWall.position = new Vector3(rightX - wallThickness / 2f, 0f, 0f);
            rightWall.localScale = new Vector3(wallThickness, topY - bottomY, 1f);

            // Alt zemin
            float bottomWidth = (rightWall.position.x - wallThickness / 2f) - (leftWall.position.x + wallThickness / 2f);
            bottomWall.position = new Vector3(0f, bottomY + bottomThickness / 2f, 0f);
            bottomWall.localScale = new Vector3(bottomWidth, bottomThickness, 1f);
        }
        #endregion

        #region Slot Generation
        public void RegenerateSlots()
        {
            // Temizle
            foreach (var d in slotDividers)
                if (d != null) Destroy(d.gameObject);
            slotDividers.Clear();

            // Kaç divider
            int dividerNeeded = slotCount - 1;

            // Oluştur
            for (int i = 1; i < slotCount; i++)
            {
                float leftEdge = leftWall.position.x + wallThickness / 2f;
                float rightEdge = rightWall.position.x - wallThickness / 2f;
                float slotWidth = (rightEdge - leftEdge) / slotCount;

                float x = leftEdge + slotWidth * i;
                Vector3 pos = new Vector3(x, bottomWall.position.y + bottomThickness, 0f);

                GameObject obj = Instantiate(slotDividerPrefab, pos, Quaternion.identity, slotParent);
                slotDividers.Add(obj.transform);
            }
        }
        #endregion

        #region Peg Generation
        public void GeneratePegs()
        {
            float leftEdge = leftWall.position.x + wallThickness / 2f;
            float rightEdge = rightWall.position.x - wallThickness / 2f;
            float totalWidth = rightEdge - leftEdge;

            // Toplam peg sayısını tahmini olarak hesaplamak için
            int totalNeeded = 0;
            for (int r = 0; r < rows; r++)
                totalNeeded += (r % 2 == 0) ? pegsPerRow : pegsPerRow + 1;

            // --- Fazla peg varsa sil ---
            if (pegs.Count > totalNeeded)
            {
                for (int i = totalNeeded; i < pegs.Count; i++)
                    if (pegs[i] != null)
                        Destroy(pegs[i].gameObject);

                pegs.RemoveRange(totalNeeded, pegs.Count - totalNeeded);
            }

            // --- Eksik peg varsa oluştur ---
            while (pegs.Count < totalNeeded)
            {
                GameObject peg = Instantiate(pegPrefab, pegParent);
                pegs.Add(peg.transform);
            }

            // --- Pegleri pozisyonlandır ---
            int index = 0;
            for (int row = 0; row < rows; row++)
            {
                float y = bottomWall.position.y + yOffsetFromBottom + row * ySpacing;

                int pegsThisRow = (row % 2 == 0) ? pegsPerRow : pegsPerRow + 1;
                float xSpacing = totalWidth / (pegsThisRow + 1);

                for (int col = 0; col < pegsThisRow; col++)
                {
                    float x = leftEdge + xSpacing * (col + 1);
                    pegs[index].position = new Vector3(x, y, 0f);
                    index++;
                }
            }

            Debug.Log($"[LevelManager] Pegs generated with zigzag: {totalNeeded}");
        }

        #endregion

    }
}
