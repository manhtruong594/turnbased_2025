using UnityEngine;
using System;
using TurnBasedGame.Core;

namespace TurnBasedGame.Resources
{
    /// <summary>
    /// Quản lý hệ thống MP (Mana Points) cho cả 2 người chơi
    /// Xử lý việc cộng/trừ MP và kiểm tra điều kiện tiêu tốn
    /// </summary>
    public class MPManager : MonoBehaviour
    {
        public static MPManager Instance { get; private set; }

        [Header("MP Settings")]
        [SerializeField] private int maxMP = 20;
        [SerializeField] private int startingMP = 0;

        // Events
        public event Action<PlayerID, int, int> OnMPChanged; // (player, currentMP, maxMP)

        // MP cho mỗi người chơi
        private int player1MP;
        private int player2MP;

        public int MaxMP => maxMP;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            InitializeMP();
        }

        /// <summary>
        /// Khởi tạo MP ban đầu cho cả 2 người chơi
        /// </summary>
        private void InitializeMP()
        {
            player1MP = startingMP;
            player2MP = startingMP;

            OnMPChanged?.Invoke(PlayerID.Player1, player1MP, maxMP);
            OnMPChanged?.Invoke(PlayerID.Player2, player2MP, maxMP);
        }

        /// <summary>
        /// Cộng MP cho người chơi
        /// </summary>
        public void AddMP(PlayerID player, int amount)
        {
            if (amount <= 0) return;

            if (player == PlayerID.Player1)
            {
                player1MP = Mathf.Min(player1MP + amount, maxMP);
                OnMPChanged?.Invoke(PlayerID.Player1, player1MP, maxMP);
                Debug.Log($"Player 1 gained {amount} MP. Current MP: {player1MP}/{maxMP}");
            }
            else
            {
                player2MP = Mathf.Min(player2MP + amount, maxMP);
                OnMPChanged?.Invoke(PlayerID.Player2, player2MP, maxMP);
                Debug.Log($"Player 2 gained {amount} MP. Current MP: {player2MP}/{maxMP}");
            }
        }

        /// <summary>
        /// Trừ MP của người chơi
        /// </summary>
        public bool SpendMP(PlayerID player, int amount)
        {
            if (amount <= 0) return true;
            if (!HasEnoughMP(player, amount)) return false;

            if (player == PlayerID.Player1)
            {
                player1MP -= amount;
                OnMPChanged?.Invoke(PlayerID.Player1, player1MP, maxMP);
                Debug.Log($"Player 1 spent {amount} MP. Current MP: {player1MP}/{maxMP}");
            }
            else
            {
                player2MP -= amount;
                OnMPChanged?.Invoke(PlayerID.Player2, player2MP, maxMP);
                Debug.Log($"Player 2 spent {amount} MP. Current MP: {player2MP}/{maxMP}");
            }

            return true;
        }

        /// <summary>
        /// Kiểm tra người chơi có đủ MP không
        /// </summary>
        public bool HasEnoughMP(PlayerID player, int amount)
        {
            return GetCurrentMP(player) >= amount;
        }

        /// <summary>
        /// Lấy MP hiện tại của người chơi
        /// </summary>
        public int GetCurrentMP(PlayerID player)
        {
            return player == PlayerID.Player1 ? player1MP : player2MP;
        }

        /// <summary>
        /// Reset MP về giá trị ban đầu (dùng khi bắt đầu trận mới)
        /// </summary>
        public void ResetMP()
        {
            InitializeMP();
        }
    }
}
