using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using TurnBasedGame.Unit;
using RedBjorn.ProtoTiles.Example;
using UnityEngine.UI;
using TurnBasedGame.ObjectPool;

namespace TurnBasedGame.Skills
{
    /// <summary>
    ///  Component cầu nối giữa UnitAttack và Skill System
    /// </summary>
    public class UnitSkillsBridge
    {
        private Dictionary<ISkill, SkillButton> _skillButtonMap = new Dictionary<ISkill, SkillButton>();

        public SkillButton CreateSkillButton(GameObject skillButtonPrefab, Transform skillButtonContainer, ISkill skill, Action<ISkill> onSkillSelected)
        {
            var skillBtnObj = ObjectPoolManager.Instance.Spawn(skillButtonPrefab, skillButtonContainer);
            var skillButton = skillBtnObj.GetComponent<SkillButton>();
            skillButton.Initialize(skill, onSkillSelected);
            _skillButtonMap[skill] = skillButton;
            return skillButton;
        }

        public void DisposeSkillButtons()
        {
            foreach (var skillButton in _skillButtonMap.Values)
            {
                ObjectPoolManager.Instance.Despawn(skillButton.GetComponent<PooledObject>());
            }
            _skillButtonMap.Clear();
        }

    }
}
