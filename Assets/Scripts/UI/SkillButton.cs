using System;
using TMPro;
using TurnBasedGame.Skills;
using UnityEngine;
using UnityEngine.UI;

public class SkillButton : MonoBehaviour
{
    [SerializeField] Button _myButton;
    [SerializeField] TextMeshProUGUI _descriptionText;
    [SerializeField] Image _icon;

    public void Initialize(ISkill skill, Action<ISkill> onClickAction)
    {
        if (_descriptionText != null)
            _descriptionText.text = skill.Description;

        if (_icon != null)
            _icon.sprite = skill.Icon;

        if (_myButton != null)
        {
            _myButton.onClick.RemoveAllListeners();
            _myButton.onClick.AddListener(() => onClickAction?.Invoke(skill));
        }
    }
}
