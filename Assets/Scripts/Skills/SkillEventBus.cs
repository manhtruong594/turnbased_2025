using UnityEngine;
using System;
using TurnBasedGame.Unit;

namespace TurnBasedGame.Skills
{
    /// <summary>
    /// Observer Pattern - Event bus cho skill system
    /// Phát các event khi có hành động liên quan đến skill
    /// </summary>
    public class SkillEventBus : MonoBehaviour
    {
        private static SkillEventBus instance;
        public static SkillEventBus Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("[SkillEventBus]");
                    instance = go.AddComponent<SkillEventBus>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        #region Events
        /// <summary>Event khi skill được sử dụng</summary>
        public event Action<ISkill, UnitController, Vector3Int> OnSkillUsed;
        
        /// <summary>Event khi unit học skill mới</summary>
        public event Action<ISkill, UnitController> OnSkillLearned;
        
        /// <summary>Event khi cooldown của skill kết thúc</summary>
        public event Action<ISkill> OnCooldownComplete;
        
        /// <summary>Event khi skill không thể sử dụng</summary>
        public event Action<ISkill, UnitController, string> OnSkillFailed;
        #endregion

        #region Trigger Methods
        public void TriggerSkillUsed(ISkill skill, UnitController caster, Vector3Int targetPos)
        {
            OnSkillUsed?.Invoke(skill, caster, targetPos);
        }

        public void TriggerSkillLearned(ISkill skill, UnitController learner)
        {
            OnSkillLearned?.Invoke(skill, learner);
        }

        public void TriggerCooldownComplete(ISkill skill)
        {
            OnCooldownComplete?.Invoke(skill);
        }

        public void TriggerSkillFailed(ISkill skill, UnitController caster, string reason)
        {
            OnSkillFailed?.Invoke(skill, caster, reason);
        }
        #endregion

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
    }
}
