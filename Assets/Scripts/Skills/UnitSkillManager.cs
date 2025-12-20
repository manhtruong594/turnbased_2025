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

        private UnitMove owner;
        private ISkill normalSkill;
        private List<ISkill> activeSkills = new List<ISkill>();

        public ISkill NormalSkill => normalSkill;
        public IReadOnlyList<ISkill> ActiveSkills => activeSkills;
        public IReadOnlyList<ISkill> AllSkills
        {
            get
            {
                var all = new List<ISkill>();
                if (normalSkill != null) all.Add(normalSkill);
                all.AddRange(activeSkills);
                return all;
            }
        }

        #region Initialization
        public void Initialize(UnitMove owner)
        {
            this.owner = owner;
            LoadStartingSkills();
        }

        private void LoadStartingSkills()
        {
            foreach (var skillData in startingSkills)
            {
                if (skillData == null) continue;
                AddSkill(skillData);
            }
        }
        #endregion

        #region Skill Management
        public void AddSkill(SkillBase skillData)
        {
            var skill = skillData.Clone();
            
            if (skill.Type == SkillType.Normal)
            {
                normalSkill = skill;
                Debug.Log($"{owner.name} học Normal Skill: {skill.SkillName}");
            }
            else
            {
                activeSkills.Add(skill);
                Debug.Log($"{owner.name} học {skill.Type} Skill: {skill.SkillName}");
            }

            SkillEventBus.Instance?.TriggerSkillLearned(skill, owner);
        }

        public bool RemoveSkill(ISkill skill)
        {
            if (skill == normalSkill)
            {
                normalSkill = null;
                return true;
            }

            return activeSkills.Remove(skill);
        }

        public ISkill GetSkillByName(string skillName)
        {
            if (normalSkill?.SkillName == skillName)
                return normalSkill;

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

            if (!skill.CanUse(owner, targetPos))
            {
                Debug.LogWarning($"Cannot use {skill.SkillName}");
                return false;
            }

            skill.Execute(owner, targetPos);
            return true;
        }

        public bool UseSkillByName(string skillName, Vector3Int targetPos)
        {
            var skill = GetSkillByName(skillName);
            return UseSkill(skill, targetPos);
        }

        public bool UseNormalSkill(Vector3Int targetPos)
        {
            if (normalSkill == null)
            {
                Debug.LogWarning($"{owner.name} không có Normal Skill!");
                return false;
            }

            return UseSkill(normalSkill, targetPos);
        }
        #endregion

        #region Skill Queries
        public List<ISkill> GetUsableSkills(Vector3Int targetPos)
        {
            var usable = new List<ISkill>();

            if (normalSkill != null && normalSkill.CanUse(owner, targetPos))
                usable.Add(normalSkill);

            usable.AddRange(activeSkills.Where(s => s.CanUse(owner, targetPos)));

            return usable;
        }

        public List<ISkill> GetReadySkills()
        {
            var ready = new List<ISkill>();

            if (normalSkill != null)
                ready.Add(normalSkill);

            ready.AddRange(activeSkills.Where(s => s.CurrentCooldown <= 0));

            return ready;
        }

        public bool HasSkillInRange(Vector3Int targetPos)
        {
            return GetUsableSkills(targetPos).Count > 0;
        }

        public List<Vector3Int> GetAllValidTargets()
        {
            var allTargets = new HashSet<Vector3Int>();

            if (normalSkill != null)
            {
                foreach (var target in normalSkill.GetValidTargets(owner))
                    allTargets.Add(target);
            }

            foreach (var skill in activeSkills)
            {
                if (skill.CurrentCooldown > 0) continue;
                
                foreach (var target in skill.GetValidTargets(owner))
                    allTargets.Add(target);
            }

            return allTargets.ToList();
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
            normalSkill?.ReduceCooldown();

            foreach (var skill in activeSkills)
            {
                skill.ReduceCooldown();
                
                if (skill.CurrentCooldown == 0)
                {
                    SkillEventBus.Instance?.TriggerCooldownComplete(skill);
                }
            }
        }

        public void ResetAllCooldowns()
        {
            normalSkill?.ResetCooldown();
            
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
            Debug.Log($"=== {owner.name} Skills ===");
            
            if (normalSkill != null)
                Debug.Log($"Normal: {normalSkill.SkillName}");

            foreach (var skill in activeSkills)
            {
                Debug.Log($"{skill.Type}: {skill.SkillName} (CD: {skill.CurrentCooldown}/{skill.Cooldown})");
            }
        }
        #endregion
    }
}
