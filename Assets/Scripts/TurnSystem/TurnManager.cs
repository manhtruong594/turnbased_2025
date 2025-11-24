using UnityEngine;
using System;
using TurnBasedGame.Unit;
using System.Collections;
using TMPro;

namespace TurnBasedGame.Core
{
    /// <summary>
    /// Quản lý hệ thống chuyển lượt giữa 2 người chơi
    /// Singleton pattern để đảm bảo chỉ có 1 instance
    /// </summary>
    public class TurnManager : MonoBehaviour
    {
        public static TurnManager Instance { get; private set; }

        [Header("Turn Settings")]
        public PlayerID StartingPlayer = PlayerID.Player1;
        public float TurnTransitionDelay = 0.5f;
        [SerializeField] private float _flatTimeLimit = 20f;
        // Events để các script khác subscribe
        public event Action<TurnState> OnTurnStateChanged;
        public event Action<PlayerID> OnPlayerTurnStarted;
        public event Action<PlayerID> OnPlayerTurnEnded;

        private TurnState currentState;
        private PlayerID currentPlayer;
        private int turnCount;

        public TurnState CurrentState => currentState;
        public PlayerID CurrentPlayer => currentPlayer;
        public int TurnCount => turnCount;

        [Header("Display")]
        public TextMeshProUGUI ClockText;
        public float Timer { get; private set; }
        private bool _isTriggerTimer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        IEnumerator Start()
        {
            yield return null;
            yield return new WaitForEndOfFrame();
            InitializeGame();
        }
        void Update()
        {
            if (!_isTriggerTimer)
                return;
            Timer -= Time.deltaTime;
            ClockText.text = Mathf.CeilToInt(Timer).ToString();
            if (Timer <= 0)
            {
                EndCurrentTurn();
            }
        }
        /// <summary>
        /// Khởi tạo game và bắt đầu lượt đầu tiên
        /// </summary>
        public void InitializeGame()
        {
            turnCount = 0;
            currentPlayer = StartingPlayer;
            ChangeState(TurnState.Initialization);

            Invoke(nameof(StartFirstTurn), TurnTransitionDelay);
        }

        private void StartFirstTurn()
        {
            TurnState firstTurnState = StartingPlayer == PlayerID.Player1
                ? TurnState.Player1Turn
                : TurnState.Player2Turn;

            ChangeState(firstTurnState);
        }

        /// <summary>
        /// Thay đổi trạng thái game
        /// </summary>
        private void ChangeState(TurnState newState)
        {
            if (currentState == newState) return;

            currentState = newState;
            OnTurnStateChanged?.Invoke(currentState);
            Timer = 0;
            HandleStateChange(newState);
        }

        /// <summary>
        /// Xử lý logic khi state thay đổi
        /// </summary>
        private void HandleStateChange(TurnState state)
        {
            switch (state)
            {
                case TurnState.Initialization:
                    Debug.Log("=== Game Initialization ===");
                    break;

                case TurnState.Player1Turn:
                    StartPlayerTurn(PlayerID.Player1);
                    break;

                case TurnState.Player2Turn:
                    StartPlayerTurn(PlayerID.Player2);
                    break;

                case TurnState.GameEnd:
                    HandleGameEnd();
                    break;
            }
        }

        /// <summary>
        /// Bắt đầu lượt của người chơi
        /// </summary>
        private void StartPlayerTurn(PlayerID player)
        {
            currentPlayer = player;
            turnCount++;

            Debug.Log($"=== Turn {turnCount}: Player {(int)player}'s Turn Started ===");
            OnPlayerTurnStarted?.Invoke(currentPlayer);
        }

        /// <summary>
        /// Kết thúc lượt hiện tại và chuyển sang người chơi tiếp theo
        /// </summary>
        public void EndCurrentTurn()
        {
            _isTriggerTimer = false;
            if (currentState == TurnState.GameEnd || currentState == TurnState.Initialization)
            {
                Debug.LogWarning("Cannot end turn in current state!");
                return;
            }

            Debug.Log($"=== Player {(int)currentPlayer}'s Turn Ended ===");
            OnPlayerTurnEnded?.Invoke(currentPlayer);

            // Chuyển lượt sau delay ngắn
            Invoke(nameof(SwitchToNextPlayer), TurnTransitionDelay);
        }

        /// <summary>
        /// Chuyển sang người chơi tiếp theo
        /// </summary>
        private void SwitchToNextPlayer()
        {
            if (currentState == TurnState.Player1Turn)
            {
                ChangeState(TurnState.Player2Turn);
            }
            else if (currentState == TurnState.Player2Turn)
            {
                ChangeState(TurnState.Player1Turn);
            }
        }

        /// <summary>
        /// Kết thúc game với người chơi thắng cuộc
        /// </summary>
        public void EndGame(PlayerID winner)
        {
            Debug.Log($"=== Game End: Player {(int)winner} Wins! ===");
            ChangeState(TurnState.GameEnd);
        }

        /// <summary>
        /// Xử lý khi game kết thúc
        /// </summary>
        private void HandleGameEnd()
        {
            // Logic xử lý khi game kết thúc
            Debug.Log("Game has ended!");
        }

        /// <summary>
        /// Kiểm tra xem có phải lượt của player này không
        /// </summary>
        public bool IsPlayerTurn(PlayerID player)
        {
            return currentPlayer == player &&
                   (currentState == TurnState.Player1Turn || currentState == TurnState.Player2Turn);
        }

        public void CalculateTimeLimitInTurn(PlayerID player)
        {
            try
            {
                Timer = _flatTimeLimit + UnitSpawner.Instance.GetUnitCount(player) * 10;
            }
            catch
            {
                Timer = _flatTimeLimit;
            }
            _isTriggerTimer = true;
        }

        public void SetTimeFixedTimeInTurn(float fixedTime)
        {
            Timer = fixedTime;
            _isTriggerTimer = true;
        }
    }
}
