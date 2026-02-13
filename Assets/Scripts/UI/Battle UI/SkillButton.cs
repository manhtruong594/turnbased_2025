using System;
using TMPro;
using TurnBasedGame.ObjectPool;
using TurnBasedGame.Skills;
using UnityEngine;
using UnityEngine.UI;

public class SkillButton : PooledObject
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

    public void UpdateCooldownDisplay(int currentCooldown, int maxCooldown)
    {
        // Cập nhật giao diện nút dựa trên cooldown hiện tại
        if (currentCooldown > 0)
        {
            // Ví dụ: làm mờ nút và hiển thị số cooldown
            _myButton.interactable = false;
            _descriptionText.text = $"Cooldown: {currentCooldown}/{maxCooldown}";
        }
        else
        {
            // Nút sẵn sàng sử dụng
            _myButton.interactable = true;
            // Giả sử bạn có một biến skill để lấy mô tả gốc
            // _descriptionText.text = skill.Description; 
        }
    }
}
