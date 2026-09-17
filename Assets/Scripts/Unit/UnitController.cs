using System;
using System.Collections;
using System.Collections.Generic;
using RedBjorn.ProtoTiles;
using RedBjorn.ProtoTiles.Example;
using TurnBasedGame.Command;
using TurnBasedGame.Core;
using TurnBasedGame.Skills;
using TurnBasedGame.SpellCard;
using TurnBasedGame.UI;
using UnityEngine;
using UnityEngine.UI;

namespace TurnBasedGame.Unit
{
    public class UnitController : MonoBehaviour
    {
        public float Speed = 5f;
        public bool IsSelected { get; private set; }
        public Transform RotationNode;
        [SerializeField] Button _cancelMoveButton;

        readonly UnitRuntimeStats runtimeStats = new();
        Coroutine _movingCoroutine;
        Coroutine _deathCoroutine;
        private Vector3Int _previousMovePosition;
        private bool _canUndoMove;
        private ulong _undoAtSequence;
        private ActiveStatusEffect[] _effectsBeforeMove;
        private TurnBasedGame.Multiplayer.Protocol.MatchRandomState _randomBeforeMove;
        private TurnBasedGame.Capture.CapturePoint _movedCapturePoint;
        private PlayerID? _previousCaptureOwner;
        public bool IsMoving => _movingCoroutine != null;
        internal bool CanUndoAt(ulong sequence) => _canUndoMove && (!LocalMatchAuthority.IsAuthoritative || _undoAtSequence == sequence) && CanAct();
        internal void RefreshReplicaPresentation()
        {
            if (IsActionCommitted || !TurnBasedGame.Multiplayer.MatchGameplayBootstrap.CanControl(GetOwner()))
            {
                ChangeSelected(false);
                AreaPathManager.Instance?.ResetAll(this);
            }
            _attackComponent.RefreshReplica();
        }

        internal Action CaptureRollback()
        {
            int health = runtimeStats.Health;
            bool dead = runtimeStats.IsDead, moved = runtimeStats.IsMoveCompleted, acted = runtimeStats.IsActionCompleted;
            bool attackMode = runtimeStats.IsInAttackMode, undo = _canUndoMove;
            var grid = currentGridPosition; var world = transform.position; var rotation = transform.rotation;
            var restoreEffects = _buffHandler?.CaptureRollback();
            var cooldowns = new List<Action>();
            if (_attackComponent != null)
                foreach (var entry in _attackComponent.ActiveSkills)
                    if (entry is SkillBase skill) cooldowns.Add(skill.CaptureRollback());
            return () =>
            {
                if (this == null) return;
                if (_movingCoroutine != null) StopCoroutine(_movingCoroutine);
                if (_deathCoroutine != null && !dead) StopCoroutine(_deathCoroutine);
                _movingCoroutine = null; _deathCoroutine = null; OnCompleteMove = null;
                runtimeStats.Health = health; runtimeStats.IsDead = dead;
                runtimeStats.IsMoveCompleted = moved; runtimeStats.IsActionCompleted = acted;
                runtimeStats.IsInAttackMode = attackMode; _canUndoMove = undo;
                currentGridPosition = grid; transform.SetPositionAndRotation(world, rotation);
                restoreEffects?.Invoke(); foreach (var restore in cooldowns) restore();
            };
        }
        private Transform _myTrans;
        public Vector3Int currentGridPosition { get; private set; }
        public ulong UnitRuntimeId { get; private set; }
        public string UnitContentId { get; private set; }

        internal void AssignMatchIdentity(ulong runtimeId, string contentId)
        {
            UnitRuntimeId = runtimeId;
            UnitContentId = contentId;
        }

        internal void ApplyReplica(TurnBasedGame.Multiplayer.Protocol.MatchStateChange state)
        {
            StopAllCoroutines(); _movingCoroutine = null; _deathCoroutine = null; OnCompleteMove = null;
            _canUndoMove = state.Value4 != 0;
            runtimeStats.Health = state.Value; runtimeStats.IsDead = false;
            runtimeStats.IsMoveCompleted = state.Value2 != 0; runtimeStats.IsActionCompleted = state.Value3 != 0;
            currentGridPosition = new Vector3Int(state.Position.X, state.Position.Y, state.Position.Z);
            transform.position = MapManager.Instance.MapEntity.WorldPosition(currentGridPosition);
            MapManager.Instance.RegisterUnit(currentGridPosition, this);
            if (_cancelMoveButton != null) _cancelMoveButton.interactable = _canUndoMove;
            _healthBar?.UpdateHealthBar(GetHealthPercent());
        }

