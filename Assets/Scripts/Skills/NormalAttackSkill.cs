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
        [SerializeField] private int baseDamage = 10;
        [SerializeField] private bool useAttackStat = true;

        protected override void ExecuteEffect(UnitMove caster, Vector3Int targetPos)
        {
            var target = MapManager.Instance?.GetUnitAtTile(targetPos);
            if (target == null || target.IsDead()) return;

            int finalDamage = CalculateDamage(caster);
            target.TakeDamage(finalDamage);
            
            Debug.Log($"{caster.name} đánh thường {target.name} gây {finalDamage} damage!");
        }

        private int CalculateDamage(UnitMove caster)
        {
            if (useAttackStat)
                return baseDamage + (int)caster.UnitData.attackDamage;
            return baseDamage;
        }

        public override List<Vector3Int> GetValidTargets(UnitMove caster)
        {
            var targets = new List<Vector3Int>();
            var map = GetMap(caster);
            var myTile = map.Tile(caster.transform.position);
            if (myTile == null) return targets;

            var tilesInRange = map.WalkableTiles(myTile.Position, range);
            
            foreach (var tile in tilesInRange)
            {
                if (ValidateTarget(caster, tile.Position))
                    targets.Add(tile.Position);
            }

            return targets;
        }

        public override List<Vector3Int> GetAffectedTiles(Vector3Int targetPos)
        {
            return new List<Vector3Int> { targetPos };
        }
    }
}
