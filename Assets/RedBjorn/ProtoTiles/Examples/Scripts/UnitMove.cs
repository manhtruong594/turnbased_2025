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
        public float Speed = 5f;
        public Transform RotationNode;
        public bool IsSelected { get; private set; }
        public bool IsMoveCompleted { get; private set; }
        public bool IsActionCompleted { get; private set; }
        public bool IsDead {get; private set; }
        readonly UnitRuntimeStats runtimeStats = new();

        Coroutine _movingCoroutine;
        public Vector3Int currentGridPosition { get; private set; }

        [Header("Other Components")]
        [SerializeField] private UnitData unitData;
        [SerializeField] private UnitAttack _attackComponent;
        [SerializeField] private HealthBar _healthBar;
        [SerializeField] private GameObject _actionPanel;

        private void Awake()
        {
            _attackComponent = GetComponent<UnitAttack>();
        }

        public void Init(PlayerID owner, Vector3Int startGridPos)
        {
            ResetComponents();
            UpdateGridPosition(startGridPos);
            CreateStats();
            _attackComponent.Init(runtimeStats);
            runtimeStats.OnFinishTurn += FinishTurnActions;

            void CreateStats()
            {
                runtimeStats.ReCalculateStats(unitData)
                    .SetOwner(owner)
                    .AssignMap(MapManager.Instance.MapEntity);
            }
        }

        public void ResetMove()
        {
            IsSelected = false;
            IsMoveCompleted = false;
            IsActionCompleted = false;
            IsDead = false;
        }

        #region  Movement Methods
        public void Move(List<TileEntity> path, Action onComplete = null)
        {
            if (path != null)
            {
                if (_movingCoroutine != null)
                {
                    StopCoroutine(_movingCoroutine);
                }
                _movingCoroutine = StartCoroutine(Moving(path));
            }
            else
            {
                IsMoveCompleted = true;
                onComplete?.Invoke();
            }
        }

        IEnumerator Moving(List<TileEntity> path, Action onComplete = null)
        {
            var nextIndex = 0;
            transform.position = runtimeStats.MapEntity.Settings.Projection(transform.position);
            _actionPanel.SetActive(false);
            while (nextIndex < path.Count)
            {
                var targetPoint = runtimeStats.MapEntity.WorldPosition(path[nextIndex]);
                var stepDir = (targetPoint - transform.position) * Speed;
                if (runtimeStats.MapEntity.RotationType == RotationType.LookAt)
                {
                    RotationNode.rotation = Quaternion.LookRotation(stepDir, Vector3.up);
                }
                else if (runtimeStats.MapEntity.RotationType == RotationType.Flip)
                {
                    RotationNode.rotation = runtimeStats.MapEntity.Settings.Flip(stepDir);
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
            _actionPanel.SetActive(IsSelected);
            onComplete?.Invoke();
        }

        public void ChangeSelected(bool select)
        {
            IsSelected = select;
            _actionPanel.SetActive(select);
            _attackComponent.ExitAttackMode();
        }
        #endregion

        #region  Support Methods
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
        public int GetMoveRange() => runtimeStats.MoveRange;
        public PlayerID GetOwner() => runtimeStats.Owner;
        public void TakeDamage(float damage)
        {
            runtimeStats.Health -= (int)damage;
            _healthBar.UpdateHealthBar((float)runtimeStats.Health / runtimeStats.MaxHealth);
            if (runtimeStats.Health <= 0)
            {
                Destroy(gameObject);
            }
        }

        public void FinishTurnActions()
        {
            IsMoveCompleted = true;
            IsActionCompleted = true;
            ChangeSelected(false);
            AreaPathManager.Instance.ResetAll(this);
        }

        #endregion

    }

    public class UnitRuntimeStats
    {
        public int Health;
        public float AttackDamage;
        public int MaxHealth;
        public int MoveRange;
        public int AttackRange;
        public MapEntity MapEntity { get; private set; }
        public PlayerID Owner;
        public Action OnFinishTurn;

        public UnitRuntimeStats()
        {
        }

        public UnitRuntimeStats SetOwner(PlayerID owner)
        {
            Owner = owner;
            return this;
        }

        public UnitRuntimeStats AssignMap(MapEntity map)
        {
            MapEntity = map;
            return this;
        }

        public UnitRuntimeStats ReCalculateStats(UnitData baseData)
        {
            Health = baseData.Health;
            MaxHealth = baseData.Health;
            MoveRange = Mathf.FloorToInt(baseData.moveRange);
            AttackRange = Mathf.FloorToInt(baseData.attackRange);
            AttackDamage = baseData.attackDamage;
            OnFinishTurn = null;
            return this;
        }
    }
}
