using TurnBasedGame.SpellCard;
using TurnBasedGame.Unit;
using UnityEngine;

namespace TurnBasedGame.Skills
{
    [CreateAssetMenu(fileName = "AdjudicatorAccusationSkill", menuName = "Skills/Adjudicator Accusation", order = 12)]
    public class AdjudicatorAccusationSkill : SkillBase
    {
        [Header("Damage Settings")]
        [SerializeField, Min(0f)] private float damageMultiplier = 1f;

        [Header("Guard Break Settings")]
        [SerializeField, Min(0)] private int guardBreakValue = 20;
        [SerializeField, Min(1)] private int guardBreakDuration = 2;

        protected override void ExecuteEffect(UnitController caster, Vector3Int targetPos)
        {
            var target = MapManager.Instance?.GetUnitAtTile(targetPos);
            if (target == null || target.IsDead())
                return;

            int damage = Mathf.RoundToInt(caster.GetCurrentDamage() * damageMultiplier);
            target.TakeDamage(damage);
            if (target.IsDead())
                return;

            target.BuffHandler?.AddEffect(new ActiveStatusEffect(
                StatusEffectType.GuardBreak,
                guardBreakValue,
                guardBreakDuration,
                caster.GetOwner()));
        }
    }
}