        private void OnDestroy()
        {
            LocalMatchAuthority.Runtime.RemoveUnit(this);
        }

        [Header("Other Components")]
        [SerializeField] private UnitData unitData;
        [SerializeField] private UnitAttack _attackComponent;
        [SerializeField] private BuffDebuffHandler _buffHandler;
        [SerializeField] private HealthBar _healthBar;
        [SerializeField] private UnitAnimator _unitAnimator;
        [SerializeField] private GameObject _actionPanel;

        private void Awake()
        {
            _attackComponent = GetComponent<UnitAttack>();
            _myTrans = transform;
            _cancelMoveButton.onClick.AddListener(() =>
            {
                // H?y di chuy?n v� tr? v? v? tr� ban d?u
                LocalMatchAuthority.SubmitHumanUnitAction(this, true);
            });
        }

        public void Init(PlayerID owner, Vector3Int startGridPos)
        {
            CreateStats();
            ResetComponents();
            _attackComponent.Init(runtimeStats, this);
            InitBuffHandler();
            UpdateGridPosition(startGridPos);

            void CreateStats()
            {
                runtimeStats.ReCalculateStats(unitData)
                    .SetOwner(owner)
                    .AssignMap(MapManager.Instance.MapEntity);
            }
        }

        private void InitBuffHandler()
        {
            if (_buffHandler == null)
                _buffHandler = GetComponent<BuffDebuffHandler>();
            _buffHandler?.Init(this);
        }

        public void ResetMove()
        {
            if (!LocalMatchAuthority.IsAuthoritative) return;
            _canUndoMove = false;
            IsSelected = false;
            runtimeStats.IsMoveCompleted = false;
            runtimeStats.IsActionCompleted = false;
        }

        #region  Movement Methods
        public void Move(List<TileEntity> path, Action onComplete = null)
        {
            if (path == null || path.Count == 0)
            {
                onComplete?.Invoke();
                return;
            }

            var destination = path[path.Count - 1].Position;
            var result = LocalMatchAuthority.SubmitHumanMove(this, destination, onComplete);
            if (!result.Succeeded && !result.Pending)
                onComplete?.Invoke();
        }

        internal bool TryMoveAuthorized(
            List<TileEntity> path,
            Action onComplete,
            out string failureReason)
        {
            if (!CanMove() || Speed <= 0 || path == null || path.Count == 0)
            {
                failureReason = "Unit không thể di chuyển hoặc path không hợp lệ.";
                return false;
            }

            _tempPath = path;
            OnCompleteMove = onComplete;
            _previousMovePosition = currentGridPosition;
            _randomBeforeMove = LocalMatchAuthority.Random.Capture();
            _effectsBeforeMove = _buffHandler != null ? new List<ActiveStatusEffect>(_buffHandler.ActiveEffects).ToArray() : null;
            _undoAtSequence = LocalMatchAuthority.ServerSequence + 1;
            _movedCapturePoint = TurnBasedGame.Capture.CapturePointManager.Instance?.GetPointAt(path[path.Count - 1].Position);
            _previousCaptureOwner = _movedCapturePoint != null ? _movedCapturePoint.Owner : null;
            _canUndoMove = true;
            UpdateGridPosition(path[path.Count - 1].Position);
            runtimeStats.IsMoveCompleted = true;
            MoveCommand();
            failureReason = null;
            return true;
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
            AreaPathManager.Instance.IsLocked = true;
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
            _movingCoroutine = null;
            _actionPanel.SetActive(!TurnBasedGame.UI.BattleHUDToolkit.IsAvailable && IsSelected);
            _attackComponent.ShowToolkitActions(IsSelected);
            _cancelMoveButton.interactable = true;
            _unitAnimator.StopMoving();
            AreaPathManager.Instance.IsLocked = false;
            var complete = OnCompleteMove;
            OnCompleteMove = null;
            complete?.Invoke();
        }

        public void ChangeSelected(bool select)
        {
            IsSelected = select;
            _actionPanel.SetActive(!TurnBasedGame.UI.BattleHUDToolkit.IsAvailable && select);
            _attackComponent.ShowToolkitActions(select);
            _attackComponent.ExitAttackMode();
        }

