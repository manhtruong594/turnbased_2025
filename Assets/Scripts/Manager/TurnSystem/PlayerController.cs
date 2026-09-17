using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TurnBasedGame.Unit;
using TurnBasedGame.UI;
using TurnBasedGame.SpellCard;
using TurnBasedGame.Command;
using UnityEngine.Serialization;

namespace TurnBasedGame.Core
{
    /// <summary>
    /// Controller cơ bản cho người chơi
    /// Xử lý input và hiển thị trạng thái turn
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [Header("Player Info")]
        [SerializeField] private PlayerID playerID;
        [SerializeField] private PlayerDataSO _playerData;
        [SerializeField] private IntReference _actionLefts;
        [FormerlySerializedAs("_aiOpponent")]

        [Header("UI References")]
        public Button EndTurnButton;
        [SerializeField] DiceUI _diceUI;
        [SerializeField] private PlayerUI _myUI;
        [SerializeField] private SpawnPanel _spawnPanel;
        [SerializeField] private SpellCardPanel _spellCardPanel;
        private bool _isMyTurn;
        private List<UnitController> _myUnits = new List<UnitController>();

        private void Start()
        {
            playerID = MatchContext.LocalPlayer;
            SubscribeToEvents();
            SetupUI();
            if (MatchContext.Mode == MatchMode.NetworkPvP && LocalMatchAuthority.IsAuthoritative)
                SpellCardManager.Instance.InitializeHand(PlayerID.Player2, _playerData.SelectedSpells);
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            if (EndTurnButton != null)
            {
                EndTurnButton.onClick.AddListener(() =>
                {
                    LocalMatchAuthority.SubmitHumanEndTurn();
                });
            }
            BattleHUDToolkit.Instance?.BindPlayer(playerID, SubmitEndTurn);
            if (GameMediator.Instance != null)
            {
                GameMediator.Instance.OnPlayerTurnStarted += HandleTurnStarted;
                GameMediator.Instance.OnPlayerTurnEnded += HandleTurnEnded;
                GameMediator.Instance.OnReplicaApplied += HandleReplicaApplied;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (EndTurnButton != null)
            {
                EndTurnButton.onClick.RemoveAllListeners();
            }
            if (GameMediator.Instance != null)
            {
                GameMediator.Instance.OnPlayerTurnStarted -= HandleTurnStarted;
                GameMediator.Instance.OnPlayerTurnEnded -= HandleTurnEnded;
                GameMediator.Instance.OnReplicaApplied -= HandleReplicaApplied;
            }
        }

        /// <summary>
        /// Setup UI components
        /// </summary>
        private void SetupUI()
        {
            _spawnPanel?.Initialize(_playerData.SelectedDeck, playerID);
            _spellCardPanel?.Initialize(_playerData.SelectedSpells, playerID);
            _myUI?.Setup(playerID.ToString());
            _diceUI?.BindToolkit(playerID);
            BattleHUDToolkit.Instance?.SetPlayerName(playerID, playerID.ToString());
            UpdateUI(false);
        }

        private void SubmitEndTurn()
        {
            LocalMatchAuthority.SubmitHumanEndTurn();
        }

        /// <summary>
        /// Xử lý khi lượt bắt đầu
        /// </summary>
        private void HandleTurnStarted(PlayerID player)
        {
            if (TurnBasedGame.Multiplayer.MatchGameplayBootstrap.Active) { HandleReplicaApplied(); return; }
            _isMyTurn = (player == playerID);
            StopAllCoroutines();
            if (_isMyTurn)
            {
                StartCoroutine(OnMyTurnStarted());
                UpdateUI(_isMyTurn);
            }
        }

        private void HandleReplicaApplied()
        {
            var turn = TurnManager.Instance;
            _isMyTurn = TurnBasedGame.Multiplayer.MatchGameplayBootstrap.InputReady && turn != null &&
                turn.CurrentPlayer == playerID && turn.CurrentState != TurnState.Initialization && turn.CurrentState != TurnState.GameEnd;
            _myUnits = UnitSpawner.Instance.GetPlayerUnits(playerID);
            _myUI?.ShowActionPanel(_isMyTurn);
            BattleHUDToolkit.Instance?.SetTurnActive(playerID, _isMyTurn);
            if (EndTurnButton != null) EndTurnButton.interactable = _isMyTurn;
            _diceUI?.ApplyReplica(_isMyTurn, LocalMatchAuthority.UsedDice);
            _spellCardPanel?.UpdateInteractable();
        }

        /// <summary>
        /// Xử lý khi lượt kết thúc
        /// </summary>
        private void HandleTurnEnded(PlayerID player)
        {
            StopAllCoroutines();
            if (player == playerID)
            {
                StartCoroutine(OnMyTurnEnded());
                UpdateUI(false);
            }

        }

        private IEnumerator OnMyTurnEnded()
        {
            if (_myUnits.Count == 0)
                yield break;
            yield return null;
        }

        /// <summary>
        /// Logic khi đến lượt của mình
        /// </summary>
        private IEnumerator OnMyTurnStarted()
        {
            yield return null;
            _actionLefts.Value = 2;
            _myUnits = UnitSpawner.Instance.GetPlayerUnits(playerID);
        }

        /// <summary>
        /// Update UI dựa trên trạng thái hiện tại
        /// </summary>
        private void UpdateUI(bool isMyTurn)
        {
            _myUI?.ShowActionPanel(isMyTurn);
            _diceUI?.ActiveDicePanel(isMyTurn);
            BattleHUDToolkit.Instance?.SetTurnActive(playerID, isMyTurn);
            if (EndTurnButton != null)
            {
                EndTurnButton.interactable = isMyTurn;
            }
        }
    }
}
