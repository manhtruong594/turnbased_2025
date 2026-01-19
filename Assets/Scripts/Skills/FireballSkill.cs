using UnityEngine;
using System.Collections.Generic;
using TurnBasedGame.Unit;
using RedBjorn.ProtoTiles.Example;
using TurnBasedGame.ObjectPool;

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

        public override bool CanUse(UnitMove caster, Vector3Int targetPos)
        {
            if (!ValidateCooldown()) return false;
            if (!ValidateMana(caster)) return false;
            if (!ValidateRange(caster, targetPos)) return false;
            if (!ValidateCustomConditions(caster, targetPos)) return false;

            return true;
        }

        protected override void ExecuteEffect(UnitMove caster, Vector3Int targetPos)
        {
            var map = GetMap();
            
            // Spawn VFX từ pool thay vì Instantiate
            if (VfxPrefab != null && ObjectPoolManager.Instance != null)
            {
                var worldPos = map.WorldPosition(targetPos);
                ObjectPoolManager.Instance.Spawn(VfxPrefab).transform.SetPositionAndRotation(worldPos, Quaternion.identity);
            }

            // Gây damage cho tất cả units trong AOE
            var affectedTiles = map.Area(targetPos, aoeRadius);
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
       
        public override List<Vector3Int> GetAffectedTiles(Vector3Int targetPos)
        {
            var tiles = new List<Vector3Int>();
            
            // // Lấy tất cả tiles trong bán kính AOE
            // for (int x = -aoeRadius; x <= aoeRadius; x++)
            // {
            //     for (int y = -aoeRadius; y <= aoeRadius; y++)
            //     {
            //         for (int z = -aoeRadius; z <= aoeRadius; z++)
            //         {
            //             var offset = new Vector3Int(x, y, z);
            //             if (Mathf.Abs(x) + Mathf.Abs(y) + Mathf.Abs(z) <= aoeRadius)
            //             {
            //                 tiles.Add(targetPos + offset);
            //             }
            //         }
            //     }
            // }
            tiles = GetMap().Area(targetPos, aoeRadius);
            return tiles;
        }

        protected override bool ValidateCustomConditions(UnitMove caster, Vector3Int targetPos)
        {
            // Fireball có thể bắn vào ô trống
            return true;
        }
    }
}
