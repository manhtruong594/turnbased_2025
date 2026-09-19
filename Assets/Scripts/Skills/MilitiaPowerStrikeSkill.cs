using TurnBasedGame.SpellCard;
using TurnBasedGame.Unit;
using UnityEngine;

namespace TurnBasedGame.Skills
{
    [CreateAssetMenu(fileName = "MilitiaPowerStrikeSkill", menuName = "Skills/Militia Power Strike", order = 10)]
    public class MilitiaPowerStrikeSkill : SkillBase
    {
        [Header("Damage Settings")]
        [SerializeField, Min(0f)] private float damageMultiplier = 1.75f;
        [SerializeField, Min(0f)] private float shieldedTargetDamageMultiplier = 1.3f;

        protected override void ExecuteEffect(UnitController caster, Vector3Int targetPos)
        {
            var target = MapManager.Instance?.GetUnitAtTile(targetPos);
            if (target == null || target.IsDead())
                return;

            float multiplier = damageMultiplier;
            if (target.BuffHandler != null && target.BuffHandler.HasEffect(StatusEffectType.Shield))
                multiplier *= shieldedTargetDamageMultiplier;

            int damage = Mathf.RoundToInt(caster.GetCurrentDamage() * multiplier);
            target.TakeDamage(damage);
        }
    }
}
