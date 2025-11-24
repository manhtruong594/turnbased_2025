using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TurnBasedGame.Core;
using TurnBasedGame.Resources;

namespace TurnBasedGame.UI
{
    /// <summary>
    /// Hiển thị thanh MP cho người chơi
    /// Có thể dùng cho cả Player 1 và Player 2
    /// </summary>
    public class MPDisplayUI : MonoBehaviour
    {
        [Header("Player Assignment")]
        [SerializeField] private PlayerID playerID;

        [Header("UI Components")]
        [SerializeField] private Slider mpSlider;
        [SerializeField] private TextMeshProUGUI mpText;
        [SerializeField] private Image fillImage;

        [Header("Visual Settings")]
        [SerializeField] private Color lowMPColor = Color.red;
        [SerializeField] private Color mediumMPColor = Color.yellow;
        [SerializeField] private Color highMPColor = Color.green;
        [SerializeField] private float lowMPThreshold = 0.3f;
        [SerializeField] private float mediumMPThreshold = 0.6f;

        private void Start()
        {
            SubscribeToEvents();
            InitializeDisplay();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            if (MPManager.Instance != null)
            {
                MPManager.Instance.OnMPChanged += HandleMPChanged;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (MPManager.Instance != null)
            {
                MPManager.Instance.OnMPChanged -= HandleMPChanged;
            }
        }

        /// <summary>
        /// Khởi tạo hiển thị ban đầu
        /// </summary>
        private void InitializeDisplay()
        {
            if (MPManager.Instance != null)
            {
                int currentMP = MPManager.Instance.GetCurrentMP(playerID);
                int maxMP = MPManager.Instance.MaxMP;
                UpdateDisplay(currentMP, maxMP);
            }
        }

        /// <summary>
        /// Xử lý khi MP thay đổi
        /// </summary>
        private void HandleMPChanged(PlayerID player, int currentMP, int maxMP)
        {
            if (player != playerID) return;
            UpdateDisplay(currentMP, maxMP);
        }

        /// <summary>
        /// Cập nhật hiển thị UI
        /// </summary>
        private void UpdateDisplay(int currentMP, int maxMP)
        {
            // Cập nhật slider
            if (mpSlider)
            {
                mpSlider.maxValue = maxMP;
                mpSlider.value = currentMP;
            }

            // Cập nhật text
            if (mpText)
            {
                mpText.text = $"{currentMP}/{maxMP}";
            }

            // Cập nhật màu sắc dựa trên phần trăm MP
            UpdateFillColor(currentMP, maxMP);
        }

        /// <summary>
        /// Cập nhật màu của thanh MP dựa trên % còn lại
        /// </summary>
        private void UpdateFillColor(int currentMP, int maxMP)
        {
            if (fillImage == null) return;

            float percentage = (float)currentMP / maxMP;

            if (percentage <= lowMPThreshold)
            {
                fillImage.color = lowMPColor;
            }
            else if (percentage <= mediumMPThreshold)
            {
                fillImage.color = mediumMPColor;
            }
            else
            {
                fillImage.color = highMPColor;
            }
        }
    }
}
