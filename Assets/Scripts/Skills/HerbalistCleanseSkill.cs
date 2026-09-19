using TurnBasedGame.SpellCard;
using TurnBasedGame.Unit;
using UnityEngine;

namespace TurnBasedGame.Skills
{
    [CreateAssetMenu(fileName = "HerbalistCleanseSkill", menuName = "Skills/Herbalist Cleanse", order = 11)]
    public class HerbalistCleanseSkill : SkillBase
    {
        [Header("Shield Settings")]
        [SerializeField, Min(0f)] private float shieldValueMultiplier = 0.3f;
        [SerializeField, Min(1)] private int shieldDuration = 2;

        protected override void ExecuteEffect(UnitController caster, Vector3Int targetPos)
        {
            var target = MapManager.Instance?.GetUnitAtTile(targetPos);
            var handler = target != null && !target.IsDead() ? target.BuffHandler : null;
            if (handler == null)
                return;

            int beforeCount = handler.GetDebuffs().Count;
            handler.RemoveDebuffs();
            int afterCount = handler.GetDebuffs().Count;
            if (afterCount >= beforeCount)
                return;

            int shieldValue = Mathf.RoundToInt(caster.GetCurrentDamage() * shieldValueMultiplier);
            handler.AddEffect(new ActiveStatusEffect(
                StatusEffectType.Shield,
                shieldValue,
                shieldDuration,
                caster.GetOwner()));
        }

        protected override bool ValidateCustomConditions(UnitController caster, Vector3Int targetPos)
        {
            var target = MapManager.Instance?.GetUnitAtTile(targetPos);
            return target != null
                && !target.IsDead()
                && target.BuffHandler != null
                && target.BuffHandler.GetDebuffs().Count > 0;
        }
    }
}
