using UnityEngine;
using System.Collections.Generic;
using TurnBasedGame.Unit;
using RedBjorn.ProtoTiles.Example;

namespace TurnBasedGame.Skills
{
    /// <summary>
    /// Strategy Pattern - Interface định nghĩa hành vi chung của mọi skill
    /// </summary>
    public interface ISkill
    {
        string SkillName { get; }
        string Description { get; }
        SkillType Type { get; }
        Sprite Icon { get; }
        
        int Cooldown { get; }
        int CurrentCooldown { get; }
        int Range { get; }

        bool CanUse(UnitMove caster, Vector3Int targetPos);
        void Execute(UnitMove caster, Vector3Int targetPos);
        List<Vector3Int> GetAffectedTiles(Vector3Int targetPos);
        
        void ResetCooldown();
        void ReduceCooldown();
        ISkill Clone();
    }

    public enum SkillType
    {
        Normal,      // Skill đánh thường (không cooldown, không mana)
        Active,      // Skill chủ động
        Passive,     // Skill bị động (tự kích hoạt)
        Ultimate     // Skill ultimate
    }
}
