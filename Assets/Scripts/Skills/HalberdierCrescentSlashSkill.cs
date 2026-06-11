using System.Collections.Generic;
using TurnBasedGame.Capture;
using TurnBasedGame.SpellCard;
using TurnBasedGame.Unit;
using UnityEngine;

namespace TurnBasedGame.Skills
{
    [CreateAssetMenu(fileName = "HalberdierCrescentSlashSkill", menuName = "Skills/Halberdier Crescent Slash", order = 9)]
    public class HalberdierCrescentSlashSkill : SkillBase
    {
        [Header("Line Settings")]
        [SerializeField, Min(1)] private int lineLength = 2;
        [SerializeField, Min(0f)] private float primaryDamageMultiplier = 1.7f;
        [SerializeField, Min(0f)] private float secondaryDamageMultiplier = 1f;

        [Header("Heal Ban Settings")]
        [SerializeField, Min(1)] private int healBanDuration = 2;

        [Header("Shield Settings")]
        [SerializeField, Min(0f)] private float shieldValueMultiplier = 1f;
        [SerializeField, Min(1)] private int shieldDuration = 2;

        protected override void ExecuteEffect(UnitController caster, Vector3Int targetPos)
        {
            Vector3Int direction = SkillAreaUtility.NormalizeDirection(targetPos - caster.currentGridPosition);
            var hitTargets = DamageEnemiesInLine(caster, direction);

            if (hitTargets.Count > 0 && ShouldApplyHealBan(hitTargets[0], hitTargets.Count))
                ApplyHealBan(caster, hitTargets[0]);

            ApplyShield(caster);
        }

        protected override bool ValidateCustomConditions(UnitController caster, Vector3Int targetPos)
        {
            return SkillAreaUtility.NormalizeDirection(targetPos - caster.currentGridPosition) != Vector3Int.zero;
        }

        private List<UnitController> DamageEnemiesInLine(UnitController caster, Vector3Int direction)
        {
            var hitTargets = new List<UnitController>();
            foreach (var tile in SkillAreaUtility.GetLineTiles(caster.currentGridPosition, direction, lineLength))
            {
                var enemy = MapManager.Instance?.GetUnitAtTile(tile);
                if (enemy == null || enemy.IsDead() || enemy.GetOwner() == caster.GetOwner())
                    continue;

                float multiplier = hitTargets.Count == 0 ? primaryDamageMultiplier : secondaryDamageMultiplier;
                enemy.TakeDamage(Mathf.RoundToInt(caster.GetCurrentDamage() * multiplier));
                hitTargets.Add(enemy);
            }

            return hitTargets;
        }

        private bool ShouldApplyHealBan(UnitController primaryTarget, int hitCount)
        {
            if (primaryTarget == null || primaryTarget.IsDead())
                return false;

            return hitCount == 1 || CapturePointManager.Instance?.GetPointAt(primaryTarget.currentGridPosition) != null;
        }

        private void ApplyHealBan(UnitController caster, UnitController target)
        {
            target.BuffHandler?.AddEffect(new ActiveStatusEffect(
                StatusEffectType.HealBan,
                0,
                healBanDuration,
                caster.GetOwner()));
        }

        private void ApplyShield(UnitController caster)
        {
            int shieldValue = Mathf.RoundToInt(caster.GetCurrentDamage() * shieldValueMultiplier);
            caster.BuffHandler?.AddEffect(new ActiveStatusEffect(
                StatusEffectType.Shield,
                shieldValue,
                shieldDuration,
                caster.GetOwner()));
        }
    }
}
