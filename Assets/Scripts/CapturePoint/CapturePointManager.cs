using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TurnBasedGame.Unit;
using TurnBasedGame.Core;

namespace TurnBasedGame.Capture
{
    /// <summary>
    /// Quản lý tất cả cứ điểm trên bản đồ.
    /// Xử lý logic chiếm điểm khi unit di chuyển (OnUnitEnter)
    /// và kiểm tra điều kiện thắng cuối mỗi turn.
    /// </summary>
    public class CapturePointManager : BaseManager
    {
        public static CapturePointManager Instance { get; private set; }

        [Header("Capture Points")]
        [SerializeField] private List<CapturePoint> _capturePoints = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public override void Initialize(GameMediator mediator)
        {
            base.Initialize(mediator);
            CollectCapturePoints();
            SubscribeEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        /// <summary>
        /// Thu thập và khởi tạo tất cả CapturePoint trong scene
        /// </summary>
        private void CollectCapturePoints()
        {
            if (_capturePoints == null || _capturePoints.Count == 0)
            {
                _capturePoints = new List<CapturePoint>(
                    FindObjectsByType<CapturePoint>(FindObjectsSortMode.None));
            }

            foreach (var point in _capturePoints)
            {
                var tile = MapManager.Instance.MapEntity.Tile(point.transform.position);
                if (tile != null)
                    point.Initialize(tile.Position);
                else
                    Debug.LogWarning($"[CapturePointManager] Point tại {point.transform.position} không nằm trên tile hợp lệ!");
            }

            Debug.Log($"[CapturePointManager] Khởi tạo {_capturePoints.Count} cứ điểm");
        }

        #region Event Subscription

        private void SubscribeEvents()
        {
            if (_gameMediator == null) return;
            _gameMediator.OnUnitMoved += OnUnitMoved;
            _gameMediator.OnPlayerTurnEnded += OnTurnEnded;
        }

        private void UnsubscribeEvents()
        {
            if (_gameMediator == null) return;
            _gameMediator.OnUnitMoved -= OnUnitMoved;
            _gameMediator.OnPlayerTurnEnded -= OnTurnEnded;
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// OnUnitEnter: Unit di chuyển đến ô mới → kiểm tra chiếm cứ điểm
        /// </summary>
        private void OnUnitMoved(UnitController unit, Vector3Int oldPos, Vector3Int newPos)
        {
            var point = GetPointAt(newPos);
            if (point == null) return;

            if (point.TryCapture(unit.GetOwner()))
                _gameMediator.NotifyCapturePointCaptured(point.GridPosition, unit.GetOwner());
        }

        /// <summary>
        /// Cuối turn → cập nhật trạng thái và kiểm tra điều kiện thắng
        /// </summary>
        private void OnTurnEnded(PlayerID player)
        {
            RefreshAllPoints();
            CheckWinCondition();
        }

        #endregion

        /// <summary>
        /// Duyệt lại tất cả cứ điểm, cập nhật sở hữu theo unit hiện tại đang đứng trên đó
        /// </summary>
        private void RefreshAllPoints()
        {
            foreach (var point in _capturePoints)
            {
                var unit = MapManager.Instance.GetUnitAtTile(point.GridPosition);
                if (unit != null && !unit.IsDead())
                    point.TryCapture(unit.GetOwner());
            }
        }

        /// <summary>
        /// Kiểm tra điều kiện thắng: một phe chiếm TẤT CẢ cứ điểm → EndGame
        /// </summary>
        private void CheckWinCondition()
        {
            if (_capturePoints.Count == 0) return;

            bool allCaptured = _capturePoints.All(p => !p.IsNeutral);
            if (!allCaptured) return;

            var firstOwner = _capturePoints[0].Owner;
            bool sameOwner = _capturePoints.All(p => p.Owner == firstOwner);

            if (sameOwner && firstOwner.HasValue)
            {
                Debug.Log($"[CapturePointManager] === {firstOwner.Value} thắng bằng cách chiếm tất cả cứ điểm! ===");
                _gameMediator.NotifyGameEnd(firstOwner.Value);
            }
        }

        #region Public API

        public CapturePoint GetPointAt(Vector3Int gridPos)
        {
            return _capturePoints.FirstOrDefault(p => p.GridPosition == gridPos);
        }

        public int GetCaptureCount(PlayerID player)
        {
            return _capturePoints.Count(p => p.IsCapturedBy(player));
        }

        public List<CapturePoint> GetPointsOwnedBy(PlayerID player)
        {
            return _capturePoints.Where(p => p.IsCapturedBy(player)).ToList();
        }

        public int TotalPoints => _capturePoints.Count;

        #endregion
    }
}
