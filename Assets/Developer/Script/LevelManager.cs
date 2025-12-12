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
        [SerializeField] private Transform bucketParent;
        [SerializeField] private Transform pegParent;

        [Header("Bucket Settings")]
        [SerializeField] private Bucket bucketPrefab;
        [SerializeField] private float bucketHeight = 1f;
        [SerializeField] private List<Bucket> bucketList = new List<Bucket>();

        [Header("Peg Settings")]
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

        private LevelData currentLevelData;
        private int bucketCount;

        #region Events
        private void OnEnable()
        {
            GameEvents.OnLevelDataLoaded += ApplyLevelData;
        }

        private void OnDisable()
        {
            GameEvents.OnLevelDataLoaded -= ApplyLevelData;
        }
        #endregion

        // ------------------------------------------------
        // LEVEL DATA
        // ------------------------------------------------
        private void ApplyLevelData(LevelData data)
        {
            currentLevelData = data;

            if (data.buckets == null || data.buckets.Count == 0)
            {
                Debug.LogError("[LevelManager] LevelData has no bucket definitions!");
                return;
            }

            bucketCount = data.buckets.Count;
            CreateLevel();
        }

        // ------------------------------------------------
        // LEVEL CREATION
        // ------------------------------------------------
        private void CreateLevel()
        {
            screenBottomY = cam.ScreenToWorldPoint(
                new Vector3(0f, 0f, -cam.transform.position.z)
            ).y;

            ClearPreviousObjects();
            GenerateBuckets();
            GeneratePegs();
            NotifyLevelChanged();
        }

        // ------------------------------------------------
        // CLEAR
        // ------------------------------------------------
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

        // ------------------------------------------------
        // BUCKETS
        // ------------------------------------------------
        private void GenerateBuckets()
        {
            float leftX = cam.ViewportToWorldPoint(new Vector3(0f, 0f, 0f)).x;
            float rightX = cam.ViewportToWorldPoint(new Vector3(1f, 0f, 0f)).x;
            float width = rightX - leftX;
            float bucketWidth = width / bucketCount;

            float bottomY = screenBottomY + levelVerticalOffset;

            for (int i = 0; i < bucketCount; i++)
            {
                float centerX = leftX + bucketWidth * (i + 0.5f);
                bucketCenters.Add(centerX);

                Bucket bucketComp = Instantiate(bucketPrefab, bucketParent);
                bucketComp.transform.position = new Vector3(centerX, bottomY, 0f);
                bucketComp.SetWidth(bucketWidth);

                var bucketData = currentLevelData.buckets[i];
                bucketComp.SetScore(bucketData.score);
                bucketComp.SetColor(bucketData.color);

                bucketComp.gameObject.SetActive(true);
                bucketList.Add(bucketComp);
            }
        }

        // ------------------------------------------------
        // PEGS
        // ------------------------------------------------
        private void GeneratePegs()
        {
            float firstRowY =
                screenBottomY
                + levelVerticalOffset
                + bucketHeight
                + pegStartOffsetY;

            pegRows.Clear();

            for (int row = 0; row < pegRowsCount; row++)
            {
                float y = firstRowY + row * pegVerticalSpacing;
                List<Vector2> rowList = new List<Vector2>();

                if (row % 2 == 0)
                {
                    for (int i = 0; i < bucketCenters.Count; i++)
                        rowList.Add(new Vector2(bucketCenters[i], y));
                }
                else
                {
                    for (int i = 0; i < bucketCenters.Count - 1; i++)
                    {
                        float mid = (bucketCenters[i] + bucketCenters[i + 1]) * 0.5f;
                        rowList.Add(new Vector2(mid, y));
                    }
                }

                pegRows.Add(rowList);
            }

            foreach (var row in pegRows)
            {
                foreach (var pos in row)
                {
                    GameObject peg = Instantiate(pegPrefab, pos, Quaternion.identity, pegParent);
                    peg.gameObject.SetActive(true);
                    pegList.Add(peg.transform);
                }
            }
        }

        // ------------------------------------------------
        // NOTIFY
        // ------------------------------------------------
        private void NotifyLevelChanged()
        {
            if (pegRows.Count == 0)
                return;

            var topRow = pegRows[pegRows.Count - 1];
            GameEvents.TriggerLevelChanged(topRow);
        }

        // ------------------------------------------------
        // SPAWN ACCESSOR
        // ------------------------------------------------
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
    }
}
