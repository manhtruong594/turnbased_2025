using UnityEngine;
using System.Collections.Generic;
using TurnBasedGame.Unit;
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
        [SerializeField] private float multipleDmg = 1.25f;
        [SerializeField] private int aoeRadius = 1;

        public override bool CanUse(UnitController caster, Vector3Int targetPos)
        {
             if (!ValidateCooldown()) 
            {
                Debug.LogWarning($"{skillName} is on cooldown.");
                return false;
            }
            if (!ValidateMP(caster))
            {
                Debug.LogWarning($"Not enough MP to use {skillName}. Required MP: {MPCost}.");
                return false;
            }
            if (!ValidateRange(caster, targetPos)) {
                Debug.LogWarning($"{targetPos} is out of range for {skillName}.");
                return false;
            }
            if (!ValidateCustomConditions(caster, targetPos)) {
                Debug.LogWarning($"{skillName} cannot be used due to custom conditions.");
                return false;
            }

            return true;
        }

        protected override void ExecuteEffect(UnitController caster, Vector3Int targetPos)
        {
            var map = GetMap();
            // Gây damage cho tất cả units trong AOE
            var affectedTiles = map.Area(targetPos, aoeRadius);
            int hitCount = 0;
            int finalDamage = Mathf.RoundToInt(caster.GetCurrentDamage() * multipleDmg);

            foreach (var tilePos in affectedTiles)
            {
                var unit = MapManager.Instance?.GetUnitAtTile(tilePos);
                if (unit != null && !unit.IsDead() && unit.GetOwner() != caster.GetOwner())
                {
                    unit.TakeDamage(finalDamage);
                    hitCount++;
                }
            }

            Debug.Log($"{caster.name} dùng Fireball gây {finalDamage} damage cho {hitCount} mục tiêu!");
        }

        protected override bool ValidateCustomConditions(UnitController caster, Vector3Int targetPos)
        {
            // Fireball có thể bắn vào ô trống
            return true;
        }
    }
}
