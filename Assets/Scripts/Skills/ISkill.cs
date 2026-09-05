using UnityEngine;
using System.Collections.Generic;
using TurnBasedGame.Unit;
using System;

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
        int MPCost { get; }
        int Range { get; }

        bool CanUse(UnitController caster, Vector3Int targetPos);
        void Execute(UnitController caster, Vector3Int targetPos);
        
        void ResetCooldown();
        void ReduceCooldown();
        ISkill Clone();
    }

    public enum SkillType
    {
        Normal,      // Skill đánh thường (không cooldown, không mana)
        Active,      // Skill chủ động
        Passive,     // Skill bị động (tự kích hoạt)
        Ultimate,     // Skill ultimate
        BuffAndDebuff,   // Skill tăng cường hoặc làm suy yếu
    }

    [Flags]
    public enum TargetType
    {
        None = 0,
        Ally = 1 << 0,
        Enemy = 1 << 1,
        Self = 1 << 2,
        EmptyTile = 1 << 3,
    }
}
