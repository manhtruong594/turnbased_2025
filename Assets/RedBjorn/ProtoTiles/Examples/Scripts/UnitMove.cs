using RedBjorn.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using TurnBasedGame.Command;
using TurnBasedGame.Core;
using TurnBasedGame.Skills;
using TurnBasedGame.Unit;
using UnityEngine;
using UnityEngine.UI;

namespace RedBjorn.ProtoTiles.Example
{
    public class UnitMove : MonoBehaviour
    {
        public float Speed = 5f;
        public bool IsSelected { get; private set; }
        public Transform RotationNode;
        [SerializeField] Button _cancelMoveButton;

        readonly UnitRuntimeStats runtimeStats = new();
        private CommandInvoker _commandInvoker = new CommandInvoker();
        ICommand _moveCommand;
        Coroutine _movingCoroutine;
        private Transform _myTrans;
        public Vector3Int currentGridPosition { get; private set; }

        [Header("Other Components")]
        [SerializeField] private UnitData unitData;
        [SerializeField] private UnitAttack _attackComponent;
        [SerializeField] private HealthBar _healthBar;
        [SerializeField] private UnitAnimator _unitAnimator;
        [SerializeField] private GameObject _actionPanel;

        private void Awake()
        {
            _attackComponent = GetComponent<UnitAttack>();
            _moveCommand= new MoveCommand(this);
            _myTrans = transform;
            _cancelMoveButton.onClick.AddListener(() =>
            {
                // Hủy di chuyển và trở về vị trí ban đầu
                _commandInvoker.UndoLastCommand();
            });
        }

        public void Init(PlayerID owner, Vector3Int startGridPos)
        {
            ResetComponents();
            UpdateGridPosition(startGridPos);
            CreateStats();
            _attackComponent.Init(runtimeStats, this);

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
            runtimeStats.IsMoveCompleted = false;
            runtimeStats.IsActionCompleted = false;
        }

        #region  Movement Methods
        public void Move(List<TileEntity> path, Action onComplete = null)
        {
            _tempPath = path;
            OnCompleteMove = onComplete;
            _commandInvoker.ExecuteCommand(_moveCommand);
        }

        public void MoveCommand()
        {
              if (_tempPath != null)
            {
                if (_movingCoroutine != null)
                {
                    StopCoroutine(_movingCoroutine);
                }
                _movingCoroutine = StartCoroutine(Moving(_tempPath));
                _unitAnimator.StartMoving();
            }
            else
            {
                runtimeStats.IsMoveCompleted = true;
                OnCompleteMove?.Invoke();
            }
        }

        List<TileEntity> _tempPath = new List<TileEntity>();
        Action OnCompleteMove;
        IEnumerator Moving(List<TileEntity> path)
        {
            var nextIndex = 0;
            _myTrans.position = runtimeStats.MapEntity.Settings.Projection(_myTrans.position);
            _actionPanel.SetActive(false);
            while (nextIndex < path.Count)
            {
                var targetPoint = runtimeStats.MapEntity.WorldPosition(path[nextIndex]);
                var stepDir = (targetPoint - _myTrans.position) * Speed;
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
                    _myTrans.position += stepDir * Time.deltaTime;
                    reached = Vector3.Dot(stepDir, (targetPoint - _myTrans.position)) < 0f;
                    yield return null;
                }
                _myTrans.position = targetPoint;
                nextIndex++;
            }
            UpdateGridPosition(path[path.Count - 1].Position);
            runtimeStats.IsMoveCompleted = true;
            _actionPanel.SetActive(IsSelected);
            OnCompleteMove?.Invoke();
            _cancelMoveButton.interactable = true;
            _unitAnimator.StopMoving();
        }

        public void ChangeSelected(bool select)
        {
            IsSelected = select;
            _actionPanel.SetActive(select);
            _attackComponent.ExitAttackMode();
        }

        public void UndoMoveAction(Vector3Int previousPosition)
        {
            // Di chuyển về vị trí trước đó
            _myTrans.position = runtimeStats.MapEntity.WorldPosition(previousPosition);
            UpdateGridPosition(previousPosition);
            runtimeStats.IsMoveCompleted = false;
            _cancelMoveButton.interactable = false;
            AreaPathManager.Instance.RealeaseSelectedUnit();
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
        #endregion

        #region  Support Methods

        public void PerformSkill(SkillBase skill, Vector3Int targetPos)
        {
            var targetPoint = runtimeStats.MapEntity.WorldPosition(targetPos);
            RotationNode.LookAt(targetPoint);
            _unitAnimator.PlayAttack(skill);
        }

        public UnitAttack AttackComponent => _attackComponent;

        public void ResetComponents()
        {
            ResetMove();
            _cancelMoveButton.interactable = false;
        }

        public UnitData UnitData => unitData;
        public int GetMoveRange() => runtimeStats.MoveRange;
        public int GetCurrentHealth() => runtimeStats.Health;
        public float GetHealthPercent() => (float)runtimeStats.Health / runtimeStats.MaxHealth;
        public PlayerID GetOwner() => runtimeStats.Owner;
        public bool IsMoveDone() => runtimeStats.IsMoveCompleted;
        public bool CanMove() => !runtimeStats.IsMoveCompleted && !runtimeStats.IsInAttackMode;
        public bool IsActionFinished() => runtimeStats.IsActionCompleted;
        public bool IsDead() => runtimeStats.IsDead;

        public void TakeDamage(float damage)
        {
            runtimeStats.Health -= (int)damage;
            _healthBar.UpdateHealthBar((float)runtimeStats.Health / runtimeStats.MaxHealth);
            if (runtimeStats.Health <= 0)
            {
                runtimeStats.IsDead = true;
                _unitAnimator.PlayDeath();
            }
            else if (damage > 0)
            {
                _unitAnimator.PlayHit();
            }
        }

        public void FinishTurnActions()
        {
            if (runtimeStats.IsActionCompleted)
                return;
            runtimeStats.IsMoveCompleted = true;
            runtimeStats.IsActionCompleted = true;
            _attackComponent.FinishAttack();
            ChangeSelected(false);
            AreaPathManager.Instance.ResetAll(this);
        }

        #endregion

    }

    public class UnitRuntimeStats
    {
        public int Health;
        public int MaxHealth;
        public int MoveRange;
        public MapEntity MapEntity { get; private set; }
        public PlayerID Owner;
        public Action OnFinishTurn;
        public bool IsInAttackMode;         // <=> attack mode active
        public bool IsMoveCompleted;        // <=> move done
        public bool IsActionCompleted;      // <=> attack done || move done & attack done
        public bool IsDead;

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
            IsInAttackMode = false;
            IsMoveCompleted = false;
            IsActionCompleted = false;
            IsDead = false;
            OnFinishTurn = null;
            return this;
        }
    }
}
