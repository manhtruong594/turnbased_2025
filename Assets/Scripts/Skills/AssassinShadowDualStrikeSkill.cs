using TurnBasedGame.SpellCard;
using TurnBasedGame.Unit;
using UnityEngine;

namespace TurnBasedGame.Skills
{
    [CreateAssetMenu(fileName = "AssassinShadowDualStrikeSkill", menuName = "Skills/Assassin Shadow Dual Strike", order = 10)]
    public class AssassinShadowDualStrikeSkill : SkillBase
    {
        [Header("Damage Settings")]
        [SerializeField, Min(0f)] private float firstHitMultiplier = 0.9f;
        [SerializeField, Min(0f)] private float secondHitMultiplier = 0.9f;
        [SerializeField, Min(0f)] private float executeSecondHitMultiplier = 1.4f;
        [SerializeField, Range(0f, 1f)] private float executeHealthThresholdPercent = 0.4f;

        [Header("Shadow Step Settings")]
        [SerializeField, Min(1)] private int shadowStepDuration = 1;

        protected override void ExecuteEffect(UnitController caster, Vector3Int targetPos)
        {
            var target = MapManager.Instance?.GetUnitAtTile(targetPos);
            if (target == null || target.IsDead())
                return;

            if (!TeleportUtility.TryFindAdjacentTileAroundTarget(caster, target, out var teleportTile))
                return;

            bool targetWasAlive = !target.IsDead();
            caster.TeleportTo(teleportTile);
            ApplyStrike(caster, target, firstHitMultiplier);

            if (!target.IsDead())
                ApplyStrike(caster, target, ResolveSecondHitMultiplier(target));

            if (targetWasAlive && target.IsDead())
                ApplyShadowStep(caster);
        }

        protected override bool ValidateCustomConditions(UnitController caster, Vector3Int targetPos)
        {
            var target = MapManager.Instance?.GetUnitAtTile(targetPos);
            return target != null &&
                   !target.IsDead() &&
                   TeleportUtility.TryFindAdjacentTileAroundTarget(caster, target, out _);
        }

        private void ApplyStrike(UnitController caster, UnitController target, float multiplier)
        {
            int damage = Mathf.RoundToInt(caster.GetCurrentDamage() * multiplier);
            target.TakeDamage(damage);
        }

        private float ResolveSecondHitMultiplier(UnitController target)
        {
            return target.GetHealthPercent() <= executeHealthThresholdPercent
                ? executeSecondHitMultiplier
                : secondHitMultiplier;
        }

        private void ApplyShadowStep(UnitController caster)
        {
            caster.BuffHandler?.AddEffect(new ActiveStatusEffect(
                StatusEffectType.ShadowStep,
                0,
                shadowStepDuration,
                caster.GetOwner()));
        }
    }
}
