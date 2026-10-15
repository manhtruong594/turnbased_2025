using UnityEngine;
using System.Collections.Generic;
using TurnBasedGame.Unit;
using TurnBasedGame.ObjectPool;
using Unity.VisualScripting;
using System;
using RedBjorn.ProtoTiles.Example;
using TurnBasedGame.Resources;
using TurnBasedGame.EditorSupport;
using TurnBasedGame.SpellCard;

namespace TurnBasedGame.Skills
{
    public enum SkillBuffVisualAction
    {
        Add,
        Remove
    }

    public readonly struct SkillVisualBuffData
    {
        public readonly BuffDebuffHandler Handler;
        public readonly ActiveStatusEffect Effect;
        public readonly SkillBuffVisualAction Action;

        public SkillVisualBuffData(BuffDebuffHandler handler, ActiveStatusEffect effect, SkillBuffVisualAction action)
        {
            Handler = handler;
            Effect = effect;
            Action = action;
        }
    }

    public readonly struct SkillVisualHitData
    {
        public readonly UnitController Target;
        public readonly int Value;
        public readonly int HealthAfter;
        public readonly int MaxHealth;
        public readonly bool IsLethal;
        public readonly bool IsHeal;

        public SkillVisualHitData(UnitController target, int value, int healthAfter, int maxHealth, bool isLethal, bool isHeal = false)
        {
            Target = target;
            Value = value;
            HealthAfter = healthAfter;
            MaxHealth = maxHealth;
            IsLethal = isLethal;
            IsHeal = isHeal;
        }
    }

    public class VisualHitStep
    {
        public readonly List<SkillVisualHitData> Hits = new();
        public readonly List<SkillVisualBuffData> Buffs = new();
    }

    /// <summary>
    /// Base class cho tất cả skills (Template Method Pattern + Strategy Pattern)
    /// Định nghĩa template algorithm, subclass implement các bước cụ thể
    /// </summary>
    public abstract class SkillBase : ScriptableObject, ISkill
    {
        [Header("Basic Info")]
        [SerializeField] protected string skillName = "Unnamed Skill";
        [SerializeField, TextArea(3, 5)] protected string description = "Không có mô tả";
        [SerializeField] protected SkillType skillType;
        [SerializeField, SpritePreview] protected Sprite icon;
        [SerializeField] protected int cooldown;
        [SerializeField, Min(0)] protected int mpCost = 1;
        [SerializeField] protected int range = 1;

        [Header("VFX Settings")]
        [SerializeField] private SkillVfxConfig vfxConfig = new SkillVfxConfig();

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
        internal void ApplyReplicaCooldown(int value) => currentCooldown = value;
        public int MPCost => skillType == SkillType.Normal ? 0 : mpCost;
        public int Range => range;
        public string SkillContentId { get; private set; }
        internal Action CaptureRollback()
        {
            int savedCooldown = currentCooldown;
            return () =>
            {
                currentCooldown = savedCooldown;
                _isExecuting = false;
                _pendingHitSteps.Clear();
                _currentStep = null;
            };
        }
        public SkillVfxConfig VfxConfig => GetVfxConfig();

        public bool CanTargetAllies => (targetTypes & TargetType.Ally) != 0;
        public bool CanTargetEnemies => (targetTypes & TargetType.Enemy) != 0;
        public bool CanTargetSelf => (targetTypes & TargetType.Self) != 0;
        public bool CanTargetEmptyTile => (targetTypes & TargetType.EmptyTile) != 0;
        #endregion

        public static SkillBase ActiveExecutingSkill { get; private set; }
        private readonly Queue<VisualHitStep> _pendingHitSteps = new();
        private VisualHitStep _currentStep;

        public void RecordDamageHit(UnitController target, int damage, int healthAfterHit, int maxHealth, bool isLethal)
        {
            if (_currentStep == null)
                _currentStep = new VisualHitStep();

            _currentStep.Hits.Add(new SkillVisualHitData(target, damage, healthAfterHit, maxHealth, isLethal, false));
        }

