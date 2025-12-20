using UnityEngine;
using System.Collections.Generic;
using TurnBasedGame.Unit;
using RedBjorn.ProtoTiles.Example;

namespace TurnBasedGame.Skills
{
    /// <summary>
    /// Concrete Strategy - Fireball AOE Skill
    /// Skill tấn công diện rộng, có cooldown và mana cost
    /// </summary>
    [CreateAssetMenu(fileName = "Fireball", menuName = "Skills/Fireball", order = 1)]
    public class FireballSkill : SkillBase
    {
        [Header("Fireball Settings")]
        [SerializeField] private int damage = 25;
        [SerializeField] private int aoeRadius = 1;
        [SerializeField] private GameObject fireballVFX;

        protected override void ExecuteEffect(UnitMove caster, Vector3Int targetPos)
        {
            var map = GetMap(caster);
            
            // Spawn VFX nếu có
            if (fireballVFX != null)
            {
                var worldPos = map.WorldPosition(targetPos);
                Instantiate(fireballVFX, worldPos, Quaternion.identity);
            }

            // Gây damage cho tất cả units trong AOE
            var affectedTiles = GetAffectedTiles(targetPos);
            int hitCount = 0;
            
            foreach (var tilePos in affectedTiles)
            {
                var unit = MapManager.Instance?.GetUnitAtTile(tilePos);
                if (unit != null && !unit.IsDead() && unit.GetOwner() != caster.GetOwner())
                {
                    unit.TakeDamage(damage);
                    hitCount++;
                }
            }

            Debug.Log($"{caster.name} dùng Fireball gây {damage} damage cho {hitCount} mục tiêu!");
        }

        public override List<Vector3Int> GetValidTargets(UnitMove caster)
        {
            var targets = new List<Vector3Int>();
            var map = GetMap(caster);
            var myTile = map.Tile(caster.transform.position);
            if (myTile == null) return targets;

            var tilesInRange = map.WalkableTiles(myTile.Position, range);
            
            // Fireball có thể bắn vào ô trống
            foreach (var tile in tilesInRange)
            {
                targets.Add(tile.Position);
            }

            return targets;
        }

        public override List<Vector3Int> GetAffectedTiles(Vector3Int targetPos)
        {
            var tiles = new List<Vector3Int>();
            
            // Lấy tất cả tiles trong bán kính AOE
            for (int x = -aoeRadius; x <= aoeRadius; x++)
            {
                for (int y = -aoeRadius; y <= aoeRadius; y++)
                {
                    for (int z = -aoeRadius; z <= aoeRadius; z++)
                    {
                        var offset = new Vector3Int(x, y, z);
                        if (Mathf.Abs(x) + Mathf.Abs(y) + Mathf.Abs(z) <= aoeRadius)
                        {
                            tiles.Add(targetPos + offset);
                        }
                    }
                }
            }

            return tiles;
        }

        protected override bool ValidateCustomConditions(UnitMove caster, Vector3Int targetPos)
        {
            // Fireball có thể bắn vào ô trống
            return true;
        }
    }
}
