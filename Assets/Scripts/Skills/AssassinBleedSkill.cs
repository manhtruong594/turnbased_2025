using TurnBasedGame.Core;
using TurnBasedGame.SpellCard;
using TurnBasedGame.Unit;
using UnityEngine;

namespace TurnBasedGame.Skills
{
    /// <summary>
    /// Assassin skill: gây sát thương và áp dụng hiệu ứng chảy máu (Bleed) trong N lượt.
    /// Bleed gây damage mỗi đầu lượt của mục tiêu.
    /// </summary>
    [CreateAssetMenu(fileName = "AssassinBleedSkill", menuName = "Skills/Assassin Bleed", order = 4)]
    public class AssassinBleedSkill : SkillBase
    {
        [Header("Damage Settings")]
        [SerializeField, Min(0f)] private float damageMultiplier = 1f;

        [Header("Bleed Settings")]
        [SerializeField, Min(1)] private int bleedDamagePerTurn = 10;
        [SerializeField, Range(1, 10)] private int bleedDuration = 3;

        protected override void ExecuteEffect(UnitController caster, Vector3Int targetPos)
        {
            var target = MapManager.Instance?.GetUnitAtTile(targetPos);
            if (target == null || target.IsDead()) return;

            // Gây sát thương trực tiếp
            int baseDamage = Mathf.RoundToInt(caster.GetCurrentDamage() * damageMultiplier);
            target.TakeDamage(baseDamage);
            Debug.Log($"[AssassinBleed] {caster.name} gây {baseDamage} sát thương lên {target.name}");

            // Áp dụng hiệu ứng chảy máu
            ApplyBleedEffect(caster.GetOwner(), target);
        }

        private void ApplyBleedEffect(PlayerID caster, UnitController target)
        {
            var handler = target.BuffHandler;
            if (handler == null) return;

            handler.AddEffect(new ActiveStatusEffect(
                StatusEffectType.Bleed,
                bleedDamagePerTurn,
                bleedDuration,
                caster
            ));

            Debug.Log($"[AssassinBleed] {target.name} bị chảy máu: {bleedDamagePerTurn} dmg/turn trong {bleedDuration} lượt");
        }
    }
}
