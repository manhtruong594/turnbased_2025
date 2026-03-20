using RedBjorn.Utils;
using System.Collections.Generic;
using TurnBasedGame.Core;
using TurnBasedGame.Unit;
using UnityEngine;

namespace RedBjorn.ProtoTiles.Example
{
    /// <summary>
    /// Singleton manager để tạo và quản lý visualize Area và Path cho Unit
    /// gửi reqquest cho Unit thông báo di chuyển khi đủ điều kiện
    /// </summary>
    public class AreaPathManager : BaseManager
    {
        public static AreaPathManager Instance { get; private set; }

        [Header("Prefabs")]
        public AreaOutline AreaPrefab;
        public AreaOutline AttackAreaPrefab;
        public PathDrawer PathPrefab;
        public bool IsLocked = false;

        MapEntity _cachedMap;

        AreaOutline _area;
        AreaOutline _attackArea;
        PathDrawer _path;
        private UnitController selectedUnit;
        private UnitController previousSelectedUnit;
        TileEntity _tileClicked;

        #region  Unity Core and Initialization
        void Awake()
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
            EnsureCreated();
        }

        void EnsureCreated()
        {
            if (_area == null && AreaPrefab != null)
            {
                _area = Spawner.Spawn(AreaPrefab, Vector3.zero, Quaternion.identity);
                _area.Hide();
            }
            if (_attackArea == null && AttackAreaPrefab != null)
            {
                _attackArea = Spawner.Spawn(AttackAreaPrefab, Vector3.zero, Quaternion.identity);
                _attackArea.Hide();
            }
            if (_path == null && PathPrefab != null)
            {
                _path = Spawner.Spawn(PathPrefab, Vector3.zero, Quaternion.identity);
                // Khởi tạo path ở trạng thái ẩn và không kích hoạt
                if (_cachedMap != null)
                {
                    _path.Show(new List<Vector3>() { }, _cachedMap);
                }
                _path.InactiveState();
                _path.IsEnabled = false;
                _path.Hide();
            }
        }

        private void Update()
        {
            if (IsLocked) return;
            var mousePos = MyInput.GroundPosition(_cachedMap.Settings.Plane());
            if (MyInput.GetOnWorldUp(_cachedMap.Settings.Plane()))
            {
                _tileClicked = _cachedMap.Tile(mousePos);
                if (_tileClicked == null)
                    return;

                HandleWorldClickAndMove(mousePos);
            }

            if (_path && _path.IsEnabled)
            {
                var tile = _cachedMap.Tile(mousePos);
                if (tile != null && tile.Vacant)
                {
                    var path = _cachedMap.PathPoints(selectedUnit.transform.position, _cachedMap.WorldPosition(tile.Position), selectedUnit.GetMoveRange());
                    _path.Show(path, _cachedMap);
                    _path.ActiveState();
                    _area.ActiveState();
                }
                else
                {
                    _path.InactiveState();
                    _area.InactiveState();
                }
            }
        }

        #endregion

        #region  Core Methods
        void HandleWorldClickAndMove(Vector3 clickPos)
        {
            var clickedUnit = MapManager.Instance?.GetUnitAtTile(_tileClicked.Position);

            if (clickedUnit != null && !clickedUnit.IsActionFinished())
            {
                if (clickedUnit.IsSelected)
                {
                    clickedUnit.ChangeSelected(false);
                    selectedUnit = null;
                    previousSelectedUnit = selectedUnit;
                    HideMoveArea();
                    HidePath();
                    _gameMediator.NotifyUnitDeselected(clickedUnit);
                }
                else
                {
                    if (selectedUnit != null)
                    {
                        selectedUnit.ChangeSelected(false);
                        previousSelectedUnit = selectedUnit;
                    }
                    clickedUnit.ChangeSelected(true);
                    selectedUnit = clickedUnit;
                    if (!selectedUnit.IsMoveDone())
                    {
                        ShowMoveArea(_cachedMap.WalkableBorder(selectedUnit.transform.position, selectedUnit.GetMoveRange()));
                        SetPathEnabled(true);
                    }
                    _gameMediator.NotifyUnitSelected(clickedUnit);
                }
                Debug.Log($"Clicked on unit: {clickedUnit.name}");
                return;
            }

            if (selectedUnit == null || !selectedUnit.CanMove())
                return;

            if (_tileClicked.Vacant)
            {
                // handle move
                HideMoveArea();
                SetPathEnabled(false);
                HidePath();
                var path = _cachedMap.PathTiles(selectedUnit.transform.position, clickPos, selectedUnit.GetMoveRange());
                selectedUnit.Move(path, OnCompleteMove);
            }
        }

        void OnCompleteMove()
        {
            HideMoveArea();
            HidePath();
        }

        #endregion

        #region  Support Methods
        public void ShowMoveArea(List<Vector3> border, MapEntity map = null)
        {
            if (_area != null)
                _area.Show(border, _cachedMap);
        }

        public void HideMoveArea()
        {
            if (_area != null)
                _area.Hide();
        }

        public void HidePath()
        {
            if (_path != null)
            {
                _path.Hide();
                _path.IsEnabled = false;
            }
        }

        public void SetPathEnabled(bool enabled)
        {
            if (_path != null)
                _path.IsEnabled = enabled;
        }

        public void ShowAttackArea(List<Vector3> border)
        {
            if (_attackArea != null)
                _attackArea.Show(border, _cachedMap);
            HideMoveArea();
            HidePath();
        }

        public void HideAttackArea()
        {
            if (_attackArea != null)
                _attackArea.Hide();
        }

        public void RealeaseSelectedUnit()
        {
            if (selectedUnit != null)
            {
                selectedUnit.ChangeSelected(false);
                selectedUnit = null;
            }
        }

        public void ResetAll(UnitController unit)
        {
            if (selectedUnit != unit)
                return;
            selectedUnit = null;
            HideMoveArea();
            HidePath();
            HideAttackArea();
            if (_area != null) _area.InactiveState();
            if (_path != null) _path.InactiveState();
            if (_attackArea != null) _attackArea.InactiveState();
            if (_path != null) _path.IsEnabled = false;
        }

        public void SetCachedMap(MapEntity map)
        {
            _cachedMap = map;
        }
    }
    #endregion
}