        public void UndoMoveAction(Vector3Int previousPosition)
        {
            LocalMatchAuthority.SubmitHumanUnitAction(this, true);
        }

        internal bool TryUndoMoveAuthorized(out string reason)
        {
            reason = "Không thể hủy di chuyển.";
            if (!_canUndoMove || IsMoving || !CanAct() || LocalMatchAuthority.ServerSequence != _undoAtSequence ||
                !MapManager.Instance.IsTileAvailable(_previousMovePosition)) return false;
            _canUndoMove = false;
            if (_movedCapturePoint != null) _movedCapturePoint.RestoreOwnerAuthorized(_previousCaptureOwner);
            // Di chuyển về vị trí trước đó
            _myTrans.position = runtimeStats.MapEntity.WorldPosition(_previousMovePosition);
            UpdateGridPosition(_previousMovePosition);
            if (_buffHandler != null && _effectsBeforeMove != null) _buffHandler.RestoreEffectsAuthorized(_effectsBeforeMove);
            LocalMatchAuthority.Random.Restore(_randomBeforeMove);
            runtimeStats.IsMoveCompleted = false;
            _cancelMoveButton.interactable = false;
            AreaPathManager.Instance.RealeaseSelectedUnit();
            reason = null;
            return true;
        }

        /// <summary>
        /// Cập nhật vị trí grid của unit trên MapManager
        /// </summary>
        public void UpdateGridPosition(Vector3Int newGridPos)
        {
            if (!LocalMatchAuthority.IsAuthoritative) return;
            var oldPosition = currentGridPosition;
            if (MapManager.Instance.GetUnitAtTile(oldPosition) == this)
                MapManager.Instance.UnregisterUnit(oldPosition);
            currentGridPosition = newGridPos;
            MapManager.Instance.RegisterUnit(newGridPos, this);
            GameMediator.Instance?.NotifyUnitMoved(this, oldPosition, newGridPos);
        }

        public void TeleportTo(Vector3Int newGridPos)
        {
            if (!LocalMatchAuthority.IsAuthoritative) return;
            var map = runtimeStats.MapEntity ?? MapManager.Instance?.MapEntity;
            if (map == null || map.Tile(newGridPos) == null)
                return;

            if (_movingCoroutine != null)
            {
                StopCoroutine(_movingCoroutine);
                _movingCoroutine = null;
            }

            _myTrans.position = map.WorldPosition(newGridPos);
            UpdateGridPosition(newGridPos);
        }
        #endregion

        #region  Support Methods

        public void PerformSkill(SkillBase skill, Vector3Int targetPos)
        {
            var targetPoint = runtimeStats.MapEntity.WorldPosition(targetPos);
            RotationNode.LookAt(targetPoint);
            _unitAnimator.PlayAttack(skill);
        }

        public void OnTurnBegin()
        {
            if (!LocalMatchAuthority.IsAuthoritative || IsDead()) return;
            TileHazardManager.Instance?.TryApplyTurnStartEffect(this);
            _attackComponent.ReduceSkillsCooldowns();
            ResetComponents();
            _buffHandler?.TickEffects();
        }
    
        public void ResetComponents()
        {
            ResetMove();
            _cancelMoveButton.interactable = false;
        }

        public UnitAttack AttackComponent => _attackComponent;
        public UnitData UnitData => unitData;
        public int GetMoveRange()
        {
            int moveRange = runtimeStats.MoveRange + (_buffHandler?.GetMoveBonus() ?? 0);
            int penalty = _buffHandler?.GetMovePercentPenalty() ?? 0;
            return Mathf.Max(0, Mathf.FloorToInt(moveRange * (1f - penalty / 100f)));
        }
        public int GetCurrentHealth() => runtimeStats.Health;
        public float GetHealthPercent() => (float)runtimeStats.Health / runtimeStats.MaxHealth;
        public PlayerID GetOwner() => runtimeStats.Owner;
        public bool IsMoveDone() => runtimeStats.IsMoveCompleted;
        internal bool IsActionCommitted => runtimeStats.IsActionCompleted;
        public int GetCurrentDamage()
        {
            int flatDamage = runtimeStats.BaseDamage + (_buffHandler?.GetDamageBonus() ?? 0);
            int percentBonus = _buffHandler?.GetDamagePercentBonus() ?? 0;
            int percentPenalty = _buffHandler?.GetDamagePercentPenalty() ?? 0;
            return Mathf.Max(0, Mathf.RoundToInt(flatDamage * (1f + (percentBonus - percentPenalty) / 100f)));
        }
        public bool CanMove()
        {
            if (!CanAct()) return false;
            if (_movingCoroutine != null) return false;
            if (runtimeStats.IsMoveCompleted || runtimeStats.IsInAttackMode) return false;
            if (_buffHandler != null && _buffHandler.IsRooted()) return false;
            return true;
        }
        public bool IsActionFinished() => !CanAct();
        public bool CanAct() => !runtimeStats.IsDead && !runtimeStats.IsActionCompleted &&
                                (_buffHandler == null || !_buffHandler.PreventsAction());
        public bool IsDead() => runtimeStats.IsDead;

