using UnityEngine;
using System.Collections.Generic;
using TurnBasedGame.Unit;
using RedBjorn.ProtoTiles.Example;

namespace TurnBasedGame.UI
{
    /// <summary>
    /// Panel UI hiển thị danh sách các unit có thể spawn
    /// Quản lý các SpawnUnitButton
    /// </summary>
    public class SpawnPanel : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Danh sách các unit có thể spawn")]
        [SerializeField] private List<UnitMove> availableUnits = new List<UnitMove>();

        [Header("UI References")]
        [SerializeField] private Transform buttonContainer;
        [SerializeField] private GameObject spawnButtonPrefab;

        [Header("Settings")]
        [SerializeField] private bool showOnStart = true;

        private List<SpawnUnitButton> spawnButtons = new List<SpawnUnitButton>();

        private void Start()
        {
            if (!showOnStart)
            {
                Hide();
            }
        }
        public void Initialize(List<UnitMove> units)
        {
            SetAvailableUnits(units);
        }

        /// <summary>
        /// Tạo các button spawn cho từng unit
        /// </summary>
        private void GenerateSpawnButtons()
        {
            // Clear existing buttons
            ClearButtons();

            if (availableUnits == null || availableUnits.Count == 0)
            {
                Debug.LogWarning("No units available to spawn");
                return;
            }

            foreach (var unitData in availableUnits)
            {
                if (unitData == null) continue;

                CreateSpawnButton(unitData);
            }
        }

        /// <summary>
        /// Tạo một spawn button cho unit
        /// </summary>
        private void CreateSpawnButton(UnitMove unit)
        {
            if (spawnButtonPrefab == null || buttonContainer == null)
            {
                Debug.LogWarning("Spawn button prefab or container is null");
                return;
            }

            GameObject buttonObj = Instantiate(spawnButtonPrefab, buttonContainer);
            SpawnUnitButton button = buttonObj.GetComponent<SpawnUnitButton>();

            if (button != null)
            {
                button.SetUnitData(unit);
                spawnButtons.Add(button);
            }
            else
            {
                Debug.LogWarning("SpawnButtonPrefab doesn't have SpawnUnitButton component");
            }
        }

        /// <summary>
        /// Thêm unit vào danh sách có thể spawn
        /// </summary>
        public void AddAvailableUnit(UnitMove unit)
        {
            if (unit == null || availableUnits.Contains(unit))
                return;

            availableUnits.Add(unit);
            CreateSpawnButton(unit);
        }

        /// <summary>
        /// Xóa unit khỏi danh sách có thể spawn
        /// </summary>
        public void RemoveAvailableUnit(UnitMove unit)
        {
            if (unit == null || !availableUnits.Contains(unit))
                return;

            availableUnits.Remove(unit);
            GenerateSpawnButtons(); // Regenerate all buttons
        }

        /// <summary>
        /// Set danh sách units có thể spawn
        /// </summary>
        public void SetAvailableUnits(List<UnitMove> units)
        {
            availableUnits = new List<UnitMove>(units);
            GenerateSpawnButtons();
        }

        /// <summary>
        /// Xóa tất cả buttons
        /// </summary>
        private void ClearButtons()
        {
            foreach (var button in spawnButtons)
            {
                if (button != null)
                {
                    Destroy(button.gameObject);
                }
            }
            spawnButtons.Clear();
        }

        /// <summary>
        /// Hiển thị panel
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Ẩn panel
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Toggle hiển thị/ẩn panel
        /// </summary>
        public void Toggle()
        {
            gameObject.SetActive(!gameObject.activeSelf);
        }
    }
}
