using System.Collections.Generic;
using TurnBasedGame.SpellCard;
using TurnBasedGame.Unit;
using UnityEngine;

namespace TurnBasedGame.Skills
{
    [CreateAssetMenu(fileName = "MagicianFireSealSkill", menuName = "Skills/Magician Fire Seal", order = 7)]
    public class MagicianFireSealSkill : SkillBase
    {
        [Header("Damage Settings")]
        [SerializeField, Min(0f)] private float damageMultiplier = 1.4f;
        [SerializeField, Min(0)] private int aoeRadius = 1;
        [SerializeField, Min(0)] private int bonusDamageAgainstBurningTargetPercent = 25;

        [Header("Burn Settings")]
        [SerializeField, Range(0f, 1f)] private float burnChanceOnImpact = 0.25f;
        [SerializeField, Range(0f, 1f)] private float burnChanceFromGround = 0.5f;
        [SerializeField, Min(1)] private int burnDuration = 2;
        [SerializeField, Min(0f)] private float burnDamageMultiplier = 0.4f;

        [Header("Burning Ground Settings")]
        [SerializeField, Min(1)] private int burningGroundDuration = 2;

        public override bool CanUse(UnitController caster, Vector3Int targetPos)
        {
            if (!ValidateCooldown() || !ValidateRange(caster, targetPos))
                return false;

            return IsEnemyOrEmptyTile(caster, targetPos) && ValidateCustomConditions(caster, targetPos);
        }

        protected override void ExecuteEffect(UnitController caster, Vector3Int targetPos)
        {
            var affectedTiles = SkillAreaUtility.GetRadiusTiles(targetPos, aoeRadius);
            ApplyImpactDamage(caster, affectedTiles);
            CreateBurningGround(caster, affectedTiles);
        }

        protected override bool ValidateCustomConditions(UnitController caster, Vector3Int targetPos)
        {
            return MapManager.Instance?.MapEntity?.Tile(targetPos) != null;
        }

        private bool IsEnemyOrEmptyTile(UnitController caster, Vector3Int targetPos)
        {
            var target = MapManager.Instance?.GetUnitAtTile(targetPos);
            return target == null || (!target.IsDead() && target.GetOwner() != caster.GetOwner());
        }

        private void ApplyImpactDamage(UnitController caster, IEnumerable<Vector3Int> affectedTiles)
        {
            int burnDamage = CalculateBurnDamage(caster);
            foreach (var enemy in SkillAreaUtility.GetEnemiesInTiles(caster.GetOwner(), affectedTiles))
            {
                enemy.TakeDamage(CalculateImpactDamage(caster, enemy));
                TryApplyBurn(caster, enemy, burnDamage, burnChanceOnImpact);
            }
        }

        private int CalculateImpactDamage(UnitController caster, UnitController target)
        {
            float multiplier = damageMultiplier;
            if (target.BuffHandler != null && target.BuffHandler.HasEffect(StatusEffectType.Burn))
                multiplier *= 1f + bonusDamageAgainstBurningTargetPercent / 100f;

            return Mathf.RoundToInt(caster.GetCurrentDamage() * multiplier);
        }

        private int CalculateBurnDamage(UnitController caster)
        {
            return Mathf.Max(1, Mathf.RoundToInt(caster.GetCurrentDamage() * burnDamageMultiplier));
        }

        private void TryApplyBurn(UnitController caster, UnitController target, int burnDamage, float chance)
        {
            if (target == null || target.IsDead() || Random.value > chance)
                return;

            target.BuffHandler?.AddEffect(new ActiveStatusEffect(
                StatusEffectType.Burn,
                burnDamage,
                burnDuration,
                caster.GetOwner()));
        }

        private void CreateBurningGround(UnitController caster, IEnumerable<Vector3Int> affectedTiles)
        {
            int burnDamage = CalculateBurnDamage(caster);
            foreach (var tile in affectedTiles)
            {
                TileHazardManager.InstanceOrCreate.AddHazard(
                    TileHazardType.BurningGround,
                    tile,
                    burningGroundDuration,
                    caster.GetOwner(),
                    burnDamage,
                    burnChanceFromGround,
                    burnDuration);
            }
        }
    }
}
