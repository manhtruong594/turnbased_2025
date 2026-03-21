using UnityEngine;
using System.Collections.Generic;
using TurnBasedGame.Unit;

namespace TurnBasedGame.Skills
{
    /// <summary>
    /// Concrete Strategy - Heal Skill
    /// Skill hồi máu cho đồng minh hoặc bản thân
    /// </summary>
    [CreateAssetMenu(fileName = "HealSkill", menuName = "Skills/Heal", order = 2)]
    public class HealSkill : SkillBase
    {
        [Header("Heal Settings")]
        [SerializeField] private float multiple = 1.5f;

        protected override void ExecuteEffect(UnitController caster, Vector3Int targetPos)
        {
            var target = MapManager.Instance?.GetUnitAtTile(targetPos);
            if (target == null || target.IsDead()) return;

            // Heal target
            int healAmount = Mathf.RoundToInt(caster.GetCurrentDamage() * multiple);
            target.Heal(healAmount);
        }

        public override List<Vector3Int> GetAffectedTiles(Vector3Int targetPos)
        {
            return new List<Vector3Int> { targetPos };
        }

        protected override bool ValidateCustomConditions(UnitController caster, Vector3Int targetPos)
        {
            var target = MapManager.Instance?.GetUnitAtTile(targetPos);
            if (target == null) return false;

            // Không heal unit đã full máu
            return target.GetHealthPercent() < 1.0f;
        }
    }
}
