using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TurnBasedGame.Unit;
using TurnBasedGame.Core;
using TurnBasedGame.Resources;
using RedBjorn.ProtoTiles.Example;

namespace TurnBasedGame.UI
{
    /// <summary>
    /// UI component để spawn unit
    /// Hiển thị thông tin unit và xử lý button spawn
    /// </summary>
    public class SpawnUnitButton : MonoBehaviour
    {
        [Header("Unit Configuration")]
        [SerializeField] private UnitMove unit;

        [Header("UI References")]
        [SerializeField] private Button spawnButton;
        [SerializeField] private TextMeshProUGUI unitNameText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private Image unitIcon;
        [SerializeField] private Image backgroundImage;

        [Header("Colors")]
        [SerializeField] private Color affordableColor = Color.white;
        [SerializeField] private Color unaffordableColor = Color.gray;

        private void Awake()
        {
            SetupButton();
        }

        private void Start()
        {
            UpdateButtonState();
            
            // Lắng nghe sự kiện MP thay đổi
            if (GameMediator.Instance != null)
            {
                GameMediator.Instance.OnMPChanged += OnMPChanged;
            }
        }

        private void OnDestroy()
        {
            if (GameMediator.Instance != null)
            {
                GameMediator.Instance.OnMPChanged -= OnMPChanged;
            }
        }

        private void SetupButton()
        {
            if (spawnButton == null)
            {
                spawnButton = GetComponent<Button>();
            }

            if (spawnButton != null)
            {
                spawnButton.onClick.AddListener(OnSpawnButtonClicked);
            }

            UpdateUI();
        }

        /// <summary>
        /// Set unit data cho button này
        /// </summary>
        public void SetUnitData(UnitMove unitMove)
        {
            unit = unitMove;
            UpdateUI();
            UpdateButtonState();
        }

        private void UpdateUI()
        {
            if (unit == null) return;

            if (unitNameText != null)
            {
                unitNameText.text = unit.UnitData.unitName;
            }

            if (costText != null)
            {
                costText.text = $"{unit.UnitData.spawnCost} MP";
            }

            if (unitIcon != null && unit.UnitData.icon != null)
            {
                unitIcon.sprite = unit.UnitData.icon;
                unitIcon.enabled = true;
            }
        }

        private void OnSpawnButtonClicked()
        {
            if (unit == null || TurnManager.Instance == null || UnitSpawner.Instance == null)
            {
                return;
            }

            PlayerID currentPlayer = TurnManager.Instance.CurrentPlayer;

            // Kiểm tra có đủ MP không
            if (MPManager.Instance != null && !MPManager.Instance.HasEnoughMP(currentPlayer, unit.UnitData.spawnCost))
            {
                Debug.LogWarning($"Not enough MP to spawn {unit.UnitData.unitName}");
                ShowNotEnoughMPFeedback();
                return;
            }

            // Thử spawn unit
            bool success = UnitSpawner.Instance.SpawnUnit(unit, currentPlayer);

            if (success)
            {
                Debug.Log($"Successfully spawned {unit.UnitData.unitName}");
                OnSpawnSuccess();
            }
            else
            {
                Debug.LogWarning($"Failed to spawn {unit.UnitData.unitName}");
                OnSpawnFailed();
            }
        }

        private void OnMPChanged(PlayerID player, int currentMP, int maxMP)
        {
            // Chỉ update nếu là lượt của người chơi này
            if (TurnManager.Instance != null && player == TurnManager.Instance.CurrentPlayer)
            {
                UpdateButtonState();
            }
        }

        private void UpdateButtonState()
        {
            if (unit == null || spawnButton == null) return;

            bool canAfford = CanAffordUnit();
            bool hasSpawnPoint = HasAvailableSpawnPoint();
            bool canSpawn = canAfford && hasSpawnPoint;

            spawnButton.interactable = canSpawn;

            // Cập nhật visual
            if (backgroundImage != null)
            {
                backgroundImage.color = canAfford ? affordableColor : unaffordableColor;
            }

            // Tooltip hoặc feedback
            UpdateTooltip(canAfford, hasSpawnPoint);
        }

        private bool CanAffordUnit()
        {
            if (MPManager.Instance == null || TurnManager.Instance == null)
                return false;

            PlayerID currentPlayer = TurnManager.Instance.CurrentPlayer;
            return MPManager.Instance.HasEnoughMP(currentPlayer, unit.UnitData.spawnCost);
        }

        private bool HasAvailableSpawnPoint()
        {
            if (UnitSpawner.Instance == null || TurnManager.Instance == null)
                return false;

            PlayerID currentPlayer = TurnManager.Instance.CurrentPlayer;
            var availablePoints = UnitSpawner.Instance.GetAvailableSpawnPoints(currentPlayer);
            return availablePoints.Count > 0;
        }

        private void UpdateTooltip(bool canAfford, bool hasSpawnPoint)
        {
            // TODO: Implement tooltip system
            if (!canAfford)
            {
                // Show "Not enough MP" tooltip
            }
            else if (!hasSpawnPoint)
            {
                // Show "No spawn point available" tooltip
            }
        }

        private void ShowNotEnoughMPFeedback()
        {
            // TODO: Add visual/audio feedback
            Debug.Log("Not enough MP!");
        }

        private void OnSpawnSuccess()
        {
            // TODO: Add success feedback (sound, particle, etc.)
            UpdateButtonState();
        }

        private void OnSpawnFailed()
        {
            // TODO: Add failure feedback
            UpdateButtonState();
        }
    }
}
