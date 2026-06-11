using TurnBasedGame.SpellCard;
using TurnBasedGame.Unit;
using UnityEngine;

namespace TurnBasedGame.Skills
{
    [CreateAssetMenu(fileName = "BerserkerBloodHammerSkill", menuName = "Skills/Berserker Blood Hammer", order = 5)]
    public class BerserkerBloodHammerSkill : SkillBase
    {
        [Header("Damage Settings")]
        [SerializeField, Min(0f)] private float primaryDamageMultiplier = 2.2f;
        [SerializeField, Min(0f)] private float secondaryDamageMultiplier = 1.1f;
        [SerializeField, Range(0f, 1f)] private float selfCurrentHealthCostPercent = 0.1f;

        [Header("Blood Rage Settings")]
        [SerializeField, Min(1)] private int bloodRageDuration = 1;
        [SerializeField, Min(0)] private int bloodRageDamagePercent = 20;

        protected override void ExecuteEffect(UnitController caster, Vector3Int targetPos)
        {
            var target = MapManager.Instance?.GetUnitAtTile(targetPos);
            if (target == null || target.IsDead())
                return;

            bool targetWasAlive = !target.IsDead();
            int primaryDamage = Mathf.RoundToInt(caster.GetCurrentDamage() * primaryDamageMultiplier);
            target.TakeDamage(primaryDamage);

            ApplySecondaryDamage(caster, targetPos);
            ApplySelfCost(caster);

            if (targetWasAlive && target.IsDead())
                ApplyBloodRage(caster);
        }

        private void ApplySecondaryDamage(UnitController caster, Vector3Int centerPos)
        {
            var map = GetMap();
            if (map == null)
                return;

            int secondaryDamage = Mathf.RoundToInt(caster.GetCurrentDamage() * secondaryDamageMultiplier);
            foreach (var tilePos in map.Area(centerPos, 1))
            {
                if (tilePos == centerPos)
                    continue;

                var unit = MapManager.Instance?.GetUnitAtTile(tilePos);
                if (unit == null || unit.IsDead() || unit.GetOwner() == caster.GetOwner())
                    continue;

                unit.TakeDamage(secondaryDamage);
            }
        }

        private void ApplySelfCost(UnitController caster)
        {
            int currentHealth = caster.GetCurrentHealth();
            int selfDamage = Mathf.RoundToInt(currentHealth * selfCurrentHealthCostPercent);
            if (currentHealth > 1)
                selfDamage = Mathf.Clamp(selfDamage, 1, currentHealth - 1);

            caster.TakeNonLethalDamage(selfDamage);
        }

        private void ApplyBloodRage(UnitController caster)
        {
            caster.BuffHandler?.AddEffect(new ActiveStatusEffect(
                StatusEffectType.BloodRage,
                bloodRageDamagePercent,
                bloodRageDuration,
                caster.GetOwner()));
        }
    }
}
