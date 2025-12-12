using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlinkoPrototype
{
    public class HistoryUIController : MonoBehaviour
    {
        [Header("History Layout")]
        [SerializeField] private RectTransform historyContainer;
        [SerializeField] private HistoryEntry historyEntryPrefab;
        [SerializeField] private HorizontalLayoutGroup layoutGroup;

        [Header("Prefab Settings")]
        [SerializeField] private float prefabWidth = 75f;

        private readonly List<HistoryEntry> activeEntries = new List<HistoryEntry>();
        private readonly Queue<HistoryEntry> pool = new Queue<HistoryEntry>();

        private int maxVisible = 0;
        private int poolSize = 0;

        // ------------------------------------------------------
        // UNITY
        // ------------------------------------------------------
        private void Start()
        {
            if (layoutGroup == null)
                layoutGroup = historyContainer.GetComponent<HorizontalLayoutGroup>();

            CalculatePoolSize();
            CreatePool();
        }

        private void OnEnable()
        {
            // 🔥 SADECE GAMEPLAY
            GameEvents.OnBallScored += HandleBallScored;

            // 🔥 SADECE RESTORE
            GameEvents.OnHistoryRestore += RebuildHistory;
        }

        private void OnDisable()
        {
            GameEvents.OnBallScored -= HandleBallScored;
            GameEvents.OnHistoryRestore -= RebuildHistory;
        }

        // ------------------------------------------------------
        // RESTORE
        // ------------------------------------------------------
        private void RebuildHistory(List<RewardPackage> rewards)
        {
            ClearAll();

            if (rewards == null)
                return;

            foreach (var reward in rewards)
            {
                AddEntry(reward.bucketScore);
            }
        }

        // ------------------------------------------------------
        // GAMEPLAY
        // ------------------------------------------------------
        private void HandleBallScored(int amount)
        {
            AddEntry(amount);
        }

        // ------------------------------------------------------
        // CORE ADD LOGIC (FIFO ROTATION)
        // ------------------------------------------------------
        private void AddEntry(int amount)
        {
            HistoryEntry entry;

            // 1) Yer varsa → yeni entry
            if (activeEntries.Count < maxVisible)
            {
                entry = pool.Dequeue();
                activeEntries.Add(entry);
            }
            else
            {
                // 2) FIFO rotation
                entry = activeEntries[0];
                activeEntries.RemoveAt(0);

                entry.gameObject.SetActive(false);
                activeEntries.Add(entry);
            }

            entry.SetEntryText($"+{amount}");
            entry.transform.SetAsLastSibling();
            entry.gameObject.SetActive(true);
        }

        // ------------------------------------------------------
        // CLEAR
        // ------------------------------------------------------
        private void ClearAll()
        {
            foreach (var entry in activeEntries)
            {
                entry.gameObject.SetActive(false);
                pool.Enqueue(entry);
            }

            activeEntries.Clear();
        }

        // ------------------------------------------------------
        // POOL
        // ------------------------------------------------------
        private void CalculatePoolSize()
        {
            float containerWidth = historyContainer.rect.width;
            float leftPadding = layoutGroup.padding.left;
            float rightPadding = layoutGroup.padding.right;
            float spacing = layoutGroup.spacing;

            float availableWidth = containerWidth - (leftPadding + rightPadding);

            maxVisible = Mathf.FloorToInt((availableWidth + spacing) / (prefabWidth + spacing));
            poolSize = Mathf.Max(2, maxVisible + 1); // güvenlik payı
        }

        private void CreatePool()
        {
            for (int i = 0; i < poolSize; i++)
            {
                HistoryEntry entry = Instantiate(historyEntryPrefab, historyContainer);
                entry.SetEntryText("");
                entry.gameObject.SetActive(false);
                pool.Enqueue(entry);
            }
        }
    }
}
