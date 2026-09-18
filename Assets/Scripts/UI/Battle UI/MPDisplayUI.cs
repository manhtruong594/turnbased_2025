using UnityEngine;
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
            if (GameMediator.Instance != null)
            {
                GameMediator.Instance.OnMPChanged += HandleMPChanged;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (GameMediator.Instance != null)
            {
                GameMediator.Instance.OnMPChanged -= HandleMPChanged;
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
            BattleHUDToolkit.Instance?.SetMP(playerID, currentMP, maxMP);
        }
    }
}
