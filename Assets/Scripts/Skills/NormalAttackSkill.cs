using UnityEngine;
using System.Collections.Generic;
using TurnBasedGame.Unit;
using RedBjorn.ProtoTiles.Example;

namespace TurnBasedGame.Skills
{
    /// <summary>
    /// Concrete Strategy - Normal Attack Skill
    /// Skill đánh thường, không cooldown, không mana
    /// </summary>
    [CreateAssetMenu(fileName = "NormalAttack", menuName = "Skills/Normal Attack", order = 0)]
    public class NormalAttackSkill : SkillBase
    {
        [Header("Attack Settings")]
        [SerializeField] private int dmg = 10;
        [SerializeField] private bool useAttackStat = true;

        protected override void ExecuteEffect(UnitMove caster, Vector3Int targetPos)
        {
            var target = MapManager.Instance?.GetUnitAtTile(targetPos);
            if (target == null || target.IsDead()) return;

            int finalDamage = CalculateDamage(caster);
            target.TakeDamage(finalDamage);
        }
        
        private int CalculateDamage(UnitMove caster)
        {
            return dmg;
        }
        
        public override List<Vector3Int> GetAffectedTiles(Vector3Int targetPos)
        {
            return new List<Vector3Int> { targetPos };
        }
    }
}
