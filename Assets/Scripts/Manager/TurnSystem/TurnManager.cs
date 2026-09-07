using UnityEngine;
using System;
using TurnBasedGame.Unit;
using System.Collections;
using TMPro;
using TurnBasedGame.Command;

namespace TurnBasedGame.Core
{
    public class TurnManager : BaseManager
    {
        public static TurnManager Instance { get; private set; }

        [Header("Turn Settings")]
        public PlayerID StartingPlayer = PlayerID.Player1;
        public float TurnTransitionDelay = 0.5f;
        [SerializeField] private float _flatTimeLimit = 20f;

        private TurnState currentState;
        private PlayerID currentPlayer;
        private int turnCount;

        public TurnState CurrentState => currentState;
        public PlayerID CurrentPlayer => currentPlayer;
        public int TurnCount => turnCount;
        public PlayerID? Winner => _winner;

        [Header("Display")]
        public TextMeshProUGUI ClockText;
        public float Timer { get; private set; }
        private bool _isTriggerTimer;
        private bool _turnTransitionPending;
        private PlayerID? _winner;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public override void Initialize(GameMediator mediator)
        {
            base.Initialize(mediator);
            turnCount = 0;
            currentPlayer = StartingPlayer;
            _turnTransitionPending = false;
            LocalMatchAuthority.Reset();
            ChangeState(TurnState.Initialization);

            Invoke(nameof(StartFirstTurn), TurnTransitionDelay);
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
            _turnTransitionPending = false;

            Debug.Log($"=== Turn {turnCount}: Player {(int)player}'s Turn Started ===");
            _gameMediator.NotifyPlayerTurnStarted(currentPlayer);
        }

        /// <summary>
        /// Kết thúc lượt hiện tại và chuyển sang người chơi tiếp theo
        /// </summary>
        public void EndCurrentTurn()
        {
            var result = LocalMatchAuthority.SubmitEndTurn(currentPlayer);
            if (!result.Succeeded)
                Debug.LogWarning($"Cannot end turn: {result.FailureReason}");
        }

        internal bool TryEndCurrentTurn(PlayerID actor, int expectedTurn, out string failureReason)
        {
            if (currentState == TurnState.GameEnd || currentState == TurnState.Initialization)
            {
                failureReason = "Không thể kết thúc lượt ở state hiện tại.";
                return false;
            }

            if (actor != currentPlayer || expectedTurn != turnCount)
            {
                failureReason = "Yêu cầu kết thúc lượt không khớp lượt hiện tại.";
                return false;
            }

            if (_turnTransitionPending)
            {
                failureReason = "Lượt hiện tại đang trong quá trình kết thúc.";
                return false;
            }

            _turnTransitionPending = true;
            _isTriggerTimer = false;

            Debug.Log($"=== Player {(int)currentPlayer}'s Turn Ended ===");
            _gameMediator.NotifyPlayerTurnEnded(currentPlayer);

            // Chỉ chuyển lượt nếu game chưa kết thúc
            if (currentState == TurnState.GameEnd)
            {
                failureReason = null;
                return true;
            }

            // Chuyển lượt sau delay ngắn
            Invoke(nameof(SwitchToNextPlayer), TurnTransitionDelay);
            failureReason = null;
            return true;
        }

        /// <summary>
        /// Chuyển sang người chơi tiếp theo
        /// </summary>
        private void SwitchToNextPlayer()
        {
            if (currentState == TurnState.GameEnd) return;

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
        /// Kết thúc game khi một phe chiếm tất cả cứ điểm
        /// </summary>
        public void TriggerGameEnd(PlayerID winner)
        {
            _winner = winner;
            _isTriggerTimer = false;
            CancelInvoke();
            ChangeState(TurnState.GameEnd);
        }

        /// <summary>
        /// Xử lý khi game kết thúc
        /// </summary>
        private void HandleGameEnd()
        {
            Debug.Log($"=== Game End! Winner: {(_winner.HasValue ? _winner.Value.ToString() : "None")} ===");
        }

        public void CalculateTimeLimitInTurn(int unitCount)
        {
            try
            {
                Timer = _flatTimeLimit + unitCount * 10;
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
