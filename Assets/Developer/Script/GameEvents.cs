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

        public static Action OnGameReset;
        public static void TriggerGameReset() => OnGameReset?.Invoke();

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

        #region Level

        public static Action<LevelData> OnLevelDataLoaded;
        public static void TriggerLevelDataLoaded(LevelData data)
            => OnLevelDataLoaded?.Invoke(data);

        #endregion

        #region Level Completion
        public static Action OnLevelCompleted;
        public static void TriggerLevelCompleted() => OnLevelCompleted?.Invoke();
        #endregion

        #region Level Started
        public static Action<int> OnLevelStarted;
        public static void TriggerLevelStarted(int level)
            => OnLevelStarted?.Invoke(level);
        #endregion

        #region GameState
        public static Action<GameState> OnGameStateChanged;

        public static void TriggerGameStateChanged(GameState state)
        {
            OnGameStateChanged?.Invoke(state);
        }
        #endregion

        public static event System.Action<int> OnBallCountRestore;
        public static void TriggerBallCountRestore(int count)
        {
            OnBallCountRestore?.Invoke(count);
        }

        #region Data History
        public static Action<List<RewardPackage>> OnHistoryRestore;

        public static void TriggerHistoryRestore(List<RewardPackage> rewards)
        {
            OnHistoryRestore?.Invoke(rewards);
        }

        public static Action OnGameStateRestored;
        public static void TriggerGameStateRestored()
        {
            OnGameStateRestored?.Invoke();
        }
        #endregion

    }
}
