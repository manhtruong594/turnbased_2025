using System.Collections.Generic;
using TurnBasedGame.SpellCard;
using TurnBasedGame.Unit;
using UnityEngine;

namespace TurnBasedGame.Skills
{
    [CreateAssetMenu(fileName = "AdjudicatorForbiddenSealSkill", menuName = "Skills/Adjudicator Forbidden Seal", order = 13)]
    public class AdjudicatorForbiddenSealSkill : SkillBase
    {
        [Header("Area Settings")]
        [SerializeField, Min(0)] private int aoeRadius = 1;

        [Header("Damage Settings")]
        [SerializeField, Min(0f)] private float damageMultiplier = 1.6f;

        [Header("Weaken Settings")]
        [SerializeField, Min(0)] private int weakenValue = 25;
        [SerializeField, Min(1)] private int weakenDuration = 2;

        [Header("Heal Ban Settings")]
        [SerializeField, Range(0f, 1f)] private float healBanChance = 0.5f;
        [SerializeField, Min(1)] private int healBanDuration = 2;

        protected override void ExecuteEffect(UnitController caster, Vector3Int targetPos)
        {
            var affectedTiles = GetSortedAffectedTiles(targetPos);
            var enemies = SkillAreaUtility.GetEnemiesInTiles(caster.GetOwner(), affectedTiles);
            int damage = Mathf.RoundToInt(caster.GetCurrentDamage() * damageMultiplier);

            foreach (var enemy in enemies)
            {
                enemy.TakeDamage(damage);
                if (enemy.IsDead())
                    continue;

                enemy.BuffHandler?.AddEffect(new ActiveStatusEffect(
                    StatusEffectType.Weaken,
                    weakenValue,
                    weakenDuration,
                    caster.GetOwner()));

                if (TurnBasedGame.Command.LocalMatchAuthority.Random.Value() <= healBanChance)
                {
                    enemy.BuffHandler?.AddEffect(new ActiveStatusEffect(
                        StatusEffectType.HealBan,
                        0,
                        healBanDuration,
                        caster.GetOwner()));
                }
            }
        }

        protected override bool ValidateCustomConditions(UnitController caster, Vector3Int targetPos)
        {
            if (MapManager.Instance?.MapEntity?.Tile(targetPos) == null)
                return false;

            var centerUnit = MapManager.Instance.GetUnitAtTile(targetPos);
            if (centerUnit != null && centerUnit.GetOwner() == caster.GetOwner())
                return false;

            var affectedTiles = GetSortedAffectedTiles(targetPos);
            return SkillAreaUtility.GetEnemiesInTiles(caster.GetOwner(), affectedTiles).Count > 0;
        }

        private List<Vector3Int> GetSortedAffectedTiles(Vector3Int center)
        {
            var affectedTiles = SkillAreaUtility.GetRadiusTiles(center, aoeRadius);
            affectedTiles.Sort(CompareTilePositions);
            return affectedTiles;
        }

        private static int CompareTilePositions(Vector3Int left, Vector3Int right)
        {
            int comparison = left.x.CompareTo(right.x);
            if (comparison != 0)
                return comparison;

            comparison = left.y.CompareTo(right.y);
            return comparison != 0 ? comparison : left.z.CompareTo(right.z);
        }
    }
}
