using System.Collections.Generic;
using UnityEngine;

namespace PlinkoPrototype
{
    public class LevelManager : MonoBehaviour
    {
        #region Singleton
        public static LevelManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            cam = Camera.main;

            // 🆕 Ekranın gerçek alt sınırı
            screenBottomY = cam.ScreenToWorldPoint(new Vector3(0f, 0f, -cam.transform.position.z)).y;
        }
        #endregion

        [Header("Parents")]
        [SerializeField] private Transform environmentParent;
        [SerializeField] private Transform bucketParent;
        [SerializeField] private Transform pegParent;

        [Header("Bucket Settings")]
        [SerializeField] private int bucketCount = 5;
        [SerializeField] private Bucket bucketPrefab;
        [SerializeField] private float bucketHeight = 1f;
        [SerializeField] private List<Bucket> bucketList = new List<Bucket>();

        [Header("Peg Settings (Alternating Rows)")]
        [SerializeField] private GameObject pegPrefab;
        [SerializeField] private float pegVerticalSpacing = 2f;
        [SerializeField] private float pegStartOffsetY = 2f;
        [SerializeField] private int pegRowsCount = 5;
        [SerializeField] private float ballSpawnHeightOffset = 1f;
        [SerializeField] private List<Transform> pegList = new List<Transform>();

        [Header("Level Offset")]
        [SerializeField] private float levelVerticalOffset = 1f;

        private Camera cam;
        private float screenBottomY;

        private readonly List<float> bucketCenters = new List<float>();
        private readonly List<List<Vector2>> pegRows = new List<List<Vector2>>();

        // Level oluşturulur
        public void CreateLevel(int levelIndex = 0)
        {
            ClearPreviousObjects();
            GenerateBuckets();
            GeneratePegs();
            ApplyLevelOffset();
            NotifyLevelChanged();
        }

        #region Clear
        private void ClearPreviousObjects()
        {
            foreach (Transform t in bucketParent)
                Destroy(t.gameObject);

            foreach (Transform t in pegParent)
                Destroy(t.gameObject);

            bucketCenters.Clear();
            pegRows.Clear();
            bucketList.Clear();
            pegList.Clear();
        }
        #endregion

        #region Buckets
        private void GenerateBuckets()
        {
            float leftX = cam.ViewportToWorldPoint(new Vector3(0f, 0f, 0f)).x;
            float rightX = cam.ViewportToWorldPoint(new Vector3(1f, 0f, 0f)).x;
            float width = rightX - leftX;
            float bucketWidth = width / bucketCount;

            // 🆕 Bucket başlangıcı ekranın en altı
            float bottomY = screenBottomY;

            bucketCenters.Clear();

            for (int i = 0; i < bucketCount; i++)
            {
                float centerX = leftX + bucketWidth * (i + 0.5f);
                bucketCenters.Add(centerX);

                Bucket bucketComp = Instantiate(bucketPrefab, bucketParent);
                bucketComp.transform.position = new Vector3(centerX, bottomY, 0f);

                bucketComp.gameObject.SetActive(true);
                bucketComp.SetWidth(bucketWidth);
                bucketComp.SetScore(0);

                bucketList.Add(bucketComp);
            }
        }
        #endregion

        #region Pegs (Alternating Rows)
        private void GeneratePegs()
        {
            // 🆕 Peg başlangıcı ekranın altından yukarı doğru
            float firstRowY = screenBottomY + bucketHeight + pegStartOffsetY;

            pegRows.Clear();

            for (int row = 0; row < pegRowsCount; row++)
            {
                float y = firstRowY + row * pegVerticalSpacing;
                List<Vector2> rowList = new List<Vector2>();

                // ÇİFT SATIR → bucketCount peg (tam merkezlerde)
                if (row % 2 == 0)
                {
                    for (int i = 0; i < bucketCenters.Count; i++)
                        rowList.Add(new Vector2(bucketCenters[i], y));
                }
                else
                {
                    // TEK SATIR → bucketCount - 1 peg (iki merkez ortası)
                    for (int i = 0; i < bucketCenters.Count - 1; i++)
                    {
                        float mid = (bucketCenters[i] + bucketCenters[i + 1]) * 0.5f;
                        rowList.Add(new Vector2(mid, y));
                    }
                }

                pegRows.Add(rowList);
            }

            // Peg instantiate
            foreach (var row in pegRows)
            {
                foreach (var pos in row)
                {
                    GameObject peg = Instantiate(pegPrefab, pos, Quaternion.identity, pegParent);
                    pegList.Add(peg.transform);
                    peg.gameObject.SetActive(true);
                }
            }
        }
        #endregion

        #region Offset
        private void ApplyLevelOffset()
        {
            if (environmentParent == null)
                return;

            Vector3 pos = environmentParent.position;
            pos.y += levelVerticalOffset;
            environmentParent.position = pos;
        }
        #endregion

        #region Events
        private void NotifyLevelChanged()
        {
            if (pegRows.Count == 0)
                return;

            var topRow = pegRows[pegRows.Count - 1];
            GameEvents.TriggerLevelChanged(topRow);
        }
        #endregion

        #region Spawn Accessor
        public Vector2 GetRandomSpawnPosition()
        {
            if (pegRows.Count == 0)
                return Vector2.zero;

            var topRow = pegRows[pegRows.Count - 1];

            float minX = topRow[0].x;
            float maxX = topRow[topRow.Count - 1].x;
            float y = topRow[0].y + ballSpawnHeightOffset;

            float randomX = Random.Range(minX, maxX);
            return new Vector2(randomX, y);
        }
        #endregion
    }
}
