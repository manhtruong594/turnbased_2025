using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TurnBasedGame.Core;
using TurnBasedGame.Resources;

namespace TurnBasedGame.Unit
{
    /// <summary>
    /// Quản lý việc spawn unit trong game
    /// Xử lý logic: kiểm tra MP, tìm spawn point hợp lệ, tạo unit
    /// </summary>
    public class UnitSpawner : MonoBehaviour
    {
        public static UnitSpawner Instance { get; private set; }

        [Header("Spawn Settings")]
        [Tooltip("Transform cha chứa các unit được spawn")]
        [SerializeField] private Transform unitsContainer;

        [Header("Spawn Points")]
        [Tooltip("Danh sách spawn points trong scene")]
        [SerializeField] private List<SpawnPoint> spawnPoints = new List<SpawnPoint>();

        private Dictionary<PlayerID, List<SpawnPoint>> playerSpawnPoints = new Dictionary<PlayerID, List<SpawnPoint>>();
        
        private Dictionary<PlayerID, List<Unit>> playerUnits = new Dictionary<PlayerID, List<Unit>>();

        // Events
        public System.Action<Unit> OnUnitDestroyed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (unitsContainer == null)
            {
                unitsContainer = transform;
            }
        }

        private void Start()
        {
            InitializeSpawnPoints();
            InitializePlayerUnits();
        }

        /// <summary>
        /// Khởi tạo Dictionary chứa units cho mỗi người chơi
        /// </summary>
        private void InitializePlayerUnits()
        {
            playerUnits.Clear();
            playerUnits[PlayerID.Player1] = new List<Unit>();
            playerUnits[PlayerID.Player2] = new List<Unit>();
        }

        /// <summary>
        /// Khởi tạo và phân loại spawn points theo owner
        /// </summary>
        private void InitializeSpawnPoints()
        {
            // Tìm tất cả spawn points trong scene nếu chưa được gán
            if (spawnPoints == null || spawnPoints.Count == 0)
            {
                spawnPoints = new List<SpawnPoint>(FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None));
            }
            foreach (var point in spawnPoints)
            {
                point.Initialize(point.Owner, MapManager.Instance.MapEntity.Tile(point.transform.position).Position);
            }
            // Phân loại spawn points theo người chơi
            playerSpawnPoints.Clear();
            playerSpawnPoints[PlayerID.Player1] = new List<SpawnPoint>();
            playerSpawnPoints[PlayerID.Player2] = new List<SpawnPoint>();

            foreach (var point in spawnPoints)
            {
                if (playerSpawnPoints.ContainsKey(point.Owner))
                {
                    playerSpawnPoints[point.Owner].Add(point);
                }
            }

