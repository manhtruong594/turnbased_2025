using UnityEngine;
using System.Collections.Generic;
using TurnBasedGame.Unit;

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
        [SerializeField] private List<UnitData> availableUnits = new List<UnitData>();

        [Header("UI References")]
        [SerializeField] private Transform buttonContainer;
        [SerializeField] private GameObject spawnButtonPrefab;

        [Header("Settings")]
        [SerializeField] private bool showOnStart = true;

        private List<SpawnUnitButton> spawnButtons = new List<SpawnUnitButton>();

        private void Start()
        {
            GenerateSpawnButtons();
            
            if (!showOnStart)
            {
                Hide();
            }
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
        private void CreateSpawnButton(UnitData unitData)
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
                button.SetUnitData(unitData);
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
        public void AddAvailableUnit(UnitData unitData)
        {
            if (unitData == null || availableUnits.Contains(unitData))
                return;

            availableUnits.Add(unitData);
            CreateSpawnButton(unitData);
        }

        /// <summary>
        /// Xóa unit khỏi danh sách có thể spawn
        /// </summary>
        public void RemoveAvailableUnit(UnitData unitData)
        {
            if (unitData == null || !availableUnits.Contains(unitData))
                return;

            availableUnits.Remove(unitData);
            GenerateSpawnButtons(); // Regenerate all buttons
        }

        /// <summary>
        /// Set danh sách units có thể spawn
        /// </summary>
        public void SetAvailableUnits(List<UnitData> units)
        {
            availableUnits = new List<UnitData>(units);
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
