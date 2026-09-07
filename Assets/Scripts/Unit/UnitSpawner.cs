using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TurnBasedGame.Resources;
using TurnBasedGame.Unit;
using TurnBasedGame.Core;
using TurnBasedGame.Command;

namespace TurnBasedGame.Unit
{
    /// <summary>
    /// Quản lý việc spawn unit trong game
    /// Xử lý logic: kiểm tra MP, tìm spawn point hợp lệ, tạo unit
    /// </summary>
    public class UnitSpawner : BaseManager
    {
        public static UnitSpawner Instance { get; private set; }

        [Header("Spawn Settings")]
        [Tooltip("Transform cha chứa các unit được spawn")]
        [SerializeField] private Transform unitsContainer;

        [Header("Spawn Points")]
        [Tooltip("Danh sách spawn points trong scene")]
        [SerializeField] private List<SpawnPoint> spawnPoints = new List<SpawnPoint>();

        private Dictionary<PlayerID, List<SpawnPoint>> playerSpawnPoints = new Dictionary<PlayerID, List<SpawnPoint>>();
        
        private Dictionary<PlayerID, List<UnitController>> playerUnits = new Dictionary<PlayerID, List<UnitController>>();

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
        override public void Initialize(GameMediator mediator)
        {
            base.Initialize(mediator);
            InitializeSpawnPoints();
            InitializePlayerUnits();
        }

        /// <summary>
        /// Khởi tạo Dictionary chứa units cho mỗi người chơi
        /// </summary>
        private void InitializePlayerUnits()
        {
            playerUnits.Clear();
            playerUnits[PlayerID.Player1] = new List<UnitController>();
            playerUnits[PlayerID.Player2] = new List<UnitController>();
        }

        /// <summary>
        /// Khởi tạo và phân loại spawn points theo owner
        /// </summary>
        private void InitializeSpawnPoints()
        {
            // Tìm tất cả spawn points trong scene nếu chưa được gán
            spawnPoints = new List<SpawnPoint>(FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None));
            var validSpawnPoints = new List<SpawnPoint>(spawnPoints.Count);
            foreach (var point in spawnPoints)
            {
                if (point == null)
                {
                    continue;
                }

                var tile = MapManager.Instance?.MapEntity?.Tile(point.transform.position);
                if (tile == null || !tile.Vacant)
                {
                    Debug.LogWarning($"[UnitSpawner] Bỏ qua spawn point '{point.name}' tại {point.transform.position}: tile không tồn tại hoặc không thể di chuyển.", point);
                    continue;
                }

                point.Initialize(point.Owner, tile.Position);
                validSpawnPoints.Add(point);
            }
            spawnPoints = validSpawnPoints;
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
        public bool SpawnUnit(UnitController unit, PlayerID owner, SpawnPoint spawnPoint = null)
        {
            return LocalMatchAuthority.SubmitSpawn(owner, unit, spawnPoint).Succeeded;
        }

        internal bool TrySpawnUnitAuthorized(
            UnitController unit,
            PlayerID owner,
            SpawnPoint spawnPoint,
            out string failureReason)
        { 
            if (spawnPoint == null)
            {
                spawnPoint = GetAvailableSpawnPoint(owner);
            }

            if (spawnPoint == null)
            {
                failureReason = "Không có spawn point khả dụng.";
                return false;
            }

            // Kiểm tra spawn point có hợp lệ không
            if (!spawnPoint.BelongsTo(owner) || !spawnPoint.IsAvailable)
            {
                Debug.LogWarning("Spawn point is not valid");
                failureReason = "Spawn point không thuộc người chơi hoặc đã bị chiếm.";
                return false;
            }

            if (MapManager.Instance == null ||
                !MapManager.Instance.IsTileAvailable(spawnPoint.GridPosition))
            {
                failureReason = "Tile tại spawn point không khả dụng.";
                return false;
            }

            // Spawn unit
            var unitClone = Instantiate(unit, spawnPoint.transform.position, Quaternion.identity, unitsContainer);
            
            unitClone.Init(owner, spawnPoint.GridPosition);
            spawnPoint.MarkAsOccupied();

            // Lưu trữ unit vào dictionary theo owner
            if (!playerUnits.ContainsKey(owner))
            {
                playerUnits[owner] = new List<UnitController>();
            }
            playerUnits[owner].Add(unitClone);
            _gameMediator.NotifySpawnUnit(unitClone, unitClone.UnitData.spawnCost);

            Debug.Log($"Successfully spawned {unitClone.UnitData.unitName} for {owner} at {spawnPoint.GridPosition}");
            failureReason = null;
            return true;
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

        public List<UnitController> GetPlayerUnits(PlayerID player)
        {
            if (!playerUnits.ContainsKey(player))
            {
                return new List<UnitController>();
            }
            return playerUnits[player];
        }
    }
}