        public void RecordHealHit(UnitController target, int healAmount, int healthAfterHit, int maxHealth)
        {
            if (_currentStep == null)
                _currentStep = new VisualHitStep();

            _currentStep.Hits.Add(new SkillVisualHitData(target, healAmount, healthAfterHit, maxHealth, false, true));
        }

        public void RecordBuffAction(BuffDebuffHandler handler, ActiveStatusEffect effect, SkillBuffVisualAction action)
        {
            if (_currentStep == null)
                _currentStep = new VisualHitStep();

            _currentStep.Buffs.Add(new SkillVisualBuffData(handler, effect, action));
        }

        public void AdvanceHitStep()
        {
            if (_currentStep != null && (_currentStep.Hits.Count > 0 || _currentStep.Buffs.Count > 0))
            {
                _pendingHitSteps.Enqueue(_currentStep);
                _currentStep = new VisualHitStep();
            }
        }

        public void FlushRemainingVisualHits()
        {
            while (_pendingHitSteps.Count > 0)
            {
                ApplyEffect();
            }
        }

        protected bool _isExecuting = false;
        protected virtual bool CanDirectTargetUntargetableUnit => false;
        ValueTuple<UnitController, Vector3Int> _currentExecutionContext;

        #region Template Method - Validation Pipeline
        public virtual bool CanUse(UnitController caster, Vector3Int targetPos)
        {
            if (caster == null || caster.IsDead() || (!caster.IgnoreSkillUseLimitsForTest && !caster.CanAct()))
            {
                Debug.LogWarning($"{skillName} cannot be used because the caster cannot act.");
                return false;
            }
            if (!caster.IgnoreSkillUseLimitsForTest && !ValidateCooldown())
            {
                Debug.LogWarning($"{skillName} is on cooldown.");
                return false;
            }
            if (!ValidateMP(caster))
            {
                Debug.LogWarning($"Not enough MP to use {skillName}. Required MP: {MPCost}.");
                return false;
            }
            if (!ValidateRange(caster, targetPos))
            {
                Debug.LogWarning($"{targetPos} is out of range for {skillName}.");
                return false;
            }
            if (!ValidateTarget(caster, targetPos))
            {
                Debug.LogWarning($"{skillName} cannot target the selected tile.");
                return false;
            }
            if (!ValidateCustomConditions(caster, targetPos))
            {
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

        protected virtual bool ValidateMP(UnitController caster)
        {
            if (caster != null && caster.IgnoreSkillUseLimitsForTest)
                return true;
            if (MPCost <= 0)
                return true;

            return caster != null
                && MPManager.Instance != null
                && MPManager.Instance.HasEnoughMP(caster.GetOwner(), MPCost);
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

            if (targetUnit.IsDead()) return false;
            if (targetUnit == caster)
                return CanTargetSelf;

            bool isSameOwner = targetUnit.GetOwner() == caster.GetOwner();

            if (isSameOwner && CanTargetAllies) return true;
            if (!isSameOwner && CanTargetEnemies)
            {
                if (!CanDirectTargetUntargetableUnit &&
                    targetUnit.BuffHandler != null &&
                    targetUnit.BuffHandler.IsUntargetableDirectly())
                {
                    return false;
                }

                return true;
            }

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
            var result = TurnBasedGame.Command.LocalMatchAuthority.SubmitHumanSkill(caster, this, targetPos,
                completed => { if (!completed.Succeeded) SkillEventBus.Instance?.TriggerSkillFailed(this, caster, completed.FailureReason); });
            if (!result.Succeeded && !result.Pending)
                SkillEventBus.Instance?.TriggerSkillFailed(this, caster, result.FailureReason);
        }

        internal bool ExecuteAuthorized(UnitController caster, Vector3Int targetPos)
        {
            if (_isExecuting) return false;
            if (!TrySpendMP(caster))
            {
                Debug.LogWarning($"Not enough MP to use {skillName}. Required MP: {MPCost}.");
                SkillEventBus.Instance?.TriggerSkillFailed(this, caster, "Not enough MP.");
                return false;
            }

            _currentExecutionContext = (caster, targetPos);
            _isExecuting = true;
            _pendingHitSteps.Clear();
            _currentStep = new VisualHitStep();
            ActiveExecutingSkill = this;
            try
            {
                ExecuteEffect(caster, targetPos);
                if (_currentStep != null && (_currentStep.Hits.Count > 0 || _currentStep.Buffs.Count > 0))
                {
                    _pendingHitSteps.Enqueue(_currentStep);
                    _currentStep = null;
                }
                StartCooldown();
            }
            finally
            {
                _isExecuting = false;
                ActiveExecutingSkill = null;
                _currentStep = null;
            }

            if (caster != null) caster.FinishTurnActionsAuthorized();
            // Animation cues are presentation only; gameplay has already resolved once.
            TurnBasedGame.Command.LocalMatchAuthority.PublishAfterCommit(() =>
            {
                if (caster != null && !caster.IsDead()) caster.PerformSkill(this, targetPos);
                OnExecuteComplete(caster, targetPos);
            });
            return true;
        }

        private bool TrySpendMP(UnitController caster)
        {
            if (caster != null && caster.IgnoreSkillUseLimitsForTest)
                return true;
            if (MPCost <= 0)
                return true;

            return caster != null
                && MPManager.Instance != null
                && MPManager.Instance.SpendMP(caster.GetOwner(), MPCost);
        }

        protected virtual void StartCooldown()
        {
            if (skillType != SkillType.Normal)
            {
                currentCooldown = cooldown;
            }
        }

        public virtual void ApplyEffect()
        {
            if (_pendingHitSteps.Count > 0)
            {
                var step = _pendingHitSteps.Dequeue();
                foreach (var hit in step.Hits)
                {
                    if (hit.Target != null)
                        hit.Target.ShowVisualHit(hit);
                }
                foreach (var buff in step.Buffs)
                {
                    if (buff.Handler != null)
                        buff.Handler.ShowVisualBuff(buff);
                }
            }
        }

        protected virtual void OnExecuteComplete(UnitController caster, Vector3Int targetPos)
        {
            SkillEventBus.Instance?.TriggerSkillUsed(this, caster, targetPos);
        }
        protected virtual void ExecuteEffect(UnitController caster, Vector3Int targetPos)
        {
        }

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
            var clone = Instantiate(this);
            clone.SkillContentId = TurnBasedGame.Command.LocalMatchAuthority.Content?.GetId(this);
            return clone;
        }
        public GameObject GetCastVfxPrefab()
        {
            return GetVfxConfig().CastVfxPrefab;
        }

        public GameObject GetReleaseVfxPrefab()
        {
            var config = GetVfxConfig();
            return config.ReleaseVfxPrefab;
        }

        public GameObject GetProjectilePrefab()
        {
            return GetVfxConfig().ProjectilePrefab;
        }

        public GameObject GetImpactVfxPrefab()
        {
            var config = GetVfxConfig();
            if (config.ImpactVfxPrefab != null)
                return config.ImpactVfxPrefab;
            return null;
        }

        public SkillEffectApplyTiming ResolveEffectApplyTiming(bool hasProjectile)
        {
            var config = GetVfxConfig();
            if (config.EffectApplyTiming != SkillEffectApplyTiming.Automatic)
                return config.EffectApplyTiming;

            return hasProjectile ? SkillEffectApplyTiming.OnProjectileImpact : SkillEffectApplyTiming.OnAnimationImpact;
        }

        private SkillVfxConfig GetVfxConfig()
        {
            if (vfxConfig == null)
                vfxConfig = new SkillVfxConfig();

            return vfxConfig;
        }

        public Vector3 GetCurrentTargetWorldPosition()
        {
            var targetPos = _currentExecutionContext.Item2;
            return GetMap().WorldPosition(targetPos);
        }

        #endregion
    }
}
