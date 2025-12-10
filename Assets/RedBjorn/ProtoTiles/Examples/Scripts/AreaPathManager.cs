using RedBjorn.Utils;
using System.Collections.Generic;
using TurnBasedGame.Core;
using UnityEngine;

namespace RedBjorn.ProtoTiles.Example
{
    /// <summary>
    /// Singleton manager để tạo và quản lý một instance chung của
    /// `AreaOutline` và `PathDrawer` nhằm tái sử dụng giữa các unit.
    /// </summary>
    public class AreaPathManager : BaseManager
    {
        public static AreaPathManager Instance { get; private set; }

        [Header("Prefabs")]
        public AreaOutline AreaPrefab;
        public AreaOutline AttackAreaPrefab;
        public PathDrawer PathPrefab;

        MapEntity _cachedMap;

        AreaOutline _area;
        AreaOutline _attackArea;
        PathDrawer _path;
        private UnitMove selectedUnit;
        private UnitMove previousSelectedUnit;
        TileEntity _tileClicked;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            var clickPos = MyInput.GroundPosition(_cachedMap.Settings.Plane());
            _tileClicked = _cachedMap.Tile(clickPos);
            if (_tileClicked == null)
                return;

            HandleWorldClick();

            if (selectedUnit == null)
                return;

            if (_tileClicked.Vacant)
            {
                // handle move
                HideArea();
                SetPathEnabled(false);
                HidePath();
                var path = _cachedMap.PathTiles(selectedUnit.transform.position, clickPos, selectedUnit.Range);
                selectedUnit.Move(path);
            }

            PathUpdate();
        }

        void HandleWorldClick()
        {
            var clickedUnit = MapManager.Instance?.GetUnitAtTile(_tileClicked.Position);

            if (clickedUnit != null)
            {
                if (clickedUnit.IsSelected)
                {
                    selectedUnit = null;
                    previousSelectedUnit = selectedUnit;
                    HideArea();
                    HidePath();
                    _gameMediator.NotifyUnitDeselected(clickedUnit);
                }
                else
                {
                    clickedUnit.IsSelected = true;
                    selectedUnit = clickedUnit;
                    ShowArea(_cachedMap.WalkableBorder(selectedUnit.transform.position, selectedUnit.Range));
                    SetPathEnabled(true);
                    _gameMediator.NotifyUnitSelected(clickedUnit);
                }
                Debug.Log($"Clicked on unit: {clickedUnit.name}");
                return;
            }
        }

        void PathUpdate()
        {
            if (_path && _path.IsEnabled)
            {
                var tile = _cachedMap.Tile(MyInput.GroundPosition(_cachedMap.Settings.Plane()));
                if (tile != null && tile.Vacant)
                {
                    var path = _cachedMap.PathPoints(selectedUnit.transform.position, _cachedMap.WorldPosition(tile.Position), selectedUnit.Range);
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

        public override void Initialize(GameMediator mediator)
        {
            base.Initialize(mediator);
            EnsureCreated();
        }

        public void ShowArea(List<Vector3> border, MapEntity map = null)
        {
            if (_area != null)
                _area.Show(border, _cachedMap);
        }

        public void HideArea()
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

        public void ResetAll()
        {
            HideArea();
            HidePath();
            if (_area != null) _area.InactiveState();
            if (_path != null) _path.InactiveState();
            if (_path != null) _path.IsEnabled = false;
        }

        public void SetCachedMap(MapEntity map)
        {
            _cachedMap = map;
        }
    }
}
