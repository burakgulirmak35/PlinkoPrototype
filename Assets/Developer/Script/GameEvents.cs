using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlinkoPrototype
{
    public static class GameEvents
    {
        public static Action OnTapStart;
        public static Action OnTapEnd;
        public static Action OnHoldStart;
        public static Action OnHoldEnd;

        public static Action<List<Vector2>> OnLevelChanged;


        public static void TriggerTapStart() => OnTapStart?.Invoke();
        public static void TriggerTapEnd() => OnTapEnd?.Invoke();
        public static void TriggerHoldStart() => OnHoldStart?.Invoke();
        public static void TriggerHoldEnd() => OnHoldEnd?.Invoke();

        public static void TriggerLevelChanged(List<Vector2> topRow)
            => OnLevelChanged?.Invoke(topRow);

        #region Score
        public static Action<int> OnBallScored;
        public static void TriggerBallScored(int score)
            => OnBallScored?.Invoke(score);
        #endregion

        #region Ball Count
        public static event Action<int> OnBallCountChanged;
        public static void TriggerBallCountChanged(int count)
        {
            OnBallCountChanged?.Invoke(count);
        }
        #endregion
    }
}
