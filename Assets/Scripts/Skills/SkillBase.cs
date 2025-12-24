using UnityEngine;
using System.Collections.Generic;
using TurnBasedGame.Unit;
using RedBjorn.ProtoTiles.Example;
using TurnBasedGame.ObjectPool;
using System.Collections;
using Unity.VisualScripting;

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
        [SerializeField] protected float delay = 0f;
        [SerializeField] protected GameObject vfxPrefab;

        [Header("Target Settings")]
        [SerializeField] protected bool canTargetAllies;
        [SerializeField] protected bool canTargetEnemies = true;
        [SerializeField] protected bool canTargetSelf;
        [SerializeField] protected bool canTargetEmptyTile;

        protected int currentCooldown;

        #region ISkill Properties
        public string SkillName => skillName;
        public string Description => description;
        public SkillType Type => skillType;
        public Sprite Icon => icon;
        public int Cooldown => cooldown;
        public int CurrentCooldown => currentCooldown;
        public int Range => range;
        #endregion

        #region Template Method - Validation Pipeline
        public virtual bool CanUse(UnitMove caster, Vector3Int targetPos)
        {
            if (!ValidateCooldown()) return false;
            if (!ValidateMana(caster)) return false;
            if (!ValidateRange(caster, targetPos)) return false;
            if (!ValidateTarget(caster, targetPos)) return false;
            if (!ValidateCustomConditions(caster, targetPos)) return false;

            return true;
        }

        protected virtual bool ValidateCooldown()
        {
            if (skillType == SkillType.Normal) return true;
            return currentCooldown <= 0;
        }

        protected virtual bool ValidateMana(UnitMove caster)
        {
            // todo: tính toán mana cost với các hiệu ứng giảm mana nếu có
            return true;
        }

        protected virtual bool ValidateRange(UnitMove caster, Vector3Int targetPos)
        {
            var distance = MapManager.Instance.GetDistance(caster.currentGridPosition, targetPos);
            return distance <= range;
        }

        protected virtual bool ValidateTarget(UnitMove caster, Vector3Int targetPos)
        {
            var targetUnit = MapManager.Instance?.GetUnitAtTile(targetPos);
            
            if (targetUnit == null)
                return canTargetEmptyTile;

            if (targetUnit == caster)
                return canTargetSelf;

            bool isSameOwner = targetUnit.GetOwner() == caster.GetOwner();
            
            if (isSameOwner && canTargetAllies) return true;
            if (!isSameOwner && canTargetEnemies) return true;

            return false;
        }

        protected virtual bool ValidateCustomConditions(UnitMove caster, Vector3Int targetPos)
        {
            return true;
        }
        #endregion

        #region Template Method - Execution Pipeline
        public void Execute(UnitMove caster, Vector3Int targetPos)
        {
            Updater.Instance.StartCoroutine(ExcuteAsync(caster, targetPos));
        }

        IEnumerator ExcuteAsync(UnitMove caster, Vector3Int targetPos)
        {
            OnExecuteStart(caster, targetPos);
            
            ConsumeMana(caster);
            yield return new WaitForSeconds(delay);
            ExecuteEffect(caster, targetPos);
            StartCooldown();
            
            OnExecuteComplete(caster, targetPos);
        }

        protected virtual void OnExecuteStart(UnitMove caster, Vector3Int targetPos)
        {
            Debug.Log($"{caster.name} sử dụng {skillName}");
        }

        protected virtual void ConsumeMana(UnitMove caster)
        {
            // if (skillType != SkillType.Normal)
            // {
            //     caster.RuntimeStats.CurrentMana -= manaCost;
            // }
        }

        protected abstract void ExecuteEffect(UnitMove caster, Vector3Int targetPos);
       
        protected virtual void StartCooldown()
        {
            if (skillType != SkillType.Normal)
            {
                currentCooldown = cooldown;
            }
        }

        protected virtual void OnExecuteComplete(UnitMove caster, Vector3Int targetPos)
        {
            SkillEventBus.Instance?.TriggerSkillUsed(this, caster, targetPos);
        }
        #endregion

        #region Abstract Methods - Phải implement ở subclass
        public abstract List<Vector3Int> GetAffectedTiles(Vector3Int targetPos);
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
