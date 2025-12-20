using UnityEngine;
using System.Collections.Generic;
using TurnBasedGame.Unit;
using RedBjorn.ProtoTiles.Example;

namespace TurnBasedGame.Skills
{
    /// <summary>
    /// Concrete Strategy - Shield Skill
    /// Skill tạo lá chắn cho đồng minh trong phạm vi AOE
    /// </summary>
    [CreateAssetMenu(fileName = "ShieldSkill", menuName = "Skills/Shield", order = 3)]
    public class ShieldSkill : SkillBase
    {
        [Header("Shield Settings")]
        [SerializeField] private int shieldAmount = 20;
        [SerializeField] private int aoeRadius = 2;
        [SerializeField] private int duration = 2; // Số turn
        [SerializeField] private GameObject shieldVFX;

        protected override void ExecuteEffect(UnitMove caster, Vector3Int targetPos)
        {
            var affectedTiles = GetAffectedTiles(targetPos);
            int shieldedCount = 0;

            foreach (var tilePos in affectedTiles)
            {
                var unit = MapManager.Instance?.GetUnitAtTile(tilePos);
                if (unit != null && !unit.IsDead() && unit.GetOwner() == caster.GetOwner())
                {
                    // TODO: Implement shield system
                    // unit.AddShield(shieldAmount, duration);
                    shieldedCount++;

                    // Spawn VFX
                    if (shieldVFX != null)
                    {
                        Instantiate(shieldVFX, unit.transform.position, Quaternion.identity);
                    }
                }
            }

            Debug.Log($"{caster.name} tạo {shieldAmount} shield cho {shieldedCount} đồng minh!");
        }

        public override List<Vector3Int> GetValidTargets(UnitMove caster)
        {
            var targets = new List<Vector3Int>();
            var map = GetMap(caster);
            var myTile = map.Tile(caster.transform.position);
            if (myTile == null) return targets;

            var tilesInRange = map.WalkableTiles(myTile.Position, range);
            
            // Shield có thể cast vào ô trống để tạo AOE
            foreach (var tile in tilesInRange)
            {
                targets.Add(tile.Position);
            }

            return targets;
        }

        public override List<Vector3Int> GetAffectedTiles(Vector3Int targetPos)
        {
            var tiles = new List<Vector3Int>();
            
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
       
    }
}
