using UnityEngine;
using TMPro;

namespace TurnBasedGame.Core
{
    /// <summary>
    /// Hiển thị thông tin turn trên UI chung
    /// </summary>
    public class TurnUIDisplay : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI currentPlayerText;
        [SerializeField] private TextMeshProUGUI turnCountText;
        [SerializeField] private TextMeshProUGUI gameStateText;

        private void Start()
        {
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.OnTurnStateChanged += UpdateStateDisplay;
                TurnManager.Instance.OnPlayerTurnStarted += UpdatePlayerDisplay;
            }

            UpdateDisplay();
        }

        private void OnDestroy()
        {
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.OnTurnStateChanged -= UpdateStateDisplay;
                TurnManager.Instance.OnPlayerTurnStarted -= UpdatePlayerDisplay;
            }
        }

        private void UpdateStateDisplay(TurnState state)
        {
            UpdateDisplay();
        }

        private void UpdatePlayerDisplay(PlayerID player)
        {
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (TurnManager.Instance == null) return;

            if (currentPlayerText != null)
            {
                currentPlayerText.text = $"Player {(int)TurnManager.Instance.CurrentPlayer}";
            }

            if (turnCountText != null)
            {
                turnCountText.text = $"Turn: {TurnManager.Instance.TurnCount}";
            }

            if (gameStateText != null)
            {
                gameStateText.text = GetStateDisplayText(TurnManager.Instance.CurrentState);
            }
        }

        private string GetStateDisplayText(TurnState state)
        {
            return state switch
            {
                TurnState.Initialization => "Initializing...",
                TurnState.Player1Turn => "Player 1's Turn",
                TurnState.Player2Turn => "Player 2's Turn",
                TurnState.GameEnd => "Game Ended",
                _ => "Unknown State"
            };
        }
    }
}
