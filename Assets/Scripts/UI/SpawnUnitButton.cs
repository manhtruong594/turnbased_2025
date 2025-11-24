using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TurnBasedGame.Unit;
using TurnBasedGame.Core;
using TurnBasedGame.Resources;

namespace TurnBasedGame.UI
{
    /// <summary>
    /// UI component để spawn unit
    /// Hiển thị thông tin unit và xử lý button spawn
    /// </summary>
    public class SpawnUnitButton : MonoBehaviour
    {
        [Header("Unit Configuration")]
        [SerializeField] private UnitData unitData;

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
            if (MPManager.Instance != null)
            {
                MPManager.Instance.OnMPChanged += OnMPChanged;
            }
        }

        private void OnDestroy()
        {
            if (MPManager.Instance != null)
            {
                MPManager.Instance.OnMPChanged -= OnMPChanged;
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
        public void SetUnitData(UnitData data)
        {
            unitData = data;
            UpdateUI();
            UpdateButtonState();
        }

        private void UpdateUI()
        {
            if (unitData == null) return;

            if (unitNameText != null)
            {
                unitNameText.text = unitData.unitName;
            }

            if (costText != null)
            {
                costText.text = $"{unitData.spawnCost} MP";
            }

            if (unitIcon != null && unitData.icon != null)
            {
                unitIcon.sprite = unitData.icon;
                unitIcon.enabled = true;
            }
        }

        private void OnSpawnButtonClicked()
        {
            if (unitData == null)
            {
                Debug.LogWarning("Cannot spawn: No unit data assigned");
                return;
            }

            if (TurnManager.Instance == null)
            {
                Debug.LogWarning("TurnManager not found");
                return;
            }

            if (UnitSpawner.Instance == null)
            {
                Debug.LogWarning("UnitSpawner not found");
                return;
            }

            PlayerID currentPlayer = TurnManager.Instance.CurrentPlayer;

            // Kiểm tra có đủ MP không
            if (MPManager.Instance != null && !MPManager.Instance.HasEnoughMP(currentPlayer, unitData.spawnCost))
            {
                Debug.LogWarning($"Not enough MP to spawn {unitData.unitName}");
                ShowNotEnoughMPFeedback();
                return;
            }

            // Thử spawn unit
            bool success = UnitSpawner.Instance.SpawnUnit(unitData, currentPlayer);

            if (success)
            {
                Debug.Log($"Successfully spawned {unitData.unitName}");
                OnSpawnSuccess();
            }
            else
            {
                Debug.LogWarning($"Failed to spawn {unitData.unitName}");
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
            if (unitData == null || spawnButton == null) return;

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
            return MPManager.Instance.HasEnoughMP(currentPlayer, unitData.spawnCost);
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
