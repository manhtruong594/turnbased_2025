using System.Collections.Generic;
using UnityEngine;
using TurnBasedGame.Core;
using TurnBasedGame.EditorSupport;
using TurnBasedGame.Skills;

namespace TurnBasedGame.Unit
{
    /// <summary>
    /// ScriptableObject chứa cấu hình cho từng loại unit
    /// Sử dụng để tạo các template unit khác nhau
    /// </summary>
    [CreateAssetMenu(fileName = "New Unit", menuName = "TurnBased/Unit Data")]
    public class UnitData : ScriptableObject
    {
        [Header("Basic Info")]
        public string unitName = "Unit";
        [TextArea(2, 4)]
        public string description;
        public int Health = 100;
        public int BaseDamage = 10;
        
        [Range(1, 20)]
        public int spawnCost = 3;

        [Header("Movement")]
        [Range(1, 10)]
        public int moveRange = 3;
        
        [Tooltip("Icon hiển thị trên UI")]
        [SpritePreview]
        public Sprite icon;

        [Header("Skills")]
        [SerializeField] private List<SkillBase> startingSkills = new();

        public IReadOnlyList<SkillBase> StartingSkills => startingSkills;
    }
}
