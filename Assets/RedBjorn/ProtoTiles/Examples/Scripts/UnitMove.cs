using RedBjorn.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using TurnBasedGame.Unit;
using UnityEngine;

namespace RedBjorn.ProtoTiles.Example
{
    public class UnitMove : MonoBehaviour
    {
        public float Speed = 5;
        public float Range = 10f;
        public Transform RotationNode;
        public AreaOutline AreaPrefab;
        public PathDrawer PathPrefab;
        public bool IsSelected;
        public bool IsMoveCompleted;
        public bool IsActionCompleted;

        MapEntity _cachedMap;
        AreaOutline Area;
        PathDrawer Path;
        Coroutine MovingCoroutine;

        public Action SelectAction;
        public Action DeselectAction;
        Vector3Int currentGridPosition;
        private Unit _thisUnit;

        private void Awake()
        {
            _thisUnit = GetComponent<Unit>();
        }

        void Update()
        {
            if (IsActionCompleted)
                return;
            if (MyInput.GetOnWorldUp(_cachedMap.Settings.Plane()))
            {
                HandleWorldClick();
            }
            PathUpdate();
        }

        public void Init(MapEntity map, Vector3Int startGridPos)
        {
            currentGridPosition = startGridPos;
            _cachedMap = map;
            Area = Spawner.Spawn(AreaPrefab, Vector3.zero, Quaternion.identity);
            UpdateGridPosition(startGridPos);
            // AreaShow();
            PathCreate();
        }

        public void ResetMove()
        {
            IsMoveCompleted = false;
            IsActionCompleted = false;
        }

        void HandleWorldClick()
        {
            var clickPos = MyInput.GroundPosition(_cachedMap.Settings.Plane());
            var tile = _cachedMap.Tile(clickPos);
            if (tile == null)
                return;
            var clickedUnit = MapManager.Instance?.GetUnitAtTile(tile.Position);
            if (clickedUnit != null && clickedUnit == this)
            {
                if (IsSelected)
                {
                    ChangeDeselected();
                }
                else
                {
                    IsSelected = true;
                    SelectAction?.Invoke();
                    AreaShow();
                    Path.IsEnabled = true;
                }
                Debug.Log($"Clicked on unit: {clickedUnit.name}");
                return;
            }

            if (!IsSelected)
            {
                return;
            }

            if (tile.Vacant)
            {
                AreaHide();
                Path.IsEnabled = false;
                PathHide();
                var path = _cachedMap.PathTiles(transform.position, clickPos, Range);

                Move(path);
            }
        }

        public void Move(List<TileEntity> path)
        {
            if (path != null)
            {
                if (MovingCoroutine != null)
                {
                    StopCoroutine(MovingCoroutine);
                }
                MovingCoroutine = StartCoroutine(Moving(path));
            }
            else
            {
                IsMoveCompleted = true;
                IsActionCompleted = true; // temp test
                ChangeDeselected();
            }
        }

        IEnumerator Moving(List<TileEntity> path)
        {
            var nextIndex = 0;
            transform.position = _cachedMap.Settings.Projection(transform.position);

            while (nextIndex < path.Count)
            {
                var targetPoint = _cachedMap.WorldPosition(path[nextIndex]);
                var stepDir = (targetPoint - transform.position) * Speed;
                if (_cachedMap.RotationType == RotationType.LookAt)
                {
                    RotationNode.rotation = Quaternion.LookRotation(stepDir, Vector3.up);
                }
                else if (_cachedMap.RotationType == RotationType.Flip)
                {
                    RotationNode.rotation = _cachedMap.Settings.Flip(stepDir);
                }
                var reached = stepDir.sqrMagnitude < 0.01f;
                while (!reached)
                {

                    transform.position += stepDir * Time.deltaTime;
                    reached = Vector3.Dot(stepDir, (targetPoint - transform.position)) < 0f;
                    yield return null;
                }
                transform.position = targetPoint;
                nextIndex++;
            }
            UpdateGridPosition(path[path.Count - 1].Position);
            IsMoveCompleted = true;
            ChangeDeselected();
        }

        void AreaShow()
        {
            AreaHide();
            Area.Show(_cachedMap.WalkableBorder(transform.position, Range), _cachedMap);
        }

        void AreaHide()
        {
            Area.Hide();
        }

        void PathCreate()
        {
            if (!Path)
            {
                Path = Spawner.Spawn(PathPrefab, Vector3.zero, Quaternion.identity);
                Path.Show(new List<Vector3>() { }, _cachedMap);
                Path.InactiveState();
                Path.IsEnabled = false;
            }
        }

        void PathHide()
        {
            if (Path)
            {
                Path.Hide();
            }
        }

        void ChangeDeselected()
        {
            DeselectAction?.Invoke();
            AreaHide();
            Path.IsEnabled = false;
            PathHide();
            IsSelected = false;
        }

        void PathUpdate()
        {
            if (Path && Path.IsEnabled)
            {
                var tile = _cachedMap.Tile(MyInput.GroundPosition(_cachedMap.Settings.Plane()));
                if (tile != null && tile.Vacant)
                {
                    var path = _cachedMap.PathPoints(transform.position, _cachedMap.WorldPosition(tile.Position), Range);
                    Path.Show(path, _cachedMap);
                    Path.ActiveState();
                    Area.ActiveState();
                }
                else
                {
                    Path.InactiveState();
                    Area.InactiveState();
                }
            }
        }

        /// <summary>
        /// Cập nhật vị trí grid của unit trên MapManager
        /// </summary>
        public void UpdateGridPosition(Vector3Int newGridPos)
        {
            MapManager.Instance.UnregisterUnit(currentGridPosition);
            currentGridPosition = newGridPos;
            MapManager.Instance.RegisterUnit(newGridPos, this);
        }

        public Unit GetUnit()
        {
            return _thisUnit;
        }

        public void MoveTowardsTarget(Vector3Int targetGridPos)
        {
            var pathTiles = _cachedMap.PathTiles(transform.position, targetGridPos, Range);
            Move(pathTiles);
        }
    }
}
