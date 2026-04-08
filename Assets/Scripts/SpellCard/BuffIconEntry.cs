using TMPro;
using TurnBasedGame.ObjectPool;
using UnityEngine;
using UnityEngine.UI;

namespace TurnBasedGame.SpellCard
{
    /// <summary>
    /// Một icon trong danh sách buff/debuff: hiển thị sprite + số lượt còn lại.
    /// </summary>
    public class BuffIconEntry : PooledObject
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TextMeshProUGUI _turnText;

        private void Awake()
        {
            if (_icon == null) _icon = GetComponent<Image>();
            if (_turnText == null) _turnText = GetComponentInChildren<TextMeshProUGUI>();
        }

        public void Setup(Sprite sprite, int remainingTurns, Color tint = default)
        {
            if (_icon == null) _icon = GetComponent<Image>();
            if (_turnText == null) _turnText = GetComponentInChildren<TextMeshProUGUI>();

            if (_icon != null)
            {
                _icon.sprite = sprite;
                _icon.color = tint == default ? Color.white : tint;
            }
            UpdateTurns(remainingTurns);
        }

        public void UpdateTurns(int remainingTurns)
        {
            if (_turnText != null)
                _turnText.text = remainingTurns > 0 ? remainingTurns.ToString() : "";
        }
    }
}
