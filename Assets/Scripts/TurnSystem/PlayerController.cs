using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System;
using System.Collections.Generic;
using TurnBasedGame.Unit;

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
        [SerializeField] private IntReference _actionLefts;
        [SerializeField] private AIController _aiOpponent;
        [Header("UI References (Optional)")]
        [SerializeField] private GameObject turnIndicator;
        [SerializeField] private PlayerUI _myUI;
        private bool _isMyTurn;
        //private float _currentTimeLimit;
        private List<Unit.Unit> _myUnits = new List<Unit.Unit>();

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

        /// <summary>
        /// Subscribe vào events của TurnManager
        /// </summary>
        private void SubscribeToEvents()
        {
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.OnPlayerTurnStarted += HandleTurnStarted;
                TurnManager.Instance.OnPlayerTurnEnded += HandleTurnEnded;
            }
        }

        /// <summary>
        /// Unsubscribe khỏi events
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.OnPlayerTurnStarted -= HandleTurnStarted;
                TurnManager.Instance.OnPlayerTurnEnded -= HandleTurnEnded;
            }
        }

        /// <summary>
        /// Setup UI components
        /// </summary>
        private void SetupUI()
        {
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
            }
            else
            {
                StartCoroutine(WaitOpponentTurn());
            }

            UpdateUI(_isMyTurn);
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
            //UpdateUI(_isMyTurn);
        }

        private IEnumerator OnMyTurnEnded()
        {
            Debug.Log($"Player {(int)playerID}: My turn ended!");
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
                unit.ResetComponents();
            }
            TurnManager.Instance.CalculateTimeLimitInTurn(playerID);
            // yield return new WaitForSeconds(_currentTimeLimit); 
            // yield return null;
            // TurnManager.Instance.EndCurrentTurn();
        }
        
        IEnumerator WaitOpponentTurn()
        {
            yield return null;
            TurnManager.Instance.SetTimeFixedTimeInTurn(30f);
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

            if (turnIndicator != null)
            {
                turnIndicator.SetActive(isMyTurn);
            }
        }
    }
}