            Debug.Log($"Initialized spawn points - P1: {playerSpawnPoints[PlayerID.Player1].Count}, P2: {playerSpawnPoints[PlayerID.Player2].Count}");
        }

        /// <summary>
        /// Spawn unit tại vị trí spawn point
        /// Kiểm tra MP và spawn point hợp lệ
        /// </summary>
        public bool SpawnUnit(UnitData unitData, PlayerID owner, SpawnPoint spawnPoint = null)
        {
            if (unitData == null || unitData.unitPrefab == null)
            {
                return false;
            }

            // Kiểm tra MP
            if (!CheckAndSpendMP(owner, unitData.spawnCost))
            {
                Debug.LogWarning($"Not enough MP to spawn {unitData.unitName}. Cost: {unitData.spawnCost}");
                return false;
            }

            // Tìm spawn point nếu chưa được chỉ định
            if (spawnPoint == null)
            {
                spawnPoint = GetAvailableSpawnPoint(owner);
            }

            if (spawnPoint == null)
            {
                Debug.LogWarning($"No available spawn point for {owner}");
                // Hoàn lại MP vì không spawn được
                MPManager.Instance?.AddMP(owner, unitData.spawnCost);
                return false;
            }

            // Kiểm tra spawn point có hợp lệ không
            if (!spawnPoint.BelongsTo(owner) || !spawnPoint.IsAvailable)
            {
                Debug.LogWarning("Spawn point is not valid");
                MPManager.Instance?.AddMP(owner, unitData.spawnCost);
                return false;
            }

            // Spawn unit
            GameObject unitObj = Instantiate(unitData.unitPrefab, spawnPoint.transform.position, Quaternion.identity, unitsContainer);
            Unit unit = unitObj.GetComponent<Unit>();

            if (unit == null)
            {
                unit = unitObj.AddComponent<Unit>();
            }

            // Initialize unit
            unit.Initialize(unitData, owner, spawnPoint.GridPosition);

            // Đánh dấu spawn point đã sử dụng
            spawnPoint.MarkAsOccupied();

            // Lưu trữ unit vào dictionary theo owner
            if (!playerUnits.ContainsKey(owner))
            {
                playerUnits[owner] = new List<Unit>();
            }
            playerUnits[owner].Add(unit);

            // Trigger event
            //OnUnitSpawned?.Invoke(unit);

            Debug.Log($"Successfully spawned {unitData.unitName} for {owner} at {spawnPoint.GridPosition}");
            return true;
        }

        /// <summary>
        /// Kiểm tra và tiêu tốn MP
        /// </summary>
        private bool CheckAndSpendMP(PlayerID player, int cost)
        {
            if (MPManager.Instance == null)
            {
                Debug.LogWarning("MPManager not found!");
                return false;
            }

            return MPManager.Instance.SpendMP(player, cost);
        }

        /// <summary>
        /// Lấy spawn point khả dụng của người chơi
        /// </summary>
        public SpawnPoint GetAvailableSpawnPoint(PlayerID player)
        {
            if (!playerSpawnPoints.ContainsKey(player))
            {
                return null;
            }

            var availablePoints = playerSpawnPoints[player]
                .Where(p => p.IsAvailable)
                .ToList();

            if (availablePoints.Count == 0)
            {
                return null;
            }

            // Trả về spawn point đầu tiên hoặc random
            return availablePoints[0];
        }

        /// <summary>
        /// Lấy danh sách spawn points khả dụng của người chơi
        /// </summary>
        public List<SpawnPoint> GetAvailableSpawnPoints(PlayerID player)
        {
            if (!playerSpawnPoints.ContainsKey(player))
            {
                return new List<SpawnPoint>();
            }

            return playerSpawnPoints[player]
                .Where(p => p.IsAvailable)
                .ToList();
        }

        /// <summary>
        /// Lấy tất cả units của một người chơi
        /// </summary>
        public List<Unit> GetPlayerUnits(PlayerID player)
        {
            if (!playerUnits.ContainsKey(player))
            {
                return new List<Unit>();
            }
            return playerUnits[player];
        }

        /// <summary>
        /// Hủy unit (khi bị tiêu diệt hoặc game end)
        /// </summary>
        public void DestroyUnit(Unit unit)
        {
            if (unit == null) return;

            // Xóa unit khỏi dictionary của owner
            if (playerUnits.ContainsKey(unit.Owner))
            {
                playerUnits[unit.Owner].Remove(unit);
            }

            OnUnitDestroyed?.Invoke(unit);

            // Có thể giải phóng spawn point nếu cần
            // (tùy game design - có thể spawn lại hay không)

            Destroy(unit.gameObject);
        }

        /// <summary>
        /// Xóa tất cả units (reset game)
        /// </summary>
        public void ClearAllUnits()
        {
            // Xóa tất cả units của mỗi player
            foreach (var kvp in playerUnits)
            {
                foreach (var unit in kvp.Value.ToList())
                {
                    if (unit != null)
                    {
                        Destroy(unit.gameObject);
                    }
                }
            }

            // Clear dictionary
            foreach (var player in playerUnits.Keys.ToList())
            {
                playerUnits[player].Clear();
            }

            // Reset spawn points
            foreach (var point in spawnPoints)
            {
                point.MarkAsAvailable();
            }
        }

        /// <summary>
        /// Đếm số lượng units của người chơi
        /// </summary>
        public int GetUnitCount(PlayerID player)
        {
            if (!playerUnits.ContainsKey(player))
            {
                return 0;
            }
            return playerUnits[player].Count;
        }
    }
}
