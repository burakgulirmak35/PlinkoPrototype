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

        private void Start()
        {
            if (layoutGroup == null)
                layoutGroup = historyContainer.GetComponent<HorizontalLayoutGroup>();

            CalculatePoolSize();
            CreatePool();
        }

        private void OnEnable()
        {
            GameEvents.OnBallScored += HandleBallScored;
        }

        private void OnDisable()
        {
            GameEvents.OnBallScored -= HandleBallScored;
        }

        // ------------------------------------------------------
        // POOL CALCULATION
        // ------------------------------------------------------
        private void CalculatePoolSize()
        {
            float containerWidth = historyContainer.rect.width;
            float leftPadding = layoutGroup.padding.left;
            float rightPadding = layoutGroup.padding.right;
            float spacing = layoutGroup.spacing;

            float availableWidth = containerWidth - (leftPadding + rightPadding);

            maxVisible = Mathf.FloorToInt((availableWidth + spacing) / (prefabWidth + spacing));

            poolSize = maxVisible + 1; // +1 güvenlik
            if (poolSize < 2)
                poolSize = 2;
        }

        // ------------------------------------------------------
        // CREATE POOL
        // ------------------------------------------------------
        private void CreatePool()
        {
            for (int i = 0; i < poolSize; i++)
            {
                HistoryEntry entry = Instantiate(historyEntryPrefab, historyContainer);
                entry.SetEntryText("");
                entry.gameObject.SetActive(false); // Başlangıçta gizli
                pool.Enqueue(entry);
            }
        }

        // ------------------------------------------------------
        // ADD NEW ENTRY (FIFO ROTATION LOGIC)
        // ------------------------------------------------------
        private void HandleBallScored(int amount)
        {
            HistoryEntry entry;

            // 1) Eğer aktif entry sayısı maxVisible'dan küçükse → yeni slot aç
            if (activeEntries.Count < maxVisible)
            {
                entry = pool.Dequeue();   // Pool'dan yeni entry al
                activeEntries.Add(entry); // Aktif listeye ekle
            }
            else
            {
                // 2) Ekran doluysa → en soldaki entry disable edilir, rotation yapılır
                entry = activeEntries[0];
                activeEntries.RemoveAt(0);

                entry.gameObject.SetActive(false); // Eski görünümü temizle
                activeEntries.Add(entry);          // Sona ekle
            }

            // 3) Entry’i yeni değerle güncelle
            entry.SetEntryText($"+{amount}");

            // 4) Entry’i en sağ child haline getir (UI'da sağda görünmesi için)
            entry.transform.SetAsLastSibling();

            // 5) Görünür yap
            entry.gameObject.SetActive(true);
        }
    }
}
