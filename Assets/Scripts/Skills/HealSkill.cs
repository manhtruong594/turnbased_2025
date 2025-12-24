using UnityEngine;
using System.Collections.Generic;
using TurnBasedGame.Unit;
using RedBjorn.ProtoTiles.Example;

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
        [SerializeField] private int healAmount = 30;

        protected override void ExecuteEffect(UnitMove caster, Vector3Int targetPos)
        {
            var target = MapManager.Instance?.GetUnitAtTile(targetPos);
            if (target == null || target.IsDead()) return;

            // Heal target
            // int currentHealth = target.runtimeStats.Health;
            // int maxHealth = target.runtimeStats.MaxHealth;
            // int actualHeal = Mathf.Min(healAmount, maxHealth - currentHealth);
            
            // target.runtimeStats.Health = Mathf.Min(currentHealth + healAmount, maxHealth);
            // // Spawn VFX nếu có
            // if (healVFX != null)
            // {
            //     Instantiate(healVFX, target.transform.position, Quaternion.identity);
            // }

            // Debug.Log($"{caster.name} hồi {actualHeal} HP cho {target.name}!");
        }

        public override List<Vector3Int> GetAffectedTiles(Vector3Int targetPos)
        {
            return new List<Vector3Int> { targetPos };
        }

        protected override bool ValidateCustomConditions(UnitMove caster, Vector3Int targetPos)
        {
            var target = MapManager.Instance?.GetUnitAtTile(targetPos);
            if (target == null) return false;

            // Không heal unit đã full máu
            return target.GetHealthPercent() < 1.0f;
        }
    }
}
