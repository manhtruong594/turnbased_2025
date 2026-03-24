using UnityEngine;
using System.Collections.Generic;
using TurnBasedGame.Unit;
using TurnBasedGame.ObjectPool;
using System.Collections;
using Unity.VisualScripting;
using System;
using RedBjorn.ProtoTiles.Example;

namespace TurnBasedGame.Skills
{
    /// <summary>
    /// Base class cho tất cả skills (Template Method Pattern + Strategy Pattern)
    /// Định nghĩa template algorithm, subclass implement các bước cụ thể
    /// </summary>
    public abstract class SkillBase : ScriptableObject, ISkill
    {
        [Header("Basic Info")]
        [SerializeField] protected string skillName = "Unnamed Skill";
        [SerializeField, TextArea(3, 5)] protected string description = "No description";
        [SerializeField] protected SkillType skillType;
        [SerializeField] protected Sprite icon;
        [SerializeField] protected int cooldown;
        [SerializeField] protected int range = 1;

        [Header("Effects Settings")]
        public GameObject VfxPrefab;
        public GameObject VfxHitPrefab;
        public bool HasDelayApplyEffect = false;
        public float DelayApplyEffectTime = 0.5f;
        
        [Header("Target Settings")]
        [SerializeField] protected TargetType targetTypes;

        protected int currentCooldown;

        #region ISkill Properties
        public string SkillName => skillName;
        public string Description => description;
        public SkillType Type => skillType;
        public Sprite Icon => icon;
        public int Cooldown => cooldown;
        public int CurrentCooldown => currentCooldown;
        public int Range => range;

        public bool CanTargetAllies => (targetTypes & TargetType.Ally) != 0;
        public bool CanTargetEnemies => (targetTypes & TargetType.Enemy) != 0;
        public bool CanTargetSelf => (targetTypes & TargetType.Self) != 0;
        public bool CanTargetEmptyTile => (targetTypes & TargetType.EmptyTile) != 0;
        #endregion

        protected bool _isExecuting = false;
        ValueTuple<UnitController, Vector3Int> _currentExecutionContext;

        #region Template Method - Validation Pipeline
        public virtual bool CanUse(UnitController caster, Vector3Int targetPos)
        {
            if (!ValidateCooldown()) 
            {
                Debug.LogWarning($"{skillName} is on cooldown.");
                return false;
            }
            if (!ValidateRange(caster, targetPos)) {
                Debug.LogWarning($"{targetPos} is out of range for {skillName}.");
                return false;
            }
            if (!ValidateTarget(caster, targetPos)){
                Debug.LogWarning($"{skillName} cannot target the selected tile.");
                return false;
            }
            if (!ValidateCustomConditions(caster, targetPos)) {
                Debug.LogWarning($"{skillName} cannot be used due to custom conditions.");
                return false;
            }

            return true;
        }

        protected virtual bool ValidateCooldown()
        {
            if (skillType == SkillType.Normal) return true;
            return currentCooldown <= 0;
        }

        protected virtual bool ValidateRange(UnitController caster, Vector3Int targetPos)
        {
            var distance = MapManager.Instance.GetDistance(caster.currentGridPosition, targetPos);
            return distance <= range;
        }

        protected virtual bool ValidateTarget(UnitController caster, Vector3Int targetPos)
        {
            var targetUnit = MapManager.Instance?.GetUnitAtTile(targetPos);
            
            if (targetUnit == null)
                return CanTargetEmptyTile;

            if (targetUnit == caster || targetUnit.IsDead())
                return CanTargetSelf;

            bool isSameOwner = targetUnit.GetOwner() == caster.GetOwner();
            
            if (isSameOwner && CanTargetAllies) return true;
            if (!isSameOwner && CanTargetEnemies) return true;

            return false;
        }

        protected virtual bool ValidateCustomConditions(UnitController caster, Vector3Int targetPos)
        {
            return true;
        }
        #endregion

        #region Template Method - Execution Pipeline
        public void Execute(UnitController caster, Vector3Int targetPos)
        {
            Updater.Instance.StartCoroutine(ExcuteAsync(caster, targetPos));
        }

        const float EXECUTE_TIMEOUT = 10f;

        IEnumerator ExcuteAsync(UnitController caster, Vector3Int targetPos)
        {
            _isExecuting = true;
            _currentExecutionContext = (caster, targetPos);
            caster.PerformSkill(this, targetPos);
            StartCooldown();

            float elapsed = 0f;
            while (_isExecuting)
            {
                elapsed += Time.deltaTime;
                if (elapsed >= EXECUTE_TIMEOUT)
                {
                    Debug.LogError($"[{skillName}] OnHitTarget was never called! Forcing completion after {EXECUTE_TIMEOUT}s.");
                    _isExecuting = false;
                    break;
                }
                yield return null;
            }
            OnExecuteComplete(caster, targetPos);
        }
 
        protected virtual void StartCooldown()
        {
            if (skillType != SkillType.Normal)
            {
                currentCooldown = cooldown;
            }
        }

        public void ApplyEffect()
        {
            ExecuteEffect(_currentExecutionContext.Item1, _currentExecutionContext.Item2);
            _isExecuting = false;
        }

        protected virtual void OnExecuteComplete(UnitController caster, Vector3Int targetPos)
        {
            SkillEventBus.Instance?.TriggerSkillUsed(this, caster, targetPos);
            caster.FinishTurnActions();
        }

        public Vector3 GetCurrentTargetWorldPosition()
        {
            var (caster, targetPos) = _currentExecutionContext;
            return GetMap().WorldPosition(targetPos);
        }

        #endregion

        #region Abstract Methods - Phải implement ở subclass
        public abstract List<Vector3Int> GetAffectedTiles(Vector3Int targetPos);
        
        /// <summary>
        /// xử lý effect và tính toán tác động của skill
        /// </summary>
        protected abstract void ExecuteEffect(UnitController caster, Vector3Int targetPos);
        #endregion

        #region Cooldown Management
        public void ResetCooldown() => currentCooldown = 0;
        
        public void ReduceCooldown()
        {
            if (currentCooldown > 0)
                currentCooldown--;
        }
        #endregion

        #region Utility Methods

        protected RedBjorn.ProtoTiles.MapEntity GetMap()
        {
            return MapManager.Instance.MapEntity;
        }

        public virtual ISkill Clone()
        {
            return Instantiate(this);
        }
        #endregion
    }
}
