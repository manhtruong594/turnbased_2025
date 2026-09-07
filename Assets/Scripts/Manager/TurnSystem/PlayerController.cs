using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System;
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
        [SerializeField] private AIController _aiOpponent;

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
            SubscribeToEvents();
            SetupUI();
            if (_aiOpponent != null)
                _aiOpponent.Initialize(playerID);
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
                    LocalMatchAuthority.SubmitEndTurn(playerID);
                });
            }
            if (GameMediator.Instance != null)
            {
                GameMediator.Instance.OnPlayerTurnStarted += HandleTurnStarted;
                GameMediator.Instance.OnPlayerTurnEnded += HandleTurnEnded;
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
            }
        }

        /// <summary>
        /// Setup UI components
        /// </summary>
        private void SetupUI()
        {
            _spawnPanel.Initialize(_playerData.SelectedDeck);
            _spellCardPanel.Initialize(_playerData.SelectedSpells, playerID);
            _myUI.Setup(playerID.ToString());
            UpdateUI(false);
        }

        /// <summary>
        /// Xử lý khi lượt bắt đầu
        /// </summary>
        private void HandleTurnStarted(PlayerID player)
        {
            _isMyTurn = (player == playerID);
            StopAllCoroutines();
            if (_isMyTurn)
            {
                StartCoroutine(OnMyTurnStarted());
                UpdateUI(_isMyTurn);
            }
            else
            {
                StartCoroutine(WaitOpponentTurn());
            }

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
            foreach (var unit in _myUnits)
            {
                unit.FinishTurnActions();
            }
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
            foreach (var unit in _myUnits)
            {
                unit.OnTurnBegin();
            }
            TurnManager.Instance.CalculateTimeLimitInTurn(_myUnits.Count);
        }

        IEnumerator WaitOpponentTurn()
        {
            yield return null;
            TurnManager.Instance.SetTimeFixedTimeInTurn(30f);       // hack chờ AI
            if (_aiOpponent != null)
            {
                yield return StartCoroutine(_aiOpponent.ExecuteAITurn());
            }
        }

        /// <summary>
        /// Update UI dựa trên trạng thái hiện tại
        /// </summary>
        private void UpdateUI(bool isMyTurn)
        {
            _myUI.ShowActionPanel(isMyTurn);
            _diceUI.ActiveDicePanel(isMyTurn);
            if (EndTurnButton != null)
            {
                EndTurnButton.interactable = isMyTurn;
            }
        }
    }
}
