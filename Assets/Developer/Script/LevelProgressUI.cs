using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlinkoPrototype
{
    public class LevelProgressUI : MonoBehaviour
    {
        [SerializeField] private Slider progressSlider;
        [SerializeField] private TextMeshProUGUI txtProgress;
        [SerializeField] private GameObject fxLevelUp;

        private int ballsScored = 0;
        private int ballsRequired = 1;

        private void OnEnable()
        {
            GameEvents.OnLevelStarted += HandleLevelStarted;
            GameEvents.OnBallScored += HandleBallScored;
            GameEvents.OnLevelCompleted += HandleLevelCompleted;
        }

        private void OnDisable()
        {
            GameEvents.OnLevelStarted -= HandleLevelStarted;
            GameEvents.OnBallScored -= HandleBallScored;
            GameEvents.OnLevelCompleted -= HandleLevelCompleted;
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

        private void HandleLevelCompleted()
        {
            StartCoroutine(ShowLevelUpEffect());
        }

        private IEnumerator ShowLevelUpEffect()
        {
            fxLevelUp.SetActive(true);
            yield return new WaitForSeconds(2f);
            fxLevelUp.SetActive(false);
        }

        private void UpdateUI()
        {
            if (ballsScored > ballsRequired) ballsScored = ballsRequired;
            float progress = (float)ballsScored / (float)ballsRequired;
            progressSlider.value = progress;
            txtProgress.text = $"{ballsScored} / {ballsRequired}";
        }
    }
}
