using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using TurnBasedGame.Unit;
using RedBjorn.ProtoTiles.Example;

namespace TurnBasedGame.Skills
{
    /// <summary>
    /// Context trong Strategy Pattern - Quản lý và thực thi skills của unit
    /// Facade Pattern - Đơn giản hóa việc sử dụng skill system
    /// </summary>
    public class UnitSkillManager : MonoBehaviour
    {
        [Header("Starting Skills")]
        [SerializeField] private List<SkillBase> startingSkills = new List<SkillBase>();

        private UnitMove _owner;
        private SkillBase _normalSkill;
        private List<ISkill> activeSkills = new List<ISkill>();

        public ISkill NormalSkill => _normalSkill; 
        public SkillBase _selectedSkill;

        #region Initialization
        public void Initialize(UnitMove owner)
        {
            this._owner = owner;
            LoadStartingSkills();
        }

        private void LoadStartingSkills()
        {
        }
        #endregion

        #region Skill Management
 
        public ISkill GetSkillByName(string skillName)
        {
            if (_normalSkill?.SkillName == skillName)
                return _normalSkill;

            return activeSkills.FirstOrDefault(s => s.SkillName == skillName);
        }
        #endregion

        #region Skill Execution
        public bool UseSkill(ISkill skill, Vector3Int targetPos)
        {
            if (skill == null)
            {
                Debug.LogWarning("Skill is null!");
                return false;
            }

            if (!skill.CanUse(_owner, targetPos))
            {
                Debug.LogWarning($"Cannot use {skill.SkillName}");
                return false;
            }

            skill.Execute(_owner, targetPos);
            return true;
        }

        public bool UseSkillByName(string skillName, Vector3Int targetPos)
        {
            var skill = GetSkillByName(skillName);
            return UseSkill(skill, targetPos);
        }

        public bool UseNormalSkill(Vector3Int targetPos)
        {
            if (_normalSkill == null)
            {
                Debug.LogWarning($"{_owner.name} không có Normal Skill!");
                return false;
            }

            return UseSkill(_normalSkill, targetPos);
        }
        #endregion

        #region Skill Queries
        public List<ISkill> GetUsableSkills(Vector3Int targetPos)
        {
            var usable = new List<ISkill>();

            if (_normalSkill != null && _normalSkill.CanUse(_owner, targetPos))
                usable.Add(_normalSkill);

            usable.AddRange(activeSkills.Where(s => s.CanUse(_owner, targetPos)));

            return usable;
        }


        public bool HasSkillInRange(Vector3Int targetPos)
        {
            return GetUsableSkills(targetPos).Count > 0;
        }

        #endregion

        #region Turn Management
        public void OnTurnStart()
        {
            // Có thể thêm logic kích hoạt passive skills
        }

        public void OnTurnEnd()
        {
            ReduceAllCooldowns();
        }

        private void ReduceAllCooldowns()
        {
            _normalSkill?.ReduceCooldown();

            foreach (var skill in activeSkills)
            {
                skill.ReduceCooldown();
            }
        }

        public void ResetAllCooldowns()
        {
            _normalSkill?.ResetCooldown();
            
            foreach (var skill in activeSkills)
            {
                skill.ResetCooldown();
            }
        }
        #endregion

        #region Debug
        [ContextMenu("Debug Skills")]
        private void DebugSkills()
        {
            Debug.Log($"=== {_owner.name} Skills ===");
            
            if (_normalSkill != null)
                Debug.Log($"Normal: {_normalSkill.SkillName}");

            foreach (var skill in activeSkills)
            {
                Debug.Log($"{skill.Type}: {skill.SkillName}");
            }
        }
        #endregion
    }
}
