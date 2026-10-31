using TurnBasedGame.Capture;
using TurnBasedGame.SpellCard;
using TurnBasedGame.Unit;
using UnityEngine;

namespace TurnBasedGame.Skills
{
    [CreateAssetMenu(fileName = "KnightHolySwordStanceSkill", menuName = "Skills/Knight Holy Sword Stance", order = 6)]
    public class KnightHolySwordStanceSkill : SkillBase
    {
        [Header("Line Damage Settings")]
        [SerializeField, Min(1)] private int lineLength = 3;
        [SerializeField, Min(0f)] private float damageMultiplier = 1.6f;

        [Header("Guard Break Settings")]
        [SerializeField, Min(1)] private int guardBreakDuration = 1;
        [SerializeField, Min(0)] private int guardBreakIncomingDamagePercent = 20;

        [Header("Stance Guard Settings")]
        [SerializeField, Min(1)] private int stanceGuardDuration = 1;
        [SerializeField, Min(0)] private int stanceGuardDamageReductionPercent = 20;
        [SerializeField, Min(0)] private int capturePointBonusDuration = 1;

        protected override void ExecuteEffect(UnitController caster, Vector3Int targetPos)
        {
            Vector3Int direction = GetDirection(caster.currentGridPosition, targetPos);
            if (direction == Vector3Int.zero)
                return;

            int damage = Mathf.RoundToInt(caster.GetCurrentDamage() * damageMultiplier);
            UnitController firstEnemy = null;

            for (int step = 1; step <= lineLength; step++)
            {
                Vector3Int tilePos = caster.currentGridPosition + direction * step;
                var unit = MapManager.Instance?.GetUnitAtTile(tilePos);
                if (unit == null || unit.IsDead() || unit.GetOwner() == caster.GetOwner())
                    continue;

                firstEnemy ??= unit;
                unit.TakeDamage(damage);
            }

            ApplyGuardBreak(caster, firstEnemy);
            ApplyStanceGuard(caster);
        }

        protected override bool ValidateCustomConditions(UnitController caster, Vector3Int targetPos)
        {
            return GetDirection(caster.currentGridPosition, targetPos) != Vector3Int.zero;
        }

        private static Vector3Int GetDirection(Vector3Int from, Vector3Int to)
        {
            Vector3Int delta = to - from;
            return new Vector3Int(
                Mathf.Clamp(delta.x, -1, 1),
                Mathf.Clamp(delta.y, -1, 1),
                Mathf.Clamp(delta.z, -1, 1));
        }

        private void ApplyGuardBreak(UnitController caster, UnitController firstEnemy)
        {
            if (firstEnemy == null || firstEnemy.IsDead())
                return;

            firstEnemy.BuffHandler?.AddEffect(new ActiveStatusEffect(
                StatusEffectType.GuardBreak,
                guardBreakIncomingDamagePercent,
                guardBreakDuration,
                caster.GetOwner()));
        }

        private void ApplyStanceGuard(UnitController caster)
        {
            int duration = stanceGuardDuration;
            if (CapturePointManager.Instance?.GetPointAt(caster.currentGridPosition) != null)
                duration += capturePointBonusDuration;

            caster.BuffHandler?.AddEffect(new ActiveStatusEffect(
                StatusEffectType.StanceGuard,
                stanceGuardDamageReductionPercent,
                duration,
                caster.GetOwner()));
        }
    }
}
