using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TurnBasedGame.Unit;
using TurnBasedGame.UI;
using TurnBasedGame.SpellCard;
using TurnBasedGame.Command;

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
        [SerializeField] DiceUI _diceUI;
        private bool _isMyTurn;
        private List<UnitController> _myUnits = new List<UnitController>();

        private void Start()
        {
            playerID = MatchContext.LocalPlayer;
            SubscribeToEvents();
            SetupUI();
            if (MatchContext.Mode == MatchMode.NetworkPvP && LocalMatchAuthority.IsAuthoritative)
                SpellCardManager.Instance.InitializeHand(PlayerID.Player2,
                    TurnBasedGame.Multiplayer.MatchSessionController.Instance != null
                        ? TurnBasedGame.Multiplayer.MatchSessionController.Instance.Spells(PlayerID.Player2) : _playerData.SelectedSpells);
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            BattleHUDToolkit.Instance?.BindPlayer(playerID, SubmitEndTurn);
            if (MatchContext.IsLocalPvP)
                BattleHUDToolkit.Instance?.BindPlayer(MatchContext.OpponentOf(playerID), SubmitEndTurn);
            if (GameMediator.Instance != null)
            {
                GameMediator.Instance.OnPlayerTurnStarted += HandleTurnStarted;
                GameMediator.Instance.OnPlayerTurnEnded += HandleTurnEnded;
                GameMediator.Instance.OnReplicaApplied += HandleReplicaApplied;
            }
        }

        private void UnsubscribeFromEvents()
        {
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
            SetupPlayerUI(playerID);
            UpdateUI(playerID, false);
            if (!MatchContext.IsLocalPvP) return;

            PlayerID opponent = MatchContext.OpponentOf(playerID);
            SetupPlayerUI(opponent);
            UpdateUI(opponent, false);
        }

        private void SetupPlayerUI(PlayerID player)
        {
            var session = TurnBasedGame.Multiplayer.MatchSessionController.Instance;
            var spells = session != null && session.InMatch ? session.Spells(player) : _playerData.SelectedSpells;
            var units = session != null && session.InMatch ? session.Units(player) : _playerData.SelectedDeck;
            SpellCardManager.Instance?.InitializeHand(player, spells);
            _diceUI?.BindToolkit(player);
            BattleHUDToolkit.Instance?.SetPlayerName(player, player.ToString());
            BattleHUDToolkit.Instance?.SetSpawnUnits(player, units);
            BattleHUDToolkit.Instance?.SetSpellHand(player, spells);
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
            _isMyTurn = MatchContext.CanHumanControl(player);
            StopAllCoroutines();
            if (_isMyTurn)
            {
                StartCoroutine(OnMyTurnStarted(player));
                UpdateUI(player, true);
            }
        }

        private void HandleReplicaApplied()
        {
            var turn = TurnManager.Instance;
            _isMyTurn = TurnBasedGame.Multiplayer.MatchGameplayBootstrap.InputReady && turn != null &&
                turn.CurrentPlayer == playerID && turn.CurrentState != TurnState.Initialization && turn.CurrentState != TurnState.GameEnd;
            _myUnits = UnitSpawner.Instance.GetPlayerUnits(playerID);
            BattleHUDToolkit.Instance?.SetTurnActive(playerID, _isMyTurn);
            _diceUI?.ApplyReplica(_isMyTurn, LocalMatchAuthority.UsedDice);
        }

        /// <summary>
        /// Xử lý khi lượt kết thúc
        /// </summary>
        private void HandleTurnEnded(PlayerID player)
        {
            StopAllCoroutines();
            if (MatchContext.CanHumanControl(player))
            {
                StartCoroutine(OnMyTurnEnded());
                UpdateUI(player, false);
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
        private IEnumerator OnMyTurnStarted(PlayerID player)
        {
            yield return null;
            _actionLefts.Value = 2;
            _myUnits = UnitSpawner.Instance.GetPlayerUnits(player);
        }

        /// <summary>
        /// Update UI dựa trên trạng thái hiện tại
        /// </summary>
        private void UpdateUI(PlayerID player, bool isMyTurn)
        {
            BattleHUDToolkit.Instance?.SetTurnActive(player, isMyTurn);
            _diceUI?.ActiveDicePanel(player, isMyTurn);
        }
    }
}
