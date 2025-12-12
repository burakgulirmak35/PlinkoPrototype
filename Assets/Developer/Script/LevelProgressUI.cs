using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlinkoPrototype
{
    public class LevelProgressUI : MonoBehaviour
    {
        [SerializeField] private Slider progressSlider;
        [SerializeField] private TextMeshProUGUI txtProgress;

        private int ballsScored = 0;
        private int ballsRequired = 1;

        private void OnEnable()
        {
            GameEvents.OnLevelStarted += HandleLevelStarted;
            GameEvents.OnBallScored += HandleBallScored;
        }

        private void OnDisable()
        {
            GameEvents.OnLevelStarted -= HandleLevelStarted;
            GameEvents.OnBallScored -= HandleBallScored;
        }

        private void HandleLevelStarted(int level)
        {
            ballsScored = 0;
            ballsRequired = GameManager.Instance.GetBallsRequiredForLevel();

            UpdateUI();
        }

        private void HandleBallScored(int amount)
        {
            ballsScored++;
            UpdateUI();
        }

        private void UpdateUI()
        {
            float progress = (float)ballsScored / ballsRequired;
            progressSlider.value = Mathf.Clamp01(progress);

            txtProgress.text = $"{ballsScored} / {ballsRequired}";
        }
    }
}
