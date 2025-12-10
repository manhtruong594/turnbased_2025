using RedBjorn.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using TurnBasedGame.Core;
using TurnBasedGame.Unit;
using UnityEngine;

namespace RedBjorn.ProtoTiles.Example
{
    public class UnitMove : MonoBehaviour
    {
        public float Speed = 5;
        public float Range = 10f;
        public Transform RotationNode;
        public bool IsSelected { get; private set; }
        public bool IsMoveCompleted;
        public bool IsActionCompleted;

        MapEntity _cachedMap;
        Coroutine MovingCoroutine;
        Vector3Int currentGridPosition;

        [Header("Other Components")]
        [SerializeField] private UnitData unitData;
        [SerializeField] private UnitAttack _attackComponent;
        private PlayerID ownerID;
        public PlayerID Owner => ownerID;
        
        private void Awake()
        {
            _attackComponent = GetComponent<UnitAttack>();
        }

        public void Init(PlayerID owner, Vector3Int startGridPos)
        {
            IsSelected = false;
            currentGridPosition = startGridPos;
            ownerID = owner;
            _cachedMap = MapManager.Instance.MapEntity;
            UpdateGridPosition(startGridPos);
            _attackComponent.Init(_cachedMap);
        }

        public void ResetMove()
        {
            IsMoveCompleted = false;
            IsActionCompleted = false;
        }

        public void Move(List<TileEntity> path, Action onComplete = null)
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
                onComplete?.Invoke();
            }
        }

        IEnumerator Moving(List<TileEntity> path, Action onComplete = null)
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
            onComplete?.Invoke();
        }
  
        public void ChangeSelected(bool select)
        {
            IsSelected = select;
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
 
        public UnitAttack AttackComponent => _attackComponent;

        public void ResetComponents()
        {
            ResetMove();
        }
        public UnitData UnitData => unitData;
    }
}
