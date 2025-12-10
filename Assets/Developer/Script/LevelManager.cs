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
        }
        #endregion

        [Header("Parents")]
        [SerializeField] private Transform environmentParent; // Pivotu bottomWall ortasında
        [SerializeField] private Transform bucketParent;
        [SerializeField] private Transform pegParent;

        [Header("Bucket Settings")]
        [SerializeField] private int bucketCount = 5;
        [SerializeField] private GameObject bucketPrefab;
        [SerializeField] private float bucketHeight = 0.5f;
        [SerializeField] private List<Bucket> bucketList = new List<Bucket>();

        [Header("Peg Settings")]
        [SerializeField] private GameObject pegPrefab;
        [SerializeField] private float pegVerticalSpacing = 1f;
        [SerializeField] private float pegRadius = 0.12f;
        [SerializeField] private List<Transform> pegList = new List<Transform>();

        [Header("Spawn Settings")]
        [SerializeField] private float spawnHeightOffset = 1f;

        private Camera cam;

        private readonly List<float> bucketEdges = new List<float>();
        private readonly List<List<Vector2>> pegRows = new List<List<Vector2>>();

        /// <summary>
        /// Level oluşturulur (GameManager burayı çağıracak)
        /// </summary>
        public void CreateLevel(int levelIndex = 0)
        {
            ClearPreviousObjects();
            GenerateBuckets();
            GeneratePegs();
            NotifyLevelChanged();
        }

        #region Clear
        private void ClearPreviousObjects()
        {
            foreach (Transform t in bucketParent)
                Destroy(t.gameObject);

            foreach (Transform t in pegParent)
                Destroy(t.gameObject);

            bucketEdges.Clear();
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

            // Bucket’ların oturacağı Y (pivot alt-orta)
            float bottomY = environmentParent.position.y;

            bucketEdges.Clear();

            // Kenar noktaları (bucketCount + 1)
            for (int i = 0; i <= bucketCount; i++)
                bucketEdges.Add(leftX + bucketWidth * i);

            // Bucketları oluştur
            for (int i = 0; i < bucketCount; i++)
            {
                float centerX = bucketEdges[i] + bucketWidth * 0.5f;

                GameObject bucketObj = Instantiate(bucketPrefab, bucketParent);
                bucketObj.transform.position = new Vector3(centerX, bottomY, 0f); // pivot bottom-center

                var bucketComp = bucketObj.GetComponent<Bucket>();
                if (bucketComp != null)
                {
                    // Genişliği Bucket scripti ayarlayacak (9-sliced Sprite + left/right walls vs.)
                    bucketComp.SetWidth(bucketWidth);

                    // Şimdilik skor 0, ileride level datasından besleyebilirsin
                    bucketComp.SetScore(0);

                    bucketList.Add(bucketComp);
                }
            }
        }
        #endregion

        #region Pegs
        private void GeneratePegs()
        {
            // Peglerin başlayacağı Y: bucketların biraz üstü
            float bottomY = environmentParent.position.y + bucketHeight;

            pegRows.Clear();

            // Alt satır: bucketEdges kadar peg (köşeler)
            List<Vector2> firstRow = new List<Vector2>();
            for (int i = 0; i < bucketEdges.Count; i++)
            {
                float x = bucketEdges[i];
                firstRow.Add(new Vector2(x, bottomY));
            }
            pegRows.Add(firstRow);

            // Örnek: 6 edge → satırlar: 6,5,4,3 peg için totalRows = 4
            int totalRows = bucketEdges.Count - 2;

            for (int r = 1; r <= totalRows; r++)
            {
                List<Vector2> prevRow = pegRows[r - 1];
                int prevCount = prevRow.Count;
                int pegCount = prevCount - 1; // her üst satırda 1 eksik

                List<Vector2> newRow = new List<Vector2>();

                for (int i = 0; i < pegCount; i++)
                {
                    float xLeft = prevRow[i].x;
                    float xRight = prevRow[i + 1].x;
                    float midX = (xLeft + xRight) * 0.5f;
                    float y = bottomY + r * pegVerticalSpacing;

                    newRow.Add(new Vector2(midX, y));
                }

                pegRows.Add(newRow);
            }

            // Peg prefabları spawn
            foreach (var row in pegRows)
            {
                foreach (var pos in row)
                {
                    GameObject peg = Instantiate(pegPrefab, pegParent);
                    peg.transform.position = pos;
                    peg.transform.localScale = Vector3.one * pegRadius * 2f;
                    pegList.Add(peg.transform);
                }
            }
        }
        #endregion

        #region Events
        private void NotifyLevelChanged()
        {
            if (pegRows.Count == 0) return;

            // En üst satır (ör: 3 peg)
            var topRow = pegRows[pegRows.Count - 1];
            GameEvents.TriggerLevelChanged(topRow);
        }
        #endregion

        #region Spawn Accessor
        public Vector2 GetRandomSpawnPosition()
        {
            if (pegRows.Count == 0) return Vector2.zero;

            var topRow = pegRows[pegRows.Count - 1];

            float minX = topRow[0].x;
            float maxX = topRow[topRow.Count - 1].x;
            float y = topRow[0].y + spawnHeightOffset;

            float randomX = Random.Range(minX, maxX);
            return new Vector2(randomX, y);
        }
        #endregion
    }
}
