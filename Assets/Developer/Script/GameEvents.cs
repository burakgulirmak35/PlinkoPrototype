using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlinkoPrototype
{
    public static class GameEvents
    {
        // -------------------------
        // TAP & HOLD EVENTS
        // -------------------------
        public static Action OnTapStart;
        public static Action OnTapEnd;
        public static Action OnHoldStart;
        public static Action OnHoldEnd;

        // -------------------------
        // LEVEL CHANGED EVENT
        // -------------------------
        // LevelManager --> BallManager
        // En üst satır peg noktalarını gönderir
        public static Action<List<Vector2>> OnLevelChanged;

        public static void TriggerLevelChanged(List<Vector2> topRow)
        {
            // Koruma: null gönderilir ise event tetiklenmesin
            if (topRow == null || topRow.Count == 0)
                return;

            // Güvenli olması için kopyasını veriyoruz
            OnLevelChanged?.Invoke(new List<Vector2>(topRow));
        }
    }
}