        public BuffDebuffHandler BuffHandler => _buffHandler;

        public void TakeDamage(float damage, bool canBreakFreeze = true)
        {
            if (!LocalMatchAuthority.IsAuthoritative || IsDead()) return;
            // �p d?ng Shield gi?m s�t thuong
            if (damage > 0 && _buffHandler != null)
                damage = _buffHandler.ModifyIncomingDamage(damage, canBreakFreeze);

            runtimeStats.Health -= (int)damage;
            runtimeStats.Health = Mathf.Clamp(runtimeStats.Health, 0, runtimeStats.MaxHealth);
            _healthBar.UpdateHealthBar((float)runtimeStats.Health / runtimeStats.MaxHealth);
            if (runtimeStats.Health <= 0)
            {
                runtimeStats.IsDead = true;
                _unitAnimator.PlayDeath();
                MapManager.Instance.UnregisterUnit(currentGridPosition);
                LocalMatchAuthority.Runtime.RemoveUnit(this);
                UnitSpawner.Instance?.RemoveDeadUnit(this);
                _deathCoroutine = StartCoroutine(DelayDead());
            }
            else if (damage > 0)
            {
                _unitAnimator.PlayHit();
            }
            FloatingTextSpawner.Instance?.SpawnDamage(transform.position, (int)damage);
        }

        public void TakeNonLethalDamage(float damage)
        {
            if (!LocalMatchAuthority.IsAuthoritative || IsDead()) return;
            int safeDamage = Mathf.Max(0, Mathf.FloorToInt(damage));
            if (safeDamage <= 0 || runtimeStats.Health <= 1)
                return;

            int previousHealth = runtimeStats.Health;
            runtimeStats.Health = Mathf.Max(1, runtimeStats.Health - safeDamage);
            int actualDamage = previousHealth - runtimeStats.Health;
            _healthBar.UpdateHealthBar((float)runtimeStats.Health / runtimeStats.MaxHealth);
            FloatingTextSpawner.Instance?.SpawnDamage(transform.position, actualDamage);
        }
        
        public void Heal(int healAmount)
        {
            if (!LocalMatchAuthority.IsAuthoritative || IsDead()) return;
            if (_buffHandler != null && !_buffHandler.CanReceiveHealing())
            {
                Debug.Log($"[Heal] {name} cannot receive healing due to HealBan.");
                return;
            }

            runtimeStats.Health += healAmount;
            runtimeStats.Health = Mathf.Clamp(runtimeStats.Health, 0, runtimeStats.MaxHealth);
            _healthBar.UpdateHealthBar((float)runtimeStats.Health / runtimeStats.MaxHealth);
            FloatingTextSpawner.Instance?.SpawnHeal(transform.position, healAmount);
        }

        IEnumerator DelayDead()
        {
            yield return new WaitForSeconds(2f);
            DestroyImmediate(gameObject);
        }

        public void FinishTurnActions()
        {
            LocalMatchAuthority.SubmitHumanUnitAction(this);
        }

        internal void FinishTurnActionsAuthorized()
        {
            if (runtimeStats.IsActionCompleted)
                return;
            runtimeStats.IsMoveCompleted = true;
            runtimeStats.IsActionCompleted = true;
            _canUndoMove = false;
            _buffHandler?.RemoveExpiredEffects();
            _attackComponent.ExitAttackMode();
            ChangeSelected(false);
            AreaPathManager.Instance.ResetAll(this);
        }

        #endregion

    }

    public class UnitRuntimeStats
    {
        public int Health;
        public int MaxHealth;
        public int BaseDamage;
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
            BaseDamage = baseData.BaseDamage;
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
